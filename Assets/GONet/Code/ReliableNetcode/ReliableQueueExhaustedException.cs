using System;

namespace ReliableNetcode
{
    /// <summary>
    /// Exception thrown when a reliable message channel's queue reaches capacity and cannot accept more messages.
    ///
    /// This indicates severe network congestion or processing bottleneck where messages are being queued
    /// faster than they can be sent and acknowledged. When this occurs, the message is dropped to prevent
    /// unbounded memory growth.
    ///
    /// Common causes:
    /// - Sustained high message rate (100+ messages/sec) combined with high packet loss
    /// - Slow ACKs from receiver (high RTT > 250ms)
    /// - SendBuffer full (1024 capacity) AND queue backup
    ///
    /// Solutions:
    /// - Increase maxReliableMessageQueueSize in GONetGlobal (default: 2000)
    /// - Reduce message send rate (batch messages, throttle updates)
    /// - Investigate network conditions (packet loss, latency)
    /// - Check for processing bottlenecks (frame rate drops, GC spikes)
    /// </summary>
    public class ReliableQueueExhaustedException : Exception
    {
        /// <summary>
        /// Current queue depth when exhaustion occurred
        /// </summary>
        public int CurrentQueueDepth { get; }

        /// <summary>
        /// Maximum queue size limit
        /// </summary>
        public int MaxQueueSize { get; }

        /// <summary>
        /// Size of the message that was dropped (in bytes)
        /// </summary>
        public int DroppedMessageSize { get; }

        /// <summary>
        /// Channel ID (0 = Reliable, 2 = Unreliable, etc.)
        /// </summary>
        public int ChannelId { get; }

        public ReliableQueueExhaustedException(
            int currentQueueDepth,
            int maxQueueSize,
            int droppedMessageSize,
            int channelId)
            : base($"Reliable message queue exhausted: {currentQueueDepth}/{maxQueueSize} messages. " +
                   $"Dropped message: {droppedMessageSize} bytes on channel {channelId}. " +
                   $"Increase maxReliableMessageQueueSize or reduce message rate.")
        {
            CurrentQueueDepth = currentQueueDepth;
            MaxQueueSize = maxQueueSize;
            DroppedMessageSize = droppedMessageSize;
            ChannelId = channelId;
        }
    }
}
