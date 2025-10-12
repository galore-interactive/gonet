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
    /// Adaptive pool scaling manager for GONet packet pools.
    ///
    /// PHILOSOPHY: "Do what the user wants, warn when risky"
    ///
    /// - DEFAULT BEHAVIOR: Auto-scale pool size based on actual network demand
    /// - Scales UP when utilization exceeds thresholds (prevents packet drops)
    /// - Scales DOWN when utilization is consistently low (conserves memory)
    /// - WARNS AGGRESSIVELY when approaching memory/bandwidth limits
    /// - Respects absolute maximum (expert override for bandwidth-constrained scenarios)
    ///
    /// DESIGN RATIONALE:
    /// The original fixed-size pool (1000 packets) was arbitrary and caused failures at high spawn rates.
    /// Adaptive scaling eliminates this limitation while still allowing experts to cap bandwidth if needed.
    ///
    /// SCALING ALGORITHM:
    /// - Monitors pool utilization every UPDATE_INTERVAL seconds
    /// - HIGH utilization (>75%): Scale up by SCALE_UP_FACTOR (1.5x)
    /// - LOW utilization (<25% for SCALE_DOWN_DELAY seconds): Scale down to baseline
    /// - CEILING: Never exceeds maxPacketsPerTick (absolute safety limit)
    /// - FLOOR: Never goes below adaptivePoolBaselineSize
    ///
    /// October 2025 - Adaptive Scaling Implementation
    /// </summary>
    public class GONetAdaptivePoolScaler
    {
        // ========================================
        // Configuration (from GONetGlobal)
        // ========================================
        private bool enableAdaptiveScaling;
        private int baselineSize;
        private int absoluteMaxSize;

        // ========================================
        // Current State
        // ========================================
        private int currentPoolSize;
        private int peakUtilization;
        private float lastUpdateTime;
        private float lastScaleUpTime;
        private float lastScaleDownTime;
        private int scaleUpCount;
        private int scaleDownCount;

        // ========================================
        // Tuning Constants
        // ========================================
        private const float UPDATE_INTERVAL = 1.0f;           // Check utilization every 1 second
        private const float SCALE_UP_THRESHOLD = 0.75f;       // Scale up when >75% utilized
        private const float SCALE_DOWN_THRESHOLD = 0.25f;     // Scale down when <25% utilized
        private const float SCALE_DOWN_DELAY = 5.0f;          // Wait 5 seconds of low util before scaling down
        private const float SCALE_UP_FACTOR = 1.5f;           // Grow by 50% each time
        private const int MIN_SCALE_UP_INCREMENT = 100;       // Minimum growth: 100 packets

        // Warning thresholds
        private const float CEILING_WARNING_THRESHOLD = 0.90f; // Warn when within 90% of max
        private const float MEMORY_WARNING_MB = 500;           // Warn if pool memory exceeds 500MB

        // Throttling for log spam prevention
        private float lastCeilingWarningTime;
        private float lastMemoryWarningTime;
        private const float WARNING_THROTTLE_SECONDS = 10.0f;

        /// <summary>
        /// Initializes adaptive pool scaler with settings from GONetGlobal.
        /// </summary>
        public GONetAdaptivePoolScaler(GONetGlobal config) : this(config, Time.time)
        {
        }

        /// <summary>
        /// Initializes adaptive pool scaler with explicit time (for testing).
        /// </summary>
        /// <param name="config">GONetGlobal configuration</param>
        /// <param name="initialTime">Initial time in seconds (for testing)</param>
        public GONetAdaptivePoolScaler(GONetGlobal config, float initialTime)
        {
            RefreshConfiguration(config);

            currentPoolSize = enableAdaptiveScaling ? baselineSize : absoluteMaxSize;
            peakUtilization = 0;
            lastUpdateTime = initialTime;
            lastScaleUpTime = initialTime;
            lastScaleDownTime = initialTime;
            scaleUpCount = 0;
            scaleDownCount = 0;

            GONetLog.Info($"[ADAPTIVE-POOL] Initialized with mode: {(enableAdaptiveScaling ? "ADAPTIVE" : "FIXED")} | " +
                         $"Current size: {currentPoolSize} | Max ceiling: {absoluteMaxSize}");
        }

        /// <summary>
        /// Refreshes configuration from GONetGlobal (allows runtime changes in inspector).
        /// </summary>
        public void RefreshConfiguration(GONetGlobal config)
        {
            enableAdaptiveScaling = config.enableAdaptivePoolScaling;
            baselineSize = config.adaptivePoolBaselineSize;
            absoluteMaxSize = config.maxPacketsPerTick;

            // Validate configuration
            if (baselineSize > absoluteMaxSize)
            {
                GONetLog.Warning($"[ADAPTIVE-POOL] Invalid config: baseline ({baselineSize}) > max ({absoluteMaxSize}). " +
                                $"Clamping baseline to max.");
                baselineSize = absoluteMaxSize;
            }
        }

        /// <summary>
        /// Returns the current effective pool size limit.
        /// This is the value that should be used for flow control checks.
        /// </summary>
        public int GetCurrentPoolSize()
        {
            return currentPoolSize;
        }

        /// <summary>
        /// Updates adaptive scaling based on current pool utilization.
        /// Call this periodically (e.g., from GONet main loop).
        /// </summary>
        /// <param name="currentBorrowedCount">Number of packets currently borrowed from pool</param>
        /// <param name="numConnectedClients">Number of connected clients (for scaling heuristics)</param>
        public void Update(int currentBorrowedCount, int numConnectedClients)
        {
            Update(currentBorrowedCount, numConnectedClients, Time.time);
        }

        /// <summary>
        /// Updates adaptive scaling with explicit time (for testing).
        /// </summary>
        /// <param name="currentBorrowedCount">Number of packets currently borrowed from pool</param>
        /// <param name="numConnectedClients">Number of connected clients (for scaling heuristics)</param>
        /// <param name="currentTime">Current time in seconds (for testing - normally use Time.time)</param>
        public void Update(int currentBorrowedCount, int numConnectedClients, float currentTime)
        {
            float now = currentTime;

            // Only update at specified interval
            if (now - lastUpdateTime < UPDATE_INTERVAL)
            {
                // Track peak utilization between updates
                peakUtilization = Mathf.Max(peakUtilization, currentBorrowedCount);
                return;
            }

            // Use peak utilization from the interval (not just current snapshot)
            int utilizationToCheck = Mathf.Max(currentBorrowedCount, peakUtilization);
            float utilizationPercent = (float)utilizationToCheck / currentPoolSize;

            // Reset peak for next interval
            peakUtilization = 0;
            lastUpdateTime = now;

            // If adaptive scaling is disabled, just use fixed size
            if (!enableAdaptiveScaling)
            {
                if (currentPoolSize != absoluteMaxSize)
                {
                    currentPoolSize = absoluteMaxSize;
                    GONetLog.Info($"[ADAPTIVE-POOL] Adaptive scaling DISABLED - using fixed size: {currentPoolSize}");
                }
                return;
            }

            // === SCALE UP LOGIC ===
            if (utilizationPercent > SCALE_UP_THRESHOLD)
            {
                // HIGH UTILIZATION: Need to grow pool
                int proposedSize = Mathf.CeilToInt(currentPoolSize * SCALE_UP_FACTOR);
                int increment = proposedSize - currentPoolSize;

                // Ensure minimum growth increment
                if (increment < MIN_SCALE_UP_INCREMENT)
                {
                    increment = MIN_SCALE_UP_INCREMENT;
                    proposedSize = currentPoolSize + increment;
                }

                // Respect absolute ceiling
                int newSize = Mathf.Min(proposedSize, absoluteMaxSize);

                if (newSize > currentPoolSize)
                {
                    int oldSize = currentPoolSize;
                    currentPoolSize = newSize;
                    scaleUpCount++;
                    lastScaleUpTime = now;

                    GONetLog.Info($"[ADAPTIVE-POOL] ⬆️ SCALED UP: {oldSize} → {currentPoolSize} ({increment:+0} packets) | " +
                                 $"Utilization: {utilizationPercent:P} | Clients: {numConnectedClients} | Scale-ups: {scaleUpCount}");

                    // Check if we hit the ceiling
                    if (currentPoolSize >= absoluteMaxSize)
                    {
                        LogCeilingWarning(utilizationPercent, currentBorrowedCount, numConnectedClients);
                    }
                }
                else if (newSize == absoluteMaxSize && currentPoolSize == absoluteMaxSize)
                {
                    // Already at ceiling and still high utilization - aggressive warning
                    LogCeilingWarning(utilizationPercent, currentBorrowedCount, numConnectedClients);
                }
            }
            // === SCALE DOWN LOGIC ===
            else if (utilizationPercent < SCALE_DOWN_THRESHOLD)
            {
                // LOW UTILIZATION: Consider scaling down (but wait for sustained low usage)
                float timeSinceLastScaleUp = now - lastScaleUpTime;

                if (timeSinceLastScaleUp > SCALE_DOWN_DELAY && currentPoolSize > baselineSize)
                {
                    int oldSize = currentPoolSize;
                    currentPoolSize = baselineSize; // Drop directly to baseline (no gradual scale-down)
                    scaleDownCount++;
                    lastScaleDownTime = now;

                    GONetLog.Info($"[ADAPTIVE-POOL] ⬇️ SCALED DOWN: {oldSize} → {currentPoolSize} ({currentPoolSize - oldSize} packets) | " +
                                 $"Utilization: {utilizationPercent:P} | Reason: Sustained low usage for {SCALE_DOWN_DELAY}s");
                }
            }

            // Memory usage warning
            CheckMemoryUsage(utilizationToCheck);
        }

        /// <summary>
        /// Logs aggressive warning when pool hits ceiling and is still highly utilized.
        /// This indicates the user needs to either increase the ceiling or optimize their sync patterns.
        /// </summary>
        private void LogCeilingWarning(float utilization, int borrowedCount, int numClients)
        {
            float now = Time.time;

            // Throttle warnings to avoid spam
            if (now - lastCeilingWarningTime < WARNING_THROTTLE_SECONDS)
                return;

            lastCeilingWarningTime = now;

            string message = $"⚠️⚠️⚠️ [ADAPTIVE-POOL] CEILING REACHED ⚠️⚠️⚠️\n" +
                            $"Pool has hit absolute maximum size: {absoluteMaxSize} packets\n" +
                            $"Current utilization: {utilization:P} ({borrowedCount}/{currentPoolSize} borrowed)\n" +
                            $"Connected clients: {numClients}\n" +
                            $"\n" +
                            $"🔴 CRITICAL: Pool cannot grow further!\n" +
                            $"\n" +
                            $"This means GONet is trying to do MORE than your configured maximum allows.\n" +
                            $"GONet will continue operating and drop unreliable packets when necessary,\n" +
                            $"but you should take action to prevent degraded performance:\n" +
                            $"\n" +
                            $"RECOMMENDED ACTIONS:\n" +
                            $"1. ✅ INCREASE maxPacketsPerTick in GONetGlobal inspector\n" +
                            $"   • Current: {absoluteMaxSize}\n" +
                            $"   • Recommended: {absoluteMaxSize * 2} or higher\n" +
                            $"   • Note: This is a safety ceiling, not a performance limit!\n" +
                            $"\n" +
                            $"2. 🔍 INVESTIGATE spawn patterns:\n" +
                            $"   • Are you spawning too many objects at once?\n" +
                            $"   • Consider staggering spawns over multiple frames\n" +
                            $"\n" +
                            $"3. ⚙️ OPTIMIZE sync profiles:\n" +
                            $"   • Reduce sync frequency for distant/irrelevant objects\n" +
                            $"   • Use interpolation instead of high-frequency position sync\n" +
                            $"\n" +
                            $"4. 🎯 EXPERT OVERRIDE (bandwidth-constrained scenarios only):\n" +
                            $"   • If you INTENTIONALLY capped maxPacketsPerTick for bandwidth reasons,\n" +
                            $"     this warning is expected and can be ignored.\n" +
                            $"   • Set enableCongestionLogging=false to suppress warnings.\n";

            GONetLog.Warning(message);
        }

        /// <summary>
        /// Checks estimated memory usage and warns if excessive.
        /// </summary>
        private void CheckMemoryUsage(int borrowedCount)
        {
            float now = Time.time;

            // Throttle warnings
            if (now - lastMemoryWarningTime < WARNING_THROTTLE_SECONDS)
                return;

            // Estimate memory usage (assume average of 1KB per packet in TieredArrayPool)
            // This is conservative - actual usage depends on message sizes
            float estimatedMemoryMB = (borrowedCount * 1024f) / (1024f * 1024f);

            if (estimatedMemoryMB > MEMORY_WARNING_MB)
            {
                lastMemoryWarningTime = now;

                GONetLog.Warning($"⚠️ [ADAPTIVE-POOL] HIGH MEMORY USAGE\n" +
                                $"Estimated pool memory: ~{estimatedMemoryMB:F1} MB ({borrowedCount:N0} packets borrowed)\n" +
                                $"Threshold: {MEMORY_WARNING_MB} MB\n" +
                                $"\n" +
                                $"This is not necessarily a problem, but indicates heavy network traffic.\n" +
                                $"Monitor for performance issues and consider optimizing sync patterns if needed.");
            }
        }

        /// <summary>
        /// Gets detailed diagnostics for logging/debugging.
        /// </summary>
        public string GetDiagnostics()
        {
            return $"[ADAPTIVE-POOL] Mode: {(enableAdaptiveScaling ? "ADAPTIVE" : "FIXED")} | " +
                   $"Current: {currentPoolSize} | Baseline: {baselineSize} | Max: {absoluteMaxSize} | " +
                   $"Scale-ups: {scaleUpCount} | Scale-downs: {scaleDownCount}";
        }
    }
}
