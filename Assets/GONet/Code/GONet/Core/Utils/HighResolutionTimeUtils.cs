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
    /// It is known the precision of <see cref="DateTime.Now"/> and <see cref="DateTime.UtcNow"/> is low (@ ~15ms), 
    /// which is not acceptable in many cases (especially in games).
    /// Use this class when high precision timing matters.
    /// </summary>
    public static class HighResolutionTimeUtils
    {
        private static bool hasResyncd = false;
        private static DateTime lastResyncTime;
        private static DateTime lastResyncTimeUtc;
        private static Stopwatch highResolutionStopwatch;
        private static long lastResyncDiffTicks;

        /// <summary>
        /// Since it appears that Stopwatch does get out of sync with the system time (by as much as half a second per hour),
        /// it makes sense to reset the hybrid DateTime class based on the amount of time that passes between calls to check
        /// the time (via a call to <see cref="Resync"/>).
        /// </summary>
        private static readonly long AUTO_RESYNC_AFTER_TICKS = TimeSpan.FromSeconds(10).Ticks;
        private static readonly float AUTO_RESYNC_AFTER_TICKS_FLOAT = (float)AUTO_RESYNC_AFTER_TICKS;

        private static readonly object resync = new object();
        private static volatile int resyncCounter = 0;

        static HighResolutionTimeUtils()
        {
            lastResyncDiffTicks = 0;
            Resync();
        }

        public static DateTime UtcNow
        {
            get
            {
                if (highResolutionStopwatch.Elapsed.Ticks > AUTO_RESYNC_AFTER_TICKS)
                {
                    Resync();
                }

                long addTicks = GetHighResolutionTicksToAddToResyncBaseline();

                return lastResyncTimeUtc.AddTicks(addTicks);
            }
        }

        public static DateTime Now
        {
            get
            {
                if (highResolutionStopwatch.Elapsed.Ticks > AUTO_RESYNC_AFTER_TICKS)
                {
                    Resync();
                }

                long addTicks = GetHighResolutionTicksToAddToResyncBaseline();

                return lastResyncTime.AddTicks(addTicks);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetHighResolutionTicksToAddToResyncBaseline()
        {
            long addTicks = highResolutionStopwatch.Elapsed.Ticks;

            if (lastResyncDiffTicks != 0)
            { // IMPORTANT: This code eases the adjustment (i.e., diff) back to resync time over the entire period between resyncs to avoid a possibly dramatic jump in time just after a resync!
                float inverseLerpBetweenResyncs = addTicks / AUTO_RESYNC_AFTER_TICKS_FLOAT;
                if (inverseLerpBetweenResyncs < 1f) // if 1 or greater there will be nothing to add based on calculations
                {
                    addTicks -= (long)(lastResyncDiffTicks * (1f - inverseLerpBetweenResyncs));
                }
            }

            return addTicks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Resync()
        {
            int resyncCounter_PRE = resyncCounter;
            lock (resync)
            {
                if (resyncCounter == resyncCounter_PRE) // this would be false is another thread was also trying to do this at the same time!
                {
                    ++resyncCounter;

                    DateTime now = DateTime.Now;
                    long nowTicksBeforeResync = hasResyncd ? lastResyncTime.Ticks + highResolutionStopwatch.Elapsed.Ticks : now.Ticks;

                    ///////////////////////////////////////////////////////////////////////////////////////
                    // RE-Sync:
                    lastResyncTime = now;
                    lastResyncTimeUtc = DateTime.UtcNow;
                    highResolutionStopwatch = Stopwatch.StartNew();
                    ///////////////////////////////////////////////////////////////////////////////////////

                    long nowTicksAfterResync = lastResyncTime.Ticks;
                    lastResyncDiffTicks = nowTicksAfterResync - nowTicksBeforeResync;

                    //GONetLog.Debug("lastResyncDiffTicks (well, as ms): " + TimeSpan.FromTicks(lastResyncDiffTicks).TotalMilliseconds);

                    hasResyncd = true;
                }
            }
        }
    }
}