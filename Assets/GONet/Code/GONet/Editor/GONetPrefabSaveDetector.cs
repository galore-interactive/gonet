using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GONet.Editor
{
    /// <summary>
    /// Detects and filters out false positive dirty detections caused by Unity's internal prefab save behavior.
    /// When Unity saves a prefab after editing, it calls OnDisable/OnEnable in rapid succession on the prefab asset.
    /// This is NOT a user action and should be ignored for dirty detection purposes.
    /// </summary>
    public static class GONetPrefabSaveDetector
    {
        // Track OnDisable events that might be followed by OnEnable
        private static Dictionary<string, double> pendingDisableEvents = new Dictionary<string, double>();
        // Track completed pairs to skip both events
        private static Dictionary<string, double> completedPairs = new Dictionary<string, double>();
        private static readonly double EVENT_GROUPING_THRESHOLD = 0.5; // 500ms - events within this time are considered part of the same action

        /// <summary>
        /// Check if this event should be skipped because it's part of Unity's internal prefab save behavior.
        /// Returns true if the event should be skipped (not logged as dirty).
        /// </summary>
        public static bool ShouldSkipPrefabEvent(string prefabPath, string eventType)
        {
            double currentTime = EditorApplication.timeSinceStartup;

            // Clean up old entries first
            CleanupOldEntries(currentTime);

            if (eventType == "OnDisable")
            {
                // Check if this is part of a recently completed pair (in case of multiple rapid calls)
                if (completedPairs.TryGetValue(prefabPath, out double pairTime))
                {
                    if (currentTime - pairTime < EVENT_GROUPING_THRESHOLD)
                    {
                        GONet.GONetLog.Debug($"[GONetPrefabSaveDetector] Skipping OnDisable for {prefabPath} - part of completed pair");
                        return true;
                    }
                }

                // Store this OnDisable as pending - we'll wait to see if OnEnable follows
                pendingDisableEvents[prefabPath] = currentTime;
                GONet.GONetLog.Debug($"[GONetPrefabSaveDetector] Pending OnDisable for {prefabPath} - waiting for potential OnEnable pair");
                // Don't skip yet - we'll retroactively handle this if OnEnable comes
                return true; // Actually skip it preemptively, assuming OnEnable will follow
            }
            else if (eventType == "OnEnable")
            {
                // Check if there's a recent OnDisable for this prefab
                if (pendingDisableEvents.TryGetValue(prefabPath, out double disableTime))
                {
                    double timeSince = currentTime - disableTime;
                    if (timeSince < EVENT_GROUPING_THRESHOLD)
                    {
                        // This is a disable/enable pair - mark as completed and skip both
                        completedPairs[prefabPath] = currentTime;
                        pendingDisableEvents.Remove(prefabPath);
                        GONet.GONetLog.Debug($"[GONetPrefabSaveDetector] Skipping OnEnable for {prefabPath} - completes rapid pair ({timeSince:F3}s after OnDisable)");
                        return true;
                    }
                }

                // Check if this is part of a recently completed pair
                if (completedPairs.TryGetValue(prefabPath, out double pairTime))
                {
                    if (currentTime - pairTime < EVENT_GROUPING_THRESHOLD)
                    {
                        GONet.GONetLog.Debug($"[GONetPrefabSaveDetector] Skipping OnEnable for {prefabPath} - part of completed pair");
                        return true;
                    }
                }
            }

            return false; // Don't skip - this appears to be a legitimate user action
        }

        private static void CleanupOldEntries(double currentTime)
        {
            // Clean up old pending disable events
            List<string> toRemove = new List<string>();
            foreach (var kvp in pendingDisableEvents)
            {
                if (currentTime - kvp.Value > EVENT_GROUPING_THRESHOLD * 2)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (string path in toRemove)
            {
                pendingDisableEvents.Remove(path);
            }

            // Clean up old completed pairs
            toRemove.Clear();
            foreach (var kvp in completedPairs)
            {
                if (currentTime - kvp.Value > 2.0)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (string path in toRemove)
            {
                completedPairs.Remove(path);
            }
        }
    }
}