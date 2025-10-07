using System;
using System.Net;
using System.Reflection;
using NUnit.Framework;
using NetcodeIO.NET;
using NetcodeIO.NET.Utils.IO;
using ReliableNetcode;
using ReliableNetcode.Utils;
using UnityEngine.TestTools;

namespace GONet.Tests.ReliableNetcode
{
    /// <summary>
    /// Tests for reliable channel starvation fix.
    ///
    /// NOTE: Full dequeue testing requires ACKs from real network communication.
    /// These tests validate fix constants and basic queue behavior.
    /// Integration testing with GONetSandbox required for end-to-end validation.
    /// </summary>
    [TestFixture]
    public class ReliableNetcodeStarvationTests : ReliableNetcodeTestBase
    {
        /// <summary>
        /// Test 1: Verify fix constants are present in MessageChannel.cs
        /// Validates that MAX_DEQUEUE_PER_UPDATE and MAX_DEQUEUE_TIME_MS exist with correct values.
        /// </summary>
        [Test]
        public void TestFixConstants_ArePresent()
        {
            // Read MessageChannel.cs source to verify fix constants exist
            string filePath = "Assets/GONet/Code/ReliableNetcode/MessageChannel.cs";
            string sourceCode = System.IO.File.ReadAllText(filePath);

            // Verify MAX_DEQUEUE_PER_UPDATE = 100
            Assert.IsTrue(sourceCode.Contains("MAX_DEQUEUE_PER_UPDATE = 100"),
                "Fix constant MAX_DEQUEUE_PER_UPDATE = 100 not found in MessageChannel.cs");

            // Verify MAX_DEQUEUE_TIME_MS = 0.5
            Assert.IsTrue(sourceCode.Contains("MAX_DEQUEUE_TIME_MS = 0.5"),
                "Fix constant MAX_DEQUEUE_TIME_MS = 0.5 not found in MessageChannel.cs");

            // Verify dequeue loop exists
            Assert.IsTrue(sourceCode.Contains("while (messageQueue.Count > 0 &&"),
                "Dequeue while loop not found in MessageChannel.cs");

            // Verify sendBufferSize caching optimization
            Assert.IsTrue(sourceCode.Contains("int sendBufferSize = 0;") &&
                         sourceCode.Contains("sendBufferSize++;  // Track locally"),
                "sendBufferSize caching optimization not found in MessageChannel.cs");

            UnityEngine.Debug.Log("✓ All fix constants validated: MAX_DEQUEUE_PER_UPDATE=100, MAX_DEQUEUE_TIME_MS=0.5, sendBufferSize caching present");
        }

        /// <summary>
        /// Test 2: Verify dequeue rate (100 messages per update).
        /// Uses reflection to simulate ACKs by clearing sendBuffer space.
        /// </summary>
        [Test]
        [Timeout(30000)]
        public void TestDequeueRate_Processes100MessagesPerUpdate()
        {
            LogAssert.ignoreFailingMessages = true;

            double time = 0.0;
            const double dt = 1.0 / 60.0;

            // Create standalone endpoint
            ReliableEndpoint endpoint = CreateReliableEndpoint();
            endpoint.Update(time);

            // Flood with 400 messages to overflow sendBuffer (256) into messageQueue (~144 queued)
            const int MESSAGE_COUNT = 400;
            byte[] messageData = new byte[200];

            for (int i = 0; i < MESSAGE_COUNT; i++)
            {
                endpoint.SendMessage(messageData, messageData.Length, QosType.Reliable);
            }

            // Use reflection to access internal structures
            var channelsField = typeof(ReliableEndpoint).GetField("messageChannels", BindingFlags.NonPublic | BindingFlags.Instance);
            var channels = (MessageChannel[])channelsField.GetValue(endpoint);
            var reliableChannel = channels[0];
            var queueField = reliableChannel.GetType().GetField("messageQueue", BindingFlags.NonPublic | BindingFlags.Instance);
            var messageQueue = (System.Collections.Generic.Queue<ByteBuffer>)queueField.GetValue(reliableChannel);

            var sendBufferField = reliableChannel.GetType().GetField("sendBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
            var sendBuffer = sendBufferField.GetValue(reliableChannel);

            int initialQueueCount = messageQueue.Count;
            UnityEngine.Debug.Log($"Initial: {MESSAGE_COUNT} messages sent, {initialQueueCount} queued (expected ~144)");

            // Simulate ACKs by clearing sendBuffer space using RemoveEntries
            var removeEntriesMethod = sendBuffer.GetType().GetMethod("RemoveEntries", BindingFlags.Public | BindingFlags.Instance);
            removeEntriesMethod.Invoke(sendBuffer, new object[] { 0, 255 }); // Clear all 256 entries

            // Now do ONE update - should dequeue up to 100 messages
            time += dt;
            endpoint.Update(time);
            endpoint.ProcessSendBuffer_IfAppropriate();

            int remainingQueueCount = messageQueue.Count;
            int processedCount = initialQueueCount - remainingQueueCount;

            UnityEngine.Debug.Log($"After 1 update: processed {processedCount} messages (initial: {initialQueueCount}, remaining: {remainingQueueCount})");

            // Should process up to 100 messages (MAX_DEQUEUE_PER_UPDATE)
            Assert.GreaterOrEqual(processedCount, 90, $"Should process at least 90 messages per update (got {processedCount})");
            Assert.LessOrEqual(processedCount, 100, $"Should not exceed 100 messages per update (got {processedCount})");

            UnityEngine.Debug.Log($"✓ Dequeue rate validated: {processedCount} messages processed in 1 update (MAX_DEQUEUE_PER_UPDATE=100)");

            LogAssert.ignoreFailingMessages = false;
        }

        /// <summary>
        /// Test 3: Verify sendBuffer size is 256 (architectural assumption for overflow tests).
        /// </summary>
        [Test]
        public void TestSendBuffer_SizeIs256()
        {
            // Read MessageChannel.cs to verify sendBuffer size
            string filePath = "Assets/GONet/Code/ReliableNetcode/MessageChannel.cs";
            string sourceCode = System.IO.File.ReadAllText(filePath);

            // Verify sendBuffer size = 256
            Assert.IsTrue(sourceCode.Contains("sendBuffer = new SequenceBuffer<BufferedPacket>(256)"),
                "sendBuffer size should be 256 in MessageChannel.cs");

            UnityEngine.Debug.Log("✓ sendBuffer size validated: 256");
        }
    }
}
