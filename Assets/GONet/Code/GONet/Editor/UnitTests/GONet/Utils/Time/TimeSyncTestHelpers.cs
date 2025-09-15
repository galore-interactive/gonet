using System;
using System.Reflection;
using GONet.Utils;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    /// <summary>
    /// Helper methods for time sync tests
    /// </summary>
    public static class TimeSyncTestHelpers
    {
        /// <summary>
        /// Creates a properly initialized time sync request that won't be rejected
        /// </summary>
        public static RequestMessage CreateValidTimeSyncRequest(SecretaryOfTemporalAffairs clientTime)
        {
            // Ensure we have valid elapsed ticks
            clientTime.Update();
            return new RequestMessage(clientTime.ElapsedTicks);
        }

        /// <summary>
        /// Processes a time sync with proper initialization to avoid the lastProcessedResponseTicks issue
        /// </summary>
        public static void ProcessTimeSyncSafely(
            RequestMessage request,
            SecretaryOfTemporalAffairs serverTime,
            SecretaryOfTemporalAffairs clientTime,
            bool forceAdjustment = false)
        {
            // Update server time to ensure we have a valid response time
            serverTime.Update();
            long serverResponseTicks = serverTime.ElapsedTicks;

            // Ensure the server response time is reasonable
            if (serverResponseTicks <= 0)
            {
                throw new InvalidOperationException("Server time not properly initialized");
            }

            HighPerfTimeSync.ProcessTimeSync(
                request.UID,
                serverResponseTicks,
                request,
                clientTime,
                forceAdjustment
            );
        }

        /// <summary>
        /// Waits for time interpolation to complete
        /// </summary>
        public static void WaitForInterpolation(int milliseconds = 1100)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        /// <summary>
        /// Gets the last processed response ticks for debugging
        /// </summary>
        public static long GetLastProcessedResponseTicks()
        {
            var field = typeof(HighPerfTimeSync).GetField("lastProcessedResponseTicks",
                BindingFlags.NonPublic | BindingFlags.Static);
            return (long)(field?.GetValue(null) ?? 0L);
        }

        /// <summary>
        /// Force resets the lastProcessedResponseTicks to avoid rejection issues
        /// </summary>
        public static void ResetLastProcessedResponseTicks()
        {
            var field = typeof(HighPerfTimeSync).GetField("lastProcessedResponseTicks",
                BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, 0L);
        }
    }
}