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

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GONet.Editor
{
    [CustomEditor(typeof(GONetLocal))]
    public class GONetLocalCustomInspector : GNPListCustomInspector
    {
        GONetLocal targetGNL;

        private void OnEnable()
        {
            targetGNL = (GONetLocal)target;

            /* this will not work since it runs on a non-main unity thread...darn...will use RequiresConstantRepaint instead
            var subscription = GONetMain.EventBus.Subscribe<GONetParticipantStartedEvent>(_ => Repaint());
            subscription.SetSubscriptionPriority(short.MaxValue); // setting priority to run last so the GONetGlobal instance has a chance to add the new GNP to its list before we repaint here

            var subscription2 = GONetMain.EventBus.Subscribe<SyncEvent_GONetParticipant_OwnerAuthorityId>(_ => Repaint());
            subscription2.SetSubscriptionPriority(short.MaxValue); // setting priority to run last so the GONetGlobal instance has a chance to (possibly) add/remove the GNP to/from its list before we repaint here

            var subscription3 = GONetMain.EventBus.Subscribe<GONetParticipantDisabledEvent>(_ => Repaint());
            subscription3.SetSubscriptionPriority(short.MaxValue); // setting priority to run last so the GONetGlobal instance has a chance to remove the GNP from its list before we repaint here
            */
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // CLIENT LIMBO MODE: Show limbo statistics (CLIENT ONLY)
            if (UnityEngine.Application.isPlaying && GONetMain.IsClient)
            {
                int limboCount = GONetMain.Client_GetLimboCount();

                EditorGUILayout.Separator();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Show limbo statistics with color coding
                EditorGUILayout.LabelField("CLIENT LIMBO MODE STATUS", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Participants In Limbo:", GUILayout.Width(150));

                // Color code based on limbo count
                Color previousColor = GUI.contentColor;
                if (limboCount > 0)
                {
                    GUI.contentColor = new Color(1f, 0.7f, 0f); // Orange for warning
                }
                EditorGUILayout.LabelField(limboCount.ToString(), EditorStyles.boldLabel);
                GUI.contentColor = previousColor;
                EditorGUILayout.EndHorizontal();

                // Show batch diagnostics
                string batchDiagnostics = GONetIdBatchManager.Client_GetDiagnostics();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Batch Status:", GUILayout.Width(150));
                EditorGUILayout.LabelField(batchDiagnostics);
                EditorGUILayout.EndHorizontal();

                // Show tooltip/help text
                if (limboCount > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"⚠️ {limboCount} participant(s) waiting for GONetId batch from server.\n" +
                        "These objects are NOT networked yet and cannot sync/receive RPCs.\n" +
                        "They will automatically graduate when a new batch arrives.",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "✓ All spawned participants have GONetIds assigned.\n" +
                        "Limbo mode is an edge case for rapid spawning (100+ spawns/sec).",
                        MessageType.Info);
                }

                EditorGUILayout.EndVertical();

                // LIMBO PARTICIPANTS SECTION (separate from regular participants)
                if (limboCount > 0)
                {
                    EditorGUILayout.Separator();
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // Separator line
                    DrawLimboParticipantsList();
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // Separator line
                }
            }

            const string MY = "*My Enabled GONetParticipants:";
            DrawGNPList(targetGNL.MyEnabledGONetParticipants, MY, true);
        }

        /// <summary>
        /// Draws the special LIMBO PARTICIPANTS section showing objects waiting for GONetId batch
        /// </summary>
        private void DrawLimboParticipantsList()
        {
            var limboParticipants = GONetMain.Client_GetLimboParticipants();

            if (limboParticipants != null && limboParticipants.Any())
            {
                // Header with warning background
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                Color previousBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.7f, 0f, 0.3f); // Light orange background

                EditorGUILayout.LabelField("⚠️ LIMBO PARTICIPANTS (Waiting for GONetId Batch)", EditorStyles.boldLabel);
                const string CLICK = " (click to select - these are NOT in MyEnabledGONetParticipants yet)";
                EditorGUILayout.LabelField(CLICK, EditorStyles.miniLabel);

                GUI.backgroundColor = previousBgColor;

                foreach (var gnp in limboParticipants.OrderBy(x => x.gameObject.name))
                {
                    if (gnp == null || gnp.gameObject == null)
                        continue;

                    EditorGUILayout.BeginHorizontal();

                    // Orange button for limbo participant
                    previousBgColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.7f, 0f, 0.8f); // Orange background

                    // Show name and "NO GONetId YET" instead of actual ID
                    string buttonLabel = $"{gnp.gameObject.name} - NO GONetId YET (awaiting batch)";

                    if (GUILayout.Button(buttonLabel))
                    {
                        Selection.activeGameObject = gnp.gameObject;
                    }

                    GUI.backgroundColor = previousBgColor;

                    // Show limbo mode indicator
                    Color previousColor = GUI.contentColor;
                    GUI.contentColor = new Color(1f, 0.5f, 0f); // Orange text

                    // Determine which limbo mode is active based on which tracking fields are set
                    string modeText = "LIMBO";
                    if (gnp.client_limboDisabledComponents != null && gnp.client_limboDisabledComponents.Count > 0)
                    {
                        modeText = "FROZEN"; // DisableAll mode
                    }
                    else if (gnp.client_limboDisabledRenderers != null)
                    {
                        modeText = "HIDDEN"; // DisableRenderingAndPhysics mode
                    }

                    GUILayout.Label($"⚠ {modeText}", EditorStyles.boldLabel, GUILayout.Width(90));
                    GUI.contentColor = previousColor;

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }
    }
}
