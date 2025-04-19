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

namespace GONet.Utils
{
    /// <summary>
    /// Provides high-resolution timing utilities with precision exceeding <see cref="DateTime"/> (~15ms).
    /// </summary>
    public static class HighResolutionTimeUtils
    {
        private static readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private static DateTime lastResyncTime = DateTime.Now;
        private static DateTime lastResyncTimeUtc = DateTime.UtcNow;
        private static long lastResyncDiffTicks = 0;
        private static TimeSpan autoResyncInterval = TimeSpan.FromSeconds(10);
        private static readonly object syncLock = new object();
        private static int resyncCount = 0;

        /// <summary>
        /// Gets or sets the interval after which a resync with system time occurs.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if set to a non-positive value.</exception>
        public static TimeSpan AutoResyncInterval
        {
            get => autoResyncInterval;
            set => autoResyncInterval = value > TimeSpan.Zero ? value : throw new ArgumentException("Interval must be positive.", nameof(value));
        }

        /// <summary>
        /// Gets the number of resyncs performed since initialization.
        /// </summary>
        public static int ResyncCount => resyncCount;

        /// <summary>
        /// Gets the last difference in ticks between resyncs, indicating drift.
        /// </summary>
        public static long LastResyncDiffTicks => lastResyncDiffTicks;

        /// <summary>
        /// Gets the current UTC time with high-resolution adjustments.
        /// </summary>
        public static DateTime UtcNow => GetTime(lastResyncTimeUtc);

        /// <summary>
        /// Gets the current local time with high-resolution adjustments.
        /// </summary>
        public static DateTime Now => GetTime(lastResyncTime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime GetTime(DateTime baseTime)
        {
            if (stopwatch.Elapsed > autoResyncInterval)
                Resync();

            long ticksToAdd = AdjustTicks(stopwatch.Elapsed.Ticks);
            return baseTime.AddTicks(ticksToAdd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long AdjustTicks(long elapsedTicks)
        {
            if (lastResyncDiffTicks == 0) return elapsedTicks;
            float progress = elapsedTicks / (float)autoResyncInterval.Ticks;
            return progress >= 1f ? elapsedTicks : elapsedTicks - (long)(lastResyncDiffTicks * (1f - progress));
        }

        private static void Resync()
        {
            lock (syncLock)
            {
                DateTime now = DateTime.Now;
                long ticksBefore = lastResyncTime.Ticks + stopwatch.Elapsed.Ticks;
                lastResyncTime = now;
                lastResyncTimeUtc = DateTime.UtcNow;
                stopwatch.Restart();
                lastResyncDiffTicks = lastResyncTime.Ticks - ticksBefore;
                resyncCount++;
            }
        }
    }
}