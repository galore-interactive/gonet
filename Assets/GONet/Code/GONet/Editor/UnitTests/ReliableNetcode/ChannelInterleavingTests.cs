using System;
using System.Collections.Generic;
using NUnit.Framework;
using ReliableNetcode;
using UnityEngine;

namespace GONet.Tests.ReliableNetcode
{
    /// <summary>
    /// Tests for channel interleaving - ensuring both channels work simultaneously without interference
    /// This is the CORE test for the projectile freezing bug
    /// </summary>
    [TestFixture]
    public class ChannelInterleavingTests : ReliableEndpointTestBase
    {
        [Test]
        public void MixedTraffic_1000Messages_BothChannels_NoStarvation()
        {
            LogTestProgress("Starting MixedTraffic_1000Messages_BothChannels_NoStarvation test");

            var pair = CreateEndpointPair();

            const int MESSAGES_PER_CHANNEL = 500;
            const double DELTA_TIME = 0.01;

            var unreliableSendTimes = new Dictionary<int, double>();

            // Send 1000 messages alternating between channels
            LogTestProgress($"Sending {MESSAGES_PER_CHANNEL * 2} mixed messages...");
            for (int i = 0; i < MESSAGES_PER_CHANNEL; i++)
            {
                // Reliable message
                var reliableMsg = CreateTestMessage(i, 150);
                SendTestMessage(pair, pair.Endpoint1, reliableMsg, QosType.Reliable);

                // Unreliable message
                int unreliableSeq = i + 15000;
                var unreliableMsg = CreateTestMessage(unreliableSeq, 80);
                unreliableSendTimes[unreliableSeq] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, unreliableMsg, QosType.Unreliable);

                UpdateEndpoints(pair, DELTA_TIME);

                // Log progress every 100 messages
                if ((i + 1) % 100 == 0)
                {
                    LogTestProgress($"  Progress: {(i + 1) * 2}/{MESSAGES_PER_CHANNEL * 2} sent");
                }
            }

            // Process remaining
            RunUpdateCycles(pair, 200, DELTA_TIME);

            // Calculate metrics
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
            Assert.GreaterOrEqual(reliableCount, MESSAGES_PER_CHANNEL,
                $"Reliable channel starved ({reliableCount}/{MESSAGES_PER_CHANNEL})");
            Assert.GreaterOrEqual(unreliableCount, MESSAGES_PER_CHANNEL,
                $"Unreliable channel starved ({unreliableCount}/{MESSAGES_PER_CHANNEL})");

            // Check unreliable latency
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

            LogTestProgress($"Unreliable latency - Max: {maxLatency * 1000:F1}ms, Avg: {avgLatency * 1000:F1}ms");

            Assert.Less(maxLatency, 0.5,
                $"Unreliable latency too high ({maxLatency * 1000:F1}ms) in heavy interleaved traffic");

            LogTestProgress("Test PASSED - Both channels functional in 1000-message interleaved test");
        }

        [Test]
        public void ChannelPriorityTest_UnreliableGetsSlots()
        {
            LogTestProgress("Starting ChannelPriorityTest_UnreliableGetsSlots test");

            var pair = CreateEndpointPair();

            const int RELIABLE_FLOOD_SIZE = 100;
            const int UNRELIABLE_DURING_FLOOD = 100;
            const double DELTA_TIME = 0.01;

            var unreliableSendTimes = new Dictionary<int, double>();

            // Phase 1: Queue up massive reliable flood
            LogTestProgress($"Phase 1: Queueing {RELIABLE_FLOOD_SIZE} reliable messages...");
            for (int i = 0; i < RELIABLE_FLOOD_SIZE; i++)
            {
                var reliableMsg = CreateTestMessage(i, 300);
                SendTestMessage(pair, pair.Endpoint1, reliableMsg, QosType.Reliable);
            }

            // Phase 2: While reliable queue is full, send unreliable messages
            LogTestProgress($"Phase 2: Sending {UNRELIABLE_DURING_FLOOD} unreliable while reliable floods...");
            for (int i = 0; i < UNRELIABLE_DURING_FLOOD; i++)
            {
                int seq = i + 18000;
                var unreliableMsg = CreateTestMessage(seq, 80);
                unreliableSendTimes[seq] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, unreliableMsg, QosType.Unreliable);

                UpdateEndpoints(pair, DELTA_TIME);
            }

            // Phase 3: Process everything
            LogTestProgress("Phase 3: Processing...");
            RunUpdateCycles(pair, 300, DELTA_TIME);

            // Calculate unreliable delivery metrics
            var unreliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);
            int unreliableCount = 0;
            double maxLatency = 0;

            foreach (var msg in unreliableMessages)
            {
                unreliableCount++;
                int seq = GetSequenceNumber(msg.Data);
                if (unreliableSendTimes.ContainsKey(seq))
                {
                    double latency = msg.TimeReceived - unreliableSendTimes[seq];
                    if (latency > maxLatency)
                        maxLatency = latency;
                }
            }

            var reliableCount = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Reliable).Count;

            LogTestProgress($"Reliable: {reliableCount}/{RELIABLE_FLOOD_SIZE}");
            LogTestProgress($"Unreliable: {unreliableCount}/{UNRELIABLE_DURING_FLOOD}");
            LogTestProgress($"Max unreliable latency: {maxLatency * 1000:F1}ms");

            // CRITICAL: Unreliable should get transmission slots even when reliable queue is full
            Assert.GreaterOrEqual(unreliableCount, UNRELIABLE_DURING_FLOOD * 0.95,
                $"Unreliable starved when reliable queue full ({unreliableCount}/{UNRELIABLE_DURING_FLOOD})");

            Assert.Less(maxLatency, 1.0,
                $"Unreliable blocked too long ({maxLatency * 1000:F1}ms) by reliable flood");

            LogTestProgress("Test PASSED - Unreliable gets slots even when reliable queue is full");
        }

        [Test]
        public void BurstyTraffic_BothChannels_NoInterference()
        {
            LogTestProgress("Starting BurstyTraffic_BothChannels_NoInterference test");

            var pair = CreateEndpointPair();

            const int NUM_BURSTS = 10;
            const int MESSAGES_PER_BURST = 20;
            const double DELTA_TIME = 0.01;

            var unreliableSendTimes = new Dictionary<int, double>();
            int totalReliableSent = 0;
            int totalUnreliableSent = 0;

            // Send 10 bursts, each containing both reliable and unreliable messages
            LogTestProgress($"Sending {NUM_BURSTS} bursts of {MESSAGES_PER_BURST} messages each...");
            for (int burst = 0; burst < NUM_BURSTS; burst++)
            {
                // Burst of reliable messages
                for (int i = 0; i < MESSAGES_PER_BURST; i++)
                {
                    var reliableMsg = CreateTestMessage(totalReliableSent++, 200);
                    SendTestMessage(pair, pair.Endpoint1, reliableMsg, QosType.Reliable);
                }

                // Burst of unreliable messages
                for (int i = 0; i < MESSAGES_PER_BURST; i++)
                {
                    int seq = totalUnreliableSent + 20000;
                    var unreliableMsg = CreateTestMessage(seq, 80);
                    unreliableSendTimes[seq] = pair.CurrentTime;
                    SendTestMessage(pair, pair.Endpoint1, unreliableMsg, QosType.Unreliable);
                    totalUnreliableSent++;
                }

                UpdateEndpoints(pair, DELTA_TIME);

                // Small delay between bursts
                RunUpdateCycles(pair, 5, DELTA_TIME);
            }

            // Process remaining
            RunUpdateCycles(pair, 200, DELTA_TIME);

            LogTestProgress($"Sent - Reliable: {totalReliableSent}, Unreliable: {totalUnreliableSent}");

            // Calculate metrics
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

            LogTestProgress($"Received - Reliable: {reliableCount}/{totalReliableSent}, Unreliable: {unreliableCount}/{totalUnreliableSent}");

            // Both channels should handle bursty traffic
            Assert.GreaterOrEqual(reliableCount, totalReliableSent,
                "Reliable lost messages in bursty traffic");
            Assert.GreaterOrEqual(unreliableCount, totalUnreliableSent * 0.95,
                "Unreliable lost too many messages in bursty traffic");

            // Verify unreliable latency stayed reasonable
            var unreliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);
            double maxLatency = 0;

            foreach (var msg in unreliableMessages)
            {
                if (msg.Latency > maxLatency)
                    maxLatency = msg.Latency;
            }

            LogTestProgress($"Max unreliable latency in bursty traffic: {maxLatency * 1000:F1}ms");

            Assert.Less(maxLatency, 1.0,
                $"Unreliable latency too high in bursty traffic ({maxLatency * 1000:F1}ms)");

            LogTestProgress("Test PASSED - Both channels handle bursty traffic without interference");
        }

        [Test]
        public void ContinuousHighFrequency_BothChannels_Sustained()
        {
            LogTestProgress("Starting ContinuousHighFrequency_BothChannels_Sustained test");

            var pair = CreateEndpointPair();

            const int DURATION_CYCLES = 500; // Long sustained test
            const double DELTA_TIME = 0.002; // High frequency (500Hz)

            var unreliableSendTimes = new Dictionary<int, double>();
            int reliableSent = 0;
            int unreliableSent = 0;

            // Sustained high-frequency mixed traffic
            LogTestProgress($"Running {DURATION_CYCLES} cycles of high-frequency mixed traffic...");
            for (int cycle = 0; cycle < DURATION_CYCLES; cycle++)
            {
                // Send reliable every 10 cycles
                if (cycle % 10 == 0)
                {
                    var reliableMsg = CreateTestMessage(reliableSent++, 150);
                    SendTestMessage(pair, pair.Endpoint1, reliableMsg, QosType.Reliable);
                }

                // Send unreliable every cycle (simulating continuous position updates)
                int seq = unreliableSent + 25000;
                var unreliableMsg = CreateTestMessage(seq, 80);
                unreliableSendTimes[seq] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, unreliableMsg, QosType.Unreliable);
                unreliableSent++;

                UpdateEndpoints(pair, DELTA_TIME);

                // Log progress every 100 cycles
                if ((cycle + 1) % 100 == 0)
                {
                    var received = pair.Endpoint2Received.Count;
                    LogTestProgress($"  Cycle {cycle + 1}/{DURATION_CYCLES}, Received: {received} messages");
                }
            }

            // Final processing
            RunUpdateCycles(pair, 100, DELTA_TIME);

            LogTestProgress($"Sent - Reliable: {reliableSent}, Unreliable: {unreliableSent}");

            // Calculate metrics
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

            LogTestProgress($"Received - Reliable: {reliableCount}/{reliableSent}, Unreliable: {unreliableCount}/{unreliableSent}");

            // Both channels should sustain high-frequency traffic
            Assert.GreaterOrEqual(reliableCount, reliableSent,
                "Reliable messages lost in sustained high-frequency test");
            Assert.GreaterOrEqual(unreliableCount, unreliableSent * 0.9,
                $"Too many unreliable messages lost in sustained test ({unreliableCount}/{unreliableSent})");

            // Calculate latency distribution
            var unreliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);
            double maxLatency = 0;
            double avgLatency = 0;
            int latencyCount = 0;
            int frozenMessages = 0;

            foreach (var msg in unreliableMessages)
            {
                if (msg.Latency > 0)
                {
                    avgLatency += msg.Latency;
                    latencyCount++;
                    if (msg.Latency > maxLatency)
                        maxLatency = msg.Latency;
                    if (msg.Latency > 1.0)
                        frozenMessages++;
                }
            }

            if (latencyCount > 0)
                avgLatency /= latencyCount;

            LogTestProgress($"Unreliable latency - Max: {maxLatency * 1000:F1}ms, Avg: {avgLatency * 1000:F1}ms");
            LogTestProgress($"Frozen messages (>1s): {frozenMessages}");

            // No messages should freeze in sustained high-frequency test
            Assert.AreEqual(0, frozenMessages,
                $"{frozenMessages} messages frozen in sustained high-frequency test - BUG DETECTED!");

            Assert.Less(maxLatency, 0.5,
                $"Max latency too high ({maxLatency * 1000:F1}ms) in sustained test");

            LogTestProgress("Test PASSED - Both channels sustained high-frequency traffic without freezing");
        }
    }
}
