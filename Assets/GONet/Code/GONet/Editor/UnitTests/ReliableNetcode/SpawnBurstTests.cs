using System;
using System.Collections.Generic;
using NUnit.Framework;
using ReliableNetcode;
using UnityEngine;

namespace GONet.Tests.ReliableNetcode
{
    /// <summary>
    /// Tests simulating the exact projectile freezing scenario:
    /// - Burst of 50+ spawn events on reliable channel
    /// - Continuous position updates on unreliable channel
    /// - Verify position updates don't freeze (idle for 7-9 seconds)
    /// </summary>
    [TestFixture]
    public class SpawnBurstTests : ReliableEndpointTestBase
    {
        [Test]
        public void SpawnBurst50_DoesNotStarve_PositionUpdates()
        {
            LogTestProgress("Starting SpawnBurst50_DoesNotStarve_PositionUpdates test");

            var pair = CreateEndpointPair();

            const int SPAWN_BURST_SIZE = 50;
            const int POSITION_UPDATE_COUNT = 100;
            const double DELTA_TIME = 0.016; // ~60Hz update rate

            var positionUpdateSendTimes = new Dictionary<int, double>();
            var positionUpdateReceiveTimes = new Dictionary<int, double>();

            // Phase 1: Send burst of 50 spawn messages (single frame in real scenario)
            LogTestProgress($"Phase 1: Sending burst of {SPAWN_BURST_SIZE} spawn messages...");
            for (int i = 0; i < SPAWN_BURST_SIZE; i++)
            {
                // Simulate spawn event message (larger payload)
                var spawnMessage = CreateTestMessage(i, 300);
                SendTestMessage(pair, pair.Endpoint1, spawnMessage, QosType.Reliable);
            }

            // Single update to trigger transmission
            UpdateEndpoints(pair, DELTA_TIME);

            LogTestProgress($"Spawn burst queued at time {pair.CurrentTime:F3}s");

            // Phase 2: While spawn burst is being processed, send continuous position updates
            LogTestProgress($"Phase 2: Sending {POSITION_UPDATE_COUNT} position updates during spawn processing...");

            double lastPositionGapCheck = pair.CurrentTime;
            double maxGapBetweenPositionUpdates = 0;

            for (int i = 0; i < POSITION_UPDATE_COUNT; i++)
            {
                // Simulate position update (small payload, unreliable)
                var positionMsg = CreateTestMessage(i + 5000, 80);
                positionUpdateSendTimes[i + 5000] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, positionMsg, QosType.Unreliable);

                UpdateEndpoints(pair, DELTA_TIME);

                // Track gaps in position update delivery (this is where freezing would show up)
                var receivedPositionUpdates = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);
                if (receivedPositionUpdates.Count > 0)
                {
                    double timeSinceLastUpdate = pair.CurrentTime - lastPositionGapCheck;
                    if (timeSinceLastUpdate > maxGapBetweenPositionUpdates)
                    {
                        maxGapBetweenPositionUpdates = timeSinceLastUpdate;
                    }
                    lastPositionGapCheck = pair.CurrentTime;
                }
            }

            // Phase 3: Continue updates to ensure all messages arrive
            LogTestProgress("Phase 3: Processing remaining messages...");
            RunUpdateCycles(pair, 100, DELTA_TIME);

            LogTestProgress($"Total messages received: {pair.Endpoint2Received.Count}");

            // Calculate latencies for position updates
            foreach (var msg in pair.Endpoint2Received)
            {
                if (msg.Channel == QosType.Unreliable)
                {
                    int seq = GetSequenceNumber(msg.Data);
                    if (positionUpdateSendTimes.ContainsKey(seq))
                    {
                        double latency = msg.TimeReceived - positionUpdateSendTimes[seq];
                        msg.Latency = latency;
                        positionUpdateReceiveTimes[seq] = msg.TimeReceived;
                    }
                }
            }

            var (spawnCount, positionCount) = CountMessagesByChannel(pair.Endpoint2Received);

            LogTestProgress($"Spawns received: {spawnCount}/{SPAWN_BURST_SIZE}");
            LogTestProgress($"Position updates received: {positionCount}/{POSITION_UPDATE_COUNT}");

            // CRITICAL ASSERTION #1: No position update should be delayed more than 1 second
            // (In the real bug, projectiles freeze for 7-9 seconds!)
            var positionMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);

            double maxPositionLatency = 0;
            int frozenUpdates = 0; // Position updates delayed > 1s

            foreach (var msg in positionMessages)
            {
                if (msg.Latency > 0)
                {
                    if (msg.Latency > maxPositionLatency)
                        maxPositionLatency = msg.Latency;

                    if (msg.Latency > 1.0) // Freeze threshold
                    {
                        frozenUpdates++;
                        LogTestProgress($"FROZEN UPDATE detected! Latency: {msg.Latency:F3}s");
                    }
                }
            }

            LogTestProgress($"Max position update latency: {maxPositionLatency:F3}s");
            LogTestProgress($"Max gap between position updates: {maxGapBetweenPositionUpdates:F3}s");
            LogTestProgress($"Frozen updates (>1s latency): {frozenUpdates}");

            Assert.AreEqual(0, frozenUpdates,
                $"{frozenUpdates} position updates were frozen (>1s latency) - PROJECTILE FREEZING BUG DETECTED!");

            Assert.Less(maxPositionLatency, 1.0,
                $"Position update latency too high ({maxPositionLatency:F3}s) - indicates channel starvation");

            // CRITICAL ASSERTION #2: Position updates should arrive continuously (no 7-9 second gaps)
            Assert.Less(maxGapBetweenPositionUpdates, 1.0,
                $"Gap of {maxGapBetweenPositionUpdates:F3}s between position updates - FREEZE DETECTED!");

            // All position updates should arrive
            Assert.GreaterOrEqual(positionCount, POSITION_UPDATE_COUNT * 0.95,
                $"Too many position updates lost ({positionCount}/{POSITION_UPDATE_COUNT})");

            // All spawns should eventually arrive
            Assert.GreaterOrEqual(spawnCount, SPAWN_BURST_SIZE,
                $"Spawns lost ({spawnCount}/{SPAWN_BURST_SIZE})");

            LogTestProgress("Test PASSED - No projectile freezing detected during spawn burst");
        }

        [Test]
        public void MultipleSpawnBursts_ContinuousPositionUpdates_NoFreeze()
        {
            LogTestProgress("Starting MultipleSpawnBursts_ContinuousPositionUpdates_NoFreeze test");

            var pair = CreateEndpointPair();

            const int NUM_BURSTS = 5;
            const int SPAWN_PER_BURST = 20;
            const int POSITION_UPDATES_BETWEEN_BURSTS = 30;
            const double DELTA_TIME = 0.016;

            var positionUpdateSendTimes = new Dictionary<int, double>();
            int totalSpawnsSent = 0;
            int totalPositionsSent = 0;

            // Simulate multiple spawn bursts with continuous position updates
            for (int burst = 0; burst < NUM_BURSTS; burst++)
            {
                LogTestProgress($"Burst {burst + 1}/{NUM_BURSTS}: Sending {SPAWN_PER_BURST} spawns...");

                // Send spawn burst
                for (int i = 0; i < SPAWN_PER_BURST; i++)
                {
                    var spawnMsg = CreateTestMessage(totalSpawnsSent++, 300);
                    SendTestMessage(pair, pair.Endpoint1, spawnMsg, QosType.Reliable);
                }

                UpdateEndpoints(pair, DELTA_TIME);

                // Send position updates while spawn burst processes
                LogTestProgress($"Sending {POSITION_UPDATES_BETWEEN_BURSTS} position updates...");
                for (int i = 0; i < POSITION_UPDATES_BETWEEN_BURSTS; i++)
                {
                    int seq = totalPositionsSent + 10000;
                    var posMsg = CreateTestMessage(seq, 80);
                    positionUpdateSendTimes[seq] = pair.CurrentTime;
                    SendTestMessage(pair, pair.Endpoint1, posMsg, QosType.Unreliable);

                    totalPositionsSent++;
                    UpdateEndpoints(pair, DELTA_TIME);
                }
            }

            // Process remaining
            RunUpdateCycles(pair, 150, DELTA_TIME);

            LogTestProgress($"Total sent - Spawns: {totalSpawnsSent}, Positions: {totalPositionsSent}");
            LogTestProgress($"Total received: {pair.Endpoint2Received.Count}");

            // Calculate latencies
            foreach (var msg in pair.Endpoint2Received)
            {
                if (msg.Channel == QosType.Unreliable)
                {
                    int seq = GetSequenceNumber(msg.Data);
                    if (positionUpdateSendTimes.ContainsKey(seq))
                    {
                        msg.Latency = msg.TimeReceived - positionUpdateSendTimes[seq];
                    }
                }
            }

            var (spawnCount, positionCount) = CountMessagesByChannel(pair.Endpoint2Received);

            LogTestProgress($"Spawns received: {spawnCount}/{totalSpawnsSent}");
            LogTestProgress($"Positions received: {positionCount}/{totalPositionsSent}");

            // Check for freezing across multiple bursts
            var positionMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);

            double maxLatency = 0;
            int frozenCount = 0;

            foreach (var msg in positionMessages)
            {
                if (msg.Latency > 0)
                {
                    if (msg.Latency > maxLatency)
                        maxLatency = msg.Latency;

                    if (msg.Latency > 1.0)
                        frozenCount++;
                }
            }

            LogTestProgress($"Max position latency across all bursts: {maxLatency:F3}s");
            LogTestProgress($"Frozen updates: {frozenCount}");

            Assert.AreEqual(0, frozenCount,
                $"{frozenCount} position updates frozen across multiple spawn bursts");

            Assert.Less(maxLatency, 1.0,
                $"Position latency too high ({maxLatency:F3}s) during multiple spawn bursts");

            LogTestProgress("Test PASSED - No freezing across multiple spawn bursts");
        }

        [Test]
        public void LargeSpawnBurst_100Entities_PositionUpdatesStillFlow()
        {
            LogTestProgress("Starting LargeSpawnBurst_100Entities_PositionUpdatesStillFlow test");

            var pair = CreateEndpointPair();

            const int LARGE_SPAWN_BURST = 100; // Stress test
            const int CONTINUOUS_POSITION_UPDATES = 200;
            const double DELTA_TIME = 0.016;

            var positionSendTimes = new Dictionary<int, double>();

            // Send massive spawn burst (worst case scenario)
            LogTestProgress($"Sending LARGE burst of {LARGE_SPAWN_BURST} spawns...");
            for (int i = 0; i < LARGE_SPAWN_BURST; i++)
            {
                var spawnMsg = CreateTestMessage(i, 350);
                SendTestMessage(pair, pair.Endpoint1, spawnMsg, QosType.Reliable);
            }

            UpdateEndpoints(pair, DELTA_TIME);

            // Continuously send position updates
            LogTestProgress($"Sending {CONTINUOUS_POSITION_UPDATES} position updates during massive spawn...");

            for (int i = 0; i < CONTINUOUS_POSITION_UPDATES; i++)
            {
                int seq = i + 20000;
                var posMsg = CreateTestMessage(seq, 80);
                positionSendTimes[seq] = pair.CurrentTime;
                SendTestMessage(pair, pair.Endpoint1, posMsg, QosType.Unreliable);

                UpdateEndpoints(pair, DELTA_TIME);

                // Log progress every 50 updates
                if ((i + 1) % 50 == 0)
                {
                    var received = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);
                    LogTestProgress($"  Progress: {i + 1}/{CONTINUOUS_POSITION_UPDATES} sent, {received.Count} position updates received so far");
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
                    if (positionSendTimes.ContainsKey(seq))
                    {
                        msg.Latency = msg.TimeReceived - positionSendTimes[seq];
                    }
                }
            }

            var (spawnCount, positionCount) = CountMessagesByChannel(pair.Endpoint2Received);

            LogTestProgress($"Results - Spawns: {spawnCount}/{LARGE_SPAWN_BURST}, Positions: {positionCount}/{CONTINUOUS_POSITION_UPDATES}");

            var positionMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Unreliable);

            double maxLatency = 0;
            double avgLatency = 0;
            int latencyCount = 0;
            int frozenCount = 0;

            foreach (var msg in positionMessages)
            {
                if (msg.Latency > 0)
                {
                    avgLatency += msg.Latency;
                    latencyCount++;

                    if (msg.Latency > maxLatency)
                        maxLatency = msg.Latency;

                    if (msg.Latency > 1.0)
                        frozenCount++;
                }
            }

            if (latencyCount > 0)
                avgLatency /= latencyCount;

            LogTestProgress($"Position latency - Max: {maxLatency:F3}s, Avg: {avgLatency:F3}s");
            LogTestProgress($"Frozen updates (>1s): {frozenCount}");

            // Even with 100 spawn burst, position updates should not freeze
            Assert.AreEqual(0, frozenCount,
                $"{frozenCount} position updates frozen during massive 100-entity spawn burst!");

            Assert.Less(maxLatency, 2.0,
                $"Max position latency ({maxLatency:F3}s) too high even for stress test");

            // Should receive most position updates
            Assert.GreaterOrEqual(positionCount, CONTINUOUS_POSITION_UPDATES * 0.9,
                $"Too many position updates lost in stress test ({positionCount}/{CONTINUOUS_POSITION_UPDATES})");

            LogTestProgress("Test PASSED - Position updates flow even during 100-entity spawn burst");
        }
    }
}
