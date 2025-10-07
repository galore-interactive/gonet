using System;

namespace ReliableNetcode
{
    /// <summary>
    /// Telemetry data for monitoring ReliableEndpoint channel behavior
    /// Useful for debugging channel starvation issues (e.g., projectile freezing bug)
    /// </summary>
    public struct ReliableEndpointTelemetry
    {
        /// <summary>
        /// Number of reliable messages currently queued for sending
        /// </summary>
        public int ReliableMessagesQueued;

        /// <summary>
        /// Number of unreliable messages currently queued for sending
        /// </summary>
        public int UnreliableMessagesQueued;

        /// <summary>
        /// Total bytes sent via reliable channel
        /// </summary>
        public long ReliableBytesSent;

        /// <summary>
        /// Total bytes sent via unreliable channel
        /// </summary>
        public long UnreliableBytesSent;

        /// <summary>
        /// Total bytes received via reliable channel
        /// </summary>
        public long ReliableBytesReceived;

        /// <summary>
        /// Total bytes received via unreliable channel
        /// </summary>
        public long UnreliableBytesReceived;

        /// <summary>
        /// Current RTT for reliable channel in milliseconds
        /// </summary>
        public float ReliableRTTMs;

        /// <summary>
        /// Estimated latency for unreliable channel in milliseconds
        /// </summary>
        public float UnreliableEstimatedLatencyMs;

        /// <summary>
        /// Whether congestion control is currently active on reliable channel
        /// </summary>
        public bool IsCongestionControlActive;

        /// <summary>
        /// Number of reliable messages dropped/lost
        /// </summary>
        public int ReliableMessagesDropped;

        /// <summary>
        /// Number of unreliable messages dropped/lost
        /// </summary>
        public int UnreliableMessagesDropped;

        /// <summary>
        /// Time since last reliable message was sent (seconds)
        /// </summary>
        public double TimeSinceLastReliableSend;

        /// <summary>
        /// Time since last unreliable message was sent (seconds)
        /// </summary>
        public double TimeSinceLastUnreliableSend;

        /// <summary>
        /// Packet loss percentage (0.0 - 1.0)
        /// </summary>
        public float PacketLoss;

        /// <summary>
        /// Current send bandwidth in KB/s
        /// </summary>
        public float SentBandwidthKBPS;

        /// <summary>
        /// Current receive bandwidth in KB/s
        /// </summary>
        public float ReceivedBandwidthKBPS;

        public override string ToString()
        {
            return $"[Telemetry] " +
                   $"R_Queued:{ReliableMessagesQueued} U_Queued:{UnreliableMessagesQueued} " +
                   $"R_RTT:{ReliableRTTMs:F1}ms U_Latency:{UnreliableEstimatedLatencyMs:F1}ms " +
                   $"Congestion:{IsCongestionControlActive} " +
                   $"PacketLoss:{PacketLoss * 100:F1}% " +
                   $"SendBW:{SentBandwidthKBPS:F1}KB/s RecvBW:{ReceivedBandwidthKBPS:F1}KB/s";
        }
    }
}
