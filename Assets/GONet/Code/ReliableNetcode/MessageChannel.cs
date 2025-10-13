using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableNetcode.Utils;

namespace ReliableNetcode
{
    internal abstract class MessageChannel
    {
        protected ReliablePacketController packetController;

        public abstract int ChannelID { get; }

        public Action<byte[], int> TransmitCallback;
        public Action<byte[], int> ReceiveCallback;

        public abstract void Reset();
        public abstract void Update(double newTime);
        public abstract void ReceivePacket(byte[] buffer, int bufferLength);
        public abstract void SendMessage(byte[] buffer, int bufferLength);

        public virtual string GetUsageStatistics()
        {
            return packetController == null ? string.Empty : packetController.GetUsageStatistics();
        }

        public virtual void ProcessSendBuffer_IfAppropriate()
        {
        }
    }

    /// <summary>
    /// an unreliable implementation of <see cref="MessageChannel"/>
    /// does not make any guarantees about message reliability except for ignoring duplicate messages
    /// </summary>
    internal class UnreliableMessageChannel : MessageChannel
    {
        public override int ChannelID
        {
            get
            {
                return (int)QosType.Unreliable;
            }
        }

        private ReliableConfig config;
        private SequenceBuffer<ReceivedPacketData> receiveBuffer;

        public UnreliableMessageChannel()
        {
            receiveBuffer = new SequenceBuffer<ReceivedPacketData>(256);

            config = ReliableConfig.DefaultConfig();
            config.TransmitPacketCallback = (buffer, size) => {
                TransmitCallback(buffer, size);
            };
            config.ProcessPacketCallback = (seq, buffer, size) => {
                if (!receiveBuffer.Exists(seq)) {
                    receiveBuffer.Insert(seq);
                    ReceiveCallback(buffer, size);
                }
            };

            packetController = new ReliablePacketController(config, DateTime.UtcNow.GetTotalSeconds());
        }

        public override void Reset()
        {
            packetController.Reset();
        }

        public override void Update(double newTimeSeconds)
        {
            packetController.Update(newTimeSeconds);
        }

        public override void ReceivePacket(byte[] buffer, int bufferLength)
        {
            packetController.ReceivePacket(buffer, bufferLength);
        }

        public override void SendMessage(byte[] buffer, int bufferLength)
        {
            packetController.SendPacket(buffer, bufferLength, (byte)ChannelID);
        }
    }

    /// <summary>
    /// a reliable ordered implementation of <see cref="MessageChannel"/>
    /// </summary>
    internal class ReliableMessageChannel : MessageChannel
    {
        internal class BufferedPacket
        {
            public bool writeLock = true;
            public double time;
            public ByteBuffer buffer = new ByteBuffer();
        }

        internal class OutgoingPacketSet
        {
            public List<ushort> MessageIds = new List<ushort>();
        }

        public override int ChannelID
        {
            get
            {
                return (int)QosType.Reliable;
            }
        }

        public float RTTMilliseconds => packetController.RTTMilliseconds;

        public float PacketLoss => packetController.PacketLoss;

        public float SentBandwidthKBPS => packetController.SentBandwidthKBPS;

        public float ReceivedBandwidthKBPS => packetController.ReceivedBandwidthKBPS;

        private ReliableConfig config;
        private bool congestionControl = false;
        private double congestionDisableTimer;
        private double congestionDisableInterval;
        private double lastCongestionSwitchTime;

        private ByteBuffer messagePacker = new ByteBuffer();
        private SequenceBuffer<BufferedPacket> sendBuffer;
        private SequenceBuffer<BufferedPacket> receiveBuffer;
        private SequenceBuffer<OutgoingPacketSet> ackBuffer;

        private Queue<ByteBuffer> messageQueue = new Queue<ByteBuffer>();

        private double lastBufferFlush;
        private double lastMessageSend;
        private double timeSeconds;

        private volatile ushort oldestUnacked;
        private volatile ushort sequence;
        private volatile ushort nextReceive;
        private volatile bool isTimeToProcessSendBuffer;

        // PHASE 1D: MessageQueue depth tracking for diagnostics
        private double lastQueueDepthLogTime = 0.0;
        private int lastLoggedQueueDepth = 0;

        // Configurable maximum queue size (prevents unbounded memory growth)
        private readonly int maxMessageQueueSize;

        public ReliableMessageChannel(int maxQueueSize = 2000)
        {
            maxMessageQueueSize = maxQueueSize;
            config = ReliableConfig.DefaultConfig();
            config.TransmitPacketCallback = (buffer, size) => {
                TransmitCallback(buffer, size);
            };
            config.ProcessPacketCallback = processPacket;
            config.AckPacketCallback = ackPacket;

            // PHASE 1A FIX: Increased capacity from 256 → 1024 to handle realistic production burst scenarios
            // Previous capacity (256) was insufficient for medium/high latency scenarios (100-250ms RTT)
            // New capacity (1024) handles burst up to 1000 messages even at high latency
            // Memory cost: ~3.5MB per connection (acceptable trade-off for reliability)
            // See: INVESTIGATION_BATCH_MESSAGE_LOSS_2025-10-12.md Section 10.7
            sendBuffer = new SequenceBuffer<BufferedPacket>(1024);
            receiveBuffer = new SequenceBuffer<BufferedPacket>(1024);
            ackBuffer = new SequenceBuffer<OutgoingPacketSet>(1024);

            timeSeconds = DateTime.UtcNow.GetTotalSeconds();
            lastBufferFlush = -1.0;
            lastMessageSend = 0.0;
            this.packetController = new ReliablePacketController(config, timeSeconds);

            this.congestionDisableInterval = 5.0;

            this.sequence = 0;
            this.nextReceive = 0;
            this.oldestUnacked = 0;
        }

        public override void Reset()
        {
            this.packetController.Reset();
            this.sendBuffer.Reset();
            this.ackBuffer.Reset();

            this.lastBufferFlush = -1.0;
            this.lastMessageSend = 0.0;

            this.congestionControl = false;
            this.lastCongestionSwitchTime = 0.0;
            this.congestionDisableTimer = 0.0;
            this.congestionDisableInterval = 5.0;

            this.sequence = 0;
            this.nextReceive = 0;
            this.oldestUnacked = 0;
        }

        public override void Update(double newTimeSeconds)
        {
            double dt = newTimeSeconds - timeSeconds;
            timeSeconds = newTimeSeconds;
            this.packetController.Update(timeSeconds);

            // see if we can pop messages off of the message queue and put them on the send queue
            if (messageQueue.Count > 0) {
                // Count send buffer size ONCE before dequeue loop (optimization)
                int sendBufferSize = 0;
                for (ushort seq = oldestUnacked; PacketIO.SequenceLessThan(seq, this.sequence); seq++) {
                    if (sendBuffer.Exists(seq))
                        sendBufferSize++;
                }

                // Dequeue multiple messages per update to prevent channel starvation
                // When many messages flood one reliable channel (e.g., spawn burst), this ensures
                // other reliable messages (e.g., position updates) aren't delayed excessively
                const int MAX_DEQUEUE_PER_UPDATE = 100;  // Process up to 100 messages per update
                const double MAX_DEQUEUE_TIME_MS = 0.5;  // Stop after 0.5ms to protect frame time

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int dequeuedCount = 0;

                while (messageQueue.Count > 0 &&
                       sendBufferSize < sendBuffer.Size &&
                       dequeuedCount < MAX_DEQUEUE_PER_UPDATE &&
                       stopwatch.Elapsed.TotalMilliseconds < MAX_DEQUEUE_TIME_MS)
                {
                    var message = messageQueue.Dequeue();
                    SendMessage(message.InternalBuffer, message.Length);
                    ObjPool<ByteBuffer>.Return(message);

                    sendBufferSize++;  // Track locally (safe: only this thread dequeues in this loop)
                    dequeuedCount++;
                }
            }

            // update congestion mode
            {
                // conditions are bad if round-trip-time exceeds 250ms
                bool conditionsBad = (this.packetController.RTTMilliseconds >= 250f);

                // if conditions are bad, immediately enable congestion control and reset the congestion timer
                if (conditionsBad) {
                    if (this.congestionControl == false) {
                        // if we're within 10 seconds of the last time we switched, double the threshold interval
                        if (timeSeconds - lastCongestionSwitchTime < 10.0)
                        {
                            double times2 = congestionDisableInterval * 2;
                            congestionDisableInterval = (times2 < 60.0) ? times2 : 60.0; // Math.Min(congestionDisableInterval * 2, 60.0);
                        }

                        lastCongestionSwitchTime = timeSeconds;
                    }

                    this.congestionControl = true;
                    this.congestionDisableTimer = 0.0;
                }

                // if we're in bad mode, and conditions are good, update the timer and see if we can disable congestion control
                if (this.congestionControl && !conditionsBad) {
                    this.congestionDisableTimer += dt;
                    if (this.congestionDisableTimer >= this.congestionDisableInterval) {
                        this.congestionControl = false;
                        lastCongestionSwitchTime = timeSeconds;
                        congestionDisableTimer = 0.0;
                    }
                }

                // as long as conditions are good, halve the threshold interval every 10 seconds
                if (this.congestionControl == false) {
                    congestionDisableTimer += dt;
                    if (congestionDisableTimer >= 10.0)
                    {
                        double half = congestionDisableInterval * 0.5;
                        congestionDisableInterval = (half > 5.0) ? half : 5.0; //  Math.Max(congestionDisableInterval * 0.5, 5.0);
                    }
                }
            }

            const double CONGESTED_SEND_RATE_HZ = 1.0 / 10.0;
            const double NORMAL_SEND_RATE_HZ = 1.0 / 90.0; // GONet changed from original value of 0.033
            double flushInterval = congestionControl ? CONGESTED_SEND_RATE_HZ : NORMAL_SEND_RATE_HZ;

            if (timeSeconds - lastBufferFlush >= flushInterval) {
                isTimeToProcessSendBuffer = true;
            }

            // PHASE 1D FIX: MessageQueue depth logging for production diagnostics
            // Log queue depth at different severity levels based on thresholds
            // This provides visibility into transport congestion during operation
            // See: INVESTIGATION_BATCH_MESSAGE_LOSS_2025-10-12.md Section 10.6
            int currentQueueDepth = messageQueue.Count;
            if (currentQueueDepth > 0)
            {
                // Log every 1 second at INFO level if queue is building (>50 messages)
                // Log every 0.5 seconds at WARNING level if queue is high (>200 messages)
                // Log every 0.1 seconds at CRITICAL level if queue is near limit (>400 messages)
                double logInterval = 1.0;  // Default: INFO level, 1 second
                string severity = "INFO";

                if (currentQueueDepth > 400) {
                    logInterval = 0.1;
                    severity = "CRITICAL";
                } else if (currentQueueDepth > 200) {
                    logInterval = 0.5;
                    severity = "WARNING";
                } else if (currentQueueDepth <= 50) {
                    logInterval = 5.0;  // Low queue depth: log every 5 seconds
                }

                bool shouldLog = (timeSeconds - lastQueueDepthLogTime >= logInterval);
                bool queueDepthChanged = (currentQueueDepth != lastLoggedQueueDepth);

                if (shouldLog && queueDepthChanged)
                {
                    // Use System.Diagnostics.Debug for low-level transport logging
                    // This appears in Unity Editor console during development
                    System.Diagnostics.Debug.WriteLine(
                        $"[{severity}] ReliableMessageChannel: messageQueue depth = {currentQueueDepth} " +
                        $"(sendBuffer: {sendBuffer.Size}, RTT: {packetController.RTTMilliseconds:F1}ms, " +
                        $"congestionControl: {congestionControl})");

                    lastQueueDepthLogTime = timeSeconds;
                    lastLoggedQueueDepth = currentQueueDepth;
                }
            }
            else if (lastLoggedQueueDepth > 0)
            {
                // Queue drained - log recovery
                System.Diagnostics.Debug.WriteLine(
                    $"[INFO] ReliableMessageChannel: messageQueue drained (was {lastLoggedQueueDepth}, now 0)");
                lastLoggedQueueDepth = 0;
            }
        }

        public override void ProcessSendBuffer_IfAppropriate()
        {
            if (isTimeToProcessSendBuffer)
            {
                isTimeToProcessSendBuffer = false;
                lastBufferFlush = timeSeconds;
                processSendBuffer();
            }
        }

        public override void ReceivePacket(byte[] buffer, int bufferLength)
        {
            this.packetController.ReceivePacket(buffer, bufferLength);
        }

        public override void SendMessage(byte[] buffer, int bufferLength)
        {
            int sendBufferSize = 0;
            for (ushort seq = oldestUnacked; PacketIO.SequenceLessThan(seq, this.sequence); seq++) {
                if (sendBuffer.Exists(seq))
                    sendBufferSize++;
            }

            if (sendBufferSize == sendBuffer.Size) {
                // PHASE 1B FIX: Bounds checking to prevent unbounded messageQueue growth
                // In extreme edge cases (sustained 100+ spawns/sec + high packet loss), messageQueue could grow without limit
                // This safety valve prevents memory exhaustion by throwing an exception when queue exceeds threshold
                // Threshold: Configurable via maxMessageQueueSize (default 2000 messages = ~2.4MB at 1200 bytes/message average)
                // NOTE: This is EXTREMELY RARE - requires sustained burst + high packet loss + slow ACKs simultaneously
                // See: INVESTIGATION_BATCH_MESSAGE_LOSS_2025-10-12.md Section 8.1

                if (messageQueue.Count >= maxMessageQueueSize)
                {
                    // CRITICAL: Queue exhaustion - throw exception to allow higher-level handling
                    // This indicates severe network degradation (>90% packet loss) or server overload
                    // Exception will be caught at GONet layer for error logging and diagnostics
                    throw new ReliableQueueExhaustedException(
                        currentQueueDepth: messageQueue.Count,
                        maxQueueSize: maxMessageQueueSize,
                        droppedMessageSize: bufferLength,
                        channelId: ChannelID);
                }

                ByteBuffer tempBuff = ObjPool<ByteBuffer>.Get();
                tempBuff.SetSize(bufferLength);
                tempBuff.BufferCopy(buffer, 0, 0, bufferLength);
                messageQueue.Enqueue(tempBuff);

                return;
            }

            ushort sequence = this.sequence++;
            var packet = sendBuffer.Insert(sequence);

            packet.time = -1.0;

            // ensure size for header
            int varLength = getVariableLengthBytes((ushort)bufferLength);
            packet.buffer.SetSize(bufferLength + 2 + varLength);

            using (var writer = ByteArrayReaderWriter.Get(packet.buffer.InternalBuffer)) {
                writer.Write(sequence);

                writeVariableLengthUShort((ushort)bufferLength, writer);
                writer.WriteBuffer(buffer, bufferLength);
            }

            // signal that packet is ready to be sent
            packet.writeLock = false;
        }

        private void sendAckPacket()
        {
            packetController.SendAck((byte)ChannelID);
        }

        private int getVariableLengthBytes(ushort val)
        {
            if (val > 0x7fff) {
                throw new ArgumentOutOfRangeException();
            }

            byte b2 = (byte)(val >> 7);
            return (b2 != 0) ? 2 : 1;
        }

        private void writeVariableLengthUShort(ushort val, ByteArrayReaderWriter writer)
        {
            if (val > 0x7fff) {
                throw new ArgumentOutOfRangeException();
            }

            byte b1 = (byte)(val & 0x007F); // write the lowest 7 bits
            byte b2 = (byte)(val >> 7);     // write remaining 8 bits

            // if there's a second byte to write, set the continue flag
            if (b2 != 0) {
                b1 |= 0x80;
            }

            // write bytes
            writer.Write(b1);
            if (b2 != 0)
                writer.Write(b2);
        }

        private ushort readVariableLengthUShort(ByteArrayReaderWriter reader)
        {
            ushort val = 0;

            byte b1 = reader.ReadByte();
            val |= (ushort)(b1 & 0x7F);

            if ((b1 & 0x80) != 0) {
                val |= (ushort)(reader.ReadByte() << 7);
            }

            return val;
        }

        protected List<ushort> tempList = new List<ushort>();
        protected void processSendBuffer()
        {
            int numUnacked = 0;
            for (ushort seq = oldestUnacked; PacketIO.SequenceLessThan(seq, this.sequence); seq++)
                numUnacked++;

            for (ushort seq = oldestUnacked; PacketIO.SequenceLessThan(seq, this.sequence); seq++) {
                // PHASE 1A FIX (PART 2): Use dynamic sendBuffer.Size instead of hardcoded 256
                // This hardcoded value prevented messages beyond the first 256 from being sent
                // even after increasing buffer capacity to 1024 in constructor
                // never send message ID >= ( oldestUnacked + bufferSize )
                if (seq >= (oldestUnacked + sendBuffer.Size))
                    break;

                // for any message that hasn't been sent in the last 0.1 seconds and fits in the available space of our message packer, add it
                var packet = sendBuffer.Find(seq);
                if (packet != null && !packet.writeLock) {
                    if (timeSeconds - packet.time < 0.1)
                        continue;

                    bool packetFits = false;

                    if (packet.buffer.Length < config.FragmentThreshold)
                        packetFits = (messagePacker.Length + packet.buffer.Length) <= (config.FragmentThreshold - Defines.MAX_PACKET_HEADER_BYTES);
                    else
                        packetFits = (messagePacker.Length + packet.buffer.Length) <= (config.MaxPacketSize - Defines.FRAGMENT_HEADER_BYTES - Defines.MAX_PACKET_HEADER_BYTES);

                    // if the packet won't fit, flush the message packer
                    if (!packetFits) {
                        flushMessagePacker();
                    }

                    packet.time = timeSeconds;

                    int ptr = messagePacker.Length;
                    messagePacker.SetSize(messagePacker.Length + packet.buffer.Length);
                    messagePacker.BufferCopy(packet.buffer, 0, ptr, packet.buffer.Length);

                    tempList.Add(seq);

                    lastMessageSend = timeSeconds;
                }
            }

            // if it has been 0.1 seconds since the last time we sent a message, send an empty message
            if (timeSeconds - lastMessageSend >= 0.1) {
                sendAckPacket();
                lastMessageSend = timeSeconds;
            }

            // flush any remaining messages in message packer
            flushMessagePacker();
        }

        protected void flushMessagePacker(bool bufferAck = true)
        {
            if (messagePacker.Length > 0) {
                ushort outgoingSeq = packetController.SendPacket(messagePacker.InternalBuffer, messagePacker.Length, (byte)ChannelID);
                var outgoingPacket = ackBuffer.Insert(outgoingSeq);

                // store message IDs so we can map packet-level acks to message ID acks
                outgoingPacket.MessageIds.Clear();
                outgoingPacket.MessageIds.AddRange(tempList);

                messagePacker.SetSize(0);
                tempList.Clear();
            }
        }

        protected void ackPacket(ushort seq)
        {
            // first, map seq to message IDs and ack them
            var outgoingPacket = ackBuffer.Find(seq);
            if (outgoingPacket == null)
                return;

            // process messages
            for (int i = 0; i < outgoingPacket.MessageIds.Count; i++) {
                // remove acked message from send buffer
                ushort messageID = outgoingPacket.MessageIds[i];

                if (sendBuffer.Exists(messageID)) {
                    sendBuffer.Find(messageID).writeLock = true;
                    sendBuffer.Remove(messageID);
                }
            }

            // update oldest unacked message
            bool allAcked = true;
            for (ushort sequence = oldestUnacked; sequence == this.sequence || PacketIO.SequenceLessThan(sequence, this.sequence); sequence++) {
                // if it's still in the send buffer, it hasn't been acked
                if (sendBuffer.Exists(sequence)) {
                    oldestUnacked = sequence;
                    allAcked = false;
                    break;
                }
            }

            if (allAcked)
                oldestUnacked = this.sequence;
        }

        // process incoming packets and turn them into messages
        protected void processPacket(ushort seq, byte[] packetData, int packetLen)
        {
            using (var reader = ByteArrayReaderWriter.Get(packetData)) {
                while (reader.ReadPosition < packetLen) {
                    // get message bytes and send to receive callback
                    ushort messageID = reader.ReadUInt16();
                    ushort messageLength = readVariableLengthUShort(reader);

                    if (messageLength == 0)
                        continue;

                    if (!receiveBuffer.Exists(messageID)) {
                        var receivedMessage = receiveBuffer.Insert(messageID);

                        receivedMessage.buffer.SetSize(messageLength);
                        reader.ReadBytesIntoBuffer(receivedMessage.buffer.InternalBuffer, messageLength);
                    }
                    else {
                        reader.SeekRead(reader.ReadPosition + messageLength);
                    }

                    // keep returning the next message we're expecting as long as it's available
                    while (receiveBuffer.Exists(nextReceive)) {
                        var msg = receiveBuffer.Find(nextReceive);

                        ReceiveCallback(msg.buffer.InternalBuffer, msg.buffer.Length);

                        receiveBuffer.Remove(nextReceive);
                        nextReceive++;
                    }
                }
            }
        }

        public override string GetUsageStatistics()
        {
            // PHASE 1C FIX: Added transport telemetry for messageQueue and sendBuffer utilization
            // Previously had no visibility into messageQueue depth (caused Test 9 failure)
            // Now exposes critical metrics for diagnosing congestion and transport health
            // See: INVESTIGATION_BATCH_MESSAGE_LOSS_2025-10-12.md Section 10.3

            // Calculate sendBuffer utilization (how full is it?)
            int sendBufferUtilization = 0;
            for (ushort seq = oldestUnacked; PacketIO.SequenceLessThan(seq, this.sequence); seq++) {
                if (sendBuffer.Exists(seq))
                    sendBufferUtilization++;
            }

            StringBuilder stringBuilder = new StringBuilder(2000);

            const string SB = " sendBuffer.Size: ";
            const string RB = " receiveBuffer.Size: ";
            const string AB = " ackBuffer: ";
            const string LBF = " lastBufferFlush: ";
            const string LMS = " lastMessageSend: ";
            const string TS = " timeSeconds: ";
            const string OU = " oldestUnacked: ";
            const string SEQ = " sequence: ";
            const string NR = " nextReceive: ";
            const string LCST = " lastCongestionSwitchTime: ";
            const string MQ = " messageQueue.Count: ";      // NEW: messageQueue depth
            const string SBU = " sendBufferUtilization: ";   // NEW: sendBuffer occupancy

            stringBuilder
                .Append(base.GetUsageStatistics())
                .Append(SB).Append(sendBuffer.Size)
                .Append(RB).Append(receiveBuffer.Size)
                .Append(AB).Append(ackBuffer.Size)
                .Append(LBF).Append(lastBufferFlush)
                .Append(LMS).Append(lastMessageSend)
                .Append(TS).Append(timeSeconds)
                .Append(OU).Append(oldestUnacked)
                .Append(SEQ).Append(sequence)
                .Append(NR).Append(nextReceive)
                .Append(LCST).Append(lastCongestionSwitchTime)
                .Append(MQ).Append(messageQueue.Count)          // NEW: Queue depth visibility
                .Append(SBU).Append(sendBufferUtilization)      // NEW: Buffer utilization visibility
                ;

            return stringBuilder.ToString();
        }
    }
}