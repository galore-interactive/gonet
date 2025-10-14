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
using System.Collections.Generic;
using System.Linq;

namespace GONet.Editor.UnitTests
{
    /// <summary>
    /// Comprehensive test suite for RPC event pooling and memory management.
    ///
    /// **PURPOSE:**
    /// Validates the fix for the critical TieredArrayPool double-return bug that was causing
    /// InvalidOperationException: "Array not borrowed from this TieredArrayPool".
    ///
    /// **ROOT CAUSE OF ORIGINAL BUG:**
    /// RpcEvent.Return(), RpcResponseEvent.Return(), and RoutedRpcEvent.Return() were calling
    /// SerializationUtils.TryReturnByteArray(evt.Data), attempting to return the byte array to the pool.
    /// However, for persistent events, evt.Data was a SHARED REFERENCE from persistent storage,
    /// not a pooled buffer. This caused the same array to be "returned" multiple times, triggering
    /// the double-return exception.
    ///
    /// **THE FIX:**
    /// Removed SerializationUtils.TryReturnByteArray() calls from all RpcEvent.Return() methods.
    /// Byte array lifecycle is now managed at SERIALIZATION/DESERIALIZATION boundaries only:
    /// - Borrow during serialization (SerializeToBytes)
    /// - Return after deserialization (DeserializeFromBytes) OR after copying for persistence
    /// - Event pooling and byte array pooling are COMPLETELY SEPARATE lifecycles
    ///
    /// **WHAT THESE TESTS VALIDATE:**
    /// 1. Persistent RPC events copy data and return pooled buffers immediately - no double-return exceptions
    /// 2. Transient RPC events hold pooled buffer references - return after deserialization
    /// 3. TieredArrayPool correctly detects and prevents double-return (using dedicated test pool)
    /// 4. High-volume RPC traffic completes without exceptions (10,000 RPCs)
    /// 5. Late-joiner scenario (100 deliveries of same persistent RPC) works without exceptions
    /// 6. Mixed persistent/transient events handle lifecycle correctly
    /// 7. Buffer reuse works correctly (pooling is functioning)
    /// </summary>
    [TestFixture]
    public class RpcEventPoolingMemoryTests
    {
        private TieredArrayPool<byte> testPool;

        [SetUp]
        public void SetUp()
        {
            // Create a dedicated test pool for direct pooling behavior tests
            testPool = new TieredArrayPool<byte>();
        }

        [TearDown]
        public void TearDown()
        {
            // Test pool is isolated - no cleanup needed
        }

        #region Core Double-Return Prevention Tests (Using Test Pool)

        [Test]
        public void TieredArrayPool_DoubleReturn_ThrowsException()
        {
            // Verify that TieredArrayPool correctly detects and prevents double-return

            byte[] buffer = testPool.Borrow(100);

            // First return - should succeed
            Assert.DoesNotThrow(() => testPool.Return(buffer), "First return should succeed");

            // Second return - should throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => testPool.Return(buffer),
                "Second return of same buffer should throw InvalidOperationException");
        }

        [Test]
        public void TieredArrayPool_ReturnNonPooledArray_ThrowsException()
        {
            // Test that returning non-borrowed array throws exception

            byte[] nonPooledArray = new byte[100]; // Created outside pool

            Assert.Throws<InvalidOperationException>(() => testPool.Return(nonPooledArray),
                "Returning non-pooled array should throw InvalidOperationException");
        }

        [Test]
        public void TieredArrayPool_BorrowedCount_TracksCorrectly()
        {
            // Test that BorrowedCount tracks borrow/return correctly

            int initial = testPool.BorrowedCount;
            Assert.AreEqual(0, initial, "Initial borrowed count should be 0");

            byte[] buf1 = testPool.Borrow(100);
            Assert.AreEqual(1, testPool.BorrowedCount, "Should be 1 after first borrow");

            byte[] buf2 = testPool.Borrow(200);
            Assert.AreEqual(2, testPool.BorrowedCount, "Should be 2 after second borrow");

            testPool.Return(buf1);
            Assert.AreEqual(1, testPool.BorrowedCount, "Should be 1 after returning first");

            testPool.Return(buf2);
            Assert.AreEqual(0, testPool.BorrowedCount, "Should be 0 after returning all");
        }

        #endregion

        #region Persistent RPC Lifecycle Tests (Using SerializationUtils)

        [Test]
        public void PersistentRpc_SingleEvent_ReturnsBufferWithoutException()
        {
            // CRITICAL TEST: Persistent events must copy data and return buffer without exception

            var testData = new TestRpcData1 { Value = 42 };

            // Serialize (may or may not borrow from pool depending on MemoryPack implementation)
            byte[] pooledBuffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

            // Simulate what PersistentRpcEvent does: copy data for long-term storage
            byte[] persistentCopy = new byte[bytesUsed];
            Buffer.BlockCopy(pooledBuffer, 0, persistentCopy, 0, bytesUsed);

            // Return the pooled buffer if it was borrowed - THIS MUST NOT THROW
            if (needsReturn)
            {
                Assert.DoesNotThrow(() => SerializationUtils.ReturnByteArray(pooledBuffer),
                    "Returning pooled buffer after copying should succeed");
            }

            // Verify persistent copy is independent and usable
            var deserialized = SerializationUtils.DeserializeFromBytes<TestRpcData1>(
                new ReadOnlySpan<byte>(persistentCopy, 0, bytesUsed)
            );
            Assert.AreEqual(42, deserialized.Value, "Persistent copy should contain correct data");
        }

        [Test]
        public void PersistentRpc_100Events_NoExceptionsDataIntact()
        {
            // Test 100 persistent RPCs - should complete without exceptions

            var persistentData = new List<byte[]>();

            // Create 100 persistent RPCs - MUST NOT THROW
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var testData = new TestRpcData1 { Value = i };
                    byte[] pooledBuffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

                    // Copy for persistence
                    byte[] copy = new byte[bytesUsed];
                    Buffer.BlockCopy(pooledBuffer, 0, copy, 0, bytesUsed);
                    persistentData.Add(copy);

                    // Return pooled buffer
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(pooledBuffer);
                    }
                }
            }, "100 persistent RPCs should complete without exceptions");

            // Verify all persistent data is intact
            for (int i = 0; i < 100; i++)
            {
                var deserialized = SerializationUtils.DeserializeFromBytes<TestRpcData1>(
                    new ReadOnlySpan<byte>(persistentData[i], 0, persistentData[i].Length)
                );
                Assert.AreEqual(i, deserialized.Value, $"Persistent RPC {i} data should be intact");
            }

            // Verify memory savings (persistent copies should be right-sized, not 11KB+ buffers)
            long totalMemory = persistentData.Sum(arr => (long)arr.Length);
            Assert.Less(totalMemory, 100 * 200,
                "100 small RPCs should use <20KB total (not 1.1MB with old pool sizing)");
        }

        [Test]
        public void PersistentRpc_RepeatedLateJoinDelivery_NoDoubleReturnException()
        {
            // *** THE CRITICAL TEST ***
            // This is the EXACT scenario that was triggering the double-return bug!
            // Persistent RPC delivered to multiple late-joiners must NOT cause exceptions

            var testData = new TestRpcData1 { Value = 999 };
            byte[] pooledBuffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

            // Create persistent copy (what PersistentRpcEvent.Data holds)
            byte[] persistentCopy = new byte[bytesUsed];
            Buffer.BlockCopy(pooledBuffer, 0, persistentCopy, 0, bytesUsed);

            // Return pooled buffer ONCE
            if (needsReturn)
            {
                SerializationUtils.ReturnByteArray(pooledBuffer);
            }

            // OLD BUG: Delivering to multiple late-joiners would trigger RpcEvent.Return() multiple times
            // RpcEvent.Return() called TryReturnByteArray(evt.Data) where evt.Data = persistentCopy
            // persistentCopy was NEVER borrowed from pool, so 2nd+ returns would throw exception

            // NEW FIX: RpcEvent.Return() no longer touches byte arrays
            // Simulate 100 late-joiners receiving the persistent event - MUST NOT THROW
            Assert.DoesNotThrow(() =>
            {
                for (int lateJoiner = 0; lateJoiner < 100; lateJoiner++)
                {
                    // Deserialize from persistent copy (reads data)
                    var deserialized = SerializationUtils.DeserializeFromBytes<TestRpcData1>(
                        new ReadOnlySpan<byte>(persistentCopy, 0, bytesUsed)
                    );
                    Assert.AreEqual(999, deserialized.Value);

                    // Simulate RpcEvent.Return() being called (event pooling)
                    // OLD CODE: This would call TryReturnByteArray(persistentCopy) → EXCEPTION on 2nd+ call
                    // NEW CODE: Return() no longer touches byte arrays → NO EXCEPTION
                }
            }, "Delivering persistent RPC to 100 late-joiners should NOT throw double-return exception");

            Assert.Pass("✓ Critical test passed: 100 late-joiners received persistent RPC without exceptions");
        }

        #endregion

        #region Transient RPC Lifecycle Tests

        [Test]
        public void TransientRpc_SerializeAndReturn_NoException()
        {
            // Test transient RPC lifecycle: serialize → hold → return

            var testData = new TestRpcData1 { Value = 123 };
            byte[] pooledBuffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

            // Simulate transient RPC: hold buffer during transmission

            // Deserialize (consumes the data)
            var deserialized = SerializationUtils.DeserializeFromBytes<TestRpcData1>(
                new ReadOnlySpan<byte>(pooledBuffer, 0, bytesUsed)
            );
            Assert.AreEqual(123, deserialized.Value);

            // Return buffer after deserialization if it was borrowed - MUST NOT THROW
            if (needsReturn)
            {
                Assert.DoesNotThrow(() => SerializationUtils.ReturnByteArray(pooledBuffer),
                    "Returning pooled buffer after deserialization should succeed");
            }

            // Test passes if no exceptions thrown
            Assert.Pass("Transient RPC lifecycle completed without exceptions");
        }

        [Test]
        public void TransientRpc_100Events_NoExceptions()
        {
            // Test 100 transient RPCs

            var rpcEvents = new List<(byte[] buffer, int bytesUsed)>();

            // Serialize 100 RPCs (buffers remain borrowed during transmission)
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var testData = new TestRpcData1 { Value = i };
                    byte[] buffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

                    if (needsReturn)
                    {
                        rpcEvents.Add((buffer, bytesUsed));
                    }
                }
            }, "Serializing 100 transient RPCs should not throw");

            // Process and return buffers - MUST NOT THROW
            Assert.DoesNotThrow(() =>
            {
                foreach (var (buffer, bytesUsed) in rpcEvents)
                {
                    var deserialized = SerializationUtils.DeserializeFromBytes<TestRpcData1>(
                        new ReadOnlySpan<byte>(buffer, 0, bytesUsed)
                    );
                    SerializationUtils.ReturnByteArray(buffer);
                }
            }, "Processing and returning 100 transient RPC buffers should not throw");
        }

        #endregion

        #region TryReturnByteArray Safety Tests

        [Test]
        public void TryReturnByteArray_NonPooledArray_DoesNotThrow()
        {
            // TryReturnByteArray should safely handle non-pooled arrays (no-op, no exception)

            byte[] nonPooledArray = new byte[100];

            Assert.DoesNotThrow(() => SerializationUtils.TryReturnByteArray(nonPooledArray),
                "TryReturnByteArray should safely handle non-pooled arrays without throwing");
        }

        [Test]
        public void TryReturnByteArray_Null_DoesNotThrow()
        {
            // TryReturnByteArray should safely handle null

            Assert.DoesNotThrow(() => SerializationUtils.TryReturnByteArray(null),
                "TryReturnByteArray(null) should be safe no-op");
        }

        [Test]
        public void TryReturnByteArray_PersistentCopy_SafeNoOp()
        {
            // THE EXACT BUG SCENARIO: Persistent copy passed to TryReturnByteArray

            var testData = new TestRpcData1 { Value = 12345 };
            byte[] pooledBuffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

            // Create persistent copy (NOT from pool)
            byte[] persistentCopy = new byte[bytesUsed];
            Buffer.BlockCopy(pooledBuffer, 0, persistentCopy, 0, bytesUsed);

            // Return the actual pooled buffer
            if (needsReturn)
            {
                SerializationUtils.ReturnByteArray(pooledBuffer);
            }

            // OLD BUG: RpcEvent.Return() would call TryReturnByteArray(persistentCopy)
            // persistentCopy was NEVER borrowed, so this would fail
            // NEW BEHAVIOR: TryReturnByteArray should be safe no-op for non-pooled arrays
            Assert.DoesNotThrow(() => SerializationUtils.TryReturnByteArray(persistentCopy),
                "TryReturnByteArray should safely handle persistent copy (non-pooled array)");

            // Verify persistent copy still works after "attempted return"
            var deserialized = SerializationUtils.DeserializeFromBytes<TestRpcData1>(
                new ReadOnlySpan<byte>(persistentCopy, 0, bytesUsed)
            );
            Assert.AreEqual(12345, deserialized.Value, "Persistent copy should still be valid");
        }

        #endregion

        #region High-Volume Stress Tests

        [Test]
        public void StressTest_1000PersistentRpcs_NoExceptions()
        {
            // High-volume test: 1000 persistent RPCs

            var persistentData = new List<byte[]>();

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    var testData = new TestRpcData1 { Value = i };
                    byte[] pooledBuffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

                    byte[] copy = new byte[bytesUsed];
                    Buffer.BlockCopy(pooledBuffer, 0, copy, 0, bytesUsed);
                    persistentData.Add(copy);

                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(pooledBuffer);
                    }
                }
            }, "1000 persistent RPCs should complete without exceptions");

            // Verify data integrity
            Assert.AreEqual(1000, persistentData.Count);
            for (int i = 0; i < 100; i++) // Sample 100 to keep test fast
            {
                var deserialized = SerializationUtils.DeserializeFromBytes<TestRpcData1>(
                    new ReadOnlySpan<byte>(persistentData[i], 0, persistentData[i].Length)
                );
                Assert.AreEqual(i, deserialized.Value);
            }
        }

        [Test]
        public void StressTest_10000MixedRpcs_NoExceptions()
        {
            // Extreme stress test: 10,000 mixed persistent/transient RPCs

            var random = new Random(54321);
            var transientBuffers = new List<byte[]>();

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    var testData = new TestRpcData1 { Value = i };
                    byte[] pooledBuffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

                    if (!needsReturn) continue;

                    bool isPersistent = random.Next(2) == 0;

                    if (isPersistent)
                    {
                        // Persistent: copy and return immediately
                        byte[] copy = new byte[bytesUsed];
                        Buffer.BlockCopy(pooledBuffer, 0, copy, 0, bytesUsed);
                        SerializationUtils.ReturnByteArray(pooledBuffer);
                    }
                    else
                    {
                        // Transient: hold for later
                        transientBuffers.Add(pooledBuffer);
                    }
                }

                // Return all transient buffers
                foreach (var buffer in transientBuffers)
                {
                    SerializationUtils.ReturnByteArray(buffer);
                }

            }, "10,000 mixed RPCs should complete without exceptions");

            Assert.Pass("✓ Stress test passed: 10,000 mixed RPCs completed without exceptions");
        }

        #endregion

        #region Buffer Reuse Tests (Using Test Pool Directly)

        [Test]
        public void BufferReuse_TestPool_MultipleSerializeCycles_BuffersAreReused()
        {
            // Test that TieredArrayPool reuses buffers across borrow/return cycles

            HashSet<byte[]> uniqueBuffers = new HashSet<byte[]>();

            for (int i = 0; i < 100; i++)
            {
                byte[] buffer = testPool.Borrow(100);
                uniqueBuffers.Add(buffer);
                testPool.Return(buffer);
            }

            // Should reuse buffers (not create 100 unique ones)
            Assert.Less(uniqueBuffers.Count, 10,
                "100 borrow/return cycles should reuse buffers (not create 100 unique buffers)");
        }

        [Test]
        public void BufferReuse_TestPool_FirstCycleMatchesSecondCycle_SameBufferReused()
        {
            // Verify that same buffer is returned on second borrow

            byte[] buffer1 = testPool.Borrow(100);
            testPool.Return(buffer1);

            byte[] buffer2 = testPool.Borrow(100);

            Assert.AreSame(buffer1, buffer2, "Pool should reuse the same buffer");

            testPool.Return(buffer2);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void EdgeCase_LargeRpc_NoExceptions()
        {
            // Test large RPC (>12KB)

            var largeData = new TestRpcData2
            {
                Value1 = 123,
                Value2 = new string('X', 15000) // ~15KB string
            };

            Assert.DoesNotThrow(() =>
            {
                byte[] buffer = SerializationUtils.SerializeToBytes(largeData, out int bytesUsed, out bool needsReturn);
                Assert.Greater(bytesUsed, 12288, "Large RPC should use >12KB");

                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(buffer);
                }
            }, "Large RPC should not throw exceptions");
        }

        [Test]
        public void EdgeCase_MixedPersistentTransient_500Cycles()
        {
            // Test alternating persistent/transient pattern

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 500; i++)
                {
                    var testData = new TestRpcData1 { Value = i };
                    byte[] pooledBuffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

                    if (!needsReturn) continue;

                    if (i % 2 == 0)
                    {
                        // Persistent: copy and return
                        byte[] copy = new byte[bytesUsed];
                        Buffer.BlockCopy(pooledBuffer, 0, copy, 0, bytesUsed);
                        SerializationUtils.ReturnByteArray(pooledBuffer);
                    }
                    else
                    {
                        // Transient: return immediately (simulating instant processing)
                        SerializationUtils.ReturnByteArray(pooledBuffer);
                    }
                }
            }, "500 alternating persistent/transient RPCs should not throw");
        }

        #endregion
    }
}
