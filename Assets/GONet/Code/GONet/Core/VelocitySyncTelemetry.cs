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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GONet
{
    /// <summary>
    /// Telemetry system for velocity-augmented sync performance analysis.
    /// Tracks VALUE vs VELOCITY bundle usage, velocity calculations, and extrapolation/interpolation statistics.
    /// Thread-safe and designed for zero overhead when disabled.
    /// </summary>
    public static class VelocitySyncTelemetry
    {
        #region Configuration

        /// <summary>
        /// Enable/disable telemetry tracking. When false, all tracking calls become no-ops (zero overhead).
        /// </summary>
        public static bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Enable per-GONetId tracking (more detailed but higher memory/CPU cost).
        /// Only applies when IsEnabled=true.
        /// </summary>
        public static bool EnablePerObjectTracking { get; set; } = false;

        /// <summary>
        /// Interval in seconds for periodic telemetry reports (0 = disabled).
        /// </summary>
        public static float PeriodicReportIntervalSeconds { get; set; } = 5.0f;

        /// <summary>
        /// Enable detailed per-frame trace logging (WARNING: massive log output).
        /// Logs every server send, client receive, and blending decision with full values.
        /// Only use for targeted debugging with specific GONetId/index filters.
        /// </summary>
        public static bool EnableDetailedTracing { get; set; } = false;

        /// <summary>
        /// Filter tracing to specific GONetId (0 = trace all).
        /// </summary>
        public static uint TraceGONetId { get; set; } = 0;

        /// <summary>
        /// Filter tracing to specific index (e.g., 8 for rotation). -1 = trace all.
        /// </summary>
        public static int TraceIndex { get; set; } = -1;

        #endregion

        #region Global Counters (Thread-Safe)

        private static long valueBundlesSent = 0;
        private static long valueBundlesReceived = 0;
        private static long velocityBundlesSent = 0;
        private static long velocityBundlesReceived = 0;

        private static long velocityCalculationAttempts = 0;
        private static long velocityCalculationSuccesses = 0;
        private static long velocityCalculationFailures = 0;

        private static long extrapolationUsages = 0;
        private static long interpolationUsages = 0;
        private static long standardBlendingFallbacks = 0;

        #endregion

        #region Per-Object Tracking (Optional, Higher Cost)

        private class ObjectStats
        {
            public long ValueBundlesSent;
            public long ValueBundlesReceived;
            public long VelocityBundlesSent;
            public long VelocityBundlesReceived;
            public long VelocityCalculations;
            public long Extrapolations;
            public long Interpolations;
        }

        private static readonly ConcurrentDictionary<uint, ObjectStats> perObjectStats = new ConcurrentDictionary<uint, ObjectStats>();

        #endregion

        #region Tracking Methods (Inline for Performance)

        /// <summary>
        /// Track a VALUE bundle being sent (discrete value, no velocity).
        /// </summary>
        public static void TrackValueBundleSent(uint gonetId = 0)
        {
            if (!IsEnabled) return;
            Interlocked.Increment(ref valueBundlesSent);
            if (EnablePerObjectTracking && gonetId > 0)
            {
                var stats = perObjectStats.GetOrAdd(gonetId, _ => new ObjectStats());
                Interlocked.Increment(ref stats.ValueBundlesSent);
            }
        }

        /// <summary>
        /// Track a VALUE bundle being received (discrete value, no velocity).
        /// </summary>
        public static void TrackValueBundleReceived(uint gonetId = 0)
        {
            if (!IsEnabled) return;
            Interlocked.Increment(ref valueBundlesReceived);
            if (EnablePerObjectTracking && gonetId > 0)
            {
                var stats = perObjectStats.GetOrAdd(gonetId, _ => new ObjectStats());
                Interlocked.Increment(ref stats.ValueBundlesReceived);
            }
        }

        /// <summary>
        /// Track a VELOCITY bundle being sent (velocity data only, value synthesized on receiver).
        /// </summary>
        public static void TrackVelocityBundleSent(uint gonetId = 0)
        {
            if (!IsEnabled) return;
            Interlocked.Increment(ref velocityBundlesSent);
            if (EnablePerObjectTracking && gonetId > 0)
            {
                var stats = perObjectStats.GetOrAdd(gonetId, _ => new ObjectStats());
                Interlocked.Increment(ref stats.VelocityBundlesSent);
            }
        }

        /// <summary>
        /// Track a VELOCITY bundle being received (velocity data only, value synthesized locally).
        /// </summary>
        public static void TrackVelocityBundleReceived(uint gonetId = 0)
        {
            if (!IsEnabled) return;
            Interlocked.Increment(ref velocityBundlesReceived);
            if (EnablePerObjectTracking && gonetId > 0)
            {
                var stats = perObjectStats.GetOrAdd(gonetId, _ => new ObjectStats());
                Interlocked.Increment(ref stats.VelocityBundlesReceived);
            }
        }

        /// <summary>
        /// Track a velocity calculation attempt.
        /// </summary>
        public static void TrackVelocityCalculation(bool success, uint gonetId = 0)
        {
            if (!IsEnabled) return;
            Interlocked.Increment(ref velocityCalculationAttempts);
            if (success)
            {
                Interlocked.Increment(ref velocityCalculationSuccesses);
            }
            else
            {
                Interlocked.Increment(ref velocityCalculationFailures);
            }

            if (EnablePerObjectTracking && gonetId > 0)
            {
                var stats = perObjectStats.GetOrAdd(gonetId, _ => new ObjectStats());
                Interlocked.Increment(ref stats.VelocityCalculations);
            }
        }

        /// <summary>
        /// Track extrapolation usage (velocity-aware blending projecting forward in time).
        /// </summary>
        public static void TrackExtrapolation(uint gonetId = 0)
        {
            if (!IsEnabled) return;
            Interlocked.Increment(ref extrapolationUsages);
            if (EnablePerObjectTracking && gonetId > 0)
            {
                var stats = perObjectStats.GetOrAdd(gonetId, _ => new ObjectStats());
                Interlocked.Increment(ref stats.Extrapolations);
            }
        }

        /// <summary>
        /// Track interpolation usage (blending between past snapshots).
        /// </summary>
        public static void TrackInterpolation(uint gonetId = 0)
        {
            if (!IsEnabled) return;
            Interlocked.Increment(ref interpolationUsages);
            if (EnablePerObjectTracking && gonetId > 0)
            {
                var stats = perObjectStats.GetOrAdd(gonetId, _ => new ObjectStats());
                Interlocked.Increment(ref stats.Interpolations);
            }
        }

        /// <summary>
        /// Track fallback to standard value blending (no velocity data available).
        /// </summary>
        public static void TrackStandardBlendingFallback()
        {
            if (!IsEnabled) return;
            Interlocked.Increment(ref standardBlendingFallbacks);
        }

        #endregion

        #region Reporting

        /// <summary>
        /// Generate telemetry report as formatted string.
        /// </summary>
        public static string GenerateReport(bool includePerObjectBreakdown = false)
        {
            if (!IsEnabled)
            {
                return "[VelocitySync][Telemetry] DISABLED - Enable VelocitySyncTelemetry.IsEnabled to track metrics";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("[VelocitySync][Telemetry] Summary Report");
            sb.AppendLine("========================================");

            // Bundle statistics
            long totalBundlesSent = valueBundlesSent + velocityBundlesSent;
            long totalBundlesReceived = valueBundlesReceived + velocityBundlesReceived;

            sb.AppendLine();
            sb.AppendLine("--- Bundle Statistics (Sent) ---");
            if (totalBundlesSent > 0)
            {
                float valuePercentSent = (valueBundlesSent / (float)totalBundlesSent) * 100f;
                float velocityPercentSent = (velocityBundlesSent / (float)totalBundlesSent) * 100f;
                sb.AppendLine($"  VALUE bundles:    {valueBundlesSent,8} ({valuePercentSent:F1}%)");
                sb.AppendLine($"  VELOCITY bundles: {velocityBundlesSent,8} ({velocityPercentSent:F1}%)");
                sb.AppendLine($"  TOTAL:            {totalBundlesSent,8}");
            }
            else
            {
                sb.AppendLine("  No bundles sent");
            }

            sb.AppendLine();
            sb.AppendLine("--- Bundle Statistics (Received) ---");
            if (totalBundlesReceived > 0)
            {
                float valuePercentReceived = (valueBundlesReceived / (float)totalBundlesReceived) * 100f;
                float velocityPercentReceived = (velocityBundlesReceived / (float)totalBundlesReceived) * 100f;
                sb.AppendLine($"  VALUE bundles:    {valueBundlesReceived,8} ({valuePercentReceived:F1}%)");
                sb.AppendLine($"  VELOCITY bundles: {velocityBundlesReceived,8} ({velocityPercentReceived:F1}%)");
                sb.AppendLine($"  TOTAL:            {totalBundlesReceived,8}");
            }
            else
            {
                sb.AppendLine("  No bundles received");
            }

            // Velocity calculation statistics
            sb.AppendLine();
            sb.AppendLine("--- Velocity Calculation Statistics ---");
            if (velocityCalculationAttempts > 0)
            {
                float successRate = (velocityCalculationSuccesses / (float)velocityCalculationAttempts) * 100f;
                sb.AppendLine($"  Attempts:  {velocityCalculationAttempts,8}");
                sb.AppendLine($"  Successes: {velocityCalculationSuccesses,8} ({successRate:F1}%)");
                sb.AppendLine($"  Failures:  {velocityCalculationFailures,8}");
            }
            else
            {
                sb.AppendLine("  No velocity calculations performed");
            }

            // Blending statistics
            sb.AppendLine();
            sb.AppendLine("--- Blending Statistics ---");
            long totalBlending = extrapolationUsages + interpolationUsages;
            if (totalBlending > 0)
            {
                float extrapolationPercent = (extrapolationUsages / (float)totalBlending) * 100f;
                float interpolationPercent = (interpolationUsages / (float)totalBlending) * 100f;
                sb.AppendLine($"  Extrapolation: {extrapolationUsages,8} ({extrapolationPercent:F1}%)");
                sb.AppendLine($"  Interpolation: {interpolationUsages,8} ({interpolationPercent:F1}%)");
                sb.AppendLine($"  TOTAL:         {totalBlending,8}");
            }
            else
            {
                sb.AppendLine("  No blending operations performed");
            }

            if (standardBlendingFallbacks > 0)
            {
                sb.AppendLine($"  Standard blending fallbacks: {standardBlendingFallbacks,8}");
            }

            // Per-object breakdown (if enabled and requested)
            if (includePerObjectBreakdown && EnablePerObjectTracking && perObjectStats.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- Per-Object Breakdown ---");
                sb.AppendLine($"  Tracking {perObjectStats.Count} objects");
                sb.AppendLine();

                // Sort by total activity (most active objects first)
                var sortedObjects = new List<KeyValuePair<uint, ObjectStats>>(perObjectStats);
                sortedObjects.Sort((a, b) =>
                {
                    long activityA = a.Value.ValueBundlesSent + a.Value.VelocityBundlesSent +
                                   a.Value.ValueBundlesReceived + a.Value.VelocityBundlesReceived;
                    long activityB = b.Value.ValueBundlesSent + b.Value.VelocityBundlesSent +
                                   b.Value.ValueBundlesReceived + b.Value.VelocityBundlesReceived;
                    return activityB.CompareTo(activityA);
                });

                // Show top 10 most active objects
                int objectsToShow = Math.Min(10, sortedObjects.Count);
                for (int i = 0; i < objectsToShow; i++)
                {
                    var kvp = sortedObjects[i];
                    uint gonetId = kvp.Key;
                    ObjectStats stats = kvp.Value;

                    long totalSent = stats.ValueBundlesSent + stats.VelocityBundlesSent;
                    long totalReceived = stats.ValueBundlesReceived + stats.VelocityBundlesReceived;

                    sb.AppendLine($"  GONetId:{gonetId}");
                    if (totalSent > 0)
                    {
                        float velocityPercentSent = totalSent > 0 ? (stats.VelocityBundlesSent / (float)totalSent) * 100f : 0f;
                        sb.AppendLine($"    Sent:     VALUE={stats.ValueBundlesSent,6} VELOCITY={stats.VelocityBundlesSent,6} ({velocityPercentSent:F1}% velocity)");
                    }
                    if (totalReceived > 0)
                    {
                        float velocityPercentReceived = totalReceived > 0 ? (stats.VelocityBundlesReceived / (float)totalReceived) * 100f : 0f;
                        sb.AppendLine($"    Received: VALUE={stats.ValueBundlesReceived,6} VELOCITY={stats.VelocityBundlesReceived,6} ({velocityPercentReceived:F1}% velocity)");
                    }
                    if (stats.VelocityCalculations > 0)
                    {
                        sb.AppendLine($"    Velocity calculations: {stats.VelocityCalculations,6}");
                    }
                    if (stats.Extrapolations > 0 || stats.Interpolations > 0)
                    {
                        sb.AppendLine($"    Blending: Extrap={stats.Extrapolations,6} Interp={stats.Interpolations,6}");
                    }
                }

                if (sortedObjects.Count > objectsToShow)
                {
                    sb.AppendLine($"  ... and {sortedObjects.Count - objectsToShow} more objects");
                }
            }

            sb.AppendLine("========================================");

            return sb.ToString();
        }

        /// <summary>
        /// Log telemetry report to GONet log.
        /// </summary>
        public static void LogReport(bool includePerObjectBreakdown = false)
        {
            string report = GenerateReport(includePerObjectBreakdown);
            GONetLog.Info(report);
        }

        /// <summary>
        /// Reset all telemetry counters (useful for test runs or benchmarking).
        /// </summary>
        public static void Reset()
        {
            valueBundlesSent = 0;
            valueBundlesReceived = 0;
            velocityBundlesSent = 0;
            velocityBundlesReceived = 0;
            velocityCalculationAttempts = 0;
            velocityCalculationSuccesses = 0;
            velocityCalculationFailures = 0;
            extrapolationUsages = 0;
            interpolationUsages = 0;
            standardBlendingFallbacks = 0;
            perObjectStats.Clear();

            if (IsEnabled)
            {
                GONetLog.Info("[VelocitySync][Telemetry] Counters reset");
            }
        }

        #endregion

        #region Periodic Reporting (Internal)

        /// <summary>
        /// Internal state tracking for periodic reporting.
        /// </summary>
        internal static float lastReportTime = 0f;

        /// <summary>
        /// Check if periodic report should run now (called from MonoBehaviour Update).
        /// Returns true if report was generated.
        /// </summary>
        internal static bool TryPeriodicReport(float currentTime)
        {
            if (!IsEnabled || PeriodicReportIntervalSeconds <= 0f)
            {
                return false;
            }

            if (currentTime - lastReportTime >= PeriodicReportIntervalSeconds)
            {
                LogReport(includePerObjectBreakdown: false);
                lastReportTime = currentTime;
                return true;
            }

            return false;
        }

        #endregion

        #region Detailed Tracing (Per-Frame Logging)

        private static bool ShouldTrace(uint gonetId, int index)
        {
            if (!IsEnabled || !EnableDetailedTracing) return false;
            if (TraceGONetId > 0 && gonetId != TraceGONetId) return false;
            if (TraceIndex >= 0 && index != TraceIndex) return false;
            return true;
        }

        /// <summary>
        /// Trace server authority value (what server has BEFORE deciding what to send).
        /// </summary>
        public static void TraceServerAuthority(uint gonetId, int index, string memberName, UnityEngine.Quaternion value)
        {
            if (!ShouldTrace(gonetId, index)) return;
            GONetLog.Info($"[TRACE][SERVER-AUTH] GONetId:{gonetId} idx:{index} {memberName} euler:({value.eulerAngles.x:F2},{value.eulerAngles.y:F2},{value.eulerAngles.z:F2}) quat:({value.w:F4},{value.x:F4},{value.y:F4},{value.z:F4})");
        }

        /// <summary>
        /// Trace server send decision (VALUE or VELOCITY bundle with reasoning).
        /// </summary>
        public static void TraceServerSend(uint gonetId, int index, string bundleType, UnityEngine.Quaternion quaternionValue,
            UnityEngine.Vector3 angularVelocityRad, string reason, int snapshotCount, bool velocityWithinRange)
        {
            if (!ShouldTrace(gonetId, index)) return;
            UnityEngine.Vector3 angularVelocityDeg = angularVelocityRad * UnityEngine.Mathf.Rad2Deg;
            GONetLog.Info($"[TRACE][SERVER-SEND] GONetId:{gonetId} idx:{index} bundleType:{bundleType} " +
                $"euler:({quaternionValue.eulerAngles.x:F2},{quaternionValue.eulerAngles.y:F2},{quaternionValue.eulerAngles.z:F2}) " +
                $"angVel:({angularVelocityDeg.x:F2},{angularVelocityDeg.y:F2},{angularVelocityDeg.z:F2})°/s " +
                $"snapshots:{snapshotCount} velInRange:{velocityWithinRange} reason:{reason}");
        }

        /// <summary>
        /// Trace client receive (what client receives from network).
        /// </summary>
        public static void TraceClientReceive(uint gonetId, int index, string bundleType, UnityEngine.Quaternion quaternionValue,
            UnityEngine.Vector3 angularVelocityRad, UnityEngine.Quaternion synthesizedQuaternion)
        {
            if (!ShouldTrace(gonetId, index)) return;
            UnityEngine.Vector3 angularVelocityDeg = angularVelocityRad * UnityEngine.Mathf.Rad2Deg;
            if (bundleType == "VELOCITY")
            {
                GONetLog.Info($"[TRACE][CLIENT-RECV] GONetId:{gonetId} idx:{index} bundleType:VELOCITY " +
                    $"angVel:({angularVelocityDeg.x:F2},{angularVelocityDeg.y:F2},{angularVelocityDeg.z:F2})°/s " +
                    $"synthesized:({synthesizedQuaternion.eulerAngles.x:F2},{synthesizedQuaternion.eulerAngles.y:F2},{synthesizedQuaternion.eulerAngles.z:F2})");
            }
            else
            {
                GONetLog.Info($"[TRACE][CLIENT-RECV] GONetId:{gonetId} idx:{index} bundleType:VALUE " +
                    $"euler:({quaternionValue.eulerAngles.x:F2},{quaternionValue.eulerAngles.y:F2},{quaternionValue.eulerAngles.z:F2})");
            }
        }

        /// <summary>
        /// Trace client blend decision (EXTRAPOLATION vs INTERPOLATION with result).
        /// </summary>
        public static void TraceClientBlend(uint gonetId, int index, string blendType, bool hasVelocityData, UnityEngine.Quaternion resultQuaternion)
        {
            if (!ShouldTrace(gonetId, index)) return;
            GONetLog.Info($"[TRACE][CLIENT-BLEND] GONetId:{gonetId} idx:{index} blendType:{blendType} hasVel:{hasVelocityData} " +
                $"result:({resultQuaternion.eulerAngles.x:F2},{resultQuaternion.eulerAngles.y:F2},{resultQuaternion.eulerAngles.z:F2})");
        }

        /// <summary>
        /// Trace client apply (final value applied to transform).
        /// </summary>
        public static void TraceClientApply(uint gonetId, int index, UnityEngine.Quaternion appliedQuaternion)
        {
            if (!ShouldTrace(gonetId, index)) return;
            GONetLog.Info($"[TRACE][CLIENT-APPLY] GONetId:{gonetId} idx:{index} " +
                $"applied:({appliedQuaternion.eulerAngles.x:F2},{appliedQuaternion.eulerAngles.y:F2},{appliedQuaternion.eulerAngles.z:F2})");
        }

        #endregion
    }
}
