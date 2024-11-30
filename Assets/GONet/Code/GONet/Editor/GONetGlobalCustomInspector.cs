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
        }
    }

}
