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
            // This test verifies the fix for the lastProcessedResponseTicks issue

            // Set server ahead of client
            serverTime.SetFromAuthority(clientTime.ElapsedTicks + TimeSpan.FromSeconds(5).Ticks);
            Thread.Sleep(1100);

            // Update times
            clientTime.Update();
            serverTime.Update();

            UnityEngine.Debug.Log($"Initial state - Client: {clientTime.ElapsedSeconds:F3}s, Server: {serverTime.ElapsedSeconds:F3}s");
            UnityEngine.Debug.Log($"LastProcessedResponseTicks: {TimeSyncTestHelpers.GetLastProcessedResponseTicks()}");

            // Create and process first sync
            var request1 = TimeSyncTestHelpers.CreateValidTimeSyncRequest(clientTime);
            Thread.Sleep(50); // Simulate RTT

            TimeSyncTestHelpers.ProcessTimeSyncSafely(request1, serverTime, clientTime, true);
            TimeSyncTestHelpers.WaitForInterpolation();

            // Update times
            clientTime.Update();
            serverTime.Update();

            UnityEngine.Debug.Log($"After first sync - Client: {clientTime.ElapsedSeconds:F3}s, Server: {serverTime.ElapsedSeconds:F3}s");
            UnityEngine.Debug.Log($"LastProcessedResponseTicks after sync: {TimeSyncTestHelpers.GetLastProcessedResponseTicks() / (double)TimeSpan.TicksPerSecond:F3}s");

            // Create and process second sync - this should NOT be rejected
            Thread.Sleep(1000); // Wait a bit
            var request2 = TimeSyncTestHelpers.CreateValidTimeSyncRequest(clientTime);
            Thread.Sleep(50); // Simulate RTT

            // This should work without being rejected
            TimeSyncTestHelpers.ProcessTimeSyncSafely(request2, serverTime, clientTime, false);
            TimeSyncTestHelpers.WaitForInterpolation();

            // Verify we're still in sync
            clientTime.Update();
            serverTime.Update();

            double finalDiff = Math.Abs(serverTime.ElapsedSeconds - clientTime.ElapsedSeconds);
            UnityEngine.Debug.Log($"Final difference: {finalDiff:F3}s");

            Assert.That(finalDiff, Is.LessThan(0.2), "Should maintain sync without rejecting valid responses");
        }

        [Test]
        [Category("Verification")]
        public void Should_Build_RTT_Buffer_Correctly()
        {
            // Send multiple sync requests and verify RTT buffer builds up
            for (int i = 0; i < 10; i++)
            {
                clientTime.Update();
                serverTime.Update();

                var request = TimeSyncTestHelpers.CreateValidTimeSyncRequest(clientTime);
                Thread.Sleep(20 + i * 5); // Variable RTT

                TimeSyncTestHelpers.ProcessTimeSyncSafely(request, serverTime, clientTime, i == 0);

                Thread.Sleep(100);
            }

            string bufferState = TimeSyncTestHelpers.GetRttBufferState();
            UnityEngine.Debug.Log($"RTT Buffer State: {bufferState}");

            // Verify buffer has samples
            Assert.That(bufferState, Does.Contain("ValidSamples=10"), "Should have built up RTT samples");
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