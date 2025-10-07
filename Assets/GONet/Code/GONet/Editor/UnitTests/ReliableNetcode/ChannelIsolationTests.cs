using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using ReliableNetcode;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GONet.Tests.ReliableNetcode
{
    /// <summary>
    /// Tests for channel isolation - verifying that reliable channel load doesn't starve unreliable channel
    /// This is CRITICAL for the projectile freezing issue investigation
    /// </summary>
    [TestFixture]
    public class ChannelIsolationTests : ReliableEndpointTestBase
    {
        [Test]
        public void ReliableFlood_DoesNotBlock_UnreliableChannel()
        {
            LogTestProgress("Starting ReliableFlood_DoesNotBlock_UnreliableChannel test");

            var pair = CreateEndpointPair();

            const int RELIABLE_MESSAGE_COUNT = 100;
            const int UNRELIABLE_MESSAGE_COUNT = 100;
            const double DELTA_TIME = 0.01; // 10ms per update

            var unreliableSendTimes = new Dictionary<int, double>();
            var unreliableReceiveTimes = new Dictionary<int, double>();

            // Send 100 reliable messages rapidly (simulating spawn burst)
            LogTestProgress($"Sending {RELIABLE_MESSAGE_COUNT} reliable messages...");
            for (int i = 0; i < RELIABLE_MESSAGE_COUNT; i++)
            {
                var message = CreateTestMessage(i, 200);
                SendTestMessage(pair, pair.Endpoint1, message, QosType.Reliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            LogTestProgress($"Sent {RELIABLE_MESSAGE_COUNT} reliable messages");

            // Simultaneously send 100 unreliable messages (simulating position updates)
            LogTestProgress($"Sending {UNRELIABLE_MESSAGE_COUNT} unreliable messages...");
            for (int i = 0; i < UNRELIABLE_MESSAGE_COUNT; i++)
            {
                var message = CreateTestMessage(i + 1000, 100); // Use different sequence numbers
                unreliableSendTimes[i + 1000] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, message, QosType.Unreliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            LogTestProgress($"Sent {UNRELIABLE_MESSAGE_COUNT} unreliable messages");

            // Allow time for all messages to arrive
            RunUpdateCycles(pair, 100, DELTA_TIME);

            LogTestProgress($"Endpoint2 received {pair.Endpoint2Received.Count} total messages");

            // Track unreliable message latencies
            foreach (var msg in pair.Endpoint2Received)
            {
                if (msg.Channel == QosType.Unreliable)
                {
                    int seq = GetSequenceNumber(msg.Data);
                    if (unreliableSendTimes.ContainsKey(seq))
                    {
                        double latency = msg.TimeReceived - unreliableSendTimes[seq];
                        unreliableReceiveTimes[seq] = msg.TimeReceived;
                        msg.Latency = latency;
                    }
                }
            }

            // CRITICAL ASSERTIONS:
            // 1. All unreliable messages should arrive (no starvation)
            var (reliableCount, unreliableCount) = CountMessagesByChannel(pair.Endpoint2Received);

            LogTestProgress($"Reliable messages received: {reliableCount}/{RELIABLE_MESSAGE_COUNT}");
            LogTestProgress($"Unreliable messages received: {unreliableCount}/{UNRELIABLE_MESSAGE_COUNT}");

            Assert.GreaterOrEqual(unreliableCount, UNRELIABLE_MESSAGE_COUNT,
                $"Unreliable channel starved! Only {unreliableCount}/{UNRELIABLE_MESSAGE_COUNT} messages received");

            // 2. Unreliable message latency should be under 500ms (not frozen)
            var unreliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);

            double maxLatency = 0;
            double avgLatency = 0;
            int latencyCount = 0;

            foreach (var msg in unreliableMessages)
            {
                if (msg.Latency > 0)
                {
                    avgLatency += msg.Latency;
                    latencyCount++;
                    if (msg.Latency > maxLatency)
                        maxLatency = msg.Latency;
                }
            }

            if (latencyCount > 0)
                avgLatency /= latencyCount;

            LogTestProgress($"Unreliable latency - Max: {maxLatency * 1000:F2}ms, Avg: {avgLatency * 1000:F2}ms");

            Assert.Less(maxLatency, 0.5,
                $"Unreliable messages delayed too long (max {maxLatency * 1000:F2}ms)! Channel likely starved.");

            // 3. All reliable messages should also arrive (in order)
            Assert.GreaterOrEqual(reliableCount, RELIABLE_MESSAGE_COUNT,
                $"Reliable messages lost! Only {reliableCount}/{RELIABLE_MESSAGE_COUNT} received");

            LogTestProgress("Test PASSED - Unreliable channel not starved by reliable flood");
        }

        [Test]
        public void MixedTraffic_BothChannels_NoStarvation()
        {
            LogTestProgress("Starting MixedTraffic_BothChannels_NoStarvation test");

            var pair = CreateEndpointPair();

            const int MESSAGES_PER_CHANNEL = 50;
            const double DELTA_TIME = 0.01;

            var unreliableSendTimes = new Dictionary<int, double>();

            // Send alternating reliable and unreliable messages
            LogTestProgress($"Sending {MESSAGES_PER_CHANNEL * 2} mixed messages...");
            for (int i = 0; i < MESSAGES_PER_CHANNEL; i++)
            {
                // Reliable message
                var reliableMsg = CreateTestMessage(i, 150);
                SendTestMessage(pair, pair.Endpoint1, reliableMsg, QosType.Reliable);

                // Unreliable message
                var unreliableMsg = CreateTestMessage(i + 1000, 100);
                unreliableSendTimes[i + 1000] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, unreliableMsg, QosType.Unreliable);

                UpdateEndpoints(pair, DELTA_TIME);
            }

            // Process remaining messages
            RunUpdateCycles(pair, 100, DELTA_TIME);

            LogTestProgress($"Received {pair.Endpoint2Received.Count} total messages");

            // Track latencies
            foreach (var msg in pair.Endpoint2Received)
            {
                if (msg.Channel == QosType.Unreliable)
                {
                    int seq = GetSequenceNumber(msg.Data);
                    if (unreliableSendTimes.ContainsKey(seq))
                    {
                        msg.Latency = msg.TimeReceived - unreliableSendTimes[seq];
                    }
                }
            }

            var (reliableCount, unreliableCount) = CountMessagesByChannel(pair.Endpoint2Received);

            LogTestProgress($"Reliable: {reliableCount}/{MESSAGES_PER_CHANNEL}");
            LogTestProgress($"Unreliable: {unreliableCount}/{MESSAGES_PER_CHANNEL}");

            // Both channels should deliver all messages
            Assert.GreaterOrEqual(unreliableCount, MESSAGES_PER_CHANNEL,
                "Unreliable channel starved in mixed traffic");
            Assert.GreaterOrEqual(reliableCount, MESSAGES_PER_CHANNEL,
                "Reliable channel failed in mixed traffic");

            // Unreliable latency should remain low
            var unreliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);
            double maxLatency = 0;
            foreach (var msg in unreliableMessages)
            {
                if (msg.Latency > maxLatency)
                    maxLatency = msg.Latency;
            }

            LogTestProgress($"Max unreliable latency: {maxLatency * 1000:F2}ms");

            Assert.Less(maxLatency, 0.5,
                $"Unreliable latency too high in mixed traffic ({maxLatency * 1000:F2}ms)");

            LogTestProgress("Test PASSED - Both channels functional in mixed traffic");
        }

        [Test]
        public void UnreliableChannel_HighFrequency_NotThrottledByReliable()
        {
            LogTestProgress("Starting UnreliableChannel_HighFrequency_NotThrottledByReliable test");

            var pair = CreateEndpointPair();

            const int RELIABLE_BURST_SIZE = 50;
            const int UNRELIABLE_HIGH_FREQ_COUNT = 200; // Simulating 60Hz position updates
            const double DELTA_TIME = 0.001; // 1ms updates

            // First: Send burst of reliable messages (spawn event)
            LogTestProgress($"Sending burst of {RELIABLE_BURST_SIZE} reliable messages...");
            for (int i = 0; i < RELIABLE_BURST_SIZE; i++)
            {
                var message = CreateTestMessage(i, 250);
                SendTestMessage(pair, pair.Endpoint1, message, QosType.Reliable);
            }

            UpdateEndpoints(pair, 0.01);

            // Then: Send high-frequency unreliable messages (position updates during spawn burst processing)
            LogTestProgress($"Sending {UNRELIABLE_HIGH_FREQ_COUNT} high-frequency unreliable messages...");
            var unreliableSendTimes = new Dictionary<int, double>();

            for (int i = 0; i < UNRELIABLE_HIGH_FREQ_COUNT; i++)
            {
                var message = CreateTestMessage(i + 2000, 80);
                unreliableSendTimes[i + 2000] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, message, QosType.Unreliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            // Process remaining
            RunUpdateCycles(pair, 200, DELTA_TIME);

            LogTestProgress($"Total received: {pair.Endpoint2Received.Count}");

            // Calculate metrics
            var (reliableCount, unreliableCount) = CountMessagesByChannel(pair.Endpoint2Received);

            LogTestProgress($"Reliable: {reliableCount}");
            LogTestProgress($"Unreliable: {unreliableCount}");

            // CRITICAL: High-frequency unreliable traffic should not be blocked
            Assert.GreaterOrEqual(unreliableCount, UNRELIABLE_HIGH_FREQ_COUNT * 0.9,
                $"Too many unreliable messages dropped ({unreliableCount}/{UNRELIABLE_HIGH_FREQ_COUNT})");

            // Track latency distribution
            var unreliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);
            int underThreshold = 0;

            foreach (var msg in unreliableMessages)
            {
                int seq = GetSequenceNumber(msg.Data);
                if (unreliableSendTimes.ContainsKey(seq))
                {
                    msg.Latency = msg.TimeReceived - unreliableSendTimes[seq];
                    if (msg.Latency < 0.1) // 100ms threshold
                        underThreshold++;
                }
            }

            float percentUnderThreshold = (float)underThreshold / unreliableMessages.Count * 100f;

            LogTestProgress($"Unreliable messages under 100ms latency: {percentUnderThreshold:F1}%");

            Assert.Greater(percentUnderThreshold, 90f,
                $"Too many unreliable messages delayed (only {percentUnderThreshold:F1}% under threshold)");

            LogTestProgress("Test PASSED - High-frequency unreliable not throttled by reliable burst");
        }
    }
}
