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

        public ReliableMessageChannel()
        {
            config = ReliableConfig.DefaultConfig();
            config.TransmitPacketCallback = (buffer, size) => {
                TransmitCallback(buffer, size);
            };
            config.ProcessPacketCallback = processPacket;
            config.AckPacketCallback = ackPacket;

            sendBuffer = new SequenceBuffer<BufferedPacket>(256);
            receiveBuffer = new SequenceBuffer<BufferedPacket>(256);
            ackBuffer = new SequenceBuffer<OutgoingPacketSet>(256);

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
                int sendBufferSize = 0;
                for (ushort seq = oldestUnacked; PacketIO.SequenceLessThan(seq, this.sequence); seq++) {
                    if (sendBuffer.Exists(seq))
                        sendBufferSize++;
                }

                if (sendBufferSize < sendBuffer.Size) {
                    var message = messageQueue.Dequeue();
                    SendMessage(message.InternalBuffer, message.Length);
                    ObjPool<ByteBuffer>.Return(message);
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
                // never send message ID >= ( oldestUnacked + bufferSize )
                if (seq >= (oldestUnacked + 256))
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
                ; 

            return stringBuilder.ToString();
        }
    }
}