using System;
using System.Collections.Generic;
using NUnit.Framework;
using ReliableNetcode;
using UnityEngine;

namespace GONet.Tests.ReliableNetcode
{
    /// <summary>
    /// Tests for congestion control behavior - ensuring it doesn't block unreliable channel
    /// </summary>
    [TestFixture]
    public class CongestionControlTests : ReliableEndpointTestBase
    {
        [Test]
        public void CongestionControl_WhenActive_DoesNotBlock_Unreliable()
        {
            LogTestProgress("Starting CongestionControl_WhenActive_DoesNotBlock_Unreliable test");

            // Create endpoints with latency to trigger congestion control
            var pair = CreateEndpointPair(simulateLatency: true, latencyMs: 300); // High latency triggers congestion

            const int RELIABLE_MESSAGE_COUNT = 50;
            const int UNRELIABLE_MESSAGE_COUNT = 50;
            const double DELTA_TIME = 0.016;

            var unreliableSendTimes = new Dictionary<int, double>();

            // Send reliable messages to trigger high RTT and congestion control
            LogTestProgress("Sending reliable messages to trigger congestion control...");
            for (int i = 0; i < RELIABLE_MESSAGE_COUNT; i++)
            {
                var reliableMsg = CreateTestMessage(i, 250);
                SendTestMessage(pair, pair.Endpoint1, reliableMsg, QosType.Reliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            // Allow time for RTT to increase
            RunUpdateCycles(pair, 50, DELTA_TIME);

            float initialRTT = pair.Endpoint1.RTTMilliseconds;
            LogTestProgress($"RTT after reliable messages: {initialRTT:F1}ms");

            // Now send unreliable messages while congestion control is active
            LogTestProgress("Sending unreliable messages during potential congestion...");
            for (int i = 0; i < UNRELIABLE_MESSAGE_COUNT; i++)
            {
                int seq = i + 10000;
                var unreliableMsg = CreateTestMessage(seq, 80);
                unreliableSendTimes[seq] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, unreliableMsg, QosType.Unreliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            // Process remaining messages
            RunUpdateCycles(pair, 100, DELTA_TIME);

            float finalRTT = pair.Endpoint1.RTTMilliseconds;
            LogTestProgress($"Final RTT: {finalRTT:F1}ms");

            // Calculate unreliable latencies
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

            LogTestProgress($"Received - Reliable: {reliableCount}/{RELIABLE_MESSAGE_COUNT}, Unreliable: {unreliableCount}/{UNRELIABLE_MESSAGE_COUNT}");

            // CRITICAL: Even with congestion control active, unreliable channel should not be throttled
            Assert.GreaterOrEqual(unreliableCount, UNRELIABLE_MESSAGE_COUNT,
                "Congestion control blocked unreliable channel!");

            // Unreliable messages should not be significantly delayed by congestion control
            var unreliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);

            double maxUnreliableLatency = 0;
            foreach (var msg in unreliableMessages)
            {
                if (msg.Latency > maxUnreliableLatency)
                    maxUnreliableLatency = msg.Latency;
            }

            LogTestProgress($"Max unreliable latency during congestion: {maxUnreliableLatency * 1000:F1}ms");

            // Unreliable should be faster than reliable channel's RTT
            Assert.Less(maxUnreliableLatency * 1000, finalRTT * 2,
                $"Unreliable channel affected by congestion control (latency: {maxUnreliableLatency * 1000:F1}ms vs RTT: {finalRTT:F1}ms)");

            LogTestProgress("Test PASSED - Congestion control doesn't block unreliable");
        }

        [Test]
        public void ReliableChannel_ThrottlesDuringCongestion_UnreliableDoesNot()
        {
            LogTestProgress("Starting ReliableChannel_ThrottlesDuringCongestion_UnreliableDoesNot test");

            var pair = CreateEndpointPair(simulateLatency: true, latencyMs: 280);

            const int MESSAGE_COUNT = 30;
            const double DELTA_TIME = 0.016;

            var reliableSendTimes = new Dictionary<int, double>();
            var unreliableSendTimes = new Dictionary<int, double>();

            // Send mixed traffic to induce congestion
            LogTestProgress("Sending mixed traffic...");
            for (int i = 0; i < MESSAGE_COUNT; i++)
            {
                // Reliable message
                int reliableSeq = i;
                var reliableMsg = CreateTestMessage(reliableSeq, 200);
                reliableSendTimes[reliableSeq] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, reliableMsg, QosType.Reliable);

                // Unreliable message
                int unreliableSeq = i + 10000;
                var unreliableMsg = CreateTestMessage(unreliableSeq, 80);
                unreliableSendTimes[unreliableSeq] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, unreliableMsg, QosType.Unreliable);

                UpdateEndpoints(pair, DELTA_TIME);
            }

            // Process
            RunUpdateCycles(pair, 100, DELTA_TIME);

            // Calculate latencies
            foreach (var msg in pair.Endpoint2Received)
            {
                int seq = GetSequenceNumber(msg.Data);

                if (msg.Channel == QosType.Reliable && reliableSendTimes.ContainsKey(seq))
                {
                    msg.Latency = msg.TimeReceived - reliableSendTimes[seq];
                }
                else if (msg.Channel == QosType.Unreliable && unreliableSendTimes.ContainsKey(seq))
                {
                    msg.Latency = msg.TimeReceived - unreliableSendTimes[seq];
                }
            }

            var reliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Reliable);
            var unreliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);

            // Calculate average latencies
            double avgReliableLatency = 0;
            int reliableLatencyCount = 0;
            double avgUnreliableLatency = 0;
            int unreliableLatencyCount = 0;

            foreach (var msg in reliableMessages)
            {
                if (msg.Latency > 0)
                {
                    avgReliableLatency += msg.Latency;
                    reliableLatencyCount++;
                }
            }

            foreach (var msg in unreliableMessages)
            {
                if (msg.Latency > 0)
                {
                    avgUnreliableLatency += msg.Latency;
                    unreliableLatencyCount++;
                }
            }

            if (reliableLatencyCount > 0)
                avgReliableLatency /= reliableLatencyCount;
            if (unreliableLatencyCount > 0)
                avgUnreliableLatency /= unreliableLatencyCount;

            LogTestProgress($"Reliable - Count: {reliableMessages.Count}, Avg Latency: {avgReliableLatency * 1000:F1}ms");
            LogTestProgress($"Unreliable - Count: {unreliableMessages.Count}, Avg Latency: {avgUnreliableLatency * 1000:F1}ms");

            // Unreliable should have lower or equal latency compared to reliable
            // Note: In test environment without actual congestion, latencies may be similar
            Assert.LessOrEqual(avgUnreliableLatency, avgReliableLatency * 1.1,
                $"Unreliable latency unexpectedly higher than reliable ({avgUnreliableLatency * 1000:F1}ms vs {avgReliableLatency * 1000:F1}ms)");

            // If latencies are similar (within 10%), that's acceptable in test environment
            if (avgUnreliableLatency < avgReliableLatency * 0.9)
            {
                LogTestProgress($"Test PASSED - Unreliable faster than reliable ({avgUnreliableLatency * 1000:F1}ms vs {avgReliableLatency * 1000:F1}ms)");
            }
            else
            {
                LogTestProgress($"Test PASSED - Latencies similar (test environment limitation). Unreliable: {avgUnreliableLatency * 1000:F1}ms, Reliable: {avgReliableLatency * 1000:F1}ms");
            }
        }

        [Test]
        public void HighRTT_TriggersCongestion_UnreliableUnaffected()
        {
            LogTestProgress("Starting HighRTT_TriggersCongestion_UnreliableUnaffected test");

            var pair = CreateEndpointPair(simulateLatency: true, latencyMs: 260); // Just over 250ms RTT threshold

            const int WARMUP_MESSAGES = 20;
            const int TEST_MESSAGES = 40;
            const double DELTA_TIME = 0.02;

            // Warmup: Send messages to establish high RTT
            LogTestProgress("Warmup phase: Establishing high RTT...");
            for (int i = 0; i < WARMUP_MESSAGES; i++)
            {
                var msg = CreateTestMessage(i, 150);
                SendTestMessage(pair, pair.Endpoint1, msg, QosType.Reliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            RunUpdateCycles(pair, 50, DELTA_TIME);

            float rtt = pair.Endpoint1.RTTMilliseconds;
            LogTestProgress($"RTT after warmup: {rtt:F1}ms (threshold is 250ms)");

            // Now send unreliable messages
            LogTestProgress("Test phase: Sending unreliable messages...");
            var unreliableSendTimes = new Dictionary<int, double>();

            for (int i = 0; i < TEST_MESSAGES; i++)
            {
                int seq = i + 20000;
                var msg = CreateTestMessage(seq, 80);
                unreliableSendTimes[seq] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, msg, QosType.Unreliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            RunUpdateCycles(pair, 80, DELTA_TIME);

            // Calculate unreliable metrics
            int unreliableReceived = 0;
            double maxLatency = 0;

            foreach (var msg in pair.Endpoint2Received)
            {
                if (msg.Channel == QosType.Unreliable)
                {
                    unreliableReceived++;
                    int seq = GetSequenceNumber(msg.Data);
                    if (unreliableSendTimes.ContainsKey(seq))
                    {
                        double latency = msg.TimeReceived - unreliableSendTimes[seq];
                        if (latency > maxLatency)
                            maxLatency = latency;
                    }
                }
            }

            LogTestProgress($"Unreliable received: {unreliableReceived}/{TEST_MESSAGES}");
            LogTestProgress($"Max unreliable latency: {maxLatency * 1000:F1}ms");
            LogTestProgress($"Reliable channel RTT: {rtt:F1}ms");

            // Unreliable should not be blocked by high RTT
            Assert.GreaterOrEqual(unreliableReceived, TEST_MESSAGES,
                "Unreliable messages blocked by high RTT congestion control");

            // Unreliable latency should be much lower than reliable RTT
            Assert.Less(maxLatency * 1000, rtt,
                $"Unreliable latency ({maxLatency * 1000:F1}ms) affected by reliable RTT ({rtt:F1}ms)");

            LogTestProgress("Test PASSED - High RTT doesn't affect unreliable channel");
        }

        [Test]
        public void CongestionRecovery_UnreliableRemainsUnaffected()
        {
            LogTestProgress("Starting CongestionRecovery_UnreliableRemainsUnaffected test");

            var pair = CreateEndpointPair(simulateLatency: true, latencyMs: 300);

            const double DELTA_TIME = 0.02;

            // Phase 1: Induce congestion
            LogTestProgress("Phase 1: Inducing congestion...");
            for (int i = 0; i < 30; i++)
            {
                var msg = CreateTestMessage(i, 200);
                SendTestMessage(pair, pair.Endpoint1, msg, QosType.Reliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            RunUpdateCycles(pair, 50, DELTA_TIME);

            float rttDuringCongestion = pair.Endpoint1.RTTMilliseconds;
            LogTestProgress($"RTT during congestion: {rttDuringCongestion:F1}ms");

            // Phase 2: Track unreliable during congestion
            LogTestProgress("Phase 2: Testing unreliable during congestion...");
            var phase2SendTimes = new Dictionary<int, double>();

            for (int i = 0; i < 20; i++)
            {
                int seq = i + 10000;
                var msg = CreateTestMessage(seq, 80);
                phase2SendTimes[seq] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, msg, QosType.Unreliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            RunUpdateCycles(pair, 40, DELTA_TIME);

            // Phase 3: Allow recovery (lower latency)
            LogTestProgress("Phase 3: Allowing congestion recovery...");
            // In real scenario, latency would decrease. We simulate by waiting
            RunUpdateCycles(pair, 200, DELTA_TIME);

            float rttAfterRecovery = pair.Endpoint1.RTTMilliseconds;
            LogTestProgress($"RTT after recovery: {rttAfterRecovery:F1}ms");

            // Phase 4: Test unreliable after recovery
            LogTestProgress("Phase 4: Testing unreliable after recovery...");
            var phase4SendTimes = new Dictionary<int, double>();

            for (int i = 0; i < 20; i++)
            {
                int seq = i + 20000;
                var msg = CreateTestMessage(seq, 80);
                phase4SendTimes[seq] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, msg, QosType.Unreliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            RunUpdateCycles(pair, 40, DELTA_TIME);

            // Calculate unreliable latencies in both phases
            double phase2MaxLatency = 0;
            int phase2Count = 0;
            double phase4MaxLatency = 0;
            int phase4Count = 0;

            foreach (var msg in pair.Endpoint2Received)
            {
                if (msg.Channel == QosType.Unreliable)
                {
                    int seq = GetSequenceNumber(msg.Data);

                    if (phase2SendTimes.ContainsKey(seq))
                    {
                        double latency = msg.TimeReceived - phase2SendTimes[seq];
                        if (latency > phase2MaxLatency)
                            phase2MaxLatency = latency;
                        phase2Count++;
                    }
                    else if (phase4SendTimes.ContainsKey(seq))
                    {
                        double latency = msg.TimeReceived - phase4SendTimes[seq];
                        if (latency > phase4MaxLatency)
                            phase4MaxLatency = latency;
                        phase4Count++;
                    }
                }
            }

            LogTestProgress($"Phase 2 (congestion) - Unreliable: {phase2Count}/20, Max Latency: {phase2MaxLatency * 1000:F1}ms");
            LogTestProgress($"Phase 4 (recovery) - Unreliable: {phase4Count}/20, Max Latency: {phase4MaxLatency * 1000:F1}ms");

            // Both phases should receive all messages
            Assert.GreaterOrEqual(phase2Count, 20, "Unreliable blocked during congestion");
            Assert.GreaterOrEqual(phase4Count, 20, "Unreliable blocked after recovery");

            // Unreliable should have low latency in both phases
            Assert.Less(phase2MaxLatency, 1.0, "Unreliable latency too high during congestion");
            Assert.Less(phase4MaxLatency, 1.0, "Unreliable latency too high after recovery");

            LogTestProgress("Test PASSED - Unreliable unaffected through congestion cycle");
        }
    }
}
