/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
        public int Length_WrittenBytes => isUsingBitWriterReader ? bitWriter.BitsWritten / 8 : length_WrittenBytes;

        private int position_Bytes;
        public int Position_Bytes => isUsingBitWriterReader ? bitReader.BitsRead / 8 : position_Bytes;
        public int Position_Bits => isUsingBitWriterReader ? bitReader.BitsRead : position_InnerByteBit == 0 ? (position_Bytes * 8) : ((position_Bytes - 1) * 8) + position_InnerByteBit;


        BitWriter bitWriter;
        BitReader bitReader;
        bool isUsingBitWriterReader;

        /// <summary>
        /// IMPORTANT: ALWAYS use true for <paramref name="shouldUseBitWriterReader"/> for an order of magnitude higher performance in the calls on this class!!!
        /// </summary>
        private BitByBitByteArrayBuilder(bool shouldUseBitWriterReader = true)
        {
            isUsingBitWriterReader = shouldUseBitWriterReader;
            if (isUsingBitWriterReader)
            {
                bitReader = new BitReader(streamBuffer, 0);
                bitWriter = new BitWriter(streamBuffer, streamBuffer.Length);
            }

            Reset();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        [DllImport("libc.so")]
#elif (UNITY_STANDALONE_LINUX || UNITY_IOS) && ENABLE_IL2CPP && !UNITY_EDITOR
        [DllImport("libc")]
#else
        [DllImport("msvcrt.dll")]
#endif
        private static extern unsafe void* memset(void* ptr, int value, int num);
        private static unsafe void SetArrayElements(byte[] array, byte value, int elementsToSetCount)
        {
            fixed (void* ptr = array)
            {
                memset(ptr, value, elementsToSetCount);
            }
        }
        /* In case the above does not work, this method will work everywhere:
        private static void SetArrayElements(byte[] array, byte value, int elementsToSetCount)
        {
            for (int i = 0; i < elementsToSetCount; ++i)
            {
                array[i] = value;
            }
        }
        */

        /// <summary>
        /// Since the suggested use is only one of these instances per thread, this method must be called prior to any new usage for a particular purpose.
        /// </summary>
        public BitByBitByteArrayBuilder Reset()
        {
            SetArrayElements(streamBuffer, 0, streamBuffer.Length);

            length_WrittenBytes = 0;
            position_Bytes = 0;
            position_InnerByteBit = InnerByteBitPosition_MaxValue;

            if (isUsingBitWriterReader)
            {
                bitWriter.Reset();
                bitReader.Reset();
            }

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
            
            if (isUsingBitWriterReader)
            {
                bitReader.NumBytes = newDataBytesSize;
            }

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
            if (isUsingBitWriterReader)
            {
                throw new NotImplementedException();
            }

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
            if (isUsingBitWriterReader)
            {
                value = (byte)bitReader.ReadBits(bits);
                return true;
            }
            else
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
        private ulong ReadBits(byte[] buffer, int offset, ulong count)
        {
            if (isUsingBitWriterReader)
            {
                throw new NotImplementedException();
            }
            else
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
        }

        readonly byte[] tmpBuffer64 = new byte[8];
        readonly byte trueByte = 1;
        readonly byte falseByte = 0;
        readonly static byte oneBit = 1;

        public bool ReadBit(out bool oBool)
        {
            if (isUsingBitWriterReader)
            {
                oBool = bitReader.ReadBits(1) == 1;
                return true;
            }
            else
            {
                byte oByte;
                bool didRead = ReadBits(out oByte, oneBit);
                oBool = oByte == trueByte;
                return didRead;
            }
        }

        public ulong ReadUShort(out ushort oUShort, ulong bitCount = 16)
        {
            // Debug.Assert(bitCount <= 16);

            if (isUsingBitWriterReader)
            {
                oUShort = (ushort)bitReader.ReadBits((int)bitCount);
                return bitCount;
            }
            else
            {
                ulong bitsRead = ReadBits(GetClearedTmpBuffer64(), 0, bitCount);

                oUShort = System.BitConverter.ToUInt16(tmpBuffer64, 0);

                return bitsRead;
            }
        }

        /* not tested well enough to know if it leaves things in appropriate state like nothing happened...in fact some test results indicate calling this leaves things effed up....so, if we want peek...refactor and TEST TEST TEST!
        /// <summary>
        /// POST: Does NOT advance position.
        /// </summary>
        /// <param name="oChar"></param>
        /// <returns>false if not enough data to read</returns>
        public bool PeekChar(out char oChar, ulong bitCount = 16)
        {
            // Debug.Assert(bitCount <= 16);

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

        public ulong ReadFloat(out float oFloat /*, ulong bitCount = 32 */)
        {
            const ulong bitCount = 32;
            //// Debug.Assert(bitCount <= 32);

            if (isUsingBitWriterReader)
            {
                oFloat = bitReader.ReadFloat();
                return bitCount;
            }
            else
            {
                ulong bitsRead = ReadBits(GetClearedTmpBuffer64(), 0, bitCount);

                oFloat = System.BitConverter.ToSingle(tmpBuffer64, 0);

                return bitsRead;
            }
        }

        private byte[] GetClearedTmpBuffer64()
        {
            Array.Clear(tmpBuffer64, 0, 8);
            return tmpBuffer64;
        }
        
        public ulong ReadUInt(out uint oUInt, ulong bitCount = 32)
        {
            // Debug.Assert(bitCount <= 32);

            if (isUsingBitWriterReader)
            {
                oUInt = bitReader.ReadUInt((int)bitCount);

                return bitCount;
            }
            else
            {
                ulong bitsRead = ReadBits(GetClearedTmpBuffer64(), 0, bitCount);

                oUInt = System.BitConverter.ToUInt32(tmpBuffer64, 0);

                return bitsRead;
            }
        }

        public ulong ReadLong(out long oLong, ulong bitCount = 64)
        {
            // Debug.Assert(bitCount <= 64);

            if (isUsingBitWriterReader)
            {
                oLong = bitReader.ReadLong((int)bitCount);

                return bitCount;
            }
            else
            {
                ulong bitsRead = ReadBits(GetClearedTmpBuffer64(), 0, bitCount);

                oLong = System.BitConverter.ToInt64(tmpBuffer64, 0);

                return bitsRead;
            }
        }

        /// <summary>
        /// Reads a single byte from the stream and returns its value, or -1 if it could not be read.
        /// </summary>
        /// <returns>The value that was read, or -1 if it could not be read.</returns>
        public int ReadByte()
        {
            if (isUsingBitWriterReader)
            {
                return (byte)bitReader.ReadBits(8);
            }
            else
            {
                byte buffer;
                return ReadBits(out buffer, InnerByteBitPosition_MaxValue) ? buffer : -1;
            }
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
            if (isUsingBitWriterReader)
            {
                throw new NotImplementedException();
            }

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
        private void WriteBits(byte[] buffer, int offset, ulong count)
        {
            if (isUsingBitWriterReader)
            {
                throw new NotImplementedException();
            }

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
            if (isUsingBitWriterReader)
            {
                bitWriter.WriteBit(bit);
            }
            else
            {
                WriteBits(bit ? trueByte : falseByte, oneBit);
            }
        }

        public void WriteUShort(ushort iUShort, ulong bitCount = 16)
        {
            // Debug.Assert(bitCount <= 16);

            if (isUsingBitWriterReader)
            {
                bitWriter.WriteUInt(iUShort, (int)bitCount);
            }
            else
            {
                byte[] bytes = BitConverter.BorrowByteArray(); // use pool to avoid allocating byte[] if using System.BitConverter

                BitConverter.GetBytes(iUShort, bytes);
                WriteBits(bytes, 0, bitCount);

                BitConverter.ReturnByteArray(bytes);
            }
        }

        public void WriteFloat(float iFloat/*, ulong bitCount = 32*/)
        {
            const ulong bitCount = 32;
            //// Debug.Assert(bitCount <= 32);

            if (isUsingBitWriterReader)
            {
                bitWriter.WriteFloat(iFloat);
            }
            else
            {
                byte[] bytes = BitConverter.BorrowByteArray(); // use pool to avoid allocating byte[] if using System.BitConverter

                BitConverter.GetBytes(iFloat, bytes);
                WriteBits(bytes, 0, bitCount);

                BitConverter.ReturnByteArray(bytes);
            }
        }

        public void WriteUInt(uint iUInt, ulong bitCount = 32)
        {
            // Debug.Assert(bitCount <= 32);

            if (isUsingBitWriterReader)
            {
                bitWriter.WriteUInt(iUInt, (int)bitCount);
            }
            else
            {
                byte[] bytes = BitConverter.BorrowByteArray(); // use pool to avoid allocating byte[] if using System.BitConverter

                BitConverter.GetBytes(iUInt, bytes);
                WriteBits(bytes, 0, bitCount);

                BitConverter.ReturnByteArray(bytes);
            }
        }

        public void WriteLong(long iLong, ulong bitCount = 64)
        {
            // Debug.Assert(bitCount <= 64);

            if (isUsingBitWriterReader)
            {
                bitWriter.WriteLong(iLong, (int)bitCount);
            }
            else
            {
                byte[] bytes = BitConverter.BorrowByteArray(); // use pool to avoid allocating byte[] if using System.BitConverter

                BitConverter.GetBytes(iLong, bytes);
                WriteBits(bytes, 0, bitCount);

                BitConverter.ReturnByteArray(bytes);
            }
        }


        /// <summary>
        /// Writes the given number of bits from the value.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="bits">The number of bits to write.</param>
        public void WriteBits(byte value, byte bits)
        {
            if (isUsingBitWriterReader)
            {
                bitWriter.WriteBits(value, bits);
            }
            else
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
        public bool WriteCurrentPartialByte()
        {
            if (isUsingBitWriterReader)
            {
                bitWriter.FlushBits();
            }
            else
            {
                const bool paddingBit = false;

                if (position_InnerByteBit == 0)
                {
                    return false;
                }

                int paddingBitCount = 8 - position_InnerByteBit;
                for (int i = 0; i < paddingBitCount; ++i)
                {
                    WriteBit(paddingBit);
                }
            }

            return true;
        }

        #endregion Write Methods

        #region Stream replacements

        private void Stream_WriteByte(byte value)
        {
            if (isUsingBitWriterReader)
            {
                throw new NotImplementedException();
            }

            if (length_WrittenBytes == STREAM_BUFFER_MAX_SIZE)
            {
                throw new InvalidOperationException("not enough memory available.  entire buffer already used.  buffer size (bytes): " + STREAM_BUFFER_MAX_SIZE);
            }

            streamBuffer[length_WrittenBytes++] = value;
            ++position_Bytes;
        }

        private void Stream_Write(byte[] buffer, int offset, int count)
        {
            if (isUsingBitWriterReader)
            {
                throw new NotImplementedException();
            }

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
            if (isUsingBitWriterReader)
            {
                throw new NotImplementedException();
            }

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
            if (isUsingBitWriterReader)
            {
                throw new NotImplementedException();
            }

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

    /**
    Template to convert an integer value from local byte order to network byte order.
    IMPORTANT: Because most machines running yojimbo are little endian, yojimbo defines network byte order to be little endian.
    @param value The input value in local byte order. Supported integer types: uint64_t, uint, uint16_t.
    @returns The input value converted to network byte order. If this processor is little endian the output is the same as the input. If the processor is big endian, the output is the input byte swapped.
    @see yojimbo::bswap
 */

    public static class yojimbo
    {
        #region utils
        /**
            Generate cryptographically secure random data.
            @param data The buffer to store the random data.
            @param bytes The number of bytes of random data to generate.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void random_bytes(ref ulong data, int bytes) { var v = new byte[bytes]; random_bytes(v, bytes); data = System.BitConverter.ToUInt64(v, 0); }

        readonly static RNGCryptoServiceProvider rngCrypto = new RNGCryptoServiceProvider();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void random_bytes(byte[] data, int bytes) { /*assert(data != null); assert(bytes > 0); assert(bytes == data.Length);*/ rngCrypto.GetBytes(data); } //: randombytes_buf(data, bytes);

        /**
            Generate a random integer between a and b (inclusive).
            IMPORTANT: This is not a cryptographically secure random. It's used only for test functions and in the network simulator.
            @param a The minimum integer value to generate.
            @param b The maximum integer value to generate.
            @returns A pseudo random integer value in [a,b].
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int random_int(int a, int b)
        {
            //assert(a < b);
            var result = a + BufferEx.Rand() % (b - a + 1);
            //assert(result >= a);
            //assert(result <= b);
            return result;
        }

        /** 
            Generate a random float between a and b.
            IMPORTANT: This is not a cryptographically secure random. It's used only for test functions and in the network simulator.
            @param a The minimum integer value to generate.
            @param b The maximum integer value to generate.
            @returns A pseudo random float value in [a,b].
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float random_float(float a, float b)
        {
            //assert(a < b);
            var random = BufferEx.Rand() / (float)BufferEx.RAND_MAX;
            var diff = b - a;
            var r = random * diff;
            return a + r;
        }

        ///**
        //    Calculates the population count of an unsigned 32 bit integer at compile time.
        //    Population count is the number of bits in the integer that set to 1.
        //    See "Hacker's Delight" and http://www.hackersdelight.org/hdcodetxt/popArrayHS.c.txt
        //    @see yojimbo::Log2
        //    @see yojimbo::BitsRequired
        // */
        //template<uint x> struct PopCount
        //{
        //    enum {
        //        a = x - ((x >> 1) & 0x55555555),
        //        b = (((a >> 2) & 0x33333333) + (a & 0x33333333)),
        //        c = (((b >> 4) + b) & 0x0f0f0f0f),
        //        d = c + (c >> 8),
        //        e = d + (d >> 16),

        //        result = e & 0x0000003f
        //    };
        //};

        ///**
        //    Calculates the log 2 of an unsigned 32 bit integer at compile time.
        //    @see yojimbo::Log2
        //    @see yojimbo::BitsRequired
        // */

        //template<uint x> struct Log2
        //{
        //    enum {
        //        a = x | (x >> 1),
        //        b = a | (a >> 2),
        //        c = b | (b >> 4),
        //        d = c | (c >> 8),
        //        e = d | (d >> 16),
        //        f = e >> 1,

        //        result = PopCount < f >::result
        //    };
        //};

        ///**
        //    Calculates the number of bits required to serialize an integer value in [min,max] at compile time.
        //    @see Log2
        //    @see PopCount
        // */

        //template<int64_t min, int64_t max> struct BitsRequired
        //{
        //    static const uint result = (min == max) ? 0 : (Log2 < uint(max - min) >::result + 1);
        //};

        /**
            Calculates the population count of an unsigned 32 bit integer.
            The population count is the number of bits in the integer set to 1.
            @param x The input integer value.
            @returns The number of bits set to 1 in the input value.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint popcount(uint x)
        {
            var result = x - ((x >> 1) & 0x5555555555555555UL);
            result = (result & 0x3333333333333333UL) + ((result >> 2) & 0x3333333333333333UL);
            return (byte)(unchecked(((result + (result >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
        }

        /**
            Calculates the log base 2 of an unsigned 32 bit integer.
            @param x The input integer value.
            @returns The log base 2 of the input.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint log2(uint x)
        {
            var a = x | (x >> 1);
            var b = a | (a >> 2);
            var c = b | (b >> 4);
            var d = c | (c >> 8);
            var e = d | (d >> 16);
            var f = e >> 1;
            return popcount(f);
        }

        /**
            Calculates the number of bits required to serialize an integer in range [min,max].
            @param min The minimum value.
            @param max The maximum value.
            @returns The number of bits required to serialize the integer.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int bits_required(uint min, uint max) =>
            (min == max) ? 0 : (int)log2(max - min) + 1;

        /**
            Reverse the order of bytes in a 64 bit integer.
            @param value The input value.
            @returns The input value with the byte order reversed.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong bswap(ulong value)
        {
            value = (value & 0x00000000FFFFFFFF) << 32 | (value & 0xFFFFFFFF00000000) >> 32;
            value = (value & 0x0000FFFF0000FFFF) << 16 | (value & 0xFFFF0000FFFF0000) >> 16;
            value = (value & 0x00FF00FF00FF00FF) << 8 | (value & 0xFF00FF00FF00FF00) >> 8;
            return value;
        }

        /**
            Reverse the order of bytes in a 32 bit integer.
            @param value The input value.
            @returns The input value with the byte order reversed.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint bswap(uint value) =>
            (value & 0x000000ff) << 24 | (value & 0x0000ff00) << 8 | (value & 0x00ff0000) >> 8 | (value & 0xff000000) >> 24;

        /**
            Reverse the order of bytes in a 16 bit integer.
            @param value The input value.
            @returns The input value with the byte order reversed.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort bswap(ushort value) =>
            (ushort)((value & 0x00ff) << 8 | (value & 0xff00) >> 8);

        /**
            Template to convert an integer value from local byte order to network byte order.
            IMPORTANT: Because most machines running yojimbo are little endian, yojimbo defines network byte order to be little endian.
            @param value The input value in local byte order. Supported integer types: uint64_t, uint, uint16_t.
            @returns The input value converted to network byte order. If this processor is little endian the output is the same as the input. If the processor is big endian, the output is the input byte swapped.
            @see yojimbo::bswap
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T host_to_network<T>(T value) =>
#if YOJIMBO_BIG_ENDIAN
        bswap(value);
#else
        value;
#endif

        /**
            Template to convert an integer value from network byte order to local byte order.
            IMPORTANT: Because most machines running yojimbo are little endian, yojimbo defines network byte order to be little endian.
            @param value The input value in network byte order. Supported integer types: uint64_t, uint, uint16_t.
            @returns The input value converted to local byte order. If this processor is little endian the output is the same as the input. If the processor is big endian, the output is the input byte swapped.
            @see yojimbo::bswap
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T network_to_host<T>(T value) =>
#if YOJIMBO_BIG_ENDIAN
        bswap(value);
#else
        value;
#endif

        /** 
            Compares two 16 bit sequence numbers and returns true if the first one is greater than the second (considering wrapping).
            IMPORTANT: This is not the same as s1 > s2!
            Greater than is defined specially to handle wrapping sequence numbers. 
            If the two sequence numbers are close together, it is as normal, but they are far apart, it is assumed that they have wrapped around.
            Thus, sequence_greater_than( 1, 0 ) returns true, and so does sequence_greater_than( 0, 65535 )!
            @param s1 The first sequence number.
            @param s2 The second sequence number.
            @returns True if the s1 is greater than s2, with sequence number wrapping considered.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool sequence_greater_than(ushort s1, ushort s2) =>
            ((s1 > s2) && (s1 - s2 <= 32768)) ||
            ((s1 < s2) && (s2 - s1 > 32768));

        /** 
            Compares two 16 bit sequence numbers and returns true if the first one is less than the second (considering wrapping).
            IMPORTANT: This is not the same as s1 < s2!
            Greater than is defined specially to handle wrapping sequence numbers. 
            If the two sequence numbers are close together, it is as normal, but they are far apart, it is assumed that they have wrapped around.
            Thus, sequence_less_than( 0, 1 ) returns true, and so does sequence_greater_than( 65535, 0 )!
            @param s1 The first sequence number.
            @param s2 The second sequence number.
            @returns True if the s1 is less than s2, with sequence number wrapping considered.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool sequence_less_than(ushort s1, ushort s2) =>
            sequence_greater_than(s2, s1);

        /**
            Convert a signed integer to an unsigned integer with zig-zag encoding.
            0,-1,+1,-2,+2... becomes 0,1,2,3,4 ...
            @param n The input value.
            @returns The input value converted from signed to unsigned with zig-zag encoding.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int signed_to_unsigned(int n) =>
            (n << 1) ^ (n >> 31);

        /**
            Convert an unsigned integer to as signed integer with zig-zag encoding.
            0,1,2,3,4... becomes 0,-1,+1,-2,+2...
            @param n The input value.
            @returns The input value converted from unsigned to signed with zig-zag encoding.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int unsigned_to_signed(uint n) =>
            (int)((n >> 1) ^ (-(int)(n & 1)));

#if YOJIMBO_WITH_MBEDTLS

        /**
            Base 64 encode a string.
            @param input The input string value. Must be null terminated.
            @param output The output base64 encoded string. Will be null terminated.
            @param output_size The size of the output buffer (bytes). Must be large enough to store the base 64 encoded string.
            @returns The number of bytes in the base64 encoded string, including terminating null. -1 if the base64 encode failed because the output buffer was too small.
         */
        public static int base64_encode_string(string input, string output, int output_size) =>
            throw new NotImplementedException();

        /**
            Base 64 decode a string.
            @param input The base64 encoded string.
            @param output The decoded string. Guaranteed to be null terminated, even if the base64 is maliciously encoded.
            @param output_size The size of the output buffer (bytes).
            @returns The number of bytes in the decoded string, including terminating null. -1 if the base64 decode failed.
         */
        public static int base64_decode_string(string input, string output, int output_size) =>
            throw new NotImplementedException();

        /**
            Base 64 encode a block of data.
            @param input The data to encode.
            @param input_length The length of the input data (bytes).
            @param output The output base64 encoded string. Will be null terminated.
            @param output_size The size of the output buffer. Must be large enough to store the base 64 encoded string.
            @returns The number of bytes in the base64 encoded string, including terminating null. -1 if the base64 encode failed because the output buffer was too small.
         */

        public static int base64_encode_data(byte[] input, int input_length, string output, int output_size) =>
            throw new NotImplementedException();

        /**
            Base 64 decode a block of data.
            @param input The base 64 data to decode. Must be a null terminated string.
            @param output The output data. Will *not* be null terminated.
            @param output_size The size of the output buffer.
            @returns The number of bytes of decoded data. -1 if the base64 decode failed.
         */
        public static int base64_decode_data(string input, byte[] output, int output_size) =>
            throw new NotImplementedException();

        /**
            Print bytes with a label. 
            Useful for printing out packets, encryption keys, nonce etc.
            @param label The label to print out before the bytes.
            @param data The data to print out to stdout.
            @param data_bytes The number of bytes of data to print.
         */
        public static void print_bytes(string label, byte[] data, int data_bytes)
        {
            Console.Write($"{label}: ");
            for (var i = 0; i < data_bytes; ++i)
                Console.Write($"0x{(int)data[i]},");
            Console.Write($" ({data_bytes} bytes)\n");
        }

#endif

        #endregion
    }

    #region BitWriter

    /**
        Bitpacks unsigned integer values to a buffer.
        Integer bit values are written to a 64 bit scratch value from right to left.
        Once the low 32 bits of the scratch is filled with bits it is flushed to memory as a dword and the scratch value is shifted right by 32.
        The bit stream is written to memory in little endian order, which is considered network byte order for this library.
        @see BitReader
     */
    public class BitWriter
    {
        /**
            Bit writer constructor.
            Creates a bit writer object to write to the specified buffer. 
            @param data The pointer to the buffer to fill with bitpacked data.
            @param bytes The size of the buffer in bytes. Must be a multiple of 4, because the bitpacker reads and writes memory as dwords, not bytes.
         */
        public BitWriter(byte[] data, int bytes)
        {
            m_data = data;
            Reset(bytes);
        }

        internal void Reset(int? bytesUsed = default)
        {
            int bytes = bytesUsed.HasValue ? bytesUsed.Value : m_data.Length;
            m_numWords = bytes / 4;
            // yojimbo.assert(data != null);
            // yojimbo.assert((bytes % 4) == 0);
            m_numBits = m_numWords * 32;
            m_bitsWritten = 0;
            m_wordIndex = 0;
            m_scratch = 0;
            m_scratchBits = 0;
        }

        static void write32(byte[] b, int p, uint value)
        {
            p *= sizeof(uint);
            b[p] = (byte)value;
            b[p + 1] = (byte)(value >> 8);
            b[p + 2] = (byte)(value >> 0x10);
            b[p + 3] = (byte)(value >> 0x18);
        }

        /**
            Write bits to the buffer.
            Bits are written to the buffer as-is, without padding to nearest byte. Will assert if you try to write past the end of the buffer.
            A boolean value writes just 1 bit to the buffer, a value in range [0,31] can be written with just 5 bits and so on.
            IMPORTANT: When you have finished writing to your buffer, take care to call BitWrite::FlushBits, otherwise the last dword of data will not get flushed to memory!
            @param value The integer value to write to the buffer. Must be in [0,(1<<bits)-1].
            @param bits The number of bits to encode in [1,32].
            @see BitReader::ReadBits
         */
        public void WriteBits(uint value, int bits)
        {
            // yojimbo.assert(bits > 0);
            // yojimbo.assert(bits <= 32);
            // yojimbo.assert(m_bitsWritten + bits <= m_numBits);
            // yojimbo.assert(value <= (1UL << bits) - 1);

            int unwantedBitCount = 32 - bits;
            uint valuePossiblyTruncated = (value << unwantedBitCount) >> unwantedBitCount;

            m_scratch |= (ulong)valuePossiblyTruncated << m_scratchBits;

            m_scratchBits += bits;

            if (m_scratchBits >= 32)
            {
                // yojimbo.assert(m_wordIndex < m_numWords);
                write32(m_data, m_wordIndex, yojimbo.host_to_network((uint)(m_scratch & 0xFF_FF_FF_FF)));
                m_scratch >>= 32;
                m_scratchBits -= 32;
                m_wordIndex++;
            }

            m_bitsWritten += bits;
        }

        /**
            Write an alignment to the bit stream, padding zeros so the bit index becomes is a multiple of 8.
            This is useful if you want to write some data to a packet that should be byte aligned. For example, an array of bytes, or a string.
            IMPORTANT: If the current bit index is already a multiple of 8, nothing is written.
            @see BitReader::ReadAlign
         */

        public void WriteAlign()
        {
            var remainderBits = m_bitsWritten % 8;

            if (remainderBits != 0)
            {
                const uint zero = 0U;
                WriteBits(zero, 8 - remainderBits);
                // yojimbo.assert((m_bitsWritten % 8) == 0);
            }
        }

        /**
            Write an array of bytes to the bit stream.
            Use this when you have to copy a large block of data into your bitstream.
            Faster than just writing each byte to the bit stream via BitWriter::WriteBits( value, 8 ), because it aligns to byte index and copies into the buffer without bitpacking.
            @param data The byte array data to write to the bit stream.
            @param bytes The number of bytes to write.
            @see BitReader::ReadBytes
         */
        public void WriteBytes(byte[] data, int bytes)
        {
            // yojimbo.assert(AlignBits == 0);
            // yojimbo.assert(m_bitsWritten + bytes * 8 <= m_numBits);
            // yojimbo.assert((m_bitsWritten % 32) == 0 || (m_bitsWritten % 32) == 8 || (m_bitsWritten % 32) == 16 || (m_bitsWritten % 32) == 24);

            var headBytes = (4 - (m_bitsWritten % 32) / 8) % 4;
            if (headBytes > bytes)
                headBytes = bytes;
            for (var i = 0; i < headBytes; ++i)
                WriteBits(data[i], 8);
            if (headBytes == bytes)
                return;

            FlushBits();

            // yojimbo.assert(AlignBits == 0);

            var numWords = (bytes - headBytes) / 4;
            if (numWords > 0)
            {
                // yojimbo.assert((m_bitsWritten % 32) == 0);
                BufferEx.Copy(m_data, m_wordIndex * sizeof(uint), data, headBytes, numWords * 4);
                m_bitsWritten += numWords * 32;
                m_wordIndex += numWords;
                m_scratch = 0;
            }

            // yojimbo.assert(AlignBits == 0);

            var tailStart = headBytes + numWords * 4;
            var tailBytes = bytes - tailStart;
            // yojimbo.assert(tailBytes >= 0 && tailBytes < 4);
            for (var i = 0; i < tailBytes; ++i)
                WriteBits(data[tailStart + i], 8);

            // yojimbo.assert(AlignBits == 0);

            // yojimbo.assert(headBytes + numWords * 4 + tailBytes == bytes);
        }

        /**
            Flush any remaining bits to memory.
            Call this once after you've finished writing bits to flush the last dword of scratch to memory!
            @see BitWriter::WriteBits
         */
        public void FlushBits(bool shouldWriteAlignAlso = true)
        {
            if (shouldWriteAlignAlso)
            {
                WriteAlign();
            }

            if (m_scratchBits != 0)
            {
                // yojimbo.assert(m_scratchBits <= 32);
                // yojimbo.assert(m_wordIndex < m_numWords);
                write32(m_data, m_wordIndex, yojimbo.host_to_network((uint)(m_scratch & 0xFFFFFFFF)));
                m_scratch >>= 32;
                m_scratchBits = 0;
                m_wordIndex++;
            }
        }

        public void WriteBit(bool v)
        {
            if (v)
            {
                WriteBits(1, 1);
            }
            else
            {
                WriteBits(0, 1);
            }
        }

        public void WriteByte(byte v)
        {
            WriteUInt((uint)v, 8);
        }

        public unsafe void WriteFloat(float value)
        {
            uint valueAsUINT = *(uint*)(&value);
            WriteBits(valueAsUINT, 32);
        }

        public unsafe void WriteLong(long value, int bitCount = 64)
        {
            ulong valueAsULONG = *(ulong*)(&value);

            uint right = (uint)((valueAsULONG << 32) >> 32);
            WriteBits(right, bitCount > 32 ? 32 : bitCount);

            bitCount -= 32;
            if (bitCount > 0)
            {
                uint left = (uint)(valueAsULONG >> 32);
                WriteBits(left, bitCount);
            }
        }

        public void WriteUInt(uint v, int bitCount = 32)
        {
            WriteBits(v, bitCount);
        }

        /**
            How many align bits would be written, if we were to write an align right now?
            @returns Result in [0,7], where 0 is zero bits required to align (already aligned) and 7 is worst case.
         */
        public int AlignBits =>
            (8 - (m_bitsWritten % 8)) % 8;

        /** 
            How many bits have we written so far?
            @returns The number of bits written to the bit buffer.
         */
        public int BitsWritten =>
            m_bitsWritten;

        /**
            How many bits are still available to write?
            For example, if the buffer size is 4, we have 32 bits available to write, if we have already written 10 bytes then 22 are still available to write.
            @returns The number of bits available to write.
         */
        public int BitsAvailable =>
            m_numBits - m_bitsWritten;

        /**
            Get a pointer to the data written by the bit writer.
            Corresponds to the data block passed in to the constructor.
            @returns Pointer to the data written by the bit writer.
         */

        public byte[] Data =>
            m_data;

        /**
            The number of bytes flushed to memory.
            This is effectively the size of the packet that you should send after you have finished bitpacking values with this class.
            The returned value is not always a multiple of 4, even though we flush dwords to memory. You won't miss any data in this case because the order of bits written is designed to work with the little endian memory layout.
            IMPORTANT: Make sure you call BitWriter::FlushBits before calling this method, otherwise you risk missing the last dword of data.
         */
        public int BytesWritten =>
            (m_bitsWritten + 7) / 8;

        byte[] m_data;                              ///< The buffer we are writing to, as a uint * because we're writing dwords at a time.
        ulong m_scratch;                            ///< The scratch value where we write bits to (right to left). 64 bit for overflow. Once # of bits in scratch is >= 32, the low 32 bits are flushed to memory.
        int m_numBits;                              ///< The number of bits in the buffer. This is equivalent to the size of the buffer in bytes multiplied by 8. Note that the buffer size must always be a multiple of 4.
        int m_numWords;                             ///< The number of words in the buffer. This is equivalent to the size of the buffer in bytes divided by 4. Note that the buffer size must always be a multiple of 4.
        int m_bitsWritten;                          ///< The number of bits written so far.
        int m_wordIndex;                            ///< The current word index. The next word flushed to memory will be at this index in m_data.
        int m_scratchBits;                          ///< The number of bits in scratch. When this is >= 32, the low 32 bits of scratch is flushed to memory as a dword and scratch is shifted right by 32.
    }

    #endregion

    #region BitReader

    /**
        Reads bit packed integer values from a buffer.
        Relies on the user reconstructing the exact same set of bit reads as bit writes when the buffer was written. This is an unattributed bitpacked binary stream!
        Implementation: 32 bit dwords are read in from memory to the high bits of a scratch value as required. The user reads off bit values from the scratch value from the right, after which the scratch value is shifted by the same number of bits.
     */
    public class BitReader
    {
        /**
            Bit reader constructor.
            Non-multiples of four buffer sizes are supported, as this naturally tends to occur when packets are read from the network.
            However, actual buffer allocated for the packet data must round up at least to the next 4 bytes in memory, because the bit reader reads dwords from memory not bytes.
            @param data Pointer to the bitpacked data to read.
            @param bytes The number of bytes of bitpacked data to read.
            @see BitWriter
         */
        public BitReader(byte[] data, int bytes)
        {
            Reset();

            m_data = data;
            m_numBytes = bytes;
#if DEBUG
            m_numWords = (bytes + 3) / 4;
#endif
            // yojimbo.assert(data != null);
            m_numBits = m_numBytes * 8;
            
        }

        internal void Reset()
        {
            m_numBytes = 0;
#if DEBUG
            m_numWords = 0;
#endif
            m_numBits = 0;
            m_bitsRead = 0;
            m_scratch = 0;
            m_scratchBits = 0;
            m_wordIndex = 0;
        }

        static uint read32(byte[] b, int p)
        {
            p *= sizeof(uint);
            var r = b.Length - p - 4;
            var value = (uint)b[p];
            if (r > -3) value |= (uint)(b[p + 1] << 8);
            if (r > -2) value |= (uint)(b[p + 2] << 0x10);
            if (r > -1) value |= (uint)(b[p + 3] << 0x18);
            return value;
        }

        /**
            Would the bit reader would read past the end of the buffer if it read this many bits?
            @param bits The number of bits that would be read.
            @returns True if reading the number of bits would read past the end of the buffer.
         */
        public bool WouldReadPastEnd(int bits) =>
            m_bitsRead + bits > m_numBits;

        /**
            Read bits from the bit buffer.
            This function will assert in debug builds if this read would read past the end of the buffer.
            In production situations, the higher level ReadStream takes care of checking all packet data and never calling this function if it would read past the end of the buffer.
            @param bits The number of bits to read in [1,32].
            @returns The integer value read in range [0,(1<<bits)-1].
            @see BitReader::WouldReadPastEnd
            @see BitWriter::WriteBits
         */
        public uint ReadBits(int bits)
        {
            // yojimbo.assert(bits > 0);
            // yojimbo.assert(bits <= 32);
            // yojimbo.assert(m_bitsRead + bits <= m_numBits);

            m_bitsRead += bits;

            // yojimbo.assert(m_scratchBits >= 0 && m_scratchBits <= 64);

            if (m_scratchBits < bits)
            {
#if DEBUG
                // yojimbo.assert(m_wordIndex < m_numWords);
#endif
                m_scratch |= (ulong)(yojimbo.network_to_host(read32(m_data, m_wordIndex))) << m_scratchBits;
                m_scratchBits += 32;
                m_wordIndex++;
            }

            // yojimbo.assert(m_scratchBits >= bits);

            var output = (uint)(m_scratch & ((1UL << bits) - 1));

            m_scratch >>= bits;
            m_scratchBits -= bits;

            return output;
        }

        /**
            Read an align.
            Call this on read to correspond to a WriteAlign call when the bitpacked buffer was written. 
            This makes sure we skip ahead to the next aligned byte index. As a safety check, we verify that the padding to next byte is zero bits and return false if that's not the case. 
            This will typically abort packet read. Just another safety measure...
            @returns True if we successfully read an align and skipped ahead past zero pad, false otherwise (probably means, no align was written to the stream).
            @see BitWriter::WriteAlign
         */
        public bool ReadAlign()
        {
            var remainderBits = m_bitsRead % 8;
            if (remainderBits != 0)
            {
                var value = ReadBits(8 - remainderBits);
                // yojimbo.assert(m_bitsRead % 8 == 0);
                if (value != 0)
                    return false;
            }
            return true;
        }

        /**
            Read bytes from the bitpacked data.
            @see BitWriter::WriteBytes
         */
        public void ReadBytes(byte[] data, int bytes)
        {
            // yojimbo.assert(AlignBits == 0);
            // yojimbo.assert(m_bitsRead + bytes * 8 <= m_numBits);
            // yojimbo.assert((m_bitsRead % 32) == 0 || (m_bitsRead % 32) == 8 || (m_bitsRead % 32) == 16 || (m_bitsRead % 32) == 24);

            var headBytes = (4 - (m_bitsRead % 32) / 8) % 4;
            if (headBytes > bytes)
                headBytes = bytes;
            for (var i = 0; i < headBytes; ++i)
                data[i] = (byte)ReadBits(8);
            if (headBytes == bytes)
                return;

            // yojimbo.assert(AlignBits == 0);

            var numWords = (bytes - headBytes) / 4;
            if (numWords > 0)
            {
                // yojimbo.assert((m_bitsRead % 32) == 0);
                BufferEx.Copy(data, headBytes, m_data, m_wordIndex * sizeof(uint), numWords * 4);
                m_bitsRead += numWords * 32;
                m_wordIndex += numWords;
                m_scratchBits = 0;
            }

            // yojimbo.assert(AlignBits == 0);

            var tailStart = headBytes + numWords * 4;
            var tailBytes = bytes - tailStart;
            // yojimbo.assert(tailBytes >= 0 && tailBytes < 4);
            for (var i = 0; i < tailBytes; ++i)
                data[tailStart + i] = (byte)ReadBits(8);

            // yojimbo.assert(AlignBits == 0);

            // yojimbo.assert(headBytes + numWords * 4 + tailBytes == bytes);
        }

        readonly byte[] floatieBuff = new byte[8];

        public unsafe float ReadFloat()
        {
            uint value = ReadBits(32);
            float valueAsFLOAT = *(float*)(&value);
            return valueAsFLOAT;
        }

        public long ReadLong(int bitCount = 64)
        {
            uint right = ReadBits(bitCount > 32 ? 32 : bitCount);

            bitCount -= 32;
            if (bitCount > 0)
            {
                long left = (long)ReadBits(bitCount);
                return (left << 32) + right;
            }
            else
            {
                return right;
            }
        }

        public uint ReadUInt(int bitCount = 32)
        {
            return ReadBits(bitCount);
        }

        /**
            How many align bits would be read, if we were to read an align right now?
            @returns Result in [0,7], where 0 is zero bits required to align (already aligned) and 7 is worst case.
         */

        public int AlignBits =>
            (8 - m_bitsRead % 8) % 8;

        /** 
            How many bits have we read so far?
            @returns The number of bits read from the bit buffer so far.
         */

        public int BitsRead =>
            m_bitsRead;

        /**
            How many bits are still available to read?
            For example, if the buffer size is 4, we have 32 bits available to read, if we have already written 10 bytes then 22 are still available.
            @returns The number of bits available to read.
         */
        public int BitsRemaining =>
            m_numBits - m_bitsRead;

        byte[] m_data;                              ///< The bitpacked data we're reading as a dword array.
        ulong m_scratch;                            ///< The scratch value. New data is read in 32 bits at a top to the left of this buffer, and data is read off to the right.
        int m_numBits;                              ///< Number of bits to read in the buffer. Of course, we can't *really* know this so it's actually m_numBytes * 8.
        internal int NumBytes { get => m_numBytes; set { m_numBytes = value; m_numBits = value * 8; } }
        int m_numBytes;                             ///< Number of bytes to read in the buffer. We know this, and this is the non-rounded up version.
#if DEBUG
        int m_numWords;                             ///< Number of words to read in the buffer. This is rounded up to the next word if necessary.
#endif
        int m_bitsRead;                             ///< Number of bits read from the buffer so far.
        int m_scratchBits;                          ///< Number of bits currently in the scratch value. If the user wants to read more bits than this, we have to go fetch another dword from memory.
        int m_wordIndex;                            ///< Index of the next word to read from memory.
    }

    #endregion
    #region BufferEx

    public static class BufferEx
    {
        readonly static Random Random = new Random(Guid.NewGuid().GetHashCode());

        public const int RAND_MAX = 0x7fff;
        
        public static int Rand() { lock (Random) return Random.Next(RAND_MAX); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(Array dst, Array src, int length) =>
            Buffer.BlockCopy(src, 0, dst, 0, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(Array dst, int dstOffset, Array src, int srcOffset, int length) =>
            Buffer.BlockCopy(src, srcOffset, dst, dstOffset, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(ref T dst, T src = null, int? length = null) where T : class, new() =>
            dst = src ?? new T();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Slice(Array src, int srcOffset, int length)
        {
            //Arrays.CopyOfRange
            var r = new byte[length]; Buffer.BlockCopy(src, srcOffset, r, 0, length); return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetT<T>(IList<T> array, object value, int? length = null) where T : new()
        {
            for (var i = 0; i < (length ?? array.Count); i++)
                array[i] = value != null ? new T() : default(T);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetT<T>(ref T dst, object value, int? length = null) where T : new()
        {
            dst = value != null ? new T() : default(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] NewT<T>(int length) where T : new()
        {
            var array = new T[length];
            for (var i = 0; i < length; i++)
                array[i] = new T();
            return array;
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[][] NewT<T>(int length, int length2) where T : new()
        {
            var array = new T[length][];
            for (var i = 0; i < length; i++)
                array[i] = new T[length2];
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equal<T>(IList<T> first, IList<T> second, int? length = null) =>
            (length == null) || (first.Count == length && second.Count == length) ?
                Enumerable.SequenceEqual(first, second) :
                Enumerable.SequenceEqual(first.Take(length.Value), second.Take(length.Value));
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equal<T>(IList<T> first, int firstOffset, IList<T> second, int secondOffset, int? length = null) =>
            (length == null) || (first.Count - firstOffset == length && second.Count - secondOffset == length) ?
                Enumerable.SequenceEqual(first.Skip(firstOffset), second.Skip(firstOffset)) :
                Enumerable.SequenceEqual(first.Skip(firstOffset).Take(length.Value), second.Skip(firstOffset).Take(length.Value));
    }

    #endregion

}