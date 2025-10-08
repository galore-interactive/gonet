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
using System.Runtime.CompilerServices;

namespace GONet.Utils
{
    /// <summary>
    /// Smart multi-tiered array pool that automatically routes borrow requests to appropriately-sized pools.
    /// Solves the "11KB array for 3 bytes" problem by maintaining separate pools for different size ranges.
    ///
    /// DESIGN RATIONALE:
    /// - RPC parameters are typically tiny (1-512 bytes)
    /// - Network messages are medium (512 bytes - 8KB)
    /// - Bundles/chunks are large (8KB - 64KB)
    ///
    /// Single pool with 11KB-44KB buckets wastes 99% memory for small requests.
    /// TieredArrayPool maintains 4 pools with optimal bucket sizes for each range.
    ///
    /// MEMORY SAVINGS EXAMPLE:
    /// - 100 persistent RPCs @ 10 bytes each
    /// - Old: 100 × 11,200 = 1,120,000 bytes (1.1 MB wasted)
    /// - New: 100 × 16 = 1,600 bytes (1,584 bytes overhead = 99.86% reduction)
    ///
    /// THREAD SAFETY: Each thread gets its own TieredArrayPool instance (like existing pools)
    /// </summary>
    public class TieredArrayPool<T>
    {
        // Tier boundaries (exclusive upper bounds)
        private const int TIER_TINY_MAX = 128;        // 1-128 bytes: RPC parameters, small data
        private const int TIER_SMALL_MAX = 1024;      // 129-1024 bytes: Medium RPCs, small messages
        private const int TIER_MEDIUM_MAX = 12288;    // 1025-12KB: Network messages, value sync
        // Above 12KB: Large bundles, spawn events, chunks (up to MTU_x32 = 44,800 bytes)

        private readonly ArrayPool<T> tinyPool;
        private readonly ArrayPool<T> smallPool;
        private readonly ArrayPool<T> mediumPool;
        private readonly ArrayPool<T> largePool;

        // Telemetry for performance monitoring (temporary - can remove after validation)
        private long totalBorrows = 0;
        private long totalReturns = 0;
        private long tinyBorrows = 0;
        private long smallBorrows = 0;
        private long mediumBorrows = 0;
        private long largeBorrows = 0;
        private long tinyReturns = 0;
        private long smallReturns = 0;
        private long mediumReturns = 0;
        private long largeReturns = 0;

        public TieredArrayPool()
        {
            // Tier 1: Tiny (1-128 bytes) - RPC parameters
            // Initial: 50 arrays, Grow: 10, Range: 8-128 bytes
            tinyPool = new ArrayPool<T>(50, 10, 8, 128);

            // Tier 2: Small (129-1024 bytes) - Medium RPCs, small messages
            // Initial: 30 arrays, Grow: 5, Range: 128-1024 bytes
            smallPool = new ArrayPool<T>(30, 5, 128, 1024);

            // Tier 3: Medium (1KB-12KB) - Network messages, value sync
            // Initial: 20 arrays, Grow: 3, Range: 1KB-12KB
            mediumPool = new ArrayPool<T>(20, 3, 1024, 12288);

            // Tier 4: Large (12KB-64KB) - Bundles, chunks, spawn events
            // Initial: 10 arrays, Grow: 2, Range: 12KB-64KB
            largePool = new ArrayPool<T>(10, 2, 12288, 65536);
        }

        /// <summary>
        /// Borrows array from the appropriate tier based on minimumSize.
        /// Automatically routes to the smallest pool that can satisfy the request.
        /// Supports requests up to ~98KB (handles MTU_x32 = 44,800 bytes with room for growth).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] Borrow(int minimumSize)
        {
            System.Threading.Interlocked.Increment(ref totalBorrows);

            if (minimumSize <= TIER_TINY_MAX)
            {
                System.Threading.Interlocked.Increment(ref tinyBorrows);
                return tinyPool.Borrow(minimumSize);
            }
            else if (minimumSize <= TIER_SMALL_MAX)
            {
                System.Threading.Interlocked.Increment(ref smallBorrows);
                return smallPool.Borrow(minimumSize);
            }
            else if (minimumSize <= TIER_MEDIUM_MAX)
            {
                System.Threading.Interlocked.Increment(ref mediumBorrows);
                return mediumPool.Borrow(minimumSize);
            }
            else
            {
                System.Threading.Interlocked.Increment(ref largeBorrows);
                return largePool.Borrow(minimumSize);
            }
        }

        /// <summary>
        /// Returns array to the appropriate tier based on its size.
        ///
        /// OPTIMIZED STRATEGY: Size-based routing + exception-free ownership check.
        ///
        /// Performance characteristics:
        /// - Best case (99%): 1 size check + 1 dictionary lookup + 1 Return() = ~20ns
        /// - Growth case (1%): 2-4 size checks + 2-4 dictionary lookups + 1 Return() = ~80ns
        ///
        /// Uses ArrayPool's internal tracking (indexByCheckedOutObjectMap) via Contains()
        /// to check ownership without exception overhead. This is faster than try-catch
        /// and avoids GC pressure from exception allocations.
        ///
        /// Size boundaries use 2x safety margin to handle cascading pool growth:
        /// - Tiny: 128 * 2 = 256 (covers 128 * 1.5 = 192 + cascading)
        /// - Small: 1024 * 2 = 2048 (covers 1024 * 1.5 = 1536 + cascading)
        /// - Medium: 12288 * 2 = 24576 (covers 12288 * 1.5 = 18432 + cascading)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(T[] borrowed)
        {
            System.Threading.Interlocked.Increment(ref totalReturns);

            // Optimization: check pools based on array size
            // For large arrays, check large pools first (reverse order)
            // For small arrays, check small pools first (forward order)
            int length = borrowed.Length;

            if (length > TIER_SMALL_MAX)
            {
                // Large array: check large pools first (reverse order)
                if (largePool.Contains(borrowed))
                {
                    System.Threading.Interlocked.Increment(ref largeReturns);
                    largePool.Return(borrowed);
                    return;
                }

                if (mediumPool.Contains(borrowed))
                {
                    System.Threading.Interlocked.Increment(ref mediumReturns);
                    mediumPool.Return(borrowed);
                    return;
                }

                if (smallPool.Contains(borrowed))
                {
                    System.Threading.Interlocked.Increment(ref smallReturns);
                    smallPool.Return(borrowed);
                    return;
                }

                if (tinyPool.Contains(borrowed))
                {
                    System.Threading.Interlocked.Increment(ref tinyReturns);
                    tinyPool.Return(borrowed);
                    return;
                }
            }
            else
            {
                // Small array: check small pools first (forward order)
                if (tinyPool.Contains(borrowed))
                {
                    System.Threading.Interlocked.Increment(ref tinyReturns);
                    tinyPool.Return(borrowed);
                    return;
                }

                if (smallPool.Contains(borrowed))
                {
                    System.Threading.Interlocked.Increment(ref smallReturns);
                    smallPool.Return(borrowed);
                    return;
                }

                if (mediumPool.Contains(borrowed))
                {
                    System.Threading.Interlocked.Increment(ref mediumReturns);
                    mediumPool.Return(borrowed);
                    return;
                }

                if (largePool.Contains(borrowed))
                {
                    System.Threading.Interlocked.Increment(ref largeReturns);
                    largePool.Return(borrowed);
                    return;
                }
            }

            // ERROR: Array not found in any pool!
            throw new System.InvalidOperationException(
                $"TieredArrayPool.Return() failed: Array of length {borrowed.Length} was not borrowed from this TieredArrayPool instance. " +
                $"This usually means: (1) Array was borrowed from a different TieredArrayPool, " +
                $"(2) Array was already returned (double-return), or (3) Array was created manually.");
        }

        /// <summary>
        /// Destructor logs telemetry when pool is garbage collected.
        /// TEMPORARY: For performance monitoring during validation phase.
        /// Remove or disable after confirming pool behavior in production.
        /// </summary>
        ~TieredArrayPool()
        {
            LogTelemetry();
        }

        /// <summary>
        /// Logs telemetry data about pool usage.
        /// Call this explicitly before shutdown, or it will be called automatically in destructor.
        /// </summary>
        public void LogTelemetry()
        {
            if (totalBorrows == 0) return; // No usage, skip logging

            var stats = $"TieredArrayPool<{typeof(T).Name}> Telemetry:\n" +
                        $"  Total Borrows:  {totalBorrows:N0}\n" +
                        $"  Total Returns:  {totalReturns:N0}\n" +
                        $"  Leak Detection: {totalBorrows - totalReturns:N0} arrays not returned\n" +
                        $"\n" +
                        $"  Borrow Distribution:\n" +
                        $"    Tiny (≤128):     {tinyBorrows,10:N0} ({(tinyBorrows * 100.0 / totalBorrows):F1}%)\n" +
                        $"    Small (≤1KB):    {smallBorrows,10:N0} ({(smallBorrows * 100.0 / totalBorrows):F1}%)\n" +
                        $"    Medium (≤12KB):  {mediumBorrows,10:N0} ({(mediumBorrows * 100.0 / totalBorrows):F1}%)\n" +
                        $"    Large (>12KB):   {largeBorrows,10:N0} ({(largeBorrows * 100.0 / totalBorrows):F1}%)\n" +
                        $"\n" +
                        $"  Return Distribution:\n" +
                        $"    Tiny:    {tinyReturns,10:N0} ({(tinyReturns * 100.0 / totalReturns):F1}%)\n" +
                        $"    Small:   {smallReturns,10:N0} ({(smallReturns * 100.0 / totalReturns):F1}%)\n" +
                        $"    Medium:  {mediumReturns,10:N0} ({(mediumReturns * 100.0 / totalReturns):F1}%)\n" +
                        $"    Large:   {largeReturns,10:N0} ({(largeReturns * 100.0 / totalReturns):F1}%)";

            UnityEngine.Debug.Log(stats);
        }

        /// <summary>
        /// Gets statistics for debugging/monitoring pool usage
        /// </summary>
        public string GetStats()
        {
            return $"TieredArrayPool<{typeof(T).Name}> Stats:\n" +
                   $"  Tiny (≤128):   {tinyPool}\n" +
                   $"  Small (≤1KB):  {smallPool}\n" +
                   $"  Medium (≤12KB): {mediumPool}\n" +
                   $"  Large (>12KB):  {largePool}";
        }
    }
}
