/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace GONet.Utils
{
    /// <summary>
    /// Only one of these needed per thread, as it is not thread-safe (otherwise, this would be a static class).
    /// </summary>
    public sealed class BitByBitByteArrayBuilder : IDisposable
    {
        private static readonly ConcurrentDictionary<Thread, BitByBitByteArrayBuilder> builderByThreadMap = new ConcurrentDictionary<Thread, BitByBitByteArrayBuilder>(5, 5);

        public const byte InnerByteBitPosition_MaxValue = 8;
        public const byte InnerByteBitPosition_MinValue = 1;

        public const int STREAM_BUFFER_MAX_SIZE = 32 * 1024;
        private readonly byte[] streamBuffer = new byte[STREAM_BUFFER_MAX_SIZE];

        private byte currentByte;

        /// <summary>
        /// Gets or sets the position inside the byte.
        /// <para/>
        /// <see cref="InnerByteBitPosition_MaxValue"/> is the last position before the next byte.
        /// </summary>
        private byte position_InnerByteBit;
        public byte Position_InnerByteBit => position_InnerByteBit;

        private int length_WrittenBytes;
        public int Length_WrittenBytes => length_WrittenBytes;

        private int position_Bytes;
        public int Position_Bytes => position_Bytes;

        private BitByBitByteArrayBuilder()
        {
            Reset();
        }

        /// <summary>
        /// Since the suggested use is only one of these instances per thread, this method must be called prior to any new usage for a particular purpose.
        /// </summary>
        public BitByBitByteArrayBuilder Reset()
        {
            length_WrittenBytes = 0;
            position_Bytes = 0;
            position_InnerByteBit = InnerByteBitPosition_MaxValue;

            return this;
        }

        /// <summary>
        /// POST: the information inside <paramref name="newData"/> will be copied into internal data structure.
        /// </summary>
        public BitByBitByteArrayBuilder Reset_WithNewData(byte[] newData, int newDataBytesSize)
        {
            if (newData == null || newDataBytesSize < 0 || newData.Length < newDataBytesSize || newDataBytesSize > STREAM_BUFFER_MAX_SIZE)
            {
                throw new ArgumentOutOfRangeException("Just can't do it captain!  newData == null? " + (newData == null) + " newDataBytesSize: " + newDataBytesSize);
            }

            Reset();

            Buffer.BlockCopy(newData, 0, streamBuffer, 0, newDataBytesSize);
            length_WrittenBytes = newDataBytesSize;

            return this;
        }

        /// <summary>
        /// The return value has had <see cref="Reset"/> called on it!
        /// </summary>
        /// <returns></returns>
        public static BitByBitByteArrayBuilder GetBuilder()
        {
            BitByBitByteArrayBuilder builder;

            if (builderByThreadMap.TryGetValue(Thread.CurrentThread, out builder))
            {
                builder.Reset();
            }
            else
            {
                builder = new BitByBitByteArrayBuilder();
                builderByThreadMap[Thread.CurrentThread] = builder;
            }

            return builder;
        }

        /// <summary>
        /// The return value has had <see cref="Reset_WithNewData(byte[], int)"/> called on it (passing in the argument passed herein)!
        /// </summary>
        /// <returns></returns>
        public static BitByBitByteArrayBuilder GetBuilder_WithNewData(byte[] newData, int newDataBytesSize)
        {
            BitByBitByteArrayBuilder builder;

            if (!builderByThreadMap.TryGetValue(Thread.CurrentThread, out builder))
            {
                builder = new BitByBitByteArrayBuilder();
                builderByThreadMap[Thread.CurrentThread] = builder;
            }

            builder.Reset_WithNewData(newData, newDataBytesSize);

            return builder;
        }

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
        public int Read(byte[] buffer, int offset, int count)
        {
            if (position_InnerByteBit == InnerByteBitPosition_MaxValue)
            {
                return Stream_Read(buffer, offset, count);
            }

            return (int)(ReadBits(buffer, offset, (uint)count * InnerByteBitPosition_MaxValue) / InnerByteBitPosition_MaxValue);
        }

        /// <summary>
        /// Reads the given number of bits into the value and returns whether the stream could be read from or not.
        /// </summary>
        /// <param name="value">The value of the read bits.</param>
        /// <param name="bits">The number of bits to read.</param>
        /// <returns>Whether the stream could be read from or not.</returns>
        public bool ReadBits(out byte value, byte bits)
        {
            if (position_InnerByteBit == InnerByteBitPosition_MaxValue && bits == InnerByteBitPosition_MaxValue)
            {
                byte readByte = Stream_ReadByte();
                value = (byte)(readByte < 0 ? 0 : readByte);
                currentByte = value;
                return !(readByte < 0);
            }

            value = 0;
            for (byte i = 1; i <= bits; ++i)
            {
                if (position_InnerByteBit == InnerByteBitPosition_MaxValue)
                {
                    byte readByte = Stream_ReadByte();

                    if (readByte < 0)
                    {
                        return i > 1;
                    }

                    currentByte = readByte;
                }

                //private void AdvanceBitPosition()
                {
                    if (position_InnerByteBit == InnerByteBitPosition_MaxValue)
                    {
                        position_InnerByteBit = InnerByteBitPosition_MinValue;
                    }
                    else
                    { // InnerByteBitPosition_From(position_InnerByteBit + 1):
                        int tmpNewPos = position_InnerByteBit + 1;
                        position_InnerByteBit =
                            tmpNewPos < InnerByteBitPosition_MinValue
                                ? InnerByteBitPosition_MinValue
                                : (tmpNewPos > InnerByteBitPosition_MaxValue
                                    ? InnerByteBitPosition_MaxValue
                                    : (byte)tmpNewPos);
                    }
                }

                { // InnerByteBitPosition_From(i):
                    int tmpNewPos = i;
                    var targetPos =
                        tmpNewPos < InnerByteBitPosition_MinValue
                            ? InnerByteBitPosition_MinValue
                            : (tmpNewPos > InnerByteBitPosition_MaxValue
                                ? InnerByteBitPosition_MaxValue
                                : (byte)tmpNewPos);

                    //private static byte GetAdjustedValue(byte v1, byte cp2, byte tp3)
                    //{
                    byte v1 = currentByte, cp2 = position_InnerByteBit, tp3 = targetPos;
                    v1 &= (byte)(1 << (cp2 - 1));

                    byte adjustedValue = cp2 > tp3
                        ? (byte)(v1 >> (cp2 - tp3))
                        : (cp2 < tp3
                            ? (byte)(v1 << (tp3 - cp2))
                            : v1);
                    //}

                    value |= adjustedValue; // GetAdjustedValue(currentByte, position_InnerByteBit, targetPos);

                }
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
            ulong bitsRead = 0uL;
            while (count > 0)
            {
                byte nextByte;
                byte bits = 0;
                { // bits = InnerByteBitPosition_From(count):
                    ulong tmpNewPos = count;
                    bits =
                        tmpNewPos < InnerByteBitPosition_MinValue
                            ? InnerByteBitPosition_MinValue
                            : (tmpNewPos > InnerByteBitPosition_MaxValue
                                ? InnerByteBitPosition_MaxValue
                                : (byte)tmpNewPos);
                }

                if (!ReadBits(out nextByte, bits))
                {
                    buffer[offset] = 0;
                }
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
        readonly static byte oneBit = 1;

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
        public int ReadByte()
        {
            byte buffer;
            return ReadBits(out buffer, InnerByteBitPosition_MaxValue) ? buffer : -1;
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
        public void Write(byte[] buffer, int offset, int count)
        {
            if (position_InnerByteBit == InnerByteBitPosition_MaxValue)
            {
                Stream_Write(buffer, offset, count);
                currentByte = 0;
            }

            WriteBits(buffer, offset, (ulong)count * InnerByteBitPosition_MaxValue);
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
                byte bits = 0;
                { // bits = InnerByteBitPosition_From(count):
                    ulong tmpNewPos = count;
                    bits =
                        tmpNewPos < InnerByteBitPosition_MinValue
                            ? InnerByteBitPosition_MinValue
                            : (tmpNewPos > InnerByteBitPosition_MaxValue
                                ? InnerByteBitPosition_MaxValue
                                : (byte)tmpNewPos);
                }

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

            byte[] bytes = BitConverter.BorrowByteArray(); // use pool to avoid allocating byte[] if using System.BitConverter

            BitConverter.GetBytes(iUShort, bytes);
            WriteBits(bytes, 0, bitCount);

            BitConverter.ReturnByteArray(bytes);
        }

        public void WriteFloat(float iFloat, ulong bitCount = 32)
        {
            Debug.Assert(bitCount <= 32);

            byte[] bytes = BitConverter.BorrowByteArray(); // use pool to avoid allocating byte[] if using System.BitConverter

            BitConverter.GetBytes(iFloat, bytes);
            WriteBits(bytes, 0, bitCount);

            BitConverter.ReturnByteArray(bytes);
        }

        public void WriteUInt(uint iUInt, ulong bitCount = 32)
        {
            Debug.Assert(bitCount <= 32);

            byte[] bytes = BitConverter.BorrowByteArray(); // use pool to avoid allocating byte[] if using System.BitConverter

            BitConverter.GetBytes(iUInt, bytes);
            WriteBits(bytes, 0, bitCount);

            BitConverter.ReturnByteArray(bytes);
        }

        public void WriteLong(long iLong, ulong bitCount = 64)
        {
            Debug.Assert(bitCount <= 64);

            byte[] bytes = BitConverter.BorrowByteArray(); // use pool to avoid allocating byte[] if using System.BitConverter

            BitConverter.GetBytes(iLong, bytes);
            WriteBits(bytes, 0, bitCount);

            BitConverter.ReturnByteArray(bytes);
        }


        /// <summary>
        /// Writes the given number of bits from the value.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void WriteBits(byte value, byte bits)
        {
            if (position_InnerByteBit == InnerByteBitPosition_MaxValue && bits == InnerByteBitPosition_MaxValue)
            {
                Stream_WriteByte(value);
                currentByte = 0;
                return;
            }

            for (byte i = 1; i <= bits; ++i)
            {
                //private void AdvanceBitPosition()
                {
                    if (position_InnerByteBit == InnerByteBitPosition_MaxValue)
                    {
                        position_InnerByteBit = InnerByteBitPosition_MinValue;
                    }
                    else
                    { // InnerByteBitPosition_From(position_InnerByteBit + 1):
                        int tmpNewPos = position_InnerByteBit + 1;
                        position_InnerByteBit =
                            tmpNewPos < InnerByteBitPosition_MinValue
                                ? InnerByteBitPosition_MinValue
                                : (tmpNewPos > InnerByteBitPosition_MaxValue
                                    ? InnerByteBitPosition_MaxValue
                                    : (byte)tmpNewPos);
                    }
                }

                { // InnerByteBitPosition_From(i):
                    int tmpNewPos = i;
                    var currentPos =
                        tmpNewPos < InnerByteBitPosition_MinValue
                            ? InnerByteBitPosition_MinValue
                            : (tmpNewPos > InnerByteBitPosition_MaxValue
                                ? InnerByteBitPosition_MaxValue
                                : (byte)tmpNewPos);


                    //private static byte GetAdjustedValue(byte v1, byte cp2, byte tp3)
                    //{
                    byte v1 = value, cp2 = currentPos, tp3 = position_InnerByteBit;
                    v1 &= (byte)(1 << (cp2 - 1));

                    byte adjustedValue = cp2 > tp3
                        ? (byte)(v1 >> (cp2 - tp3))
                        : (cp2 < tp3 
                            ? (byte)(v1 << (tp3 - cp2))
                            : v1);
                    //}

                    currentByte |= adjustedValue; // GetAdjustedValue(value, currentPos, position_InnerByteBit);
                }

                if (position_InnerByteBit == InnerByteBitPosition_MaxValue)
                {
                    Stream_WriteByte(currentByte);
                    currentByte = 0;
                }
            }
        }

        /// <summary>
        /// Writes the value.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteByte(byte value)
        {
            WriteBits(value, InnerByteBitPosition_MaxValue);
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
        /// true if <see cref="position_InnerByteBit"/> was greater than 0 and the bits and right padding 0's were written as the final byte to the Stream_
        /// false if there was nothing additional to write to stream
        /// </returns>
        public bool WriteCurrentPartialByte(bool paddingBit = false)
        {
            if (position_InnerByteBit == 0)
            {
                return false;
            }

            int paddingBitCount = 8 - position_InnerByteBit;
            for (int i = 0; i < paddingBitCount; ++i)
            {
                WriteBit(paddingBit);
            }

            return true;
        }

        #endregion Write Methods

        #region Stream replacements

        private void Stream_WriteByte(byte value)
        {
            if (length_WrittenBytes == STREAM_BUFFER_MAX_SIZE)
            {
                throw new InvalidOperationException("not enough memory available.  entire buffer already used.  buffer size (bytes): " + STREAM_BUFFER_MAX_SIZE);
            }

            streamBuffer[length_WrittenBytes++] = value;
            ++position_Bytes;
        }

        private void Stream_Write(byte[] buffer, int offset, int count)
        {
            if (length_WrittenBytes >= (STREAM_BUFFER_MAX_SIZE - count))
            {
                throw new InvalidOperationException("not enough memory available.  buffer size (bytes): " + STREAM_BUFFER_MAX_SIZE);
            }

            // TODO arg error check for null and index blah!

            for (int i = 0; i < count; ++i)
            {
                streamBuffer[length_WrittenBytes++] = buffer[i + offset];
            }
            position_Bytes += count;
        }

        private int Stream_Read(byte[] buffer, int offset, int count)
        {
            if (position_Bytes <= (STREAM_BUFFER_MAX_SIZE - count))
            {
                throw new InvalidOperationException("not enough memory available.  buffer size (bytes): " + STREAM_BUFFER_MAX_SIZE + " position: " + position_Bytes);
            }

            // TODO arg error check for null and capacity to fit blah!

            Buffer.BlockCopy(streamBuffer, position_Bytes, buffer, 0, count);
            position_Bytes += count;

            return count;
        }

        private byte Stream_ReadByte()
        {
            return streamBuffer[position_Bytes++];
        }

        #endregion

        /// <summary>
        /// This does NOT dispose in the traditional C# <see cref="IDisposable.Dispose"/> sense.
        /// This is here so C# using statement can be used to auto-reset at end of using scope!
        /// </summary>
        public void Dispose()
        {
            Reset();
        }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to NOT modify this (essentially) private data.
        ///            This method is here for you to get access to the data and make immediate read-only use of it (e.g., COPY it into another byte[]) and forget about it.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBuffer()
        {
            return streamBuffer;
        }
    }
}