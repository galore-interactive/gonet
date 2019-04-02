using System;
using System.Diagnostics;

namespace GONet.Utils
{
    /// <summary>
    /// It is known the precision of <see cref="DateTime.Now"/> and <see cref="DateTime.UtcNow"/> is low (@ ~15ms), 
    /// which is not acceptable in many cases (especially in games).
    /// Use this class when high precision timing matters.
    /// 
    /// Due to the current implementation of the auto resync, thread safety should be examined further.
    /// </summary>
    public static class HighResolutionTimeUtils
    {
        private static DateTime initialTime;
        private static DateTime initialTimeUtc;
        private static Stopwatch highResolutionStopwatch;

        /// <summary>
        /// Since it appears that Stopwatch does get out of sync with the system time (by as much as half a second per hour),
        /// it makes sense to reset the hybrid DateTime class based on the amount of time that passes between calls to check
        /// the time (via a call to <see cref="Resync"/>).
        /// </summary>
        private static readonly long autoResyncAfterTicks = TimeSpan.FromSeconds(10).Ticks;

        static HighResolutionTimeUtils()
        {
            Resync();
        }

        public static DateTime UtcNow
        {
            get
            {
                if (highResolutionStopwatch.Elapsed.Ticks > autoResyncAfterTicks)
                {
                    Resync();
                }

                return initialTimeUtc.AddTicks(highResolutionStopwatch.Elapsed.Ticks);
            }
        }

        public static DateTime Now
        {
            get
            {
                if (highResolutionStopwatch.Elapsed.Ticks > autoResyncAfterTicks)
                {
                    Resync();
                }

                return initialTime.AddTicks(highResolutionStopwatch.Elapsed.Ticks);
            }
        }

        private static void Resync()
        {
            initialTime = DateTime.Now;
            initialTimeUtc = DateTime.UtcNow;
            highResolutionStopwatch = Stopwatch.StartNew();
        }
    }
}