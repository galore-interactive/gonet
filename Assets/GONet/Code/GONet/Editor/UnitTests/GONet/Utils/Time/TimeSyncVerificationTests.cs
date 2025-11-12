using System;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    [TestFixture]
    public class TimeSyncVerificationTests : TimeSyncTestBase
    {
        private SecretaryOfTemporalAffairs clientTime;
        private SecretaryOfTemporalAffairs serverTime;

        [SetUp]
        public void Setup()
        {
            base.BaseSetUp();

            clientTime = new SecretaryOfTemporalAffairs();
            serverTime = new SecretaryOfTemporalAffairs();

            // Ensure times are initialized
            clientTime.Update();
            serverTime.Update();
        }

        [TearDown]
        public void TearDown()
        {
            base.BaseTearDown();
        }

        [Test]
        [Category("Verification")]
        public void Should_Not_Reject_Valid_Sync_Responses()
        {
            // Initialize both times
            clientTime.Update();
            serverTime.Update();
            Thread.Sleep(100);

            // Update and get current ticks
            clientTime.Update();
            serverTime.Update();

            // Set server 5 seconds ahead of where client currently is
            long clientNow = clientTime.ElapsedTicks;
            serverTime.SetFromAuthority(clientNow + TimeSpan.FromSeconds(5).Ticks);

            // Wait for server's interpolation to complete
            Thread.Sleep(1100);

            // Update both to get current values
            clientTime.Update();
            serverTime.Update();

            double initialClientTime = clientTime.ElapsedSeconds;
            double initialServerTime = serverTime.ElapsedSeconds;
            double initialDiff = Math.Abs(initialServerTime - initialClientTime);

            UnityEngine.Debug.Log($"Initial state - Client: {initialClientTime:F3}s, Server: {initialServerTime:F3}s, Diff: {initialDiff:F3}s");

            // Verify we actually have an initial difference
            Assert.That(initialDiff, Is.GreaterThan(4.5).And.LessThan(5.5),
                "Initial setup should create ~5 second difference");

            // First sync - should bring client closer to server
            var request1 = new RequestMessage(clientTime.ElapsedTicks);
            Thread.Sleep(50); // Simulate network delay

            serverTime.Update();
            long serverResponseTime1 = serverTime.ElapsedTicks;

            HighPerfTimeSync.ProcessTimeSync(
                request1.UID,
                serverResponseTime1,
                request1,
                clientTime,
                true // Force first sync
            );

            // Wait for adjustment to complete (interpolation takes 1 second)
            Thread.Sleep(1100);
            clientTime.Update();
            serverTime.Update();

            double afterFirstSyncClient = clientTime.ElapsedSeconds;
            double afterFirstSyncServer = serverTime.ElapsedSeconds;
            double diffAfterFirst = Math.Abs(afterFirstSyncServer - afterFirstSyncClient);

            UnityEngine.Debug.Log($"After first sync - Client: {afterFirstSyncClient:F3}s, Server: {afterFirstSyncServer:F3}s, Diff: {diffAfterFirst:F3}s");

            // The first sync should have reduced the difference significantly
            Assert.That(diffAfterFirst, Is.LessThan(initialDiff),
                $"First sync should reduce time difference from {initialDiff:F3}s");

            // After one sync with 5-second initial difference, expect to be within ~1.5 seconds
            // (The interpolation over 1 second can't fully close a 5-second gap instantly)
            Assert.That(diffAfterFirst, Is.LessThan(1.5),
                "After first sync, times should be much closer (within 1.5 seconds)");

            // Wait before second sync
            Thread.Sleep(500);
            clientTime.Update();
            serverTime.Update();

            // Second sync - this should NOT be rejected and should improve sync further
            var request2 = new RequestMessage(clientTime.ElapsedTicks);
            Thread.Sleep(50); // Simulate network delay

            serverTime.Update();
            long serverResponseTime2 = serverTime.ElapsedTicks;

            // Verify this is a newer server time
            Assert.That(serverResponseTime2, Is.GreaterThan(serverResponseTime1),
                "Second server response should have newer timestamp");

            HighPerfTimeSync.ProcessTimeSync(
                request2.UID,
                serverResponseTime2,
                request2,
                clientTime,
                false // Don't force
            );

            // Wait for any adjustment
            Thread.Sleep(1100);
            clientTime.Update();
            serverTime.Update();

            double finalClientTime = clientTime.ElapsedSeconds;
            double finalServerTime = serverTime.ElapsedSeconds;
            double finalDiff = Math.Abs(finalServerTime - finalClientTime);

            UnityEngine.Debug.Log($"Final state - Client: {finalClientTime:F3}s, Server: {finalServerTime:F3}s, Diff: {finalDiff:F3}s");

            // After two syncs, should be very close
            Assert.That(finalDiff, Is.LessThan(0.2),
                "After second sync, should maintain tight synchronization");

            // Verify that the second sync wasn't rejected (it should improve or maintain sync)
            // Add small tolerance for Thread.Sleep imprecision (1ms)
            Assert.That(finalDiff, Is.LessThanOrEqualTo(diffAfterFirst + 0.001),
                "Second sync should not be rejected - difference should improve or stay same (within 1ms tolerance)");

            // The key test: verify we processed both syncs (no rejection of valid responses)
            bool secondSyncImprovedThings = finalDiff < diffAfterFirst + 0.1; // Allow small tolerance
            Assert.That(secondSyncImprovedThings, Is.True,
                "Second sync should have been processed (not rejected)");
        }

        [Test]
        [Category("Verification")]
        public void Should_Handle_Rapid_Successive_Syncs()
        {
            // This tests the scenario that was causing issues in the integration tests
            serverTime.SetFromAuthority(clientTime.ElapsedTicks + TimeSpan.FromSeconds(3).Ticks);
            Thread.Sleep(1100);

            // Rapid fire syncs
            for (int i = 0; i < 5; i++)
            {
                clientTime.Update();
                serverTime.Update();

                var request = TimeSyncTestHelpers.CreateValidTimeSyncRequest(clientTime);
                Thread.Sleep(10); // Very short delay

                UnityEngine.Debug.Log($"Sync {i} - Server time: {serverTime.ElapsedSeconds:F3}s");

                TimeSyncTestHelpers.ProcessTimeSyncSafely(request, serverTime, clientTime, i == 0);

                Thread.Sleep(200); // Short wait between syncs
            }

            // Final sync after waiting
            Thread.Sleep(1000);
            clientTime.Update();
            serverTime.Update();

            double finalDiff = Math.Abs(serverTime.ElapsedSeconds - clientTime.ElapsedSeconds);
            UnityEngine.Debug.Log($"Final difference after rapid syncs: {finalDiff:F3}s");

            Assert.That(finalDiff, Is.LessThan(0.1), "Should handle rapid successive syncs");
        }
    }
}