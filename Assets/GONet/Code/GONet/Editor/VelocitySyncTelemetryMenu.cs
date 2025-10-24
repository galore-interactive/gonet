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

using UnityEditor;
using UnityEngine;

namespace GONet.Editor
{
    /// <summary>
    /// Editor menu items for velocity sync telemetry reporting.
    /// </summary>
    public static class VelocitySyncTelemetryMenu
    {
        [MenuItem("GONet/Velocity Sync Telemetry/Generate Report", false, 100)]
        public static void GenerateReport()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Telemetry Report",
                    "Telemetry reporting is only available during Play mode.",
                    "OK");
                return;
            }

            if (!VelocitySyncTelemetry.IsEnabled)
            {
                EditorUtility.DisplayDialog("Telemetry Report",
                    "Telemetry is currently DISABLED.\n\n" +
                    "Enable telemetry via:\n" +
                    "1. GONet → Velocity Sync Telemetry → Enable\n" +
                    "2. Or add VelocitySyncTelemetryReporter component to scene",
                    "OK");
                return;
            }

            VelocitySyncTelemetry.LogReport(includePerObjectBreakdown: false);
            Debug.Log("[VelocitySyncTelemetry] Report generated (check Console for output)");
        }

        [MenuItem("GONet/Velocity Sync Telemetry/Generate Detailed Report (Per-Object)", false, 101)]
        public static void GenerateDetailedReport()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Telemetry Report",
                    "Telemetry reporting is only available during Play mode.",
                    "OK");
                return;
            }

            if (!VelocitySyncTelemetry.IsEnabled)
            {
                EditorUtility.DisplayDialog("Telemetry Report",
                    "Telemetry is currently DISABLED.\n\n" +
                    "Enable telemetry via:\n" +
                    "1. GONet → Velocity Sync Telemetry → Enable\n" +
                    "2. Or add VelocitySyncTelemetryReporter component to scene",
                    "OK");
                return;
            }

            if (!VelocitySyncTelemetry.EnablePerObjectTracking)
            {
                EditorUtility.DisplayDialog("Detailed Telemetry Report",
                    "Per-object tracking is currently DISABLED.\n\n" +
                    "Enable per-object tracking via:\n" +
                    "1. GONet → Velocity Sync Telemetry → Enable Per-Object Tracking\n" +
                    "2. Or enable in VelocitySyncTelemetryReporter inspector",
                    "OK");
                return;
            }

            VelocitySyncTelemetry.LogReport(includePerObjectBreakdown: true);
            Debug.Log("[VelocitySyncTelemetry] Detailed report generated (check Console for output)");
        }

        [MenuItem("GONet/Velocity Sync Telemetry/Reset Counters", false, 120)]
        public static void ResetCounters()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Reset Telemetry",
                    "Telemetry reset is only available during Play mode.",
                    "OK");
                return;
            }

            VelocitySyncTelemetry.Reset();
            Debug.Log("[VelocitySyncTelemetry] Counters reset");
        }

        [MenuItem("GONet/Velocity Sync Telemetry/Enable", false, 140)]
        public static void EnableTelemetry()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Enable Telemetry",
                    "Telemetry can only be enabled during Play mode.\n\n" +
                    "To enable telemetry:\n" +
                    "1. Enter Play mode\n" +
                    "2. Or add VelocitySyncTelemetryReporter component to scene before Play",
                    "OK");
                return;
            }

            VelocitySyncTelemetry.IsEnabled = true;
            Debug.Log("[VelocitySyncTelemetry] Telemetry ENABLED");
        }

        [MenuItem("GONet/Velocity Sync Telemetry/Disable", false, 141)]
        public static void DisableTelemetry()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Disable Telemetry",
                    "Telemetry can only be disabled during Play mode.",
                    "OK");
                return;
            }

            VelocitySyncTelemetry.IsEnabled = false;
            Debug.Log("[VelocitySyncTelemetry] Telemetry DISABLED");
        }

        [MenuItem("GONet/Velocity Sync Telemetry/Enable Per-Object Tracking", false, 160)]
        public static void EnablePerObjectTracking()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Enable Per-Object Tracking",
                    "Per-object tracking can only be enabled during Play mode.",
                    "OK");
                return;
            }

            VelocitySyncTelemetry.EnablePerObjectTracking = true;
            Debug.Log("[VelocitySyncTelemetry] Per-object tracking ENABLED");
        }

        [MenuItem("GONet/Velocity Sync Telemetry/Disable Per-Object Tracking", false, 161)]
        public static void DisablePerObjectTracking()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Disable Per-Object Tracking",
                    "Per-object tracking can only be disabled during Play mode.",
                    "OK");
                return;
            }

            VelocitySyncTelemetry.EnablePerObjectTracking = false;
            Debug.Log("[VelocitySyncTelemetry] Per-object tracking DISABLED");
        }
    }
}
