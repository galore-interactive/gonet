using System;
using System.Collections.Generic;
using NUnit.Framework;
using ReliableNetcode;
using UnityEngine;

namespace GONet.Tests.ReliableNetcode
{
    /// <summary>
    /// Base class for ReliableNetcode tests providing common test infrastructure
    /// </summary>
    public abstract class ReliableEndpointTestBase
    {
        protected class TestEndpointPair
        {
            public ReliableEndpoint Endpoint1;
            public ReliableEndpoint Endpoint2;
            public List<ReceivedMessage> Endpoint1Received = new List<ReceivedMessage>();
            public List<ReceivedMessage> Endpoint2Received = new List<ReceivedMessage>();
            public double CurrentTime = 0.0;
            public int Endpoint1SentCount = 0;
            public int Endpoint2SentCount = 0;
            public Queue<(byte[], int, double)> LatencyQueue1to2;
            public Queue<(byte[], int, double)> LatencyQueue2to1;

            // Track which channel each sequence number was sent on
            public Dictionary<int, QosType> SentMessageChannels = new Dictionary<int, QosType>();
        }

        protected class ReceivedMessage
        {
            public byte[] Data;
            public int Length;
            public QosType Channel;
            public double TimeReceived;
            public double Latency; // Set if we can calculate it
        }

        /// <summary>
        /// Create a pair of connected endpoints with proper callbacks
        /// </summary>
        protected TestEndpointPair CreateEndpointPair(bool simulateLatency = false, double latencyMs = 50.0)
        {
            var pair = new TestEndpointPair();
            pair.Endpoint1 = new ReliableEndpoint();
            pair.Endpoint2 = new ReliableEndpoint();

            // Set up transmission callbacks with optional latency simulation
            if (simulateLatency)
            {
                var latencyQueue1to2 = new Queue<(byte[], int, double)>();
                var latencyQueue2to1 = new Queue<(byte[], int, double)>();

                pair.Endpoint1.TransmitCallback = (buffer, length) =>
                {
                    byte[] copy = new byte[length];
                    Buffer.BlockCopy(buffer, 0, copy, 0, length);
                    latencyQueue1to2.Enqueue((copy, length, pair.CurrentTime + latencyMs / 1000.0));
                    pair.Endpoint1SentCount++;
                };

                pair.Endpoint2.TransmitCallback = (buffer, length) =>
                {
                    byte[] copy = new byte[length];
                    Buffer.BlockCopy(buffer, 0, copy, 0, length);
                    latencyQueue2to1.Enqueue((copy, length, pair.CurrentTime + latencyMs / 1000.0));
                    pair.Endpoint2SentCount++;
                };

                // Store latency queues in pair for processing during Update
                pair.LatencyQueue1to2 = latencyQueue1to2;
                pair.LatencyQueue2to1 = latencyQueue2to1;
            }
            else
            {
                // Direct transmission (no latency)
                pair.Endpoint1.TransmitCallback = (buffer, length) =>
                {
                    byte[] copy = new byte[length];
                    Buffer.BlockCopy(buffer, 0, copy, 0, length);
                    pair.Endpoint2.ReceivePacket(copy, length);
                    pair.Endpoint1SentCount++;
                };

                pair.Endpoint2.TransmitCallback = (buffer, length) =>
                {
                    byte[] copy = new byte[length];
                    Buffer.BlockCopy(buffer, 0, copy, 0, length);
                    pair.Endpoint1.ReceivePacket(copy, length);
                    pair.Endpoint2SentCount++;
                };
            }

            // Set up receive callbacks to track messages
            // Use sequence number to lookup which channel this message was sent on
            pair.Endpoint1.ReceiveCallback = (buffer, length) =>
            {
                byte[] copy = new byte[length];
                Buffer.BlockCopy(buffer, 0, copy, 0, length);

                int sequenceNumber = GetSequenceNumber(copy);
                QosType channel = pair.SentMessageChannels.ContainsKey(sequenceNumber)
                    ? pair.SentMessageChannels[sequenceNumber]
                    : QosType.Reliable;

                pair.Endpoint1Received.Add(new ReceivedMessage
                {
                    Data = copy,
                    Length = length,
                    Channel = channel,
                    TimeReceived = pair.CurrentTime
                });
            };

            pair.Endpoint2.ReceiveCallback = (buffer, length) =>
            {
                byte[] copy = new byte[length];
                Buffer.BlockCopy(buffer, 0, copy, 0, length);

                int sequenceNumber = GetSequenceNumber(copy);
                QosType channel = pair.SentMessageChannels.ContainsKey(sequenceNumber)
                    ? pair.SentMessageChannels[sequenceNumber]
                    : QosType.Reliable;

                pair.Endpoint2Received.Add(new ReceivedMessage
                {
                    Data = copy,
                    Length = length,
                    Channel = channel,
                    TimeReceived = pair.CurrentTime
                });
            };

            return pair;
        }

        /// <summary>
        /// Update both endpoints and advance time
        /// </summary>
        protected void UpdateEndpoints(TestEndpointPair pair, double deltaTime)
        {
            pair.CurrentTime += deltaTime;

            // CRITICAL: Process latency queues BEFORE Update() to deliver delayed packets
            if (pair.LatencyQueue2to1 != null)
            {
                while (pair.LatencyQueue2to1.Count > 0 && pair.LatencyQueue2to1.Peek().Item3 <= pair.CurrentTime)
                {
                    var (data, len, _) = pair.LatencyQueue2to1.Dequeue();
                    pair.Endpoint1.ReceivePacket(data, len);
                }
            }

            if (pair.LatencyQueue1to2 != null)
            {
                while (pair.LatencyQueue1to2.Count > 0 && pair.LatencyQueue1to2.Peek().Item3 <= pair.CurrentTime)
                {
                    var (data, len, _) = pair.LatencyQueue1to2.Dequeue();
                    pair.Endpoint2.ReceivePacket(data, len);
                }
            }

            pair.Endpoint1.Update(pair.CurrentTime);
            pair.Endpoint2.Update(pair.CurrentTime);
            pair.Endpoint1.ProcessSendBuffer_IfAppropriate();
            pair.Endpoint2.ProcessSendBuffer_IfAppropriate();
        }

        /// <summary>
        /// Run multiple update cycles
        /// </summary>
        protected void RunUpdateCycles(TestEndpointPair pair, int cycles, double deltaTimePerCycle)
        {
            for (int i = 0; i < cycles; i++)
            {
                UpdateEndpoints(pair, deltaTimePerCycle);
            }
        }

        /// <summary>
        /// Send a test message and track which channel it was sent on
        /// </summary>
        protected void SendTestMessage(TestEndpointPair pair, ReliableEndpoint fromEndpoint, byte[] message, QosType channel)
        {
            int sequenceNumber = GetSequenceNumber(message);
            pair.SentMessageChannels[sequenceNumber] = channel;
            fromEndpoint.SendMessage(message, message.Length, channel);
        }

        /// <summary>
        /// Create test message with sequence number
        /// </summary>
        protected byte[] CreateTestMessage(int sequenceNumber, int size = 100)
        {
            byte[] data = new byte[size];

            // Write sequence number at start (for verification)
            Buffer.BlockCopy(BitConverter.GetBytes(sequenceNumber), 0, data, 0, 4);

            // Fill rest with pattern
            for (int i = 4; i < size; i++)
            {
                data[i] = (byte)((sequenceNumber + i) % 256);
            }

            return data;
        }

        /// <summary>
        /// Extract sequence number from test message
        /// </summary>
        protected int GetSequenceNumber(byte[] data)
        {
            return BitConverter.ToInt32(data, 0);
        }

        /// <summary>
        /// Verify messages arrived in order
        /// </summary>
        protected void AssertMessagesInOrder(List<ReceivedMessage> messages, int expectedCount)
        {
            Assert.AreEqual(expectedCount, messages.Count,
                $"Expected {expectedCount} messages but received {messages.Count}");

            for (int i = 0; i < messages.Count; i++)
            {
                int sequence = GetSequenceNumber(messages[i].Data);
                Assert.AreEqual(i, sequence,
                    $"Message {i} has sequence number {sequence}, expected {i}");
            }
        }

        /// <summary>
        /// Assert all messages arrived within latency threshold
        /// </summary>
        protected void AssertLatencyUnder(List<ReceivedMessage> messages, double maxLatencySeconds)
        {
            foreach (var msg in messages)
            {
                if (msg.Latency > 0)
                {
                    Assert.Less(msg.Latency, maxLatencySeconds,
                        $"Message latency {msg.Latency:F3}s exceeds threshold {maxLatencySeconds}s");
                }
            }
        }

        /// <summary>
        /// Count messages by channel type
        /// </summary>
        protected (int reliable, int unreliable) CountMessagesByChannel(List<ReceivedMessage> messages)
        {
            int reliable = 0;
            int unreliable = 0;

            foreach (var msg in messages)
            {
                if (msg.Channel == QosType.Reliable)
                    reliable++;
                else if (msg.Channel == QosType.Unreliable)
                    unreliable++;
            }

            return (reliable, unreliable);
        }

        /// <summary>
        /// Log test progress (useful for debugging)
        /// </summary>
        protected void LogTestProgress(string message)
        {
            Debug.Log($"[ReliableNetcode Test] {message}");
        }
    }
}
