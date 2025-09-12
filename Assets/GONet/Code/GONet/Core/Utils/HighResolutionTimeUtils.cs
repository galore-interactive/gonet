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
                    long last;
                    do
                    {
                        last = Volatile.Read(ref _lastElapsedTicks);
                        if (ticks <= last) return last;
                    } while (Interlocked.CompareExchange(ref _lastElapsedTicks, ticks, last) != last);
                    return ticks;
                }
            }

            internal static void Stop()
            {
                _stopwatch.Stop();
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
                (int)TimeSpan.TicksPerMillisecond; // 1ms cache on desktop

            private static long GetPlatformResolution()
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        return TimeSpan.FromMilliseconds(15.6).Ticks; // Windows default timer
                    case RuntimePlatform.Android:
                        return TimeSpan.FromMilliseconds(10).Ticks; // Conservative Android
                    case RuntimePlatform.IPhonePlayer:
                        return TimeSpan.FromMilliseconds(1).Ticks; // iOS has good timers
                    default:
                        return TimeSpan.FromMilliseconds(1).Ticks; // macOS/Linux
                }
            }
        }

        // Constants
        private const long RESYNC_INTERVAL_TICKS = 10L * TimeSpan.TicksPerSecond; // 10 seconds
        private const long MAX_DRIFT_TICKS = 1L * TimeSpan.TicksPerSecond; // 1 second max drift
        
        public  const double TICKS_TO_SECONDS = 1.0 / TimeSpan.TicksPerSecond;

        // Thread-local cache to reduce contention
        [ThreadStatic] private static long threadLocalLastCheck;
        [ThreadStatic] private static long threadLocalLastLocalTicks;
        [ThreadStatic] private static long threadLocalLastUtcTicks;
        [ThreadStatic] private static bool threadLocalInitialized;

        private static volatile bool isInitialized = false;
        private static readonly object initLock = new object();

        // Force initialization on type load
        private static readonly bool forceInit = InitializeOnLoad();


        private static volatile int shutdownState = 0; // 0 = false, 1 = true

        private static bool isShuttingDown
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => shutdownState == 1;
        }

        static HighResolutionTimeUtils()
        {
            InitializeOnLoad();
            for (int i = 0; i < 3; i++)
            {
                var dummy = UtcNow;
                Thread.SpinWait(100);
            }
        }

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
            Application.quitting -= Shutdown; // Unregister first to avoid re-adding
            Application.quitting += () => Shutdown();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.quitting -= () => Shutdown();
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
            TimeBeginPeriod(1);
#elif UNITY_ANDROID
#elif UNITY_IOS
#endif
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlayMode()
        {
            isInitialized = false;
            timeState = new TimeState();
            resyncState = new ResyncState();
            threadLocalLastCheck = 0;
            threadLocalLastLocalTicks = 0;
            threadLocalLastUtcTicks = 0;
            threadLocalInitialized = false;
            InitializeCore();
        }
#endif

        private static void InitializeCore()
        {
            lock (initLock)
            {
                if (isInitialized) return;
                var swTicks = MonotonicStopwatch.ElapsedTicks;
                var utcNow = DateTime.UtcNow; // Single initial call for compatibility baseline
                var now = DateTime.Now;
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
        /// Gets the current UTC time with backward compatibility. Uses high-resolution deltas after initial system time anchor.
        /// </summary>
        public static DateTime UtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (isShuttingDown) return DateTime.UtcNow; // Fallback during shutdown

                if (!isInitialized) InitializeCore();
                if (!threadLocalInitialized)
                {
                    threadLocalLastCheck = 0;
                    threadLocalLastLocalTicks = 0;
                    threadLocalLastUtcTicks = 0;
                    threadLocalInitialized = true;
                }
                long swTicks = MonotonicStopwatch.ElapsedTicks;
                if (threadLocalLastUtcTicks > 0 && (swTicks - threadLocalLastCheck) < PlatformConfig.CacheDurationTicks)
                {
                    return new DateTime(threadLocalLastUtcTicks, DateTimeKind.Utc);
                }
                return GetTimeCore(true, swTicks);
            }
        }

        /// <summary>
        /// Gets the current local time with backward compatibility. Uses high-resolution deltas after initial system time anchor.
        /// </summary>
        public static DateTime Now
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (isShuttingDown) return DateTime.Now; // Fallback during shutdown

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
        /// Gets the current UTC elapsed ticks for high-precision operations.
        /// </summary>
        public static long UtcNowTicks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!isInitialized) InitializeCore();
                long swTicks = MonotonicStopwatch.ElapsedTicks;
                long baseStopwatchTicks = Volatile.Read(ref timeState.BaseStopwatchTicks);
                long baseUtcTicks = Interlocked.Read(ref timeState.BaseUtcTicks);
                return baseUtcTicks + (swTicks - baseStopwatchTicks);
            }
        }

        /// <summary>
        /// Gets the current local elapsed ticks for high-precision operations.
        /// </summary>
        public static long NowTicks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!isInitialized) InitializeCore();
                long swTicks = MonotonicStopwatch.ElapsedTicks;
                long baseStopwatchTicks = Volatile.Read(ref timeState.BaseStopwatchTicks);
                long baseLocalTicks = Interlocked.Read(ref timeState.BaseLocalTicks);
                return baseLocalTicks + (swTicks - baseStopwatchTicks);
            }
        }

        /// <summary>
        /// IMPORTANT: GONet internal use only.
        /// Gets current elapsed ticks specifically for time sync operations.
        /// Bypasses all caching for maximum accuracy.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long GetTimeSyncTicks_Internal()
        {
            if (!isInitialized) InitializeCore();
            long swTicks = MonotonicStopwatch.ElapsedTicks;
            long baseStopwatchTicks = Volatile.Read(ref timeState.BaseStopwatchTicks);
            return swTicks - baseStopwatchTicks;
        }

        /// <summary>
        /// Gets multiple time samples for NTP-style median filtering in time sync
        /// </summary>
        public static void GetTimeSyncSamples(Span<long> samples)
        {
            if (!isInitialized) InitializeCore();
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = GetTimeSyncTicks_Internal();
                if (i < samples.Length - 1)
                {
                    if (PlatformConfig.IsMobile)
                        Thread.SpinWait(100);
                    else
                        Thread.SpinWait(10);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime GetTimeCore(bool useUtc, long swTicks)
        {
            if (isShuttingDown) return useUtc ? DateTime.UtcNow : DateTime.Now;

            threadLocalLastCheck = swTicks;
            long baseLocalTicks = Interlocked.Read(ref timeState.BaseLocalTicks);
            long baseUtcTicks = Interlocked.Read(ref timeState.BaseUtcTicks);
            long baseStopwatchTicks = Interlocked.Read(ref timeState.BaseStopwatchTicks);
            if (baseLocalTicks == 0 || baseUtcTicks == 0)
            {
                InitializeCore();
                baseLocalTicks = Interlocked.Read(ref timeState.BaseLocalTicks);
                baseUtcTicks = Interlocked.Read(ref timeState.BaseUtcTicks);
                baseStopwatchTicks = Interlocked.Read(ref timeState.BaseStopwatchTicks);
            }
            long elapsedStopwatchTicks = swTicks - baseStopwatchTicks;
            double elapsedSeconds = (double)elapsedStopwatchTicks / Stopwatch.Frequency;
            long elapsedDateTimeTicks = (long)(elapsedSeconds * TimeSpan.TicksPerSecond);
            long baseTicks = useUtc ? baseUtcTicks : baseLocalTicks;
            long calculatedTicks = baseTicks + elapsedDateTimeTicks;
            ref long lastTicksRef = ref (useUtc ? ref timeState.LastUtcTicks : ref timeState.LastLocalTicks);
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

            if (useUtc)
                threadLocalLastUtcTicks = finalTicks;
            else
                threadLocalLastLocalTicks = finalTicks;

            return new DateTime(finalTicks, useUtc ? DateTimeKind.Utc : DateTimeKind.Local);
        }

        /// <summary>
        /// Cleanup method for application shutdown
        /// </summary>
        public static void Shutdown()
        {
            if (Interlocked.CompareExchange(ref shutdownState, 1, 0) == 0)
            {
                MonotonicStopwatch.Stop();

                threadLocalLastCheck = 0;
                threadLocalLastLocalTicks = 0;
                threadLocalLastUtcTicks = 0;
                threadLocalInitialized = false;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                TimeEndPeriod(1);
#endif
            }
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