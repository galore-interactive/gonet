using NUnit.Framework;
using GONet.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GONet.Tests.Utils
{
    /// <summary>
    /// Comprehensive unit tests for RingBuffer with dynamic resizing.
    /// Tests cover basic operations, dynamic resizing, metrics tracking, thread safety, and edge cases.
    /// </summary>
    [TestFixture]
    public class RingBufferTests
    {
        #region Basic Operations Tests

        [Test]
        public void Constructor_WithDefaultSize_CreatesBufferWithCapacity2048()
        {
            var buffer = new RingBuffer<int>();
            Assert.AreEqual(2048, buffer.Capacity);
        }

        [Test]
        public void Constructor_WithCustomSize_RoundsUpToPowerOfTwo()
        {
            var buffer = new RingBuffer<int>(3000);
            Assert.AreEqual(4096, buffer.Capacity); // Next power of 2 after 3000
        }

        [Test]
        public void Constructor_WithSizeAboveMax_ClampsToMaxSize()
        {
            var buffer = new RingBuffer<int>(20000);
            Assert.AreEqual(16384, buffer.Capacity); // Max size
        }

        [Test]
        public void Constructor_WithSizeBelowMin_ClampsToMinSize()
        {
            var buffer = new RingBuffer<int>(512);
            Assert.AreEqual(1024, buffer.Capacity); // Min size
        }

        [Test]
        public void TryWrite_ToEmptyBuffer_ReturnsTrue()
        {
            var buffer = new RingBuffer<int>();
            bool result = buffer.TryWrite(42);
            Assert.IsTrue(result);
        }

        [Test]
        public void TryRead_FromEmptyBuffer_ReturnsFalse()
        {
            var buffer = new RingBuffer<int>();
            bool result = buffer.TryRead(out int value);
            Assert.IsFalse(result);
            Assert.AreEqual(default(int), value);
        }

        [Test]
        public void TryWrite_ThenRead_ReturnsCorrectValue()
        {
            var buffer = new RingBuffer<int>();
            buffer.TryWrite(42);

            bool result = buffer.TryRead(out int value);
            Assert.IsTrue(result);
            Assert.AreEqual(42, value);
        }

        [Test]
        public void TryWrite_MultipleValues_MaintainsFIFOOrder()
        {
            var buffer = new RingBuffer<int>();

            for (int i = 0; i < 10; i++)
            {
                buffer.TryWrite(i);
            }

            for (int i = 0; i < 10; i++)
            {
                buffer.TryRead(out int value);
                Assert.AreEqual(i, value);
            }
        }

        [Test]
        public void Count_TracksNumberOfItems()
        {
            var buffer = new RingBuffer<int>();

            Assert.AreEqual(0, buffer.Count);

            buffer.TryWrite(1);
            Assert.AreEqual(1, buffer.Count);

            buffer.TryWrite(2);
            Assert.AreEqual(2, buffer.Count);

            buffer.TryRead(out _);
            Assert.AreEqual(1, buffer.Count);

            buffer.TryRead(out _);
            Assert.AreEqual(0, buffer.Count);
        }

        #endregion

        #region Dynamic Resizing Tests

        [Test]
        public void DynamicResize_At75PercentCapacity_DoublesBufferSize()
        {
            var buffer = new RingBuffer<int>(1024);
            int resizeCount = 0;
            int newCapacity = 0;

            buffer.OnResized = (oldCap, newCap, count) =>
            {
                resizeCount++;
                newCapacity = newCap;
            };

            // Fill to 75% capacity (768 items in 1024 buffer)
            for (int i = 0; i < 768; i++)
            {
                buffer.TryWrite(i);
            }

            // Next write should trigger resize
            buffer.TryWrite(999);

            Assert.AreEqual(1, resizeCount, "Buffer should have resized once");
            Assert.AreEqual(2048, newCapacity, "Buffer should have doubled to 2048");
            Assert.AreEqual(2048, buffer.Capacity);
        }

        [Test]
        public void DynamicResize_WhenFull_ResizesBeforeFailingWrite()
        {
            var buffer = new RingBuffer<int>(1024);

            // Fill buffer completely (1023 items, leaving 1 slot for full detection)
            for (int i = 0; i < 1023; i++)
            {
                Assert.IsTrue(buffer.TryWrite(i));
            }

            // Next write should trigger resize instead of failing
            Assert.IsTrue(buffer.TryWrite(1023), "Write should succeed after auto-resize");
            Assert.AreEqual(2048, buffer.Capacity, "Buffer should have resized");
        }

        [Test]
        public void DynamicResize_MultipleResizes_WorksCorrectly()
        {
            var buffer = new RingBuffer<int>(1024);
            var resizeSizes = new List<int>();

            buffer.OnResized = (oldCap, newCap, count) =>
            {
                resizeSizes.Add(newCap);
            };

            // Fill past multiple resize thresholds
            for (int i = 0; i < 3000; i++)
            {
                buffer.TryWrite(i);
            }

            Assert.AreEqual(2, resizeSizes.Count, "Should have resized twice");
            Assert.AreEqual(2048, resizeSizes[0]);
            Assert.AreEqual(4096, resizeSizes[1]);
            Assert.AreEqual(4096, buffer.Capacity);
        }

        [Test]
        public void DynamicResize_AtMaxCapacity_StopsResizing()
        {
            var buffer = new RingBuffer<int>(8192); // Start near max
            int resizeCount = 0;

            buffer.OnResized = (oldCap, newCap, count) =>
            {
                resizeCount++;
            };

            // Fill to trigger one resize to max (16384)
            for (int i = 0; i < 6144; i++) // 75% of 8192
            {
                buffer.TryWrite(i);
            }

            Assert.AreEqual(16384, buffer.Capacity, "Should have resized to max capacity");
            Assert.AreEqual(1, resizeCount, "Should have resized exactly once");

            // Fill to 75% of max capacity
            for (int i = 6144; i < 12288; i++) // 75% of 16384
            {
                buffer.TryWrite(i);
            }

            // Continue adding more items - should NOT trigger any more resizes
            int resizeCountBefore = resizeCount;
            for (int i = 12288; i < 15000; i++)
            {
                buffer.TryWrite(i);
            }

            Assert.AreEqual(16384, buffer.Capacity, "Should remain at max capacity");
            Assert.AreEqual(resizeCountBefore, resizeCount, "Should not resize beyond max capacity");
        }

        [Test]
        public void DynamicResize_PreservesExistingData()
        {
            var buffer = new RingBuffer<int>(1024);

            // Write some data
            for (int i = 0; i < 800; i++)
            {
                buffer.TryWrite(i);
            }

            // Trigger resize
            for (int i = 800; i < 1000; i++)
            {
                buffer.TryWrite(i);
            }

            Assert.AreEqual(2048, buffer.Capacity, "Buffer should have resized");

            // Verify all data is intact in FIFO order
            for (int i = 0; i < 1000; i++)
            {
                Assert.IsTrue(buffer.TryRead(out int value));
                Assert.AreEqual(i, value, $"Item {i} should be intact after resize");
            }
        }

        #endregion

        #region Metrics Tests

        [Test]
        public void PeakCount_TracksMaximumCount()
        {
            var buffer = new RingBuffer<int>();

            buffer.TryWrite(1);
            Assert.AreEqual(1, buffer.PeakCount);

            buffer.TryWrite(2);
            buffer.TryWrite(3);
            Assert.AreEqual(3, buffer.PeakCount);

            buffer.TryRead(out _);
            buffer.TryRead(out _);
            Assert.AreEqual(3, buffer.PeakCount, "Peak should not decrease");

            buffer.TryWrite(4);
            buffer.TryWrite(5);
            Assert.AreEqual(3, buffer.PeakCount, "Peak remains at historical max");
        }

        [Test]
        public void ResizeCount_TracksNumberOfResizes()
        {
            var buffer = new RingBuffer<int>(1024);

            Assert.AreEqual(0, buffer.ResizeCount);

            // Trigger first resize (75% of 1024 = 768)
            for (int i = 0; i < 768; i++)
            {
                buffer.TryWrite(i);
            }

            Assert.AreEqual(1, buffer.ResizeCount, "Should resize once at 75% of 1024");
            Assert.AreEqual(2048, buffer.Capacity);

            // Trigger second resize (75% of 2048 = 1536)
            // We already have 768 items, need 768 more to reach 1536
            for (int i = 768; i < 1536; i++)
            {
                buffer.TryWrite(i);
            }

            Assert.AreEqual(2, buffer.ResizeCount, "Should resize twice at 75% of 2048");
            Assert.AreEqual(4096, buffer.Capacity);
        }

        [Test]
        public void FillPercentage_CalculatesCorrectly()
        {
            var buffer = new RingBuffer<int>(1024);

            Assert.AreEqual(0f, buffer.FillPercentage);

            buffer.TryWrite(1);
            Assert.Greater(buffer.FillPercentage, 0f);

            for (int i = 0; i < 511; i++) // ~50% of 1024
            {
                buffer.TryWrite(i);
            }

            Assert.That(buffer.FillPercentage, Is.InRange(0.49f, 0.51f), "Should be ~50%");
        }

        #endregion

        #region Thread Safety Tests

        [Test]
        public void ConcurrentOperations_SingleProducerSingleConsumer_WorksCorrectly()
        {
            var buffer = new RingBuffer<int>();
            const int itemCount = 10000;
            var consumedValues = new List<int>();
            var producerDone = false;

            // Producer thread
            var producer = Task.Run(() =>
            {
                for (int i = 0; i < itemCount; i++)
                {
                    while (!buffer.TryWrite(i))
                    {
                        Thread.Sleep(0); // Yield to consumer
                    }
                }
                producerDone = true;
            });

            // Consumer thread
            var consumer = Task.Run(() =>
            {
                int consumedCount = 0;
                while (consumedCount < itemCount)
                {
                    if (buffer.TryRead(out int value))
                    {
                        consumedValues.Add(value); // No lock needed - single consumer
                        consumedCount++;
                    }
                    else
                    {
                        Thread.Sleep(0); // Yield to producer
                        if (producerDone && buffer.Count == 0)
                        {
                            break; // Producer done and buffer empty
                        }
                    }
                }
            });

            Task.WaitAll(producer, consumer);

            Assert.AreEqual(itemCount, consumedValues.Count, "Should consume all items");

            // Verify FIFO order
            for (int i = 0; i < itemCount; i++)
            {
                Assert.AreEqual(i, consumedValues[i], $"Item {i} out of order");
            }
        }

        [Test]
        public void ConcurrentResize_DuringReadWrite_MaintainsDataIntegrity()
        {
            var buffer = new RingBuffer<int>(1024);
            const int itemCount = 5000; // Will trigger multiple resizes
            var allValuesProduced = new HashSet<int>();
            var allValuesConsumed = new HashSet<int>();
            var producerDone = false;

            var producer = Task.Run(() =>
            {
                for (int i = 0; i < itemCount; i++)
                {
                    while (!buffer.TryWrite(i))
                    {
                        Thread.Sleep(1);
                    }
                    lock (allValuesProduced)
                    {
                        allValuesProduced.Add(i);
                    }
                }
                producerDone = true;
            });

            var consumer = Task.Run(() =>
            {
                int consumed = 0;
                while (consumed < itemCount)
                {
                    if (buffer.TryRead(out int value))
                    {
                        lock (allValuesConsumed)
                        {
                            allValuesConsumed.Add(value);
                        }
                        consumed++;
                    }
                    else if (producerDone)
                    {
                        Thread.Sleep(1);
                    }
                }
            });

            Task.WaitAll(producer, consumer);

            Assert.AreEqual(itemCount, allValuesProduced.Count);
            Assert.AreEqual(itemCount, allValuesConsumed.Count);
            Assert.IsTrue(allValuesProduced.SetEquals(allValuesConsumed), "All produced values should be consumed");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void TryWrite_ReferenceType_HandlesNull()
        {
            var buffer = new RingBuffer<string>();
            Assert.IsTrue(buffer.TryWrite(null));
            Assert.IsTrue(buffer.TryRead(out string value));
            Assert.IsNull(value);
        }

        [Test]
        public void TryWrite_AfterWrapAround_WorksCorrectly()
        {
            var buffer = new RingBuffer<int>(1024);

            // Fill and empty buffer multiple times to test wrap-around
            for (int cycle = 0; cycle < 5; cycle++)
            {
                for (int i = 0; i < 500; i++)
                {
                    Assert.IsTrue(buffer.TryWrite(i + cycle * 1000));
                }

                for (int i = 0; i < 500; i++)
                {
                    Assert.IsTrue(buffer.TryRead(out int value));
                    Assert.AreEqual(i + cycle * 1000, value);
                }
            }
        }

        [Test]
        public void DynamicResize_DuringWrapAround_PreservesDataIntegrity()
        {
            var buffer = new RingBuffer<int>(1024);

            // Fill buffer to 50%, then consume half to force wrap-around
            for (int i = 0; i < 500; i++)
            {
                buffer.TryWrite(i);
            }

            // Consume 400 items (readIdx now at 400, writeIdx at 500)
            for (int i = 0; i < 400; i++)
            {
                buffer.TryRead(out int value);
                Assert.AreEqual(i, value);
            }

            // Now buffer has 100 items, indices: readIdx=400, writeIdx=500
            Assert.AreEqual(100, buffer.Count);

            // Fill to wrap around and trigger resize
            // Need to add 668 more items to hit 75% of 1024 (768 total)
            for (int i = 500; i < 1168; i++)
            {
                buffer.TryWrite(i);
            }

            // Buffer should have resized (indices were wrapped before resize)
            Assert.AreEqual(2048, buffer.Capacity, "Should have resized to 2048");

            // Verify all 768 items are intact in FIFO order
            // First 100 items are 400-499 (survived from before resize)
            for (int i = 400; i < 500; i++)
            {
                Assert.IsTrue(buffer.TryRead(out int value));
                Assert.AreEqual(i, value, $"Item {i} should be intact after resize with wrap-around");
            }

            // Next 668 items are 500-1167 (added after initial consumption)
            for (int i = 500; i < 1168; i++)
            {
                Assert.IsTrue(buffer.TryRead(out int value));
                Assert.AreEqual(i, value, $"Item {i} should be intact after resize with wrap-around");
            }

            Assert.AreEqual(0, buffer.Count, "Buffer should be empty after reading all items");
        }

        [Test]
        public void DynamicResize_WithReadIndexGreaterThanWriteIndex_WorksCorrectly()
        {
            var buffer = new RingBuffer<int>(1024);

            // Fill almost to capacity
            for (int i = 0; i < 1000; i++)
            {
                buffer.TryWrite(i);
            }

            // Consume 900 items (readIdx=900, writeIdx=1000)
            for (int i = 0; i < 900; i++)
            {
                buffer.TryRead(out _);
            }

            // Now add items to wrap writeIdx around (writeIdx will be < readIdx)
            // Add 600 items: writeIdx goes 1000->1023->0->576 (wrapped!)
            for (int i = 1000; i < 1600; i++)
            {
                buffer.TryWrite(i);
            }

            // Buffer now has: readIdx=900, writeIdx=576 (wrapped state!)
            // Count should be: 100 (900-999) + 600 (1000-1599) = 700
            Assert.AreEqual(700, buffer.Count);

            // Now trigger resize by adding to 75% threshold
            // Need 68 more items to hit 768 (75% of 1024)
            for (int i = 1600; i < 1668; i++)
            {
                buffer.TryWrite(i);
            }

            Assert.AreEqual(2048, buffer.Capacity, "Should have resized");

            // Verify all 768 items are intact in correct FIFO order
            for (int i = 900; i < 1668; i++)
            {
                Assert.IsTrue(buffer.TryRead(out int value));
                Assert.AreEqual(i, value, $"Item {i} should be intact after resize from wrapped state");
            }
        }

        [Test]
        public void DynamicResize_AtVariousIndexPositions_MaintainsOrderAndCount()
        {
            // Test resize at different index positions to catch edge cases
            var testCases = new[]
            {
                (fillCount: 200, consumeCount: 0,   description: "No wrap-around"),
                (fillCount: 600, consumeCount: 400, description: "Partial wrap-around"),
                (fillCount: 900, consumeCount: 800, description: "Near-full wrap-around"),
                (fillCount: 1000, consumeCount: 950, description: "Tight wrap-around")
            };

            foreach (var testCase in testCases)
            {
                var buffer = new RingBuffer<int>(1024);

                // Initial fill
                for (int i = 0; i < testCase.fillCount; i++)
                {
                    buffer.TryWrite(i);
                }

                // Consume some items
                for (int i = 0; i < testCase.consumeCount; i++)
                {
                    buffer.TryRead(out _);
                }

                int expectedCount = testCase.fillCount - testCase.consumeCount;
                Assert.AreEqual(expectedCount, buffer.Count, $"Initial count wrong for {testCase.description}");

                // Add items to trigger resize
                int itemsToAdd = (int)(1024 * 0.75f) - expectedCount + 10; // Exceed 75% threshold
                for (int i = testCase.fillCount; i < testCase.fillCount + itemsToAdd; i++)
                {
                    buffer.TryWrite(i);
                }

                Assert.AreEqual(2048, buffer.Capacity, $"Should have resized for {testCase.description}");

                // Verify all items are still in FIFO order
                int expectedValue = testCase.consumeCount;
                int totalExpectedCount = expectedCount + itemsToAdd;

                for (int i = 0; i < totalExpectedCount; i++)
                {
                    Assert.IsTrue(buffer.TryRead(out int value), $"Failed to read item {i} for {testCase.description}");
                    Assert.AreEqual(expectedValue++, value, $"Wrong value at position {i} for {testCase.description}");
                }

                Assert.AreEqual(0, buffer.Count, $"Buffer should be empty after test: {testCase.description}");
            }
        }

        [Test]
        public void OnResized_Callback_ReceivesCorrectParameters()
        {
            var buffer = new RingBuffer<int>(1024);
            int callbackOldCap = 0;
            int callbackNewCap = 0;
            int callbackCount = 0;

            buffer.OnResized = (oldCap, newCap, count) =>
            {
                callbackOldCap = oldCap;
                callbackNewCap = newCap;
                callbackCount = count;
            };

            // Fill to exactly 75% to trigger resize
            int targetCount = (int)(1024 * 0.75f); // 768
            for (int i = 0; i < targetCount; i++)
            {
                buffer.TryWrite(i);
            }

            // The resize happens AFTER the write that crosses 75% threshold
            // So count at resize time is 768 (the items written so far)
            Assert.AreEqual(1024, callbackOldCap);
            Assert.AreEqual(2048, callbackNewCap);
            Assert.AreEqual(768, callbackCount);
        }

        [Test]
        public void Capacity_IsPowerOfTwo_AlwaysTrue()
        {
            var sizes = new[] { 1000, 2000, 3000, 4000, 8000, 15000 };

            foreach (var size in sizes)
            {
                var buffer = new RingBuffer<int>(size);
                int capacity = buffer.Capacity;

                // Check if power of 2
                bool isPowerOfTwo = (capacity & (capacity - 1)) == 0 && capacity != 0;
                Assert.IsTrue(isPowerOfTwo, $"Capacity {capacity} is not a power of 2");
            }
        }

        #endregion

        #region Performance/Stress Tests

        [Test]
        public void StressTest_RapidWriteRead_10000Items()
        {
            var buffer = new RingBuffer<int>();

            // Rapid writes
            for (int i = 0; i < 10000; i++)
            {
                Assert.IsTrue(buffer.TryWrite(i), $"Failed to write item {i}");
            }

            // Rapid reads
            for (int i = 0; i < 10000; i++)
            {
                Assert.IsTrue(buffer.TryRead(out int value), $"Failed to read item {i}");
                Assert.AreEqual(i, value);
            }
        }

        [Test]
        public void StressTest_InterleavedWriteRead_MaintainsIntegrity()
        {
            var buffer = new RingBuffer<int>();

            for (int i = 0; i < 5000; i++)
            {
                buffer.TryWrite(i);
                buffer.TryWrite(i + 5000);

                Assert.IsTrue(buffer.TryRead(out int val1));
                Assert.AreEqual(i, val1);

                Assert.IsTrue(buffer.TryRead(out int val2));
                Assert.AreEqual(i + 5000, val2);
            }
        }

        #endregion
    }
}
