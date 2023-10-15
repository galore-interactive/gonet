using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GONet.Utils
{
    [TestFixture]
    public class BitByBitByteArrayBuilderTests
    {
        const int OUTER_LOOPS = 1000;
        const int INNER_LOOPS = 250;

        [Test]
        public void BitByBitByteArrayBuilder_WriteTest()
        {
            long startTicks = DateTime.UtcNow.Ticks;
            for (int iOuter = 0; iOuter < OUTER_LOOPS; ++iOuter)
            {
                byte[] output = null;
                byte[] input = null;

                using (BitByBitByteArrayBuilder builder = BitByBitByteArrayBuilder.GetBuilder())
                {

                    for (int i = 0; i < INNER_LOOPS; ++i)
                    {
                        builder.WriteBit(true);

                        builder.WriteBits(0xff, 1);
                        builder.WriteBits(0xff, 2);
                        builder.WriteBits(0xff, 3);
                        builder.WriteBits(0xff, 4);
                        builder.WriteBits(0xff, 5);
                        builder.WriteBits(0xff, 6);
                        builder.WriteBits(0xff, 7);
                        
                        builder.WriteByte(0x12);

                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);

                        builder.WriteLong(123435678957438L, 10);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 20);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 30);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 40);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 50);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 60);
                        builder.WriteLong(123435678957438L);

                        builder.WriteUInt(0x0f_ff_ff_ff, 5);
                        builder.WriteUInt(0x0f_ff_ff_ff, 10);
                        builder.WriteUInt(0x0f_ff_ff_ff, 15);
                        builder.WriteUInt(0x0f_ff_ff_ff, 20);
                        builder.WriteUInt(0x0f_ff_ff_ff, 25);
                        builder.WriteUInt(0x0f_ff_ff_ff, 30);
                        builder.WriteUInt(0x0f_ff_ff_ff);
                    }

                    builder.WriteCurrentPartialByte();

                    output = builder.GetBuffer();
                    input = new byte[output.Length];
                    Buffer.BlockCopy(output, 0, input, 0, output.Length);
                }

                using (BitByBitByteArrayBuilder builder = BitByBitByteArrayBuilder.GetBuilder_WithNewData(input, input.Length))
                {
                    ulong bitsRead = 0;
                    for (int i = 0; i < INNER_LOOPS; ++i)
                    {
                        bool yerp;
                        builder.ReadBit(out yerp);
                        Assert.AreEqual(true, yerp);
                        bitsRead += 1;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        byte bitten;
                        builder.ReadBits(out bitten, 1);
                        Assert.AreEqual(1, bitten);
                        bitsRead += 1;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadBits(out bitten, 2);
                        Assert.AreEqual(3, bitten);
                        bitsRead += 2;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadBits(out bitten, 3);
                        Assert.AreEqual(7, bitten);
                        bitsRead += 3;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadBits(out bitten, 4);
                        Assert.AreEqual(15, bitten);
                        bitsRead += 4;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadBits(out bitten, 5);
                        Assert.AreEqual(31, bitten);
                        bitsRead += 5;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadBits(out bitten, 6);
                        Assert.AreEqual(63, bitten);
                        bitsRead += 6;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadBits(out bitten, 7);
                        Assert.AreEqual(127, bitten);
                        bitsRead += 7;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);
                        
                        byte bitito = (byte)builder.ReadByte();
                        Assert.AreEqual(0x12, bitito);
                        bitsRead += 8;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);
                        

                        float mcFloat;
                        builder.ReadFloat(out mcFloat);
                        Assert.AreEqual(12.23f, mcFloat);
                        bitsRead += 32;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadFloat(out mcFloat);
                        Assert.AreEqual(12.23f, mcFloat);
                        bitsRead += 32;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadFloat(out mcFloat);
                        Assert.AreEqual(12.23f, mcFloat);
                        bitsRead += 32;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadFloat(out mcFloat);
                        Assert.AreEqual(12.23f, mcFloat);
                        bitsRead += 32;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadFloat(out mcFloat);
                        Assert.AreEqual(12.23f, mcFloat);
                        bitsRead += 32;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadFloat(out mcFloat);
                        Assert.AreEqual(12.23f, mcFloat);
                        bitsRead += 32;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadFloat(out mcFloat);
                        Assert.AreEqual(12.23f, mcFloat);
                        bitsRead += 32;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        long lung;
                        builder.ReadLong(out lung, 10);
                        Assert.AreEqual((123435678957438UL << (64 - 10)) >> (64 - 10), lung);
                        bitsRead += 10;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadLong(out lung, 20);
                        Assert.AreEqual(1048575, lung);
                        bitsRead += 20;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadLong(out lung, 30);
                        Assert.AreEqual(1073741823, lung);
                        bitsRead += 30;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadLong(out lung, 40);
                        Assert.AreEqual(1099511627775, lung);
                        bitsRead += 40;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadLong(out lung, 50);
                        Assert.AreEqual(1125899906842623, lung);
                        bitsRead += 50;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadLong(out lung, 60);
                        Assert.AreEqual(1152921504606846975, lung);
                        bitsRead += 60;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadLong(out lung);
                        Assert.AreEqual(123435678957438UL, lung);
                        bitsRead += 64;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);


                        uint under;
                        builder.ReadUInt(out under, 5);
                        Assert.AreEqual(31, under);
                        bitsRead += 5;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadUInt(out under, 10);
                        Assert.AreEqual(1023, under);
                        bitsRead += 10;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadUInt(out under, 15);
                        Assert.AreEqual(32767, under);
                        bitsRead += 15;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadUInt(out under, 20);
                        Assert.AreEqual(1048575, under);
                        bitsRead += 20;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadUInt(out under, 25);
                        Assert.AreEqual(33554431, under);
                        bitsRead += 25;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadUInt(out under, 30);
                        Assert.AreEqual(268435455, under);
                        bitsRead += 30;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);

                        builder.ReadUInt(out under);
                        Assert.AreEqual(0x0f_ff_ff_ff, under);
                        bitsRead += 32;
                        Assert.AreEqual(bitsRead, builder.Position_Bits);
                    }
                }
            }

            long endTicks = DateTime.UtcNow.Ticks;
            Debug.Log(nameof(BitByBitByteArrayBuilder_WriteTest) + " duration(ms): " + TimeSpan.FromTicks(endTicks - startTicks).TotalMilliseconds);
        }

        [Test]
        public void BitByBitByteArrayBuilder_WriteTest_PERF()
        {
            long startTicks = DateTime.UtcNow.Ticks;
            for (int iOuter = 0; iOuter < OUTER_LOOPS; ++iOuter)
            {
                byte[] output = null;

                using (BitByBitByteArrayBuilder builder = BitByBitByteArrayBuilder.GetBuilder())
                {

                    for (int i = 0; i < INNER_LOOPS; ++i)
                    {
                        builder.WriteBit(true);

                        builder.WriteBits(0xff, 1);
                        builder.WriteBits(0xff, 2);
                        builder.WriteBits(0xff, 3);
                        builder.WriteBits(0xff, 4);
                        builder.WriteBits(0xff, 5);
                        builder.WriteBits(0xff, 6);
                        builder.WriteBits(0xff, 7);

                        builder.WriteByte(0x12);

                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);

                        builder.WriteLong(123435678957438L, 10);
                        builder.WriteLong(123435678957438L, 20);
                        builder.WriteLong(123435678957438L, 30);
                        builder.WriteLong(123435678957438L, 40);
                        builder.WriteLong(123435678957438L, 50);
                        builder.WriteLong(123435678957438L, 60);
                        builder.WriteLong(123435678957438L);

                        builder.WriteUInt(33233, 5);
                        builder.WriteUInt(33233, 10);
                        builder.WriteUInt(33233, 15);
                        builder.WriteUInt(33233, 20);
                        builder.WriteUInt(33233, 25);
                        builder.WriteUInt(33233, 30);
                        builder.WriteUInt(33233);
                    }

                    builder.WriteCurrentPartialByte();

                    output = builder.GetBuffer();
                }

                using (BitByBitByteArrayBuilder builder = BitByBitByteArrayBuilder.GetBuilder_WithNewData(output, output.Length))
                {
                    for (int i = 0; i < INNER_LOOPS; ++i)
                    {
                        bool yerp;
                        builder.ReadBit(out yerp);

                        byte bitten;
                        builder.ReadBits(out bitten, 1);
                        builder.ReadBits(out bitten, 2);
                        builder.ReadBits(out bitten, 3);
                        builder.ReadBits(out bitten, 4);
                        builder.ReadBits(out bitten, 5);
                        builder.ReadBits(out bitten, 6);
                        builder.ReadBits(out bitten, 7);

                        byte bitito = (byte)builder.ReadByte();

                        float mcFloat;
                        builder.ReadFloat(out mcFloat);
                        builder.ReadFloat(out mcFloat);
                        builder.ReadFloat(out mcFloat);
                        builder.ReadFloat(out mcFloat);
                        builder.ReadFloat(out mcFloat);
                        builder.ReadFloat(out mcFloat);
                        builder.ReadFloat(out mcFloat);

                        long lung;
                        builder.ReadLong(out lung, 10);
                        builder.ReadLong(out lung, 20);
                        builder.ReadLong(out lung, 30);
                        builder.ReadLong(out lung, 40);
                        builder.ReadLong(out lung, 50);
                        builder.ReadLong(out lung, 60);
                        builder.ReadLong(out lung);

                        uint under;
                        builder.ReadUInt(out under, 5);
                        builder.ReadUInt(out under, 10);
                        builder.ReadUInt(out under, 15);
                        builder.ReadUInt(out under, 20);
                        builder.ReadUInt(out under, 25);
                        builder.ReadUInt(out under, 30);
                        builder.ReadUInt(out under);
                    }
                }
            }

            long endTicks = DateTime.UtcNow.Ticks;
            Debug.Log(nameof(BitByBitByteArrayBuilder_WriteTest) + " duration(ms): " + TimeSpan.FromTicks(endTicks - startTicks).TotalMilliseconds);
        }

        [Test]
        public void BitWriter_WriteTest()
        {
            long startTicks = DateTime.UtcNow.Ticks;
            for (int iOuter = 0; iOuter < OUTER_LOOPS; ++iOuter)
            {

                byte[] data = new byte[32 * 1024];
                BitWriter builder = new BitWriter(data, data.Length);
                {

                    for (int i = 0; i < INNER_LOOPS; ++i)
                    {
                        builder.WriteBit(true);

                        builder.WriteBits(0xff, 1);
                        builder.WriteBits(0xff, 2);
                        builder.WriteBits(0xff, 3);
                        builder.WriteBits(0xff, 4);
                        builder.WriteBits(0xff, 5);
                        builder.WriteBits(0xff, 6);
                        builder.WriteBits(0xff, 7);

                        builder.WriteByte(0x12);

                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);

                        builder.WriteLong(123435678957438L, 10);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 20);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 30);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 40);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 50);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 60);
                        builder.WriteLong(123435678957438L);

                        builder.WriteUInt(0x0f_ff_ff_ff, 5);
                        builder.WriteUInt(0x0f_ff_ff_ff, 10);
                        builder.WriteUInt(0x0f_ff_ff_ff, 15);
                        builder.WriteUInt(0x0f_ff_ff_ff, 20);
                        builder.WriteUInt(0x0f_ff_ff_ff, 25);
                        builder.WriteUInt(0x0f_ff_ff_ff, 30);
                        builder.WriteUInt(0x0f_ff_ff_ff);
                    }

                    builder.FlushBits();
                }

                BitReader builderR = new BitReader(builder.Data, builder.BytesWritten);
                {
                    for (int i = 0; i < INNER_LOOPS; ++i)
                    {
                        Assert.AreEqual(1, builderR.ReadBits(1));

                        Assert.AreEqual(1, builderR.ReadBits(1));
                        Assert.AreEqual(3, builderR.ReadBits(2));
                        Assert.AreEqual(7, builderR.ReadBits(3));
                        Assert.AreEqual(15, builderR.ReadBits(4));
                        Assert.AreEqual(31, builderR.ReadBits(5));
                        Assert.AreEqual(63, builderR.ReadBits(6));
                        Assert.AreEqual(127, builderR.ReadBits(7));

                        builderR.ReadBits(8);
                        //Assert.AreEqual(0x12, (byte)builderR.ReadBits(8));

                        Assert.AreEqual(12.23f, builderR.ReadFloat());
                        Assert.AreEqual(12.23f, builderR.ReadFloat());
                        Assert.AreEqual(12.23f, builderR.ReadFloat());
                        Assert.AreEqual(12.23f, builderR.ReadFloat());
                        Assert.AreEqual(12.23f, builderR.ReadFloat());
                        Assert.AreEqual(12.23f, builderR.ReadFloat());
                        Assert.AreEqual(12.23f, builderR.ReadFloat());

                        Assert.AreEqual((123435678957438UL << (64 - 10)) >> (64 - 10), builderR.ReadLong(10));
                        Assert.AreEqual(1048575, builderR.ReadLong(20));
                        Assert.AreEqual(1073741823, builderR.ReadLong(30));
                        Assert.AreEqual(1099511627775, builderR.ReadLong(40));
                        Assert.AreEqual(1125899906842623, builderR.ReadLong(50));
                        Assert.AreEqual(1152921504606846975, builderR.ReadLong(60));
                        Assert.AreEqual(123435678957438L, builderR.ReadLong());

                        Assert.AreEqual(31, builderR.ReadUInt(5));
                        Assert.AreEqual(1023, builderR.ReadUInt(10));
                        Assert.AreEqual(32767, builderR.ReadUInt(15));
                        Assert.AreEqual(1048575, builderR.ReadUInt(20));
                        Assert.AreEqual(33554431, builderR.ReadUInt(25));
                        Assert.AreEqual(268435455, builderR.ReadUInt(30));
                        Assert.AreEqual(0x0f_ff_ff_ff, builderR.ReadUInt());
                    }
                }
            }

            long endTicks = DateTime.UtcNow.Ticks;
            Debug.Log(nameof(BitWriter_WriteTest) + " duration(ms): " + TimeSpan.FromTicks(endTicks - startTicks).TotalMilliseconds);
        }

        [Test]
        public void BitWriter_WriteTest_Individuals_PERF()
        {
            long startTicks = DateTime.UtcNow.Ticks;
            for (int iOuter = 0; iOuter < OUTER_LOOPS; ++iOuter)
            {

                byte[] data = new byte[32 * 1024];
                BitWriter builder = new BitWriter(data, data.Length);
                {

                    for (int i = 0; i < INNER_LOOPS; ++i)
                    {
                        builder.WriteBit(true);

                        builder.WriteBits(0xff, 1);
                        builder.WriteBits(0xff, 2);
                        builder.WriteBits(0xff, 3);
                        builder.WriteBits(0xff, 4);
                        builder.WriteBits(0xff, 5);
                        builder.WriteBits(0xff, 6);
                        builder.WriteBits(0xff, 7);

                        builder.WriteByte(0x12);

                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);
                        builder.WriteFloat(12.23f);

                        builder.WriteLong(123435678957438L, 10);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 20);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 30);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 40);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 50);
                        builder.WriteLong(0x0f_ff_ff_ff_ff_ff_ff_ff, 60);
                        builder.WriteLong(123435678957438L);

                        builder.WriteUInt(0x0f_ff_ff_ff, 5);
                        builder.WriteUInt(0x0f_ff_ff_ff, 10);
                        builder.WriteUInt(0x0f_ff_ff_ff, 15);
                        builder.WriteUInt(0x0f_ff_ff_ff, 20);
                        builder.WriteUInt(0x0f_ff_ff_ff, 25);
                        builder.WriteUInt(0x0f_ff_ff_ff, 30);
                        builder.WriteUInt(0x0f_ff_ff_ff);
                    }

                    builder.FlushBits();
                }

                BitReader builderR = new BitReader(builder.Data, builder.BytesWritten);
                {
                    for (int i = 0; i < INNER_LOOPS; ++i)
                    {
                        Assert.AreEqual(1, builderR.ReadBits(1));

                        builderR.ReadBits(1);
                        builderR.ReadBits(2);
                        builderR.ReadBits(3);
                        builderR.ReadBits(4);
                        builderR.ReadBits(5);
                        builderR.ReadBits(6);
                        builderR.ReadBits(7);

                        builderR.ReadBits(8);

                        builderR.ReadFloat();
                        builderR.ReadFloat();
                        builderR.ReadFloat();
                        builderR.ReadFloat();
                        builderR.ReadFloat();
                        builderR.ReadFloat();
                        builderR.ReadFloat();

                        builderR.ReadLong(10);
                        builderR.ReadLong(20);
                        builderR.ReadLong(30);
                        builderR.ReadLong(40);
                        builderR.ReadLong(50);
                        builderR.ReadLong(60);
                        builderR.ReadLong();

                        builderR.ReadUInt(5);
                        builderR.ReadUInt(10);
                        builderR.ReadUInt(15);
                        builderR.ReadUInt(20);
                        builderR.ReadUInt(25);
                        builderR.ReadUInt(30);
                        builderR.ReadUInt();
                    }
                }
            }

            long endTicks = DateTime.UtcNow.Ticks;
            Debug.Log(nameof(BitWriter_WriteTest) + " duration(ms): " + TimeSpan.FromTicks(endTicks - startTicks).TotalMilliseconds);
        }

        [Test]
        public void BitWriterReader_QualityTest_SimplestFailure()
        {
            byte[] writerData = new byte[100];
            BitWriter builder = new BitWriter(writerData, writerData.Length);

            builder.WriteBits(0x12, 4);
            builder.WriteBits(0x12, 8);

            builder.FlushBits();

            //////////////////////////// Write - Read boundary ///////////////////////////////

            BitReader builderR = new BitReader(builder.Data, builder.BytesWritten);
            
            byte byte1 = (byte)builderR.ReadBits(4);
            Assert.AreEqual(0x02, byte1);

            byte byte2 = (byte)builderR.ReadBits(8);
            Assert.AreEqual(0x12, byte2);
        }

        [Test]
        public void BitWriterReader_QualityTest_Individuals()
        {
            { // bool:
                PerformIndividualTest(true);
                PerformIndividualTest(false);
            }
            { // byte:
                for (byte i = byte.MinValue; i < byte.MaxValue; ++i)
                {
                    PerformIndividualTest(i);

                    PerformIndividualTest(i, 7);
                    PerformIndividualTest(i, 6);
                    PerformIndividualTest(i, 5);
                    PerformIndividualTest(i, 4);
                    PerformIndividualTest(i, 3);
                    PerformIndividualTest(i, 2);
                    PerformIndividualTest(i, 1);
                }
            }
            { // float:
                for (int i = 0; i < 100_000; ++i)
                {
                    PerformIndividualTest(UnityEngine.Random.Range(float.MinValue + 1000, float.MaxValue - 1000));
                }
            }
            { // uint:
                for (int i = 0; i < 100_000; ++i)
                {
                    uint rando = (uint)UnityEngine.Random.Range(0, int.MaxValue - 1000);
                    PerformIndividualTest(rando);

                    PerformIndividualTest(rando, 31);
                    PerformIndividualTest(rando, 30);
                    PerformIndividualTest(rando, 29);
                    PerformIndividualTest(rando, 28);
                    PerformIndividualTest(rando, 27);
                    PerformIndividualTest(rando, 26);
                    PerformIndividualTest(rando, 25);
                    PerformIndividualTest(rando, 24);
                    PerformIndividualTest(rando, 23);
                    PerformIndividualTest(rando, 22);
                    PerformIndividualTest(rando, 21);
                    PerformIndividualTest(rando, 20);
                    PerformIndividualTest(rando, 19);
                    PerformIndividualTest(rando, 18);
                    PerformIndividualTest(rando, 17);
                    PerformIndividualTest(rando, 16);
                    PerformIndividualTest(rando, 15);
                    PerformIndividualTest(rando, 14);
                    PerformIndividualTest(rando, 13);
                    PerformIndividualTest(rando, 12);
                    PerformIndividualTest(rando, 11);
                    PerformIndividualTest(rando, 10);
                    PerformIndividualTest(rando, 9);
                    PerformIndividualTest(rando, 8);
                    PerformIndividualTest(rando, 7);
                    PerformIndividualTest(rando, 6);
                    PerformIndividualTest(rando, 5);
                    PerformIndividualTest(rando, 4);
                    PerformIndividualTest(rando, 3);
                    PerformIndividualTest(rando, 2);
                    PerformIndividualTest(rando, 1);
                }
            }
            { // long:
                for (int i = 0; i < 100_000; ++i)
                {
                    long rando = (long)UnityEngine.Random.Range(int.MinValue, int.MaxValue) << 16;
                    PerformIndividualTest(rando);

                    PerformIndividualTest(rando, 63);
                    PerformIndividualTest(rando, 62);
                    PerformIndividualTest(rando, 61);
                    PerformIndividualTest(rando, 60);
                    PerformIndividualTest(rando, 59);
                    PerformIndividualTest(rando, 58);
                    PerformIndividualTest(rando, 57);
                    PerformIndividualTest(rando, 56);
                    PerformIndividualTest(rando, 55);
                    PerformIndividualTest(rando, 54);
                    PerformIndividualTest(rando, 53);
                    PerformIndividualTest(rando, 52);
                    PerformIndividualTest(rando, 51);
                    PerformIndividualTest(rando, 50);
                    PerformIndividualTest(rando, 49);
                    PerformIndividualTest(rando, 48);
                    PerformIndividualTest(rando, 47);
                    PerformIndividualTest(rando, 46);
                    PerformIndividualTest(rando, 45);
                    PerformIndividualTest(rando, 44);
                    PerformIndividualTest(rando, 43);
                    PerformIndividualTest(rando, 42);
                    PerformIndividualTest(rando, 41);
                    PerformIndividualTest(rando, 40);
                    PerformIndividualTest(rando, 39);
                    PerformIndividualTest(rando, 38);
                    PerformIndividualTest(rando, 37);
                    PerformIndividualTest(rando, 36);
                    PerformIndividualTest(rando, 35);
                    PerformIndividualTest(rando, 34);
                    PerformIndividualTest(rando, 33);
                    PerformIndividualTest(rando, 32);
                    PerformIndividualTest(rando, 31);
                    PerformIndividualTest(rando, 30);
                    PerformIndividualTest(rando, 29);
                    PerformIndividualTest(rando, 28);
                    PerformIndividualTest(rando, 27);
                    PerformIndividualTest(rando, 26);
                    PerformIndividualTest(rando, 25);
                    PerformIndividualTest(rando, 24);
                    PerformIndividualTest(rando, 23);
                    PerformIndividualTest(rando, 22);
                    PerformIndividualTest(rando, 21);
                    PerformIndividualTest(rando, 20);
                    PerformIndividualTest(rando, 19);
                    PerformIndividualTest(rando, 18);
                    PerformIndividualTest(rando, 17);
                    PerformIndividualTest(rando, 16);
                    PerformIndividualTest(rando, 15);
                    PerformIndividualTest(rando, 14);
                    PerformIndividualTest(rando, 13);
                    PerformIndividualTest(rando, 12);
                    PerformIndividualTest(rando, 11);
                    PerformIndividualTest(rando, 10);
                    PerformIndividualTest(rando, 9);
                    PerformIndividualTest(rando, 8);
                    PerformIndividualTest(rando, 7);
                    PerformIndividualTest(rando, 6);
                    PerformIndividualTest(rando, 5);
                    PerformIndividualTest(rando, 4);
                    PerformIndividualTest(rando, 3);
                    PerformIndividualTest(rando, 2);
                    PerformIndividualTest(rando, 1);
                }
            }
        }

        [Test]
        public void BitWriterReader_QualityTest_Group()
        {
            LinkedList<ValueTuple<object, int>> groupTest = new LinkedList<(object, int)>();

            { // bool:
                BuildGroupTest(groupTest, true);
                BuildGroupTest(groupTest, false);
            }
            { // byte:
                for (byte i = byte.MinValue; i < byte.MaxValue; ++i)
                {
                    BuildGroupTest(groupTest, i);

                    BuildGroupTest(groupTest, i, 7);
                    BuildGroupTest(groupTest, i, 6);
                    BuildGroupTest(groupTest, i, 5);
                    BuildGroupTest(groupTest, i, 4);
                    BuildGroupTest(groupTest, i, 3);
                    BuildGroupTest(groupTest, i, 2);
                    BuildGroupTest(groupTest, i, 1);
                }
            }
            { // float:
                for (int i = 0; i < 100_000; ++i)
                {
                    BuildGroupTest(groupTest, UnityEngine.Random.Range(float.MinValue + 1000, float.MaxValue - 1000));
                }
            }
            { // uint:
                for (int i = 0; i < 100_000; ++i)
                {
                    uint rando = (uint)UnityEngine.Random.Range(0, int.MaxValue - 1000);
                    BuildGroupTest(groupTest, rando);

                    BuildGroupTest(groupTest, rando, 31);
                    BuildGroupTest(groupTest, rando, 30);
                    BuildGroupTest(groupTest, rando, 29);
                    BuildGroupTest(groupTest, rando, 28);
                    BuildGroupTest(groupTest, rando, 27);
                    BuildGroupTest(groupTest, rando, 26);
                    BuildGroupTest(groupTest, rando, 25);
                    BuildGroupTest(groupTest, rando, 24);
                    BuildGroupTest(groupTest, rando, 23);
                    BuildGroupTest(groupTest, rando, 22);
                    BuildGroupTest(groupTest, rando, 21);
                    BuildGroupTest(groupTest, rando, 20);
                    BuildGroupTest(groupTest, rando, 19);
                    BuildGroupTest(groupTest, rando, 18);
                    BuildGroupTest(groupTest, rando, 17);
                    BuildGroupTest(groupTest, rando, 16);
                    BuildGroupTest(groupTest, rando, 15);
                    BuildGroupTest(groupTest, rando, 14);
                    BuildGroupTest(groupTest, rando, 13);
                    BuildGroupTest(groupTest, rando, 12);
                    BuildGroupTest(groupTest, rando, 11);
                    BuildGroupTest(groupTest, rando, 10);
                    BuildGroupTest(groupTest, rando, 9);
                    BuildGroupTest(groupTest, rando, 8);
                    BuildGroupTest(groupTest, rando, 7);
                    BuildGroupTest(groupTest, rando, 6);
                    BuildGroupTest(groupTest, rando, 5);
                    BuildGroupTest(groupTest, rando, 4);
                    BuildGroupTest(groupTest, rando, 3);
                    BuildGroupTest(groupTest, rando, 2);
                    BuildGroupTest(groupTest, rando, 1);
                }
            }
            { // long:
                for (int i = 0; i < 100_000; ++i)
                {
                    long rando = (long)UnityEngine.Random.Range(int.MinValue, int.MaxValue) << 16;
                    BuildGroupTest(groupTest, rando);

                    BuildGroupTest(groupTest, rando, 63);
                    BuildGroupTest(groupTest, rando, 62);
                    BuildGroupTest(groupTest, rando, 61);
                    BuildGroupTest(groupTest, rando, 60);
                    BuildGroupTest(groupTest, rando, 59);
                    BuildGroupTest(groupTest, rando, 58);
                    BuildGroupTest(groupTest, rando, 57);
                    BuildGroupTest(groupTest, rando, 56);
                    BuildGroupTest(groupTest, rando, 55);
                    BuildGroupTest(groupTest, rando, 54);
                    BuildGroupTest(groupTest, rando, 53);
                    BuildGroupTest(groupTest, rando, 52);
                    BuildGroupTest(groupTest, rando, 51);
                    BuildGroupTest(groupTest, rando, 50);
                    BuildGroupTest(groupTest, rando, 49);
                    BuildGroupTest(groupTest, rando, 48);
                    BuildGroupTest(groupTest, rando, 47);
                    BuildGroupTest(groupTest, rando, 46);
                    BuildGroupTest(groupTest, rando, 45);
                    BuildGroupTest(groupTest, rando, 44);
                    BuildGroupTest(groupTest, rando, 43);
                    BuildGroupTest(groupTest, rando, 42);
                    BuildGroupTest(groupTest, rando, 41);
                    BuildGroupTest(groupTest, rando, 40);
                    BuildGroupTest(groupTest, rando, 39);
                    BuildGroupTest(groupTest, rando, 38);
                    BuildGroupTest(groupTest, rando, 37);
                    BuildGroupTest(groupTest, rando, 36);
                    BuildGroupTest(groupTest, rando, 35);
                    BuildGroupTest(groupTest, rando, 34);
                    BuildGroupTest(groupTest, rando, 33);
                    BuildGroupTest(groupTest, rando, 32);
                    BuildGroupTest(groupTest, rando, 31);
                    BuildGroupTest(groupTest, rando, 30);
                    BuildGroupTest(groupTest, rando, 29);
                    BuildGroupTest(groupTest, rando, 28);
                    BuildGroupTest(groupTest, rando, 27);
                    BuildGroupTest(groupTest, rando, 26);
                    BuildGroupTest(groupTest, rando, 25);
                    BuildGroupTest(groupTest, rando, 24);
                    BuildGroupTest(groupTest, rando, 23);
                    BuildGroupTest(groupTest, rando, 22);
                    BuildGroupTest(groupTest, rando, 21);
                    BuildGroupTest(groupTest, rando, 20);
                    BuildGroupTest(groupTest, rando, 19);
                    BuildGroupTest(groupTest, rando, 18);
                    BuildGroupTest(groupTest, rando, 17);
                    BuildGroupTest(groupTest, rando, 16);
                    BuildGroupTest(groupTest, rando, 15);
                    BuildGroupTest(groupTest, rando, 14);
                    BuildGroupTest(groupTest, rando, 13);
                    BuildGroupTest(groupTest, rando, 12);
                    BuildGroupTest(groupTest, rando, 11);
                    BuildGroupTest(groupTest, rando, 10);
                    BuildGroupTest(groupTest, rando, 9);
                    BuildGroupTest(groupTest, rando, 8);
                    BuildGroupTest(groupTest, rando, 7);
                    BuildGroupTest(groupTest, rando, 6);
                    BuildGroupTest(groupTest, rando, 5);
                    BuildGroupTest(groupTest, rando, 4);
                    BuildGroupTest(groupTest, rando, 3);
                    BuildGroupTest(groupTest, rando, 2);
                    BuildGroupTest(groupTest, rando, 1);
                }
            }

            PerformGroupTest(groupTest);
        }

        private void BuildGroupTest(LinkedList<ValueTuple<object, int>> addToGroupTest, object value, int bitCount = 0)
        {
            addToGroupTest.AddLast(ValueTuple.Create(value, bitCount));
        }

        private void PerformGroupTest(LinkedList<ValueTuple<object, int>> groupTest)
        {
            const byte EIGHT = 0b_0000_1000;
            const byte BYTE_MASK = 0b_1111_1111;
            const byte THIRTY_TWO = 0b_0010_0000;
            const byte SIXTY_FOUR = 0b_0100_0000;

            const string BoolType = "System.Boolean";
            const string ByteType = "System.Byte";
            const string FloatType = "System.Single";
            const string UIntType = "System.UInt32";
            const string LongType = "System.Int64";

            byte[] writerData = new byte[100 * 1024 * 1024];
            BitWriter builder = new BitWriter(writerData, writerData.Length);

            foreach (var test in groupTest)
            {
                switch (test.Item1.GetType().FullName)
                {
                    case BoolType:
                        {
                            builder.WriteBit((bool)test.Item1);
                            break;
                        }
                    case ByteType:
                        {
                            int bitCount = test.Item2 == 0 ? EIGHT : test.Item2;
                            builder.WriteBits((byte)test.Item1, bitCount);
                            break;
                        }
                    case FloatType:
                        {
                            builder.WriteFloat((float)test.Item1);
                            break;
                        }
                    case UIntType:
                        {
                            int bitCount = test.Item2 == 0 ? THIRTY_TWO : test.Item2;
                            builder.WriteUInt((uint)test.Item1, bitCount);
                            break;
                        }
                    case LongType:
                        {
                            int bitCount = test.Item2 == 0 ? SIXTY_FOUR : test.Item2;
                            builder.WriteLong((long)test.Item1, bitCount);
                            break;
                        }
                }
            }
            builder.FlushBits();

            BitReader builderR = new BitReader(builder.Data, builder.BytesWritten);
            foreach (var test in groupTest)
            {
                switch (test.Item1.GetType().FullName)
                {
                    case BoolType:
                        {
                            bool read = (builderR.ReadBits(1) & 1) == 1 ? true : false;
                            Assert.AreEqual((bool)test.Item1, read);
                            break;
                        }
                    case ByteType:
                        {
                            int bitCount = test.Item2 == 0 ? EIGHT : test.Item2;
                            byte read = (byte)builderR.ReadBits(bitCount);
                            int throwAwayCount = EIGHT - bitCount;
                            byte valueSmaller = (byte)((((byte)test.Item1 << throwAwayCount) & BYTE_MASK) >> throwAwayCount);
                            Assert.AreEqual(valueSmaller, read);
                            break;
                        }
                    case FloatType:
                        {
                            float read = builderR.ReadFloat();
                            Assert.AreEqual((float)test.Item1, read);
                            break;
                        }
                    case UIntType:
                        {
                            int bitCount = test.Item2 == 0 ? THIRTY_TWO : test.Item2;
                            uint read = builderR.ReadUInt(bitCount);
                            int throwAwayCount = THIRTY_TWO - bitCount;
                            uint valueSmaller = ((uint)test.Item1 << throwAwayCount) >> throwAwayCount;
                            Assert.AreEqual(valueSmaller, read);
                            break;
                        }
                    case LongType:
                        {
                            int bitCount = test.Item2 == 0 ? SIXTY_FOUR : test.Item2;
                            long read = builderR.ReadLong(bitCount);
                            int throwAwayCount = SIXTY_FOUR - bitCount;
                            long valueSmaller = (long)(((ulong)((long)test.Item1) << throwAwayCount) >> throwAwayCount);
                            Assert.AreEqual(valueSmaller, read);
                            break;
                        }
                }
            }
        }

        private void PerformIndividualTest(bool value)
        {
            byte[] writerData = new byte[100];
            BitWriter builder = new BitWriter(writerData, writerData.Length);
            builder.WriteBit(value);
            builder.FlushBits();


            BitReader builderR = new BitReader(builder.Data, builder.BytesWritten);
            bool read = (builderR.ReadBits(1) & 1) == 1 ? true : false;

            Assert.AreEqual(value, read);
        }

        private void PerformIndividualTest(byte value, int bitCount = 8)
        {
            byte[] writerData = new byte[100];
            BitWriter builder = new BitWriter(writerData, writerData.Length);
            //builder.WriteByte(value);
            builder.WriteBits(value, bitCount);
            builder.FlushBits();

            const byte EIGHT = 0b_0000_1000;
            const byte BYTE_MASK = 0b_1111_1111;

            BitReader builderR = new BitReader(builder.Data, builder.BytesWritten);
            byte read = (byte)builderR.ReadBits(bitCount);

            if (bitCount == EIGHT)
            {
                Assert.AreEqual(value, read);
            }
            else if (bitCount < EIGHT)
            {
                int throwAwayCount = EIGHT - bitCount;
                byte valueSmaller = (byte)(((value << throwAwayCount) & BYTE_MASK) >> throwAwayCount);
                Assert.AreEqual(valueSmaller, read);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            }
        }

        private void PerformIndividualTest(float value)
        {
            byte[] writerData = new byte[100];
            BitWriter builder = new BitWriter(writerData, writerData.Length);
            builder.WriteFloat(value);
            builder.FlushBits();


            BitReader builderR = new BitReader(builder.Data, builder.BytesWritten);
            float read = builderR.ReadFloat();

            Assert.AreEqual(value, read);
        }

        private void PerformIndividualTest(uint value, int bitCount = 32)
        {
            byte[] writerData = new byte[100];
            BitWriter builder = new BitWriter(writerData, writerData.Length);
            builder.WriteUInt(value);
            builder.FlushBits();

            const byte THIRTY_TWO = 0b_0010_0000;

            BitReader builderR = new BitReader(builder.Data, builder.BytesWritten);
            uint read = builderR.ReadUInt(bitCount);
            if (bitCount == THIRTY_TWO)
            {
                Assert.AreEqual(value, read);
            }
            else if (bitCount < THIRTY_TWO)
            {
                int throwAwayCount = THIRTY_TWO - bitCount;
                uint valueSmaller = (value << throwAwayCount) >> throwAwayCount;
                Assert.AreEqual(valueSmaller, read);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            }
        }

        private void PerformIndividualTest(long value, int bitCount = 64)
        {
            byte[] writerData = new byte[100];
            BitWriter builder = new BitWriter(writerData, writerData.Length);
            builder.WriteLong(value);
            builder.FlushBits();

            const byte SIXTY_FOUR = 0b_0100_0000;

            BitReader builderR = new BitReader(builder.Data, builder.BytesWritten);
            long read = builderR.ReadLong(bitCount);

            if (bitCount == SIXTY_FOUR)
            {
                Assert.AreEqual(value, read);
            }
            else if (bitCount < SIXTY_FOUR)
            {
                int throwAwayCount = SIXTY_FOUR - bitCount;
                long valueSmaller = (long)(((ulong)value << throwAwayCount) >> throwAwayCount);
                Assert.AreEqual(valueSmaller, read);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            }
        }
    }
}

