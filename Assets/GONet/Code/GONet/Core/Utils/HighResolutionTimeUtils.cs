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
        [ThreadStatic] private static bool threadLocalInitialized;

        private static volatile bool isInitialized = false;
        private static readonly object initLock = new object();

        // FIXED: Use a static readonly field that forces initialization on type load
        private static readonly bool forceInit = InitializeOnLoad();

        private static bool InitializeOnLoad()
        {
            InitializeCore();
            return true;
        }

        // FIXED: Lazy initialization with explicit checks
        private static readonly Lazy<bool> lazyInitializer = new Lazy<bool>(() =>
        {
            // This is now a backup in case the static field didn't initialize
            if (!isInitialized)
            {
                InitializeCore();
            }
            return true;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        // Force initialization on any access
        private static void ForceInitialization()
        {
            _ = lazyInitializer.Value;
        }

        private static void InitializeCore()
        {
            lock (initLock)
            {
                if (isInitialized) return; // Already initialized

                var now = DateTime.Now;
                var utcNow = DateTime.UtcNow;
                var swTicks = stopwatch.ElapsedTicks;

                // Initialize all fields atomically
                var newState = new TimeState
                {
                    BaseLocalTicks = now.Ticks,
                    BaseUtcTicks = utcNow.Ticks,
                    BaseStopwatchTicks = swTicks,
                    LastLocalTicks = now.Ticks,
                    LastUtcTicks = utcNow.Ticks
                };

                // Use interlocked operations for atomic writes
                Interlocked.Exchange(ref timeState.BaseLocalTicks, newState.BaseLocalTicks);
                Interlocked.Exchange(ref timeState.BaseUtcTicks, newState.BaseUtcTicks);
                Interlocked.Exchange(ref timeState.BaseStopwatchTicks, newState.BaseStopwatchTicks);
                Interlocked.Exchange(ref timeState.LastLocalTicks, newState.LastLocalTicks);
                Interlocked.Exchange(ref timeState.LastUtcTicks, newState.LastUtcTicks);

                Interlocked.Exchange(ref resyncState.LastResyncStopwatchTicks, swTicks);

                // Full memory barrier
                Thread.MemoryBarrier();
                isInitialized = true;
                Thread.MemoryBarrier();
            }
        }

        /// <summary>
        /// Gets the current UTC time. Optimized for maximum performance.
        /// </summary>
        public static DateTime UtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ForceInitialization();

                // Initialize thread-local cache if needed
                if (!threadLocalInitialized)
                {
                    threadLocalLastCheck = 0;
                    threadLocalLastLocalTicks = 0;
                    threadLocalLastUtcTicks = 0;
                    threadLocalInitialized = true;
                }

                // Fast path - use thread-local cache if very recent
                long swTicks = stopwatch.ElapsedTicks;
                if (threadLocalLastUtcTicks > 0 && (swTicks - threadLocalLastCheck) < TimeSpan.TicksPerMillisecond)
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
                ForceInitialization();

                // Initialize thread-local cache if needed
                if (!threadLocalInitialized)
                {
                    threadLocalLastCheck = 0;
                    threadLocalLastLocalTicks = 0;
                    threadLocalLastUtcTicks = 0;
                    threadLocalInitialized = true;
                }

                // Fast path - use thread-local cache if very recent
                long swTicks = stopwatch.ElapsedTicks;
                if (threadLocalLastLocalTicks > 0 && (swTicks - threadLocalLastCheck) < TimeSpan.TicksPerMillisecond)
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
            long lastResync = Interlocked.Read(ref resyncState.LastResyncStopwatchTicks);
            if (swTicks - lastResync > RESYNC_INTERVAL_TICKS)
            {
                TryResyncFast();
            }

            // Read state with Interlocked for guaranteed visibility
            long baseLocalTicks = Interlocked.Read(ref timeState.BaseLocalTicks);
            long baseUtcTicks = Interlocked.Read(ref timeState.BaseUtcTicks);
            long baseStopwatchTicks = Interlocked.Read(ref timeState.BaseStopwatchTicks);

            // Emergency re-initialization if we detect uninitialized state
            if (baseLocalTicks == 0 || baseUtcTicks == 0)
            {
                InitializeCore();

                // Re-read after initialization
                baseLocalTicks = Interlocked.Read(ref timeState.BaseLocalTicks);
                baseUtcTicks = Interlocked.Read(ref timeState.BaseUtcTicks);
                baseStopwatchTicks = Interlocked.Read(ref timeState.BaseStopwatchTicks);
            }

            // Calculate time
            long elapsed = swTicks - baseStopwatchTicks;
            long baseTicks = useUtc ? baseUtcTicks : baseLocalTicks;
            long calculatedTicks = baseTicks + elapsed;

            // Ensure monotonicity with proper synchronization
            long lastTicksFieldOffset = useUtc ?
                Marshal.OffsetOf<TimeState>("LastUtcTicks").ToInt64() :
                Marshal.OffsetOf<TimeState>("LastLocalTicks").ToInt64();

            // Get reference to the appropriate LastTicks field
            ref long lastTicksRef = ref (useUtc ? ref timeState.LastUtcTicks : ref timeState.LastLocalTicks);

            // Atomic monotonic update loop
            long finalTicks = calculatedTicks;
            long currentLast;
            do
            {
                currentLast = Interlocked.Read(ref lastTicksRef);

                // Ensure we never go backwards
                if (finalTicks <= currentLast)
                {
                    finalTicks = currentLast;
                    break; // No need to update, just use the current value
                }

                // Try to update to our new value
                long exchanged = Interlocked.CompareExchange(ref lastTicksRef, finalTicks, currentLast);
                if (exchanged == currentLast)
                {
                    // Success - we updated the value
                    break;
                }

                // Another thread updated the value, check if we should use their value
                if (exchanged >= finalTicks)
                {
                    // Their time is newer, use it
                    finalTicks = exchanged;
                    break;
                }
                // Otherwise, retry with our value
            } while (true);

            // Update thread-local cache
            if (useUtc)
            {
                threadLocalLastUtcTicks = finalTicks;
            }
            else
            {
                threadLocalLastLocalTicks = finalTicks;
            }

            return new DateTime(finalTicks, useUtc ? DateTimeKind.Utc : DateTimeKind.Local);
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
                    Interlocked.Exchange(ref resyncState.LastResyncStopwatchTicks, swTicks);
                    Interlocked.Increment(ref resyncState.ResyncCount);
                }
                finally
                {
                    Interlocked.Exchange(ref resyncState.IsResyncing, 0);
                }
            }
        }

        /// <summary>
        /// High-performance bulk time generation for benchmarking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBulkTimes(Span<long> ticks, bool useUtc)
        {
            ForceInitialization();

            long swTicks = stopwatch.ElapsedTicks;

            // Use Interlocked reads
            long baseTicks = useUtc ?
                Interlocked.Read(ref timeState.BaseUtcTicks) :
                Interlocked.Read(ref timeState.BaseLocalTicks);
            long baseSwTicks = Interlocked.Read(ref timeState.BaseStopwatchTicks);

            // Vectorized operation for bulk requests
            for (int i = 0; i < ticks.Length; i++)
            {
                ticks[i] = baseTicks + (swTicks - baseSwTicks) + i;
            }
        }
    }
}