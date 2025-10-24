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

using UnityEngine;

namespace GONet
{
    /// <summary>
    /// MonoBehaviour component that provides periodic telemetry reporting for velocity-augmented sync.
    /// Add this component to GONetGlobal (or any persistent GameObject) to enable automatic reporting.
    /// </summary>
    public class VelocitySyncTelemetryReporter : GONetParticipantCompanionBehaviour
    {
        [Header("Telemetry Configuration")]
        [Tooltip("Enable telemetry tracking (zero overhead when disabled)")]
        public bool enableTelemetry = false;

        [Tooltip("Enable per-object tracking (more detailed but higher cost)")]
        public bool enablePerObjectTracking = false;

        [Tooltip("Periodic report interval in seconds (0 = disabled)")]
        [Range(0f, 60f)]
        public float reportIntervalSeconds = 5.0f;

        [Header("Detailed Tracing (WARNING: Massive Log Output)")]
        [Tooltip("Enable per-frame detailed tracing (use with filters!)")]
        public bool enableDetailedTracing = false;

        [Tooltip("Trace only this GONetId (0 = trace all objects)")]
        public uint traceGONetId = 208895; // Default to rotating cube

        [Tooltip("Trace only this index (e.g., 8 for rotation, -1 = trace all)")]
        public int traceIndex = 8; // Default to rotation

        [Header("Manual Reporting")]
        [Tooltip("Include per-object breakdown in manual reports")]
        public bool includePerObjectBreakdownInManualReports = true;

        private void Start()
        {
            // Initialize telemetry settings from inspector values
            VelocitySyncTelemetry.IsEnabled = enableTelemetry;
            VelocitySyncTelemetry.EnablePerObjectTracking = enablePerObjectTracking;
            VelocitySyncTelemetry.PeriodicReportIntervalSeconds = reportIntervalSeconds;
            VelocitySyncTelemetry.EnableDetailedTracing = enableDetailedTracing;
            VelocitySyncTelemetry.TraceGONetId = traceGONetId;
            VelocitySyncTelemetry.TraceIndex = traceIndex;

            if (enableTelemetry)
            {
                GONetLog.Info("[VelocitySyncTelemetry] Reporter initialized - telemetry ENABLED");
                GONetLog.Info($"[VelocitySyncTelemetry] Report interval: {reportIntervalSeconds}s, Per-object tracking: {enablePerObjectTracking}");
                if (enableDetailedTracing)
                {
                    GONetLog.Warning($"[VelocitySyncTelemetry] Detailed tracing ENABLED - filtering GONetId:{traceGONetId}, index:{traceIndex}");
                }
            }
            else
            {
                GONetLog.Info("[VelocitySyncTelemetry] Reporter initialized - telemetry DISABLED (zero overhead)");
            }
        }

        private void Update()
        {
            // Sync settings (allows runtime changes via inspector)
            VelocitySyncTelemetry.IsEnabled = enableTelemetry;
            VelocitySyncTelemetry.EnablePerObjectTracking = enablePerObjectTracking;
            VelocitySyncTelemetry.PeriodicReportIntervalSeconds = reportIntervalSeconds;
            VelocitySyncTelemetry.EnableDetailedTracing = enableDetailedTracing;
            VelocitySyncTelemetry.TraceGONetId = traceGONetId;
            VelocitySyncTelemetry.TraceIndex = traceIndex;

            // Periodic reporting (if enabled)
            if (enableTelemetry)
            {
                VelocitySyncTelemetry.TryPeriodicReport(Time.unscaledTime);
            }
        }

        /// <summary>
        /// Generate and log telemetry report immediately (call from console or script).
        /// </summary>
        [ContextMenu("Generate Report Now")]
        public void GenerateReportNow()
        {
            if (!VelocitySyncTelemetry.IsEnabled)
            {
                GONetLog.Warning("[VelocitySyncTelemetry] Cannot generate report - telemetry is DISABLED");
                return;
            }

            VelocitySyncTelemetry.LogReport(includePerObjectBreakdown: includePerObjectBreakdownInManualReports);
        }

        /// <summary>
        /// Reset all telemetry counters (call from console or script).
        /// </summary>
        [ContextMenu("Reset Counters")]
        public void ResetCounters()
        {
            VelocitySyncTelemetry.Reset();
        }

        /// <summary>
        /// Enable telemetry (call from console or script).
        /// </summary>
        [ContextMenu("Enable Telemetry")]
        public void EnableTelemetry()
        {
            enableTelemetry = true;
            VelocitySyncTelemetry.IsEnabled = true;
            GONetLog.Info("[VelocitySyncTelemetry] Telemetry ENABLED");
        }

        /// <summary>
        /// Disable telemetry (call from console or script).
        /// </summary>
        [ContextMenu("Disable Telemetry")]
        public void DisableTelemetry()
        {
            enableTelemetry = false;
            VelocitySyncTelemetry.IsEnabled = false;
            GONetLog.Info("[VelocitySyncTelemetry] Telemetry DISABLED");
        }
    }
}
