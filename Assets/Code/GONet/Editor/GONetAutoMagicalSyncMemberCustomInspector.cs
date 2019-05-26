using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GONet.Editor
{
    /// <summary>
    /// TODO draw properties if decorated with <see cref="GONetAutoMagicalSyncAttribute"/>.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class GONetAutoMagicalSyncMemberCustomInspector : UnityEditor.Editor
    {
        MonoBehaviour targetMonoBehaviour;

        private void OnEnable()
        {
            targetMonoBehaviour = (MonoBehaviour)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // TODO draw properties if decorated with <see cref="GONetAutoMagicalSyncAttribute"/>.

            if (targetMonoBehaviour is GONetParticipant)
            {
                DrawGONetParticipantSpecifics((GONetParticipant)targetMonoBehaviour);
            }
        }

        private void DrawGONetParticipantSpecifics(GONetParticipant targetGONetParticipant)
        {
            const string NOT_SET = "<not set>";

            if (Application.isPlaying) // this value is only really relevant during play (not to mention, the way we determine this is faulty otherwise...false positives everywhere)
            { // design time?
                EditorGUILayout.BeginHorizontal();
                const string INSTANTI = "Was Instantiated?";
                EditorGUILayout.LabelField(INSTANTI);
                bool guiEnabledPrevious = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.Toggle(targetGONetParticipant.WasInstantiated);
                GUI.enabled = guiEnabledPrevious;
                EditorGUILayout.EndHorizontal();
            }

            {
                EditorGUILayout.BeginHorizontal();
                const string DESIGN = "Design Time Location";
                EditorGUILayout.LabelField(DESIGN);
                bool guiEnabledPrevious = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.TextField(targetGONetParticipant.DesignTimeLocation);

                GUI.enabled = guiEnabledPrevious;
                EditorGUILayout.EndHorizontal();
            }

            { // codeGenerationId
                EditorGUILayout.BeginHorizontal();
                const string CODE_GEN_ID = "Code Generation Id";
                EditorGUILayout.LabelField(CODE_GEN_ID);
                bool guiEnabledPrevious = GUI.enabled;
                GUI.enabled = false;

                string value = targetGONetParticipant.codeGenerationId == GONetParticipant.CodeGenerationId_Unset ? NOT_SET : targetGONetParticipant.codeGenerationId.ToString();
                EditorGUILayout.TextField(value);

                GUI.enabled = guiEnabledPrevious;
                EditorGUILayout.EndHorizontal();
            }

            { // GoNetId
                EditorGUILayout.BeginHorizontal();
                const string GONET_ID = "GONetId";
                EditorGUILayout.LabelField(GONET_ID);
                bool guiEnabledPrevious = GUI.enabled;
                GUI.enabled = false;

                string value = targetGONetParticipant.GONetId == GONetParticipant.GONetId_Unset ? NOT_SET : targetGONetParticipant.GONetId.ToString();
                EditorGUILayout.TextField(value);

                GUI.enabled = guiEnabledPrevious;
                EditorGUILayout.EndHorizontal();
            }

            { // OwnerAuthorityId
                EditorGUILayout.BeginHorizontal();
                const string OWNER_AUTHORITY_ID = "OwnerAuthorityId";
                EditorGUILayout.LabelField(OWNER_AUTHORITY_ID);
                bool guiEnabledPrevious = GUI.enabled;
                GUI.enabled = false;

                const string GONET_SERVER = "<GONet server>";
                string value = targetGONetParticipant.OwnerAuthorityId == GONetMain.OwnerAuthorityId_Server
                    ? GONET_SERVER
                    : (targetGONetParticipant.OwnerAuthorityId == GONetMain.OwnerAuthorityId_Unset ? NOT_SET : targetGONetParticipant.OwnerAuthorityId.ToString());
                EditorGUILayout.TextField(value);

                GUI.enabled = guiEnabledPrevious;
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
