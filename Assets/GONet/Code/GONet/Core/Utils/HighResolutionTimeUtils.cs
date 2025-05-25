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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace GONet.Utils
{
    /// <summary>
    /// Ultra-high-performance monotonic timer with minimal overhead.
    /// Optimized for frequent calls (millions per second).
    /// Provides high-resolution timing utilities with precision exceeding <see cref="DateTime"/> (~15ms).
    /// </summary>
    public static class HighResolutionTimeUtils
    {
        // Stopwatch is already highly optimized and uses QPC on Windows
        private static readonly Stopwatch stopwatch = Stopwatch.StartNew();

        // Pack related fields together for better cache locality
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TimeState
        {
            public long BaseLocalTicks;
            public long BaseUtcTicks;
            public long BaseStopwatchTicks;
            public long LastLocalTicks;
            public long LastUtcTicks;
        }

        // Single cache line for all time data (64 bytes on x64)
        private static TimeState timeState;

        // Separate cache line for resync data to avoid false sharing
        [StructLayout(LayoutKind.Explicit, Size = 64)]
        private struct ResyncState
        {
            [FieldOffset(0)] public long LastResyncStopwatchTicks;
            [FieldOffset(8)] public long ResyncCount;
            [FieldOffset(16)] public int IsResyncing;
        }

        private static ResyncState resyncState;

        // Constants
        private const long RESYNC_INTERVAL_TICKS = 10L * TimeSpan.TicksPerSecond; // 10 seconds
        private const long MAX_DRIFT_TICKS = 1L * TimeSpan.TicksPerSecond; // 1 second max drift

        // Thread-local cache to reduce contention
        [ThreadStatic] private static long threadLocalLastCheck;
        [ThreadStatic] private static long threadLocalLastLocalTicks;
        [ThreadStatic] private static long threadLocalLastUtcTicks;

        static HighResolutionTimeUtils()
        {
            Initialize();
        }

        private static void Initialize()
        {
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;
            var swTicks = stopwatch.ElapsedTicks;

            timeState = new TimeState
            {
                BaseLocalTicks = now.Ticks,
                BaseUtcTicks = utcNow.Ticks,
                BaseStopwatchTicks = swTicks,
                LastLocalTicks = now.Ticks,
                LastUtcTicks = utcNow.Ticks
            };

            resyncState.LastResyncStopwatchTicks = swTicks;

            UnityEngine.Debug.Log("=== HighResolutionTimeUtils STATIC CONSTRUCTOR ===");
            UnityEngine.Debug.Log($"Start time ticks: {utcNow.Ticks}, timeState.BaseUtcTicks: {timeState.BaseUtcTicks}");
            UnityEngine.Debug.Log($"Start time as DateTime: {new DateTime(utcNow.Ticks)}");
            UnityEngine.Debug.Log($"Stopwatch.IsHighResolution: {Stopwatch.IsHighResolution}");
            UnityEngine.Debug.Log($"Stopwatch.Frequency: {Stopwatch.Frequency}");
        }

        /// <summary>
        /// Gets the current UTC time. Optimized for maximum performance.
        /// </summary>
        public static DateTime UtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Fast path - use thread-local cache if very recent
                long swTicks = stopwatch.ElapsedTicks;
                if ((swTicks - threadLocalLastCheck) < TimeSpan.TicksPerMillisecond)
                {
                    return new DateTime(threadLocalLastUtcTicks, DateTimeKind.Utc);
                }

                return GetTimeCore(true, swTicks);
            }
        }

        /// <summary>
        /// Gets the current local time. Optimized for maximum performance.
        /// </summary>
        public static DateTime Now
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Fast path - use thread-local cache if very recent
                long swTicks = stopwatch.ElapsedTicks;
                if ((swTicks - threadLocalLastCheck) < TimeSpan.TicksPerMillisecond)
                {
                    return new DateTime(threadLocalLastLocalTicks, DateTimeKind.Local);
                }

                return GetTimeCore(false, swTicks);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime GetTimeCore(bool useUtc, long swTicks)
        {
            // Update thread-local check time
            threadLocalLastCheck = swTicks;

            // Check if resync needed (branch prediction will optimize this)
            long lastResync = Volatile.Read(ref resyncState.LastResyncStopwatchTicks);
            if (swTicks - lastResync > RESYNC_INTERVAL_TICKS)
            {
                TryResyncFast();
            }

            // Calculate time with minimal operations
            TimeState state = timeState; // Struct copy for atomicity
            long elapsed = swTicks - state.BaseStopwatchTicks;
            long calculatedTicks = useUtc ?
                state.BaseUtcTicks + elapsed :
                state.BaseLocalTicks + elapsed;

            // Ensure monotonic (branchless using Math.Max)
            long lastTicks = useUtc ? state.LastUtcTicks : state.LastLocalTicks;
            calculatedTicks = Math.Max(calculatedTicks, lastTicks);

            // Update state if we advanced (likely path)
            if (calculatedTicks > lastTicks)
            {
                if (useUtc)
                {
                    // Try to update, but don't retry on contention
                    Interlocked.CompareExchange(ref timeState.LastUtcTicks, calculatedTicks, lastTicks);
                    threadLocalLastUtcTicks = calculatedTicks;
                }
                else
                {
                    Interlocked.CompareExchange(ref timeState.LastLocalTicks, calculatedTicks, lastTicks);
                    threadLocalLastLocalTicks = calculatedTicks;
                }
            }

            return new DateTime(calculatedTicks, useUtc ? DateTimeKind.Utc : DateTimeKind.Local);
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Keep resync out of hot path
        private static void TryResyncFast()
        {
            // Quick non-blocking attempt
            if (Interlocked.CompareExchange(ref resyncState.IsResyncing, 1, 0) == 0)
            {
                try
                {
                    // Minimal resync - no drift correction for max performance
                    var swTicks = stopwatch.ElapsedTicks;

                    // Only update resync timestamp
                    Volatile.Write(ref resyncState.LastResyncStopwatchTicks, swTicks);
                    Interlocked.Increment(ref resyncState.ResyncCount);
                }
                finally
                {
                    Volatile.Write(ref resyncState.IsResyncing, 0);
                }
            }
        }

        /// <summary>
        /// High-performance bulk time generation for benchmarking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBulkTimes(Span<long> ticks, bool useUtc)
        {
            long swTicks = stopwatch.ElapsedTicks;
            TimeState state = timeState;
            long baseTicks = useUtc ? state.BaseUtcTicks : state.BaseLocalTicks;
            long baseSwTicks = state.BaseStopwatchTicks;

            // Vectorized operation for bulk requests
            for (int i = 0; i < ticks.Length; i++)
            {
                ticks[i] = baseTicks + (swTicks - baseSwTicks) + i;
            }
        }
    }
}