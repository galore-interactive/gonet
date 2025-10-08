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
using System.Collections.Generic;

namespace GONet.Editor.UnitTests.Utils
{
    [TestFixture]
    public class ArrayPoolTests
    {
        [Test]
        public void BorrowReturn_BasicOperation_Success()
        {
            // Test basic borrow/return cycle
            var pool = new ArrayPool<byte>(initialSize: 10, growByCount: 5, arraySizeMinimum: 8, arraySizeMaximum: 128);

            byte[] borrowed = pool.Borrow(10);

            Assert.NotNull(borrowed, "Borrowed array should not be null");
            Assert.GreaterOrEqual(borrowed.Length, 10, "Borrowed array should satisfy minimum size");

            // Should not throw
            pool.Return(borrowed);
        }

        [Test]
        public void Contains_BeforeBorrow_ReturnsFalse()
        {
            // Test Contains() before borrowing
            var pool = new ArrayPool<byte>(10, 5, 8, 128);
            var externalArray = new byte[16];

            Assert.IsFalse(pool.Contains(externalArray), "External array should not be in pool");
        }

        [Test]
        public void Contains_AfterBorrow_ReturnsTrue()
        {
            // Test Contains() immediately after borrowing
            var pool = new ArrayPool<byte>(10, 5, 8, 128);

            byte[] borrowed = pool.Borrow(10);

            Assert.IsTrue(pool.Contains(borrowed), "Borrowed array should be tracked by pool");
        }

        [Test]
        public void Contains_AfterReturn_ReturnsFalse()
        {
            // Test Contains() after returning - this is the critical behavior!
            var pool = new ArrayPool<byte>(10, 5, 8, 128);

            byte[] borrowed = pool.Borrow(10);
            Assert.IsTrue(pool.Contains(borrowed), "Should be tracked after borrow");

            pool.Return(borrowed);
            Assert.IsFalse(pool.Contains(borrowed), "Should NOT be tracked after return");
        }

        [Test]
        public void Return_SameArrayTwice_ThrowsException()
        {
            // Test that double-return throws exception
            var pool = new ArrayPool<byte>(10, 5, 8, 128);

            byte[] borrowed = pool.Borrow(10);
            pool.Return(borrowed);

            // Second return should throw
            Assert.Throws<NotBorrowedFromPoolException>(() => pool.Return(borrowed),
                "Returning same array twice should throw NotBorrowedFromPoolException");
        }

        [Test]
        public void Return_ExternalArray_ThrowsException()
        {
            // Test that returning non-borrowed array throws exception
            var pool = new ArrayPool<byte>(10, 5, 8, 128);
            var externalArray = new byte[16];

            Assert.Throws<NotBorrowedFromPoolException>(() => pool.Return(externalArray),
                "Returning external array should throw NotBorrowedFromPoolException");
        }

        [Test]
        public void BorrowReturn_MultipleArrays_AllTrackedCorrectly()
        {
            // Test tracking multiple borrowed arrays
            var pool = new ArrayPool<byte>(10, 5, 8, 128);
            var borrowed = new List<byte[]>();

            // Borrow 5 arrays
            for (int i = 0; i < 5; i++)
            {
                borrowed.Add(pool.Borrow(10));
            }

            // All should be tracked
            foreach (var array in borrowed)
            {
                Assert.IsTrue(pool.Contains(array), "All borrowed arrays should be tracked");
            }

            // Return first 2
            pool.Return(borrowed[0]);
            pool.Return(borrowed[1]);

            // First 2 should NOT be tracked
            Assert.IsFalse(pool.Contains(borrowed[0]), "Returned array 0 should not be tracked");
            Assert.IsFalse(pool.Contains(borrowed[1]), "Returned array 1 should not be tracked");

            // Last 3 still tracked
            Assert.IsTrue(pool.Contains(borrowed[2]), "Array 2 should still be tracked");
            Assert.IsTrue(pool.Contains(borrowed[3]), "Array 3 should still be tracked");
            Assert.IsTrue(pool.Contains(borrowed[4]), "Array 4 should still be tracked");

            // Return remaining
            pool.Return(borrowed[2]);
            pool.Return(borrowed[3]);
            pool.Return(borrowed[4]);

            // None should be tracked
            foreach (var array in borrowed)
            {
                Assert.IsFalse(pool.Contains(array), "No returned arrays should be tracked");
            }
        }

        [Test]
        public void PoolGrowth_BorrowBeyondInitialCapacity_CreatesNewArrays()
        {
            // Test pool growth behavior
            var pool = new ArrayPool<byte>(initialSize: 2, growByCount: 2, arraySizeMinimum: 8, arraySizeMaximum: 128);
            var borrowed = new List<byte[]>();

            // Borrow more than initial capacity (should trigger growth)
            for (int i = 0; i < 5; i++)
            {
                borrowed.Add(pool.Borrow(10));
            }

            // All should be tracked
            foreach (var array in borrowed)
            {
                Assert.IsTrue(pool.Contains(array), $"Borrowed array should be tracked even after growth");
            }

            // Return all
            foreach (var array in borrowed)
            {
                pool.Return(array);
            }
        }

        [Test]
        public void PoolGrowth_RequestLargerSize_CreatesLargerArray()
        {
            // Test that requesting larger size causes pool to grow its size range
            var pool = new ArrayPool<byte>(initialSize: 5, growByCount: 3, arraySizeMinimum: 8, arraySizeMaximum: 128);

            // First borrow - should get array in [8, 128] range
            byte[] small = pool.Borrow(10);
            Assert.LessOrEqual(small.Length, 128, "Initial borrow should respect max");

            // Request 200 bytes (beyond initial max of 128)
            // This should cause pool to grow its size range
            byte[] large = pool.Borrow(200);
            Assert.GreaterOrEqual(large.Length, 200, "Should satisfy requested size");

            // Both should be tracked
            Assert.IsTrue(pool.Contains(small), "Small array should be tracked");
            Assert.IsTrue(pool.Contains(large), "Large array should be tracked");

            // Return both
            pool.Return(small);
            pool.Return(large);

            // Neither should be tracked after return
            Assert.IsFalse(pool.Contains(small), "Small array should not be tracked after return");
            Assert.IsFalse(pool.Contains(large), "Large array should not be tracked after return");
        }

        [Test]
        public void BorrowReturn_RandomSizing_ConsistentTracking()
        {
            // Test with various sizes to ensure tracking is consistent
            var pool = new ArrayPool<byte>(10, 5, 8, 128);
            var sizes = new[] { 10, 50, 100, 128 };

            foreach (var size in sizes)
            {
                byte[] borrowed = pool.Borrow(size);
                Assert.IsTrue(pool.Contains(borrowed), $"Size {size}: Should be tracked after borrow");

                pool.Return(borrowed);
                Assert.IsFalse(pool.Contains(borrowed), $"Size {size}: Should NOT be tracked after return");
            }
        }

        [Test]
        public void BorrowReturn_ReuseReturnedArray_NewBorrowGetsRecycledArray()
        {
            // Test that returned arrays are reused
            var pool = new ArrayPool<byte>(5, 3, 8, 128);

            byte[] first = pool.Borrow(10);
            pool.Return(first);

            byte[] second = pool.Borrow(10);

            // Should get same array back (pooling working)
            Assert.AreSame(first, second, "Pool should reuse returned array");
            Assert.IsTrue(pool.Contains(second), "Re-borrowed array should be tracked");

            pool.Return(second);
        }

        [Test]
        public void Contains_NullArray_ReturnsFalse()
        {
            // Test Contains() with null
            var pool = new ArrayPool<byte>(10, 5, 8, 128);

            Assert.IsFalse(pool.Contains(null), "Contains(null) should return false");
        }

        [Test]
        public void StressTest_ManyBorrowReturnCycles_NoExceptions()
        {
            // Stress test: many rapid borrow/return cycles
            var pool = new ArrayPool<byte>(10, 5, 8, 128);

            for (int cycle = 0; cycle < 100; cycle++)
            {
                var borrowed = new List<byte[]>();

                // Borrow 10 arrays
                for (int i = 0; i < 10; i++)
                {
                    borrowed.Add(pool.Borrow(10));
                }

                // All should be tracked
                foreach (var array in borrowed)
                {
                    Assert.IsTrue(pool.Contains(array), $"Cycle {cycle}: Array should be tracked");
                }

                // Return all
                foreach (var array in borrowed)
                {
                    pool.Return(array);
                }

                // None should be tracked
                foreach (var array in borrowed)
                {
                    Assert.IsFalse(pool.Contains(array), $"Cycle {cycle}: Array should not be tracked after return");
                }
            }
        }
    }
}
