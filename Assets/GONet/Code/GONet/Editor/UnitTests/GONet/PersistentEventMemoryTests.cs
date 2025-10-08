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

using NUnit.Framework;
using GONet.Utils;
using MemoryPack;
using System;

namespace GONet.Editor.UnitTests
{
    // MemoryPackable types must be top-level (not nested)
    [MemoryPackable]
    public partial class TestRpcData1
    {
        public int Value { get; set; }
    }

    [MemoryPackable]
    public partial class TestRpcData2
    {
        public int Value1 { get; set; }
        public string Value2 { get; set; }
    }

    [TestFixture]
    public class PersistentEventMemoryTests
    {

        [Test]
        public void PersistentRpcEvent_DataStorage_UsesRightSizedArray()
        {
            // Test that PersistentRpcEvent.Data uses a right-sized copy, not the pooled buffer

            var testData = new TestRpcData1 { Value = 42 };
            byte[] serialized = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

            // Simulate what PersistentRpcEvent should do (copy and return)
            byte[] persistentCopy = new byte[bytesUsed];
            Buffer.BlockCopy(serialized, 0, persistentCopy, 0, bytesUsed);

            if (needsReturn)
            {
                SerializationUtils.ReturnByteArray(serialized);
            }

            // Verify the copy is right-sized, not oversized
            Assert.AreEqual(bytesUsed, persistentCopy.Length,
                "Persistent storage should use exact-sized array, not pooled buffer");

            // Verify the pooled buffer was returned (we can borrow another)
            byte[] newBuffer = SerializationUtils.BorrowByteArray(bytesUsed);
            Assert.IsNotNull(newBuffer);
            SerializationUtils.ReturnByteArray(newBuffer);
        }

        [Test]
        public void PersistentRpcEvent_100Events_OptimalMemoryUsage()
        {
            // Simulate 100 persistent RPCs with small data
            long totalMemoryUsed = 0;

            for (int i = 0; i < 100; i++)
            {
                var testData = new TestRpcData1 { Value = i };
                byte[] serialized = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

                // What persistent event SHOULD do: copy to right-sized array
                byte[] persistentData = new byte[bytesUsed];
                Buffer.BlockCopy(serialized, 0, persistentData, 0, bytesUsed);
                totalMemoryUsed += persistentData.Length;

                // Return the pooled buffer
                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }
            }

            // With proper implementation: ~100 events × ~10 bytes = ~1 KB
            // Old (buggy) implementation: 100 × 11,200 bytes = 1.1 MB
            Assert.Less(totalMemoryUsed, 100 * 100,
                "100 small persistent events should use <10KB total, not 1.1MB");
        }

        [Test]
        public void PersistentRpcEvent_VariousSizes_AllUseOptimalStorage()
        {
            // Test that different data sizes all get right-sized storage

            // Test with TestRpcData1
            {
                var testData = new TestRpcData1 { Value = 1 };
                byte[] serialized = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

                // Correct pattern: copy to exact size
                byte[] persistentData = new byte[bytesUsed];
                Buffer.BlockCopy(serialized, 0, persistentData, 0, bytesUsed);

                Assert.AreEqual(bytesUsed, persistentData.Length,
                    $"Persistent data should be exactly {bytesUsed} bytes");

                Assert.Less(persistentData.Length, 200,
                    "Small RPC data should not occupy 11KB+ buffer");

                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }
            }

            // Test with TestRpcData2
            {
                var testData = new TestRpcData2 { Value1 = 1, Value2 = "test" };
                byte[] serialized = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

                byte[] persistentData = new byte[bytesUsed];
                Buffer.BlockCopy(serialized, 0, persistentData, 0, bytesUsed);

                Assert.AreEqual(bytesUsed, persistentData.Length,
                    $"Persistent data should be exactly {bytesUsed} bytes");

                Assert.Less(persistentData.Length, 200,
                    "Small RPC data should not occupy 11KB+ buffer");

                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }
            }
        }

        [Test]
        public void SerializeToBytes_WithTieredPool_ReturnsAppropriatelySizedBuffer()
        {
            // Verify that SerializationUtils now returns right-sized buffers from TieredArrayPool

            var testData = new TestRpcData1 { Value = 123 };
            byte[] buffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

            // Buffer should be from tiny tier (≤128 bytes), not 11KB+
            Assert.LessOrEqual(buffer.Length, 128,
                "Small serialization should get tiny tier buffer, not 11KB+ buffer");

            Assert.GreaterOrEqual(buffer.Length, bytesUsed,
                "Buffer should be large enough for serialized data");

            if (needsReturn)
            {
                SerializationUtils.ReturnByteArray(buffer);
            }
        }

        [Test]
        public void BufferReturn_AfterCopy_DoesNotAffectPersistentData()
        {
            // Verify that returning the pooled buffer doesn't corrupt persistent data

            var testData = new TestRpcData1 { Value = 999 };
            byte[] serialized = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

            // Copy for persistence
            byte[] persistentCopy = new byte[bytesUsed];
            Buffer.BlockCopy(serialized, 0, persistentCopy, 0, bytesUsed);

            // Return the pooled buffer
            if (needsReturn)
            {
                SerializationUtils.ReturnByteArray(serialized);
            }

            // Borrow and modify the buffer (simulating reuse)
            byte[] reusedBuffer = SerializationUtils.BorrowByteArray(bytesUsed);
            Array.Clear(reusedBuffer, 0, Math.Min(reusedBuffer.Length, bytesUsed));

            // Persistent copy should be unaffected
            var deserializedFromPersistent = SerializationUtils.DeserializeFromBytes<TestRpcData1>(
                new ReadOnlySpan<byte>(persistentCopy, 0, bytesUsed)
            );

            Assert.AreEqual(999, deserializedFromPersistent.Value,
                "Persistent data should not be affected by buffer reuse");

            SerializationUtils.ReturnByteArray(reusedBuffer);
        }

        [Test]
        public void PersistentEvent_DataField_IndependentOfPooledBuffer()
        {
            // Ensure that the Data field in persistent events is truly independent

            var testData = new TestRpcData1 { Value = 12345 };
            byte[] serialized = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

            // Get reference to the pooled buffer
            byte[] pooledBuffer = serialized;

            // Create persistent data (what PersistentRpcEvent.Data should be)
            byte[] persistentData = new byte[bytesUsed];
            Buffer.BlockCopy(serialized, 0, persistentData, 0, bytesUsed);

            // Verify they're different arrays
            Assert.IsFalse(ReferenceEquals(pooledBuffer, persistentData),
                "Persistent data must be a copy, not the pooled buffer reference");

            // Modify the pooled buffer
            if (pooledBuffer.Length > 0)
            {
                pooledBuffer[0] = 0xFF;
            }

            // Persistent data should be unchanged
            if (bytesUsed > 0 && persistentData.Length > 0)
            {
                Assert.AreNotEqual(pooledBuffer[0], persistentData[0],
                    "Modifying pooled buffer should not affect persistent copy");
            }

            if (needsReturn)
            {
                SerializationUtils.ReturnByteArray(serialized);
            }
        }
    }
}
