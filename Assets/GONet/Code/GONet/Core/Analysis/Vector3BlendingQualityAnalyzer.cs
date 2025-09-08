// Add this class to your project for quality analysis
using GONet.PluginAPI;
using GONet;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public static class Vector3BlendingQualityAnalyzer
{
    private static bool _isLoggingEnabled = false;
    private static string _logFilePath = "Vector3BlendingAnalysis.csv";
    private static System.IO.StreamWriter _logWriter;
    private static readonly object _logLock = new object();

    // Metrics tracking
    private static readonly Dictionary<int, BlendingMetrics> _metricsPerObject = new Dictionary<int, BlendingMetrics>();

    private class BlendingMetrics
    {
        public int ObjectId;
        public List<BlendingDataPoint> DataPoints = new List<BlendingDataPoint>();
        public Vector3 LastBlendedValue;
        public Vector3 LastRawValue;
        public float MaxJerk;
        public float AverageJerk;
        public int SmoothingApplicationCount;
        public int TotalBlendCount;
    }

    private class BlendingDataPoint
    {
        public long Timestamp;
        public long AtElapsedTicks;
        public int ValueCount;
        public int ValueCountUsable;
        public Vector3 RawBlendedValue;
        public Vector3 SmoothedValue;
        public bool DidApplySmoothing;
        public bool DidExtrapolate;
        public string ExtrapolationMethod; // "bezier", "acceleration", "average", "none"
        public float SmoothingStrength;
        public string SmoothingReason; // "at-rest", "direction-change", "jitter", "none"

        // Motion analysis
        public float Velocity;
        public float Acceleration;
        public float Jerk; // Rate of change of acceleration
        public float DirectionChangeAngle;

        // Buffer snapshot
        public Vector3[] BufferValues;
        public long[] BufferTicks;
    }

    public static void StartLogging(string filePath = null)
    {
        lock (_logLock)
        {
            if (_isLoggingEnabled) return;

            _logFilePath = filePath ?? _logFilePath;
            _logWriter = new System.IO.StreamWriter(_logFilePath, false);

            // Write CSV header
            _logWriter.WriteLine("ObjectId,Timestamp,AtElapsedTicks,ValueCount,ValueCountUsable," +
                "RawX,RawY,RawZ,SmoothedX,SmoothedY,SmoothedZ,DidApplySmoothing,DidExtrapolate," +
                "ExtrapolationMethod,SmoothingStrength,SmoothingReason,Velocity,Acceleration,Jerk," +
                "DirectionChangeAngle,DeltaFromSmoothed,BufferSnapshot");

            _isLoggingEnabled = true;
            GONetLog.Info($"Started Vector3 blending quality logging to: {_logFilePath}");
        }
    }

    public static void StopLogging()
    {
        lock (_logLock)
        {
            if (!_isLoggingEnabled) return;

            // Write summary statistics
            WriteSummaryStatistics();

            _logWriter?.Close();
            _logWriter = null;
            _isLoggingEnabled = false;

            GONetLog.Info("Stopped Vector3 blending quality logging");
        }
    }

    public static void LogBlendingResult(
        int objectId,
        NumericValueChangeSnapshot[] valueBuffer,
        int valueCount,
        long atElapsedTicks,
        Vector3 rawBlendedValue,
        Vector3 finalBlendedValue,
        bool didApplySmoothing,
        bool didExtrapolate,
        int valueCountUsable,
        string smoothingReason = "none",
        float smoothingStrength = 0f)
    {
        if (!_isLoggingEnabled) return;

        lock (_logLock)
        {
            var dataPoint = new BlendingDataPoint
            {
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                AtElapsedTicks = atElapsedTicks,
                ValueCount = valueCount,
                ValueCountUsable = valueCountUsable,
                RawBlendedValue = rawBlendedValue,
                SmoothedValue = finalBlendedValue,
                DidApplySmoothing = didApplySmoothing,
                DidExtrapolate = didExtrapolate,
                SmoothingStrength = smoothingStrength,
                SmoothingReason = smoothingReason
            };

            // Determine extrapolation method
            if (didExtrapolate)
            {
                if (valueCountUsable >= 4)
                    dataPoint.ExtrapolationMethod = "average-acceleration";
                else if (valueCountUsable == 3)
                    dataPoint.ExtrapolationMethod = "simple-acceleration";
                else if (valueCountUsable == 2)
                    dataPoint.ExtrapolationMethod = "bezier";
                else
                    dataPoint.ExtrapolationMethod = "unknown";
            }
            else
            {
                dataPoint.ExtrapolationMethod = "none";
            }

            // Capture buffer snapshot
            dataPoint.BufferValues = new Vector3[valueCount];
            dataPoint.BufferTicks = new long[valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                dataPoint.BufferValues[i] = valueBuffer[i].numericValue.UnityEngine_Vector3;
                dataPoint.BufferTicks[i] = valueBuffer[i].elapsedTicksAtChange;
            }

            // Calculate motion metrics
            if (!_metricsPerObject.TryGetValue(objectId, out var metrics))
            {
                metrics = new BlendingMetrics { ObjectId = objectId };
                _metricsPerObject[objectId] = metrics;
            }

            CalculateMotionMetrics(dataPoint, metrics);

            // Write to CSV
            string bufferSnapshot = string.Join(";", dataPoint.BufferValues.Select((v, i) =>
                $"{v.x:F3},{v.y:F3},{v.z:F3}@{dataPoint.BufferTicks[i]}"));

            float deltaFromSmoothed = (rawBlendedValue - finalBlendedValue).magnitude;

            _logWriter.WriteLine($"{objectId},{dataPoint.Timestamp},{atElapsedTicks}," +
                $"{valueCount},{valueCountUsable}," +
                $"{rawBlendedValue.x:F6},{rawBlendedValue.y:F6},{rawBlendedValue.z:F6}," +
                $"{finalBlendedValue.x:F6},{finalBlendedValue.y:F6},{finalBlendedValue.z:F6}," +
                $"{didApplySmoothing},{didExtrapolate},{dataPoint.ExtrapolationMethod}," +
                $"{smoothingStrength:F3},{smoothingReason}," +
                $"{dataPoint.Velocity:F3},{dataPoint.Acceleration:F3},{dataPoint.Jerk:F3}," +
                $"{dataPoint.DirectionChangeAngle:F1},{deltaFromSmoothed:F6}," +
                $"\"{bufferSnapshot}\"");

            // Update metrics
            metrics.DataPoints.Add(dataPoint);
            metrics.LastBlendedValue = finalBlendedValue;
            metrics.LastRawValue = rawBlendedValue;
            metrics.TotalBlendCount++;
            if (didApplySmoothing) metrics.SmoothingApplicationCount++;

            // Flush periodically for real-time analysis
            if (metrics.TotalBlendCount % 100 == 0)
            {
                _logWriter.Flush();
            }
        }
    }

private static void CalculateMotionMetrics(BlendingDataPoint current, BlendingMetrics metrics)
    {
        if (metrics.DataPoints.Count < 2) return;

        var prev = metrics.DataPoints[metrics.DataPoints.Count - 1];
        var prevPrev = metrics.DataPoints.Count > 2 ? metrics.DataPoints[metrics.DataPoints.Count - 2] : null;

        // Time deltas
        float dt = (current.AtElapsedTicks - prev.AtElapsedTicks) * 1e-7f;
        if (dt <= 0) return;

        // Velocity
        Vector3 velocity = (current.SmoothedValue - prev.SmoothedValue) / dt;
        current.Velocity = velocity.magnitude;

        // Direction change
        if (prev.Velocity > 0.01f && current.Velocity > 0.01f)
        {
            Vector3 prevVel = (prev.SmoothedValue - (prevPrev?.SmoothedValue ?? prev.SmoothedValue)) / dt;
            float dot = Vector3.Dot(velocity.normalized, prevVel.normalized);
            current.DirectionChangeAngle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
        }

        // Acceleration
        if (prevPrev != null)
        {
            float prevDt = (prev.AtElapsedTicks - prevPrev.AtElapsedTicks) * 1e-7f;
            if (prevDt > 0)
            {
                current.Acceleration = (current.Velocity - prev.Velocity) / dt;

                // Jerk (rate of change of acceleration)
                current.Jerk = (current.Acceleration - prev.Acceleration) / dt;

                // Track max jerk
                float absJerk = Mathf.Abs(current.Jerk);
                if (absJerk > metrics.MaxJerk)
                    metrics.MaxJerk = absJerk;
            }
        }
    }

    private static void WriteSummaryStatistics()
    {
        _logWriter.WriteLine("\n\nSUMMARY STATISTICS");
        _logWriter.WriteLine("==================");

        foreach (var kvp in _metricsPerObject)
        {
            var metrics = kvp.Value;
            var objectId = kvp.Key;

            _logWriter.WriteLine($"\nObject {objectId}:");
            _logWriter.WriteLine($"  Total Blends: {metrics.TotalBlendCount}");
            _logWriter.WriteLine($"  Smoothing Applied: {metrics.SmoothingApplicationCount} ({100f * metrics.SmoothingApplicationCount / metrics.TotalBlendCount:F1}%)");
            _logWriter.WriteLine($"  Max Jerk: {metrics.MaxJerk:F3}");

            // Calculate average jerk
            float totalJerk = metrics.DataPoints.Sum(dp => Mathf.Abs(dp.Jerk));
            metrics.AverageJerk = totalJerk / metrics.DataPoints.Count;
            _logWriter.WriteLine($"  Average Jerk: {metrics.AverageJerk:F3}");

            // Smoothing reason breakdown
            var reasonCounts = metrics.DataPoints
                .Where(dp => dp.DidApplySmoothing)
                .GroupBy(dp => dp.SmoothingReason)
                .ToDictionary(g => g.Key, g => g.Count());

            _logWriter.WriteLine("  Smoothing Reasons:");
            foreach (var reason in reasonCounts)
            {
                _logWriter.WriteLine($"    {reason.Key}: {reason.Value}");
            }

            // Extrapolation method breakdown
            var methodCounts = metrics.DataPoints
                .Where(dp => dp.DidExtrapolate)
                .GroupBy(dp => dp.ExtrapolationMethod)
                .ToDictionary(g => g.Key, g => g.Count());

            _logWriter.WriteLine("  Extrapolation Methods:");
            foreach (var method in methodCounts)
            {
                _logWriter.WriteLine($"    {method.Key}: {method.Value}");
            }
        }
    }
}
