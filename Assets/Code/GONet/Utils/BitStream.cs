using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace GONet.Utils
{
    /// <summary>
    /// Wrapper for <see cref="Stream"/>s that allows bit-level reads and writes.
    /// IMPORTANT: Does NOT call <see cref="Dispose(bool)"/> on the underlying <see cref="MemoryStream"/>.
    /// </summary>
    public sealed class BitStream : Stream
    {
        private readonly Stream stream;

        private byte currentByte;

        /// <summary>
        /// Gets or sets the position inside the byte.
        /// <para/>
        /// <see cref="BitNum.MaxValue"/> is the last position before the next byte.
        /// </summary>
        public byte BitPosition;

        #region Proxy Properties

        public override bool CanRead
        {
            get { return stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return stream.CanSeek; }
        }

        public override bool CanTimeout
        {
            get { return stream.CanTimeout; }
        }

        public override bool CanWrite
        {
            get { return stream.CanWrite; }
        }

        public override long Length
        {
            get { return stream.Length; }
        }

        public override long Position
        {
            get { return stream.Position; }
            set { stream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return stream.ReadTimeout; }
            set { stream.ReadTimeout = value; }
        }

        public Stream UnderlayingStream
        {
            get { return stream; }
        }

        public override int WriteTimeout
        {
            get { return stream.WriteTimeout; }
            set { stream.WriteTimeout = value; }
        }

        #endregion Proxy Properties

        /// <summary>
        /// Creates a new instance of the <see cref="BitStream"/> class with the given underlaying stream.
        /// </summary>
        /// <param name="underlayingStream">The underlaying stream to work on.</param>
        public BitStream(Stream underlayingStream)
        {
            BitPosition = BitNum.MaxValue;
            stream = underlayingStream;
        }

        #region Proxy Methods

        public override bool Equals(object obj)
        {
            return stream.Equals(obj);
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int GetHashCode()
        {
            return stream.GetHashCode();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override string ToString()
        {
            return stream.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            // Do nothing to the stream member, that responsibility is on the caller!
        }

        #endregion Proxy Methods

        #region Read Methods

        /// <summary>
        /// Reads the given number of bytes into the buffer, starting at the given offset and returns how many bytes were read.
        /// <para/>
        /// Any bytes that could not be read will be set to 0.
        /// </summary>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The offset to start writing into the buffer at.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>How many bytes were actually read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (BitPosition == BitNum.MaxValue)
                return stream.Read(buffer, offset, count);

            return (int)(ReadBits(buffer, offset, (uint)count * BitNum.MaxValue) / BitNum.MaxValue);
        }

        /// <summary>
        /// Reads the given number of bits into the value and returns whether the stream could be read from or not.
        /// </summary>
        /// <param name="value">The value of the read bits.</param>
        /// <param name="bits">The number of bits to read.</param>
        /// <returns>Whether the stream could be read from or not.</returns>
        public bool ReadBits(out byte value, byte bits)
        {
            if (BitPosition == BitNum.MaxValue && bits == BitNum.MaxValue)
            {
                var readByte = stream.ReadByte();
                value = (byte)(readByte < 0 ? 0 : readByte);
                currentByte = value;
                return !(readByte < 0);
            }

            value = 0;
            for (byte i = 1; i <= bits; ++i)
            {
                if (BitPosition == BitNum.MaxValue)
                {
                    var readByte = stream.ReadByte();

                    if (readByte < 0)
                        return i > 1;

                    currentByte = (byte)readByte;
                }

                AdvanceBitPosition();
                value |= GetAdjustedValue(currentByte, BitPosition, BitNum.From(i));
            }

            return true;
        }

        /// <summary>
        /// Reads the given number of bits into the buffer, starting at the given offset and returns how many bits were read.
        /// <para/>
        /// Any bytes that could not be read will be set to 0.
        /// </summary>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The offset to start writing into the buffer at.</param>
        /// <param name="count">The number of bits to read.</param>
        /// <returns>How many bits were actually read.</returns>
        public ulong ReadBits(byte[] buffer, int offset, ulong count)
        {
            var bitsRead = 0uL;
            while (count > 0)
            {
                byte nextByte;
                byte bits = BitNum.From(count);

                if (!ReadBits(out nextByte, bits))
                    buffer[offset] = 0;
                else
                {
                    buffer[offset] = nextByte;
                    bitsRead += bits;
                }

                ++offset;
                count -= bits;
            }

            return bitsRead;
        }

        readonly byte[] tmpBuffer64 = new byte[8];
        readonly byte trueByte = 1;
        readonly byte falseByte = 0;
        readonly static byte oneBit = BitNum.From(1);

        public bool ReadBit(out bool oBool)
        {
            byte oByte;
            bool didRead = ReadBits(out oByte, oneBit);
            oBool = oByte == trueByte;
            return didRead;
        }

        public ulong ReadUShort(out ushort oUShort, ulong bitCount = 16)
        {
            Debug.Assert(bitCount <= 16);

            ulong bitsRead = ReadBits(GetClearedTmpBuffer64(), 0, bitCount);

            oUShort = System.BitConverter.ToUInt16(tmpBuffer64, 0);

            return bitsRead;
        }

        /* not tested well enough to know if it leaves things in appropriate state like nothing happened...in fact some test results indicate calling this leaves things effed up....so, if we want peek...refactor and TEST TEST TEST!
        /// <summary>
        /// POST: Does NOT advance position.
        /// </summary>
        /// <param name="oChar"></param>
        /// <returns>false if not enough data to read</returns>
        public bool PeekChar(out char oChar, ulong bitCount = 16)
        {
            Debug.Assert(bitCount <= 16);

            BitNum previousBitPosition = BitPosition;
            long previousPosition = Position;

            ulong bitsRead = ReadBits(GetClearedTmpBuffer64(), 0, bitCount);

            oChar = BitConverter.ToChar(tmpBuffer64, 0);

            {  // reset
                BitPosition = previousBitPosition;
                Position = previousPosition;
            }

            return bitsRead == bitCount;
        }
        */

        public ulong ReadFloat(out float oFloat, ulong bitCount = 32)
        {
            Debug.Assert(bitCount <= 32);

            ulong bitsRead = ReadBits(GetClearedTmpBuffer64(), 0, bitCount);

            oFloat = System.BitConverter.ToSingle(tmpBuffer64, 0);

            return bitsRead;
        }

        private byte[] GetClearedTmpBuffer64()
        {
            Array.Clear(tmpBuffer64, 0, 8);
            return tmpBuffer64;
        }
        
        public ulong ReadUInt(out uint oUInt, ulong bitCount = 32)
        {
            Debug.Assert(bitCount <= 32);

            ulong bitsRead = ReadBits(GetClearedTmpBuffer64(), 0, bitCount);

            oUInt = System.BitConverter.ToUInt32(tmpBuffer64, 0);

            return bitsRead;
        }

        public ulong ReadLong(out long oLong, ulong bitCount = 64)
        {
            Debug.Assert(bitCount <= 64);

            ulong bitsRead = ReadBits(GetClearedTmpBuffer64(), 0, bitCount);

            oLong = System.BitConverter.ToInt64(tmpBuffer64, 0);

            return bitsRead;
        }

        /// <summary>
        /// Reads a single byte from the stream and returns its value, or -1 if it could not be read.
        /// </summary>
        /// <returns>The value that was read, or -1 if it could not be read.</returns>
        public override int ReadByte()
        {
            byte buffer;
            return ReadBits(out buffer, BitNum.MaxValue) ? buffer : -1;
        }

        static readonly ArrayPool<byte> byteArrayPoolForStrings = new ArrayPool<byte>(100, 1, 100, 1000);

        public void ReadString(out string value)
        {
            uint byteCount;
            ReadUInt(out byteCount);
            int iByteCount = (int)byteCount;
            byte[] bytes = byteArrayPoolForStrings.Borrow(iByteCount);
            for (uint i = 0; i < byteCount; ++i)
            {
                byte b = (byte)ReadByte();
                bytes[i] = b;
            }
            value = Encoding.UTF8.GetString(bytes, 0, iByteCount); // TODO : PERF: need to cache/inntern these beasts!
            byteArrayPoolForStrings.Return(bytes);
        }

        #endregion Read Methods

        #region Write Methods

        /// <summary>
        /// Writes the given number of bytes from the buffer, starting at the given offset.
        /// </summary>
        /// <param name="buffer">The buffer to write from.</param>
        /// <param name="offset">The offet to start reading from at.</param>
        /// <param name="count">The number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (BitPosition == BitNum.MaxValue)
            {
                stream.Write(buffer, offset, count);
                currentByte = 0;
            }

            WriteBits(buffer, offset, (ulong)count * BitNum.MaxValue);
        }

        /// <summary>
        /// Writes the given number of bits from the buffer, starting at the given offset.
        /// </summary>
        /// <param name="buffer">The buffer to write from.</param>
        /// <param name="offset">Te offset to start reading from at.</param>
        /// <param name="count">The number of bits to write.</param>
        public void WriteBits(byte[] buffer, int offset, ulong count)
        {
            while (count > 0)
            {
                byte bits = BitNum.From(count);

                WriteBits(buffer[offset], bits);

                ++offset;
                count -= bits;
            }
        }

        public void WriteBit(bool bit)
        {
            WriteBits(bit ? trueByte : falseByte, oneBit);
        }

        public void WriteUShort(ushort iUShort, ulong bitCount = 16)
        {
            Debug.Assert(bitCount <= 16);

            byte[] bytes = BitConverter.ByteArrayPool.Borrow(); // use pool to avoid allocating byte[] if using System.BitConverter

            BitConverter.GetBytes(iUShort, bytes);
            WriteBits(bytes, 0, bitCount);

            BitConverter.ByteArrayPool.Return(bytes);
        }

        public void WriteFloat(float iFloat, ulong bitCount = 32)
        {
            Debug.Assert(bitCount <= 32);

            byte[] bytes = BitConverter.ByteArrayPool.Borrow(); // use pool to avoid allocating byte[] if using System.BitConverter

            BitConverter.GetBytes(iFloat, bytes);
            WriteBits(bytes, 0, bitCount);

            BitConverter.ByteArrayPool.Return(bytes);
        }

        public void WriteUInt(uint iUInt, ulong bitCount = 32)
        {
            Debug.Assert(bitCount <= 32);

            byte[] bytes = BitConverter.ByteArrayPool.Borrow(); // use pool to avoid allocating byte[] if using System.BitConverter

            BitConverter.GetBytes(iUInt, bytes);
            WriteBits(bytes, 0, bitCount);

            BitConverter.ByteArrayPool.Return(bytes);
        }

        public void WriteLong(long iLong, ulong bitCount = 64)
        {
            Debug.Assert(bitCount <= 64);

            byte[] bytes = BitConverter.ByteArrayPool.Borrow(); // use pool to avoid allocating byte[] if using System.BitConverter

            BitConverter.GetBytes(iLong, bytes);
            WriteBits(bytes, 0, bitCount);

            BitConverter.ByteArrayPool.Return(bytes);
        }


        /// <summary>
        /// Writes the given number of bits from the value.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void WriteBits(byte value, byte bits)
        {
            if (BitPosition == BitNum.MaxValue && bits == BitNum.MaxValue)
            {
                stream.WriteByte(value);
                currentByte = 0;
                return;
            }

            for (byte i = 1; i <= bits; ++i)
            {
                AdvanceBitPosition();
                currentByte |= GetAdjustedValue(value, BitNum.From(i), BitPosition);

                if (BitPosition == BitNum.MaxValue)
                {
                    stream.WriteByte(currentByte);
                    currentByte = 0;
                }
            }
        }

        /// <summary>
        /// Writes the value.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public override void WriteByte(byte value)
        {
            WriteBits(value, BitNum.MaxValue);
        }

        public void WriteString(string value)
        {
            int iByteCount = Encoding.UTF8.GetByteCount(value);
            uint byteCount = (uint)iByteCount;
            if (byteCount > uint.MaxValue)
            {
                throw new Exception("not bueno..value as bytes > uint.MaxValue");
            }
            WriteUInt(byteCount);

            byte[] bytes = byteArrayPoolForStrings.Borrow((int)byteCount);
            Encoding.UTF8.GetBytes(value, 0, value.Length, bytes, 0);
            for (int i = 0; i < iByteCount; ++i)
            {
                WriteByte(bytes[i]);
            }
            byteArrayPoolForStrings.Return(bytes);
        }

        /// <summary>
        /// To ensure all bits are written to the stream prior to calling <see cref="MemoryStream.ToArray"/> (well, if it the <see cref="UnderlayingStream"/> is a <see cref="MemoryStream"/>).
        /// </summary>
        /// <returns>
        /// true if <see cref="BitPosition"/> was greater than 0 and the bits and right padding 0's were written as the final byte to the stream.
        /// false if there was nothing additional to write to stream
        /// </returns>
        public bool WriteCurrentPartialByte(bool paddingBit = false)
        {
            if (BitPosition == 0)
            {
                return false;
            }

            int paddingBitCount = 8 - BitPosition;
            for (int i = 0; i < paddingBitCount; ++i)
            {
                WriteBit(paddingBit);
            }

            return true;
        }

        #endregion Write Methods

        private static byte GetAdjustedValue(byte value, byte currentPosition, byte targetPosition)
        {
            value &= BitNum.GetBitPos(currentPosition);

            if (currentPosition > targetPosition)
                return (byte)(value >> (currentPosition - targetPosition));
            else if (currentPosition < targetPosition)
                return (byte)(value << (targetPosition - currentPosition));
            else
                return value;
        }

        private void AdvanceBitPosition()
        {
            if (BitPosition == BitNum.MaxValue)
                BitPosition = BitNum.MinValue;
            else
                BitPosition = BitNum.From(BitPosition + 1);
        }
    }

    public static class BitNum
    {
        public static readonly byte MaxValue = 8;
        public static readonly byte MinValue = 1;

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(byte value)
        {
            return Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(sbyte value)
        {
            return (byte)Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(ushort value)
        {
            return (byte)Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(short value)
        {
            return (byte)Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(uint value)
        {
            return (byte)Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(int value)
        {
            return (byte)Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(ulong value)
        {
            return (byte)Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(long value)
        {
            return (byte)Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(float value)
        {
            return (byte)Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(double value)
        {
            return (byte)Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitNum"/> struct with the given value.
        /// <para/>
        /// Value will be truncated to the MaxValue if it's larger or rised to the MinValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte From(decimal value)
        {
            return (byte)Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetBitPos(byte value)
        {
            return (byte)(1 << (value - 1));
        }
    }
}