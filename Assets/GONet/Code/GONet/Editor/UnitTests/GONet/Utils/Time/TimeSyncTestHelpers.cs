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
        /// Gets the current RTT buffer state for debugging
        /// </summary>
        public static string GetRttBufferState()
        {
            var timeSyncType = typeof(HighPerfTimeSync);
            var rttBufferField = timeSyncType.GetField("rttBuffer", BindingFlags.NonPublic | BindingFlags.Static);
            var rttWriteIndexField = timeSyncType.GetField("rttWriteIndex", BindingFlags.NonPublic | BindingFlags.Static);

            var buffer = rttBufferField?.GetValue(null) as Array;
            var writeIndex = (int)(rttWriteIndexField?.GetValue(null) ?? 0);

            if (buffer == null) return "RTT buffer not found";

            var sampleType = buffer.GetType().GetElementType();
            var timestampField = sampleType?.GetField("Timestamp");
            var valueField = sampleType?.GetField("Value");

            int validSamples = 0;
            float minRtt = float.MaxValue;
            float maxRtt = float.MinValue;

            for (int i = 0; i < buffer.Length; i++)
            {
                var sample = buffer.GetValue(i);
                if (sample != null && timestampField != null && valueField != null)
                {
                    long timestamp = (long)timestampField.GetValue(sample);
                    float value = (float)valueField.GetValue(sample);

                    if (timestamp > 0)
                    {
                        validSamples++;
                        minRtt = Math.Min(minRtt, value);
                        maxRtt = Math.Max(maxRtt, value);
                    }
                }
            }

            return $"RTT Buffer: WriteIndex={writeIndex}, ValidSamples={validSamples}, " +
                   $"MinRTT={minRtt:F3}s, MaxRTT={maxRtt:F3}s";
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