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
using UnityEngine;

namespace GONet.Utils
{
    /// <summary>
    /// Ultra-high-performance monotonic timer with minimal overhead.
    /// Optimized for frequent calls (millions per second) with special support for time sync operations.
    /// </summary>
    public static class HighResolutionTimeUtils
    {
        // Monotonic stopwatch wrapper to ensure time never goes backwards
        private static class MonotonicStopwatch
        {
            private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();
            private static long _lastElapsedTicks = 0;

            public static long ElapsedTicks
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    long ticks = _stopwatch.ElapsedTicks;

                    // Ensure monotonicity with lock-free algorithm
                    long last;
                    do
                    {
                        last = Volatile.Read(ref _lastElapsedTicks);
                        if (ticks <= last) return last;
                    } while (Interlocked.CompareExchange(ref _lastElapsedTicks, ticks, last) != last);

                    return ticks;
                }
            }
        }

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
            [FieldOffset(20)] public long LastDriftCorrection;
        }

        private static ResyncState resyncState;

        // Platform-specific configuration
        private static class PlatformConfig
        {
            public static readonly bool IsMobile = Application.isMobilePlatform;
            public static readonly bool IsWindows = Application.platform == RuntimePlatform.WindowsPlayer
                                                   || Application.platform == RuntimePlatform.WindowsEditor;
            public static readonly long ResolutionTicks = GetPlatformResolution();
            public static readonly int CacheDurationTicks = IsMobile ?
                (int)(TimeSpan.TicksPerMillisecond * 10) : // 10ms cache on mobile for battery
                (int)TimeSpan.TicksPerMillisecond;          // 1ms cache on desktop

            private static long GetPlatformResolution()
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        return TimeSpan.FromMilliseconds(15.6).Ticks; // Windows default timer
                    case RuntimePlatform.Android:
                        return TimeSpan.FromMilliseconds(10).Ticks;   // Conservative Android
                    case RuntimePlatform.IPhonePlayer:
                        return TimeSpan.FromMilliseconds(1).Ticks;    // iOS has good timers
                    default:
                        return TimeSpan.FromMilliseconds(1).Ticks;    // macOS/Linux
                }
            }
        }

        // Constants
        private const long RESYNC_INTERVAL_TICKS = 10L * TimeSpan.TicksPerSecond; // 10 seconds
        private const long MAX_DRIFT_TICKS = 1L * TimeSpan.TicksPerSecond;        // 1 second max drift
        private const double TICKS_TO_SECONDS = 1.0 / TimeSpan.TicksPerSecond;

        // Thread-local cache to reduce contention (configurable per platform)
        [ThreadStatic] private static long threadLocalLastCheck;
        [ThreadStatic] private static long threadLocalLastLocalTicks;
        [ThreadStatic] private static long threadLocalLastUtcTicks;
        [ThreadStatic] private static bool threadLocalInitialized;

        private static volatile bool isInitialized = false;
        private static readonly object initLock = new object();

        // Force initialization on type load
        private static readonly bool forceInit = InitializeOnLoad();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uMilliseconds);
#endif

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        static void InitializeShutdown()
        {
            Application.quitting += () => Shutdown();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.quitting += () => Shutdown();
#endif
        }

        private static bool InitializeOnLoad()
        {
            InitializePlatformTimer();
            InitializeCore();
            return true;
        }

        private static void InitializePlatformTimer()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Request 1ms timer resolution on Windows for better precision
            TimeBeginPeriod(1);
#elif UNITY_ANDROID
                // Android-specific initialization if needed
#elif UNITY_IOS
                // iOS generally has good timer resolution
#endif
        }

        private static void InitializeCore()
        {
            lock (initLock)
            {
                if (isInitialized) return;

                var now = DateTime.Now;
                var utcNow = DateTime.UtcNow;
                var swTicks = MonotonicStopwatch.ElapsedTicks;

                // Initialize all fields atomically
                var newState = new TimeState
                {
                    BaseLocalTicks = now.Ticks,
                    BaseUtcTicks = utcNow.Ticks,
                    BaseStopwatchTicks = swTicks,
                    LastLocalTicks = now.Ticks,
                    LastUtcTicks = utcNow.Ticks
                };

                Interlocked.Exchange(ref timeState.BaseLocalTicks, newState.BaseLocalTicks);
                Interlocked.Exchange(ref timeState.BaseUtcTicks, newState.BaseUtcTicks);
                Interlocked.Exchange(ref timeState.BaseStopwatchTicks, newState.BaseStopwatchTicks);
                Interlocked.Exchange(ref timeState.LastLocalTicks, newState.LastLocalTicks);
                Interlocked.Exchange(ref timeState.LastUtcTicks, newState.LastUtcTicks);

                Interlocked.Exchange(ref resyncState.LastResyncStopwatchTicks, swTicks);

                Thread.MemoryBarrier();
                isInitialized = true;
                Thread.MemoryBarrier();
            }
        }

        /// <summary>
        /// Gets the current UTC time. Optimized for maximum performance with platform-aware caching.
        /// </summary>
        public static DateTime UtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!isInitialized) InitializeCore();

                // Initialize thread-local cache if needed
                if (!threadLocalInitialized)
                {
                    threadLocalLastCheck = 0;
                    threadLocalLastLocalTicks = 0;
                    threadLocalLastUtcTicks = 0;
                    threadLocalInitialized = true;
                }

                // Fast path - use thread-local cache if very recent (platform-aware duration)
                long swTicks = MonotonicStopwatch.ElapsedTicks;
                if (threadLocalLastUtcTicks > 0 && (swTicks - threadLocalLastCheck) < PlatformConfig.CacheDurationTicks)
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
                if (!isInitialized) InitializeCore();

                if (!threadLocalInitialized)
                {
                    threadLocalLastCheck = 0;
                    threadLocalLastLocalTicks = 0;
                    threadLocalLastUtcTicks = 0;
                    threadLocalInitialized = true;
                }

                long swTicks = MonotonicStopwatch.ElapsedTicks;
                if (threadLocalLastLocalTicks > 0 && (swTicks - threadLocalLastCheck) < PlatformConfig.CacheDurationTicks)
                {
                    return new DateTime(threadLocalLastLocalTicks, DateTimeKind.Local);
                }

                return GetTimeCore(false, swTicks);
            }
        }

        /// <summary>
        /// Gets current time specifically for time sync operations.
        /// Bypasses all caching for maximum accuracy.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetTimeSyncTicks()
        {
            if (!isInitialized) InitializeCore();

            // Direct read, no caching for time sync accuracy
            long swTicks = MonotonicStopwatch.ElapsedTicks;
            long baseUtcTicks = Volatile.Read(ref timeState.BaseUtcTicks);
            long baseStopwatchTicks = Volatile.Read(ref timeState.BaseStopwatchTicks);

            return baseUtcTicks + (swTicks - baseStopwatchTicks);
        }

        /// <summary>
        /// Gets multiple time samples for NTP-style median filtering in time sync
        /// </summary>
        public static void GetTimeSyncSamples(Span<long> samples)
        {
            if (!isInitialized) InitializeCore();

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = GetTimeSyncTicks();

                // Small platform-aware delay to ensure different samples
                if (i < samples.Length - 1)
                {
                    if (PlatformConfig.IsMobile)
                        Thread.SpinWait(100); // Lighter spin on mobile
                    else
                        Thread.SpinWait(10);  // Shorter spin on desktop
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime GetTimeCore(bool useUtc, long swTicks)
        {
            threadLocalLastCheck = swTicks;

            // Check if resync needed
            long lastResync = Interlocked.Read(ref resyncState.LastResyncStopwatchTicks);
            if (swTicks - lastResync > RESYNC_INTERVAL_TICKS)
            {
                TryResyncWithSystemTime();
            }

            // Read state with guaranteed visibility
            long baseLocalTicks = Interlocked.Read(ref timeState.BaseLocalTicks);
            long baseUtcTicks = Interlocked.Read(ref timeState.BaseUtcTicks);
            long baseStopwatchTicks = Interlocked.Read(ref timeState.BaseStopwatchTicks);

            // Emergency re-initialization check
            if (baseLocalTicks == 0 || baseUtcTicks == 0)
            {
                InitializeCore();
                baseLocalTicks = Interlocked.Read(ref timeState.BaseLocalTicks);
                baseUtcTicks = Interlocked.Read(ref timeState.BaseUtcTicks);
                baseStopwatchTicks = Interlocked.Read(ref timeState.BaseStopwatchTicks);
            }

            // Calculate time
            long elapsed = swTicks - baseStopwatchTicks;
            long baseTicks = useUtc ? baseUtcTicks : baseLocalTicks;
            long calculatedTicks = baseTicks + elapsed;

            // Ensure monotonicity
            ref long lastTicksRef = ref (useUtc ? ref timeState.LastUtcTicks : ref timeState.LastLocalTicks);

            // Atomic monotonic update
            long finalTicks = calculatedTicks;
            long currentLast;
            do
            {
                currentLast = Interlocked.Read(ref lastTicksRef);
                if (finalTicks <= currentLast)
                {
                    finalTicks = currentLast;
                    break;
                }

                long exchanged = Interlocked.CompareExchange(ref lastTicksRef, finalTicks, currentLast);
                if (exchanged == currentLast) break;
                if (exchanged >= finalTicks)
                {
                    finalTicks = exchanged;
                    break;
                }
            } while (true);

            // Update thread-local cache
            if (useUtc)
                threadLocalLastUtcTicks = finalTicks;
            else
                threadLocalLastLocalTicks = finalTicks;

            return new DateTime(finalTicks, useUtc ? DateTimeKind.Utc : DateTimeKind.Local);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TryResyncWithSystemTime()
        {
            if (Interlocked.CompareExchange(ref resyncState.IsResyncing, 1, 0) == 0)
            {
                try
                {
                    var swTicks = MonotonicStopwatch.ElapsedTicks;
                    var now = DateTime.UtcNow;

                    // Check for drift
                    long currentBaseTicks = Volatile.Read(ref timeState.BaseUtcTicks);
                    long currentBaseSwTicks = Volatile.Read(ref timeState.BaseStopwatchTicks);

                    long ourTime = currentBaseTicks + (swTicks - currentBaseSwTicks);
                    long systemTime = now.Ticks;
                    long drift = systemTime - ourTime;

                    // Only correct if drift exceeds platform resolution significantly
                    if (Math.Abs(drift) > Math.Max(MAX_DRIFT_TICKS, PlatformConfig.ResolutionTicks * 10))
                    {
                        GONetLog.Warning($"[HighResTimer] Clock drift detected: {drift / 10000}ms, correcting");

                        Interlocked.Exchange(ref timeState.BaseUtcTicks, systemTime);
                        Interlocked.Exchange(ref timeState.BaseStopwatchTicks, swTicks);
                        Interlocked.Exchange(ref resyncState.LastDriftCorrection, drift);
                    }

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
        /// Cleanup method for application shutdown
        /// </summary>
        public static void Shutdown()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            TimeEndPeriod(1);
            GONetLog.Info("[HighResTimer] Windows: Released high-precision timer");
#endif
        }

        /// <summary>
        /// Gets diagnostic information about timer performance
        /// </summary>
        public static string GetDiagnostics()
        {
            long resyncCount = Interlocked.Read(ref resyncState.ResyncCount);
            long lastDrift = Interlocked.Read(ref resyncState.LastDriftCorrection);

            return $"[HighResTimer] Platform: {Application.platform}, " +
                   $"Resolution: {PlatformConfig.ResolutionTicks / 10000}ms, " +
                   $"Cache: {PlatformConfig.CacheDurationTicks / 10000}ms, " +
                   $"Resyncs: {resyncCount}, " +
                   $"LastDrift: {lastDrift / 10000}ms";
        }
    }
}