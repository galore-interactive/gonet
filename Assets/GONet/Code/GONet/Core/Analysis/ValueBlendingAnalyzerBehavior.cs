using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GONet.Utils;
using GONet.PluginAPI;

namespace GONet
{
    /// <summary>
    /// Enhanced behavior for analyzing Vector3 blending quality
    /// </summary>
    public class ValueBlendingAnalyzerBehavior : MonoBehaviour
    {
        [Header("Analysis Configuration")]
        [SerializeField] private bool autoStartLogging = true;
        [SerializeField] private string logFilePrefix = "Vector3Analysis";
        [SerializeField] private bool includeTimestamp = true;

        [Header("Analysis Filters")]
        [SerializeField] private int[] objectIdsToTrack = null; // null = track all
        [SerializeField] private float minimumPositionChange = 0.001f; // Ignore tiny movements

        [Header("Real-time Monitoring")]
        [SerializeField] private bool showRealtimeStats = true;
        [SerializeField] private int statsUpdateIntervalMs = 1000;

        private float lastStatsUpdate;
        private Dictionary<int, RealtimeStats> realtimeStats = new Dictionary<int, RealtimeStats>();

        private class RealtimeStats
        {
            public int BlendCount;
            public int SmoothingCount;
            public float AverageJerk;
            public float MaxJerk;
            public float AverageDelta;
            public DateTime LastUpdate;
        }

        void Start()
        {
            if (autoStartLogging)
            {
                string filename = logFilePrefix;
                if (includeTimestamp)
                {
                    filename += "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }
                filename += ".csv";

                Vector3BlendingQualityAnalyzer.StartLogging(filename);
                GONetLog.Info($"Started Vector3 blending analysis logging to: {filename}");
            }

            // Hook into GONet's value blending system
            RegisterBlendingCallbacks();
        }

        void OnDestroy()
        {
            Vector3BlendingQualityAnalyzer.StopLogging();
            UnregisterBlendingCallbacks();
        }

        private void RegisterBlendingCallbacks()
        {
            // This is where we'll hook into the blending system
            // You'll need to add this capability to your GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter
        }

        private void UnregisterBlendingCallbacks()
        {
            // Cleanup callbacks
        }

        void Update()
        {
            if (showRealtimeStats && Time.time - lastStatsUpdate > statsUpdateIntervalMs / 1000f)
            {
                UpdateRealtimeStats();
                lastStatsUpdate = Time.time;
            }
        }

        private void UpdateRealtimeStats()
        {
            // This would be populated by the analyzer
            // For now, showing structure
        }

        void OnGUI()
        {
            if (!showRealtimeStats || realtimeStats.Count == 0) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 600));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Vector3 Blending Quality Monitor", GUI.skin.box);

            foreach (var kvp in realtimeStats)
            {
                var stats = kvp.Value;
                GUILayout.Label($"Object {kvp.Key}:");
                GUILayout.Label($"  Blends: {stats.BlendCount} | Smoothed: {stats.SmoothingCount}");
                GUILayout.Label($"  Jerk: {stats.AverageJerk:F3} avg, {stats.MaxJerk:F3} max");
                GUILayout.Label($"  Avg Delta: {stats.AverageDelta:F6}");
                GUILayout.Space(5);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}

// Enhanced integration for GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter
namespace GONet.PluginAPI
{
    public partial class GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter
    {
        // Add this method to integrate with the analyzer
        private void LogBlendingAnalysis(
            int objectId,
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            Vector3 rawBlendedValue,
            Vector3 finalBlendedValue,
            bool didApplySmoothing,
            bool didExtrapolate,
            int valueCountUsable,
            string context = "")
        {
            // Determine smoothing reason
            string smoothingReason = "none";
            float smoothingStrength = 0f;

            if (didApplySmoothing)
            {
                // Analyze why smoothing was applied
                if (valueCount <= 3)
                {
                    smoothingReason = "low-value-count";
                    smoothingStrength = 1.0f;
                }
                else
                {
                    // Check for at-rest detection
                    if (valueCount <= 2)
                    {
                        float velocity = CalculateVelocityMagnitude(valueBuffer, valueCount);
                        if (velocity < 0.1f)
                        {
                            smoothingReason = "at-rest";
                            smoothingStrength = 0.8f;
                        }
                    }

                    // Check for direction change
                    if (smoothingReason == "none" && valueCount >= 3)
                    {
                        float directionChange = CalculateDirectionChange(valueBuffer);
                        if (directionChange > 90f)
                        {
                            smoothingReason = "direction-change";
                            smoothingStrength = directionChange / 180f;
                        }
                    }

                    // Check for jitter
                    if (smoothingReason == "none")
                    {
                        float jitter = CalculateJitter(valueBuffer, valueCount);
                        if (jitter > 0.1f)
                        {
                            smoothingReason = "jitter";
                            smoothingStrength = Math.Min(jitter / 0.5f, 1.0f);
                        }
                    }
                }
            }

            Vector3BlendingQualityAnalyzer.LogBlendingResult(
                objectId,
                valueBuffer,
                valueCount,
                atElapsedTicks,
                rawBlendedValue,
                finalBlendedValue,
                didApplySmoothing,
                didExtrapolate,
                valueCountUsable,
                smoothingReason,
                smoothingStrength
            );
        }

        private float CalculateVelocityMagnitude(NumericValueChangeSnapshot[] valueBuffer, int valueCount)
        {
            if (valueCount < 2) return 0f;

            Vector3 pos1 = valueBuffer[0].numericValue.UnityEngine_Vector3;
            Vector3 pos2 = valueBuffer[1].numericValue.UnityEngine_Vector3;
            long timeDelta = valueBuffer[0].elapsedTicksAtChange - valueBuffer[1].elapsedTicksAtChange;

            if (timeDelta <= 0) return 0f;

            float deltaTime = timeDelta * 1e-7f;
            return (pos1 - pos2).magnitude / deltaTime;
        }

        private float CalculateJitter(NumericValueChangeSnapshot[] valueBuffer, int valueCount)
        {
            if (valueCount < 3) return 0f;

            float totalVariance = 0f;
            for (int i = 0; i < valueCount - 2; i++)
            {
                Vector3 p0 = valueBuffer[i].numericValue.UnityEngine_Vector3;
                Vector3 p1 = valueBuffer[i + 1].numericValue.UnityEngine_Vector3;
                Vector3 p2 = valueBuffer[i + 2].numericValue.UnityEngine_Vector3;

                Vector3 expectedP1 = (p0 + p2) * 0.5f;
                float variance = (p1 - expectedP1).magnitude;
                totalVariance += variance;
            }

            return totalVariance / (valueCount - 2);
        }

        // Modified TryGetBlendedValue to include logging
        public bool TryGetBlendedValue_WithAnalysis(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            out GONetSyncableValue blendedValue,
            out bool didExtrapolatePastMostRecentChanges,
            int objectId) // Add objectId parameter
        {
            // Store the raw blended value before smoothing
            GONetSyncableValue rawBlendedValue = default;
            bool didApplySmoothing = false;
            int valueCountUsable = valueCount;

            // Call the original method
            bool result = TryGetBlendedValue(valueBuffer, valueCount, atElapsedTicks, out blendedValue, out didExtrapolatePastMostRecentChanges);

            if (result)
            {
                // The blendedValue before smoothing would need to be captured within TryGetBlendedValue
                // For now, we'll log what we have
                rawBlendedValue = blendedValue; // This would be set before smoothing in the actual implementation

                // Log the blending analysis
                LogBlendingAnalysis(
                    objectId,
                    valueBuffer,
                    valueCount,
                    atElapsedTicks,
                    rawBlendedValue.UnityEngine_Vector3,
                    blendedValue.UnityEngine_Vector3,
                    didApplySmoothing,
                    didExtrapolatePastMostRecentChanges,
                    valueCountUsable
                );
            }

            return result;
        }
    }
}

// Additional analysis utilities
namespace GONet.Utils
{
    public static class Vector3BlendingQualityMetrics
    {
        /// <summary>
        /// Calculate a composite quality score (0-100) for the blending results
        /// </summary>
        public static float CalculateQualityScore(
            float jerk,
            float smoothingPercentage,
            float averageDelta,
            float directionChangeFrequency)
        {
            // Lower jerk is better (weight: 40%)
            float jerkScore = Mathf.Clamp01(1f - (jerk / 100f)) * 40f;

            // Moderate smoothing is ideal (weight: 20%)
            float smoothingScore = (1f - Mathf.Abs(smoothingPercentage - 0.3f) / 0.7f) * 20f;

            // Lower delta is better (weight: 25%)
            float deltaScore = Mathf.Clamp01(1f - (averageDelta / 1f)) * 25f;

            // Fewer direction changes is better (weight: 15%)
            float directionScore = Mathf.Clamp01(1f - directionChangeFrequency) * 15f;

            return jerkScore + smoothingScore + deltaScore + directionScore;
        }

        /// <summary>
        /// Analyze buffer health and suggest improvements
        /// </summary>
        public static string AnalyzeBufferHealth(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            float minTimeBetween,
            float maxTimeBetween,
            float avgTimeBetween)
        {
            List<string> issues = new List<string>();

            // Check for timing irregularities
            float timeVariance = maxTimeBetween - minTimeBetween;
            if (timeVariance > avgTimeBetween * 0.5f)
            {
                issues.Add($"High timing variance: {timeVariance:F1}ms (avg: {avgTimeBetween:F1}ms)");
            }

            // Check for gaps
            if (maxTimeBetween > avgTimeBetween * 2f)
            {
                issues.Add($"Large gaps detected: max {maxTimeBetween:F1}ms");
            }

            // Check buffer utilization
            if (valueCount < 3)
            {
                issues.Add($"Low buffer utilization: only {valueCount} values");
            }

            return issues.Count > 0 ? string.Join("; ", issues) : "Healthy";
        }
    }
}