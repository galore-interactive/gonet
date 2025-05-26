using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GONet.Utils;
using NUnit.Framework;
using UnityEngine;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    [TestFixture]
    public class RttBufferManagementTests : TimeSyncTestBase
    {
        private SecretaryOfTemporalAffairs clientTime;
        private SecretaryOfTemporalAffairs serverTime;
        private FieldInfo rttBufferField;
        private FieldInfo rttWriteIndexField;
        private MethodInfo getMedianRttMethod;

        [SetUp]
        public void Setup()
        {
            base.BaseSetUp();

            clientTime = new SecretaryOfTemporalAffairs();
            serverTime = new SecretaryOfTemporalAffairs();

            // Use reflection to access private fields/methods for testing
            var timeSyncType = typeof(HighPerfTimeSync);
            rttBufferField = timeSyncType.GetField("rttBuffer", BindingFlags.NonPublic | BindingFlags.Static);
            rttWriteIndexField = timeSyncType.GetField("rttWriteIndex", BindingFlags.NonPublic | BindingFlags.Static);
            getMedianRttMethod = timeSyncType.GetMethod("GetFastMedianRtt", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [TearDown]
        public void TearDown()
        {
            base.BaseTearDown();
        }

        [Test]
        [Category("RTTBuffer")]
        public void Should_Handle_Buffer_Wraparound()
        {
            // Fill the buffer completely (32 entries) and verify wraparound
            for (int i = 0; i < 40; i++) // More than buffer size
            {
                var request = new RequestMessage(clientTime.ElapsedTicks);
                Thread.Sleep(10); // Small delay to simulate RTT

                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTime.ElapsedTicks,
                    request,
                    clientTime,
                    false
                );

                clientTime.Update();
                serverTime.Update();
            }

            // Get current write index - should have wrapped around
            int writeIndex = (int)rttWriteIndexField.GetValue(null);
            Assert.That(writeIndex, Is.GreaterThan(32), "Write index should have wrapped around");

            // Verify we can still get a valid median
            float median = (float)getMedianRttMethod.Invoke(null, null);
            Assert.That(median, Is.GreaterThan(0), "Median RTT should be valid after wraparound");
        }

        [Test]
        [Category("RTTBuffer")]
        [Timeout(10000)]
        public void Should_Handle_Concurrent_Buffer_Writes()
        {
            const int threadCount = 8;
            const int samplesPerThread = 100;
            var barrier = new Barrier(threadCount);
            var exceptions = new ConcurrentBag<Exception>();

            var tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(() =>
            {
                try
                {
                    var localClient = new SecretaryOfTemporalAffairs();
                    var localServer = new SecretaryOfTemporalAffairs();

                    // Set different initial times for each thread
                    localServer.SetFromAuthority(localClient.ElapsedTicks + TimeSpan.FromSeconds(threadId).Ticks);

                    barrier.SignalAndWait();

                    for (int i = 0; i < samplesPerThread; i++)
                    {
                        localClient.Update();
                        localServer.Update();

                        var request = new RequestMessage(localClient.ElapsedTicks);
                        Thread.Sleep(1); // Minimal delay

                        HighPerfTimeSync.ProcessTimeSync(
                            request.UID,
                            localServer.ElapsedTicks,
                            request,
                            localClient,
                            false
                        );
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

            Task.WaitAll(tasks);

            Assert.That(exceptions, Is.Empty, "No exceptions during concurrent writes");

            // Verify buffer integrity
            float median = (float)getMedianRttMethod.Invoke(null, null);
            Assert.That(median, Is.GreaterThan(0), "Median should be valid after concurrent writes");
        }

        [Test]
        [Category("RTTBuffer")]
        public void Should_Expire_Old_Samples()
        {
            // Add some samples
            for (int i = 0; i < 10; i++)
            {
                var request = new RequestMessage(clientTime.ElapsedTicks);
                Thread.Sleep(5);

                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTime.ElapsedTicks,
                    request,
                    clientTime,
                    false
                );

                clientTime.Update();
                serverTime.Update();
            }

            // Get initial median
            float initialMedian = (float)getMedianRttMethod.Invoke(null, null);
            Assert.That(initialMedian, Is.GreaterThan(0), "Should have valid initial median");

            // Simulate passage of time by manually adjusting timestamps in the buffer
            var buffer = rttBufferField.GetValue(null);
            var sampleType = buffer.GetType().GetElementType();
            var timestampField = sampleType.GetField("Timestamp");

            // Make all samples "old" (>10 seconds)
            long oldTimestamp = HighResolutionTimeUtils.UtcNow.Ticks - TimeSpan.FromSeconds(11).Ticks;
            for (int i = 0; i < 32; i++)
            {
                var element = ((Array)buffer).GetValue(i);
                var currentTimestamp = (long)timestampField.GetValue(element);
                if (currentTimestamp > 0) // Only update non-zero timestamps
                {
                    timestampField.SetValue(element, oldTimestamp);
                    ((Array)buffer).SetValue(element, i);
                }
            }

            // Now median should return default value (0.05f) since all samples are expired
            float expiredMedian = (float)getMedianRttMethod.Invoke(null, null);
            Assert.That(expiredMedian, Is.EqualTo(0.05f), "Should return default when all samples expired");
        }

        [Test]
        [Category("RTTBuffer")]
        public void Should_Calculate_Accurate_Median_RTT()
        {
            // Add samples with known RTT values
            var rttValues = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }; // milliseconds

            foreach (var rttMs in rttValues)
            {
                clientTime.Update();
                serverTime.Update();

                var request = new RequestMessage(clientTime.ElapsedTicks);

                // Simulate specific RTT
                Thread.Sleep(rttMs / 2); // Half RTT for request
                long serverResponseTicks = serverTime.ElapsedTicks;
                Thread.Sleep(rttMs / 2); // Half RTT for response

                clientTime.Update();
                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverResponseTicks,
                    request,
                    clientTime,
                    false
                );
            }

            float median = (float)getMedianRttMethod.Invoke(null, null);
            // With 10 samples (10-100ms), median should be around 50-60ms
            Assert.That(median, Is.InRange(0.045f, 0.065f), $"Median {median:F3} should be in expected range");
        }

        [Test]
        [Category("RTTBuffer")]
        public void Should_Handle_RTT_Extremes()
        {
            // Test with very small RTT
            var request1 = new RequestMessage(clientTime.ElapsedTicks);
            HighPerfTimeSync.ProcessTimeSync(
                request1.UID,
                serverTime.ElapsedTicks + (TimeSpan.TicksPerMillisecond / 10), // 100 microseconds
                request1,
                clientTime,
                false
            );

            // Test with very large (but valid) RTT - just under 10 second limit
            clientTime.Update();
            serverTime.Update();
            var request2 = new RequestMessage(clientTime.ElapsedTicks);
            Thread.Sleep(500); // 500ms RTT

            HighPerfTimeSync.ProcessTimeSync(
                request2.UID,
                serverTime.ElapsedTicks,
                request2,
                clientTime,
                false
            );

            // Test with invalid RTT (negative - should be rejected)
            var request3 = new RequestMessage(clientTime.ElapsedTicks + TimeSpan.FromSeconds(1).Ticks);
            HighPerfTimeSync.ProcessTimeSync(
                request3.UID,
                serverTime.ElapsedTicks,
                request3,
                clientTime,
                false
            );

            // Test with invalid RTT (>10 seconds - should be rejected)
            var request4 = new RequestMessage(clientTime.ElapsedTicks - TimeSpan.FromSeconds(11).Ticks);
            HighPerfTimeSync.ProcessTimeSync(
                request4.UID,
                serverTime.ElapsedTicks,
                request4,
                clientTime,
                false
            );

            float median = (float)getMedianRttMethod.Invoke(null, null);
            Assert.That(median, Is.GreaterThan(0), "Should have valid median despite extreme values");
        }

        [Test]
        [Category("RTTBuffer")]
        public void Should_Handle_Out_Of_Order_Responses()
        {
            // Simulate out-of-order packet delivery
            var requests = new List<(RequestMessage request, long serverTime)>();

            // Create multiple requests
            for (int i = 0; i < 5; i++)
            {
                clientTime.Update();
                serverTime.Update();
                var request = new RequestMessage(clientTime.ElapsedTicks);
                Thread.Sleep(20);
                requests.Add((request, serverTime.ElapsedTicks));
            }

            // Process responses out of order
            var processOrder = new[] { 2, 0, 4, 1, 3 };
            foreach (int index in processOrder)
            {
                var (request, serverTime) = requests[index];
                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTime,
                    request,
                    clientTime,
                    false
                );
                Thread.Sleep(10);
            }

            // Should still have valid RTT calculations
            float median = (float)getMedianRttMethod.Invoke(null, null);
            Assert.That(median, Is.GreaterThan(0), "Should handle out-of-order responses");
        }
    }
}