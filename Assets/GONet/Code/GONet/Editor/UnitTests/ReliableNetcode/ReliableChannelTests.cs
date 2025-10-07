using System;
using System.Collections.Generic;
using NUnit.Framework;
using ReliableNetcode;
using UnityEngine;

namespace GONet.Tests.ReliableNetcode
{
    /// <summary>
    /// Tests for reliable channel specific behavior - ordering guarantees
    /// </summary>
    [TestFixture]
    public class ReliableChannelTests : ReliableEndpointTestBase
    {
        [Test]
        public void ReliableChannel_GuaranteesOrder()
        {
            LogTestProgress("Starting ReliableChannel_GuaranteesOrder test");

            var pair = CreateEndpointPair();

            const int MESSAGE_COUNT = 100;
            const double DELTA_TIME = 0.01;

            // Send 100 reliable messages with sequence numbers
            LogTestProgress($"Sending {MESSAGE_COUNT} reliable messages...");
            for (int i = 0; i < MESSAGE_COUNT; i++)
            {
                var message = CreateTestMessage(i, 120);
                SendTestMessage(pair, pair.Endpoint1, message, QosType.Reliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            // Process remaining messages
            RunUpdateCycles(pair, 100, DELTA_TIME);

            // Extract reliable messages
            var reliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Reliable);

            LogTestProgress($"Received {reliableMessages.Count}/{MESSAGE_COUNT} reliable messages");

            // Verify all messages arrived
            Assert.AreEqual(MESSAGE_COUNT, reliableMessages.Count,
                $"Expected {MESSAGE_COUNT} messages, received {reliableMessages.Count}");

            // Verify messages arrived in exact order
            for (int i = 0; i < reliableMessages.Count; i++)
            {
                int sequence = GetSequenceNumber(reliableMessages[i].Data);
                Assert.AreEqual(i, sequence,
                    $"Message {i} has sequence {sequence} - OUT OF ORDER!");
            }

            LogTestProgress("Test PASSED - All messages arrived in order");
        }

        [Test]
        public void ReliableChannel_AllMessagesArrive_WithPacketLoss()
        {
            LogTestProgress("Starting ReliableChannel_AllMessagesArrive_WithPacketLoss test");

            var pair = CreateEndpointPair(simulateLatency: true, latencyMs: 50);

            const int MESSAGE_COUNT = 50;
            const double DELTA_TIME = 0.01;

            // Simulate packet loss by randomly dropping some transmissions
            int transmitAttempts = 0;
            int transmitDropped = 0;
            var originalTransmit1 = pair.Endpoint1.TransmitCallback;

            pair.Endpoint1.TransmitCallback = (buffer, length) =>
            {
                transmitAttempts++;
                // Drop 20% of packets to simulate network conditions
                if (UnityEngine.Random.value > 0.8f)
                {
                    transmitDropped++;
                    return; // Drop packet
                }
                originalTransmit1(buffer, length);
            };

            LogTestProgress($"Sending {MESSAGE_COUNT} reliable messages with simulated packet loss...");
            for (int i = 0; i < MESSAGE_COUNT; i++)
            {
                var message = CreateTestMessage(i, 150);
                SendTestMessage(pair, pair.Endpoint1, message, QosType.Reliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            // Allow plenty of time for retransmissions
            RunUpdateCycles(pair, 300, DELTA_TIME);

            var reliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Reliable);

            LogTestProgress($"Transmit attempts: {transmitAttempts}, Dropped: {transmitDropped} ({(float)transmitDropped / transmitAttempts * 100:F1}%)");
            LogTestProgress($"Received {reliableMessages.Count}/{MESSAGE_COUNT} reliable messages");

            // All messages should eventually arrive despite packet loss
            Assert.AreEqual(MESSAGE_COUNT, reliableMessages.Count,
                $"Reliable channel lost messages even with retransmission!");

            // Verify order
            for (int i = 0; i < reliableMessages.Count; i++)
            {
                int sequence = GetSequenceNumber(reliableMessages[i].Data);
                Assert.AreEqual(i, sequence, "Messages out of order despite reliable channel");
            }

            LogTestProgress("Test PASSED - All messages arrived in order despite packet loss");
        }

        [Test]
        public void ReliableChannel_LargeMessages_ArriveInOrder()
        {
            LogTestProgress("Starting ReliableChannel_LargeMessages_ArriveInOrder test");

            var pair = CreateEndpointPair();

            const int MESSAGE_COUNT = 30;
            const double DELTA_TIME = 0.01;

            // Send large messages (near fragmentation threshold)
            LogTestProgress($"Sending {MESSAGE_COUNT} large reliable messages...");
            for (int i = 0; i < MESSAGE_COUNT; i++)
            {
                var message = CreateTestMessage(i, 900); // Large payload
                SendTestMessage(pair, pair.Endpoint1, message, QosType.Reliable);
                UpdateEndpoints(pair, DELTA_TIME);
            }

            RunUpdateCycles(pair, 150, DELTA_TIME);

            var reliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Reliable);

            LogTestProgress($"Received {reliableMessages.Count}/{MESSAGE_COUNT} large messages");

            Assert.AreEqual(MESSAGE_COUNT, reliableMessages.Count,
                "Large messages lost");

            // Verify order
            for (int i = 0; i < reliableMessages.Count; i++)
            {
                int sequence = GetSequenceNumber(reliableMessages[i].Data);
                Assert.AreEqual(i, sequence, "Large messages out of order");
            }

            LogTestProgress("Test PASSED - Large messages arrived in order");
        }

        [Test]
        public void ReliableChannel_RapidBurst_MaintainsOrder()
        {
            LogTestProgress("Starting ReliableChannel_RapidBurst_MaintainsOrder test");

            var pair = CreateEndpointPair();

            const int BURST_SIZE = 200;

            // Send all messages in rapid succession (single frame burst)
            LogTestProgress($"Sending burst of {BURST_SIZE} messages in rapid succession...");
            for (int i = 0; i < BURST_SIZE; i++)
            {
                var message = CreateTestMessage(i, 100);
                SendTestMessage(pair, pair.Endpoint1, message, QosType.Reliable);
            }

            // Single update to trigger send
            UpdateEndpoints(pair, 0.016);

            // Process with regular updates
            RunUpdateCycles(pair, 300, 0.016);

            var reliableMessages = pair.Endpoint2Received.FindAll(m => m.Channel == QosType.Reliable);

            LogTestProgress($"Received {reliableMessages.Count}/{BURST_SIZE} messages from rapid burst");

            Assert.AreEqual(BURST_SIZE, reliableMessages.Count,
                $"Messages lost from rapid burst ({reliableMessages.Count}/{BURST_SIZE})");

            // Verify strict ordering
            for (int i = 0; i < reliableMessages.Count; i++)
            {
                int sequence = GetSequenceNumber(reliableMessages[i].Data);
                Assert.AreEqual(i, sequence,
                    $"Rapid burst broke ordering at index {i} (sequence: {sequence})");
            }

            LogTestProgress("Test PASSED - Rapid burst maintained strict ordering");
        }
    }
}
