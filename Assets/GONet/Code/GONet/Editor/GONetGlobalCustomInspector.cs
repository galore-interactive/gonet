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

using GONet.Generation;
using UnityEditor;
using UnityEngine;

namespace GONet.Editor
{
    [CustomEditor(typeof(GONetGlobal))]
    public class GONetGlobalCustomInspector : GNPListCustomInspector
    {
        private GONetGlobal targetGNG;
        private SerializedProperty fieldToMonitor_valueBlendingBufferLeadTimeMilliseconds;

        private void OnEnable()
        {
            targetGNG = (GONetGlobal)target;

            fieldToMonitor_valueBlendingBufferLeadTimeMilliseconds = serializedObject.FindProperty(nameof(GONetGlobal.valueBlendingBufferLeadTimeMilliseconds));
        }

        public override void OnInspectorGUI()
        {
            bool guiEnabledPrevious = GUI.enabled;
            GUI.enabled = !Application.isPlaying;

            serializedObject.Update();

            int previousValue = fieldToMonitor_valueBlendingBufferLeadTimeMilliseconds.intValue;

            EditorGUI.BeginChangeCheck();

            DrawDefaultInspector();

            // Apply changes and report as dirty if the monitored field is modified
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                int newValue = fieldToMonitor_valueBlendingBufferLeadTimeMilliseconds.intValue;

                if (newValue != previousValue)
                {
                    GONetSpawnSupport_DesignTime.AddGONetDesignTimeDirtyReason(
                        $"{nameof(GONetGlobal)} at '{DesignTimeMetadata.GetFullPath(targetGNG.GetComponent<GONetParticipant>())}' has changed the monitored field '{fieldToMonitor_valueBlendingBufferLeadTimeMilliseconds.displayName}' in editor.  NOTE: The path to the GameObject that changed might incorrectly list it is in the project, when in fact it is in a scene.");

                    EditorUtility.SetDirty(targetGNG);
                }
            }

            const string ALL = "ALL Enabled GONetParticipants:";
            DrawGNPList(targetGNG.EnabledGONetParticipants, ALL, false);

            GUI.enabled = guiEnabledPrevious;

            // Live Metrics Section (Play Mode Only)
            if (Application.isPlaying)
            {
                DrawLiveMetrics();
                Repaint(); // Force continuous update during play mode
            }
        }

        private void DrawLiveMetrics()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Live Metrics (Play Mode)", EditorStyles.boldLabel);

            var metrics = GONetMain.GetRingBufferMetrics();

            if (metrics.Length == 0)
            {
                EditorGUILayout.HelpBox("Ring buffers not initialized yet. Metrics will appear once network threads start.", MessageType.Info);
                return;
            }

            // Display metrics for each ring buffer
            foreach (var metric in metrics)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Thread: {metric.ThreadName}", EditorStyles.miniBoldLabel);

                EditorGUI.indentLevel++;

                // Capacity and current count
                EditorGUILayout.LabelField("Capacity", metric.Capacity.ToString("N0"));
                EditorGUILayout.LabelField("Current Count", $"{metric.Count:N0} ({metric.FillPercentage:P1})");

                // Color-code fill percentage for visual feedback
                Color fillColor = GetFillColor(metric.FillPercentage);
                var previousColor = GUI.color;
                GUI.color = fillColor;
                EditorGUILayout.LabelField("Fill Status", GetFillStatus(metric.FillPercentage));
                GUI.color = previousColor;

                // Peak count
                EditorGUILayout.LabelField("Peak Count", metric.PeakCount.ToString("N0"));

                // Resize count
                string resizeText = metric.ResizeCount == 0 ? "0 (never resized)" : metric.ResizeCount.ToString();
                EditorGUILayout.LabelField("Times Resized", resizeText);

                // Memory estimate (approximate)
                int memoryKB = (metric.Capacity * 8 + 128 + 24) / 1024;
                EditorGUILayout.LabelField("Memory Usage", $"~{memoryKB} KB");

                EditorGUI.indentLevel--;
            }

            // Summary section
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Summary", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            int totalCapacity = 0;
            int totalCount = 0;
            int totalMemoryKB = 0;

            foreach (var metric in metrics)
            {
                totalCapacity += metric.Capacity;
                totalCount += metric.Count;
                totalMemoryKB += (metric.Capacity * 8 + 128 + 24) / 1024;
            }

            EditorGUILayout.LabelField("Total Capacity", totalCapacity.ToString("N0"));
            EditorGUILayout.LabelField("Total Events", $"{totalCount:N0} ({(float)totalCount / totalCapacity:P1})");
            EditorGUILayout.LabelField("Total Memory", $"~{totalMemoryKB} KB");

            EditorGUI.indentLevel--;
        }

        private Color GetFillColor(float fillPercentage)
        {
            if (fillPercentage < 0.5f) return Color.green;
            if (fillPercentage < 0.75f) return Color.yellow;
            return Color.red;
        }

        private string GetFillStatus(float fillPercentage)
        {
            if (fillPercentage < 0.5f) return "Healthy (< 50%)";
            if (fillPercentage < 0.75f) return "Moderate (50-75%)";
            if (fillPercentage < 0.9f) return "High (75-90%)";
            return "Critical (> 90%)";
        }
    }

}
