/* Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GONet.Editor
{
    [CustomEditor(typeof(GONetParticipant))]
    public class GONetAutoMagicalSyncMemberCustomInspector : UnityEditor.Editor
    {
        GONetParticipant targetGONetParticipant;
        bool isMyScriptSectionUnfolded = true;

        const string SCR = " (Script)";

        private void OnEnable()
        {
            targetGONetParticipant = (GONetParticipant)target;
        }

        public override void OnInspectorGUI()
        {
            DrawGONetParticipantSpecifics(targetGONetParticipant);
        }

        bool isSyncAttrSectionFolded = true;
        Dictionary<string, bool> isFoldedByTypeMemberNameMap = new Dictionary<string, bool>(); // TODO this needs to be persistable data the unity way

        private void DrawGONetParticipantSpecifics(GONetParticipant targetGONetParticipant)
        {
            bool guiEnabledPrevious = GUI.enabled;
            GUI.enabled = false;

            const string NOT_SET = "<not set>";

            {
                EditorGUILayout.BeginHorizontal();
                const string DESIGN = "Design Time Location";
                EditorGUILayout.LabelField(DESIGN);
                EditorGUILayout.TextField(targetGONetParticipant.DesignTimeLocation);
                EditorGUILayout.EndHorizontal();
            }

            { // codeGenerationId
                EditorGUILayout.BeginHorizontal();
                const string CODE_GEN_ID = "Code Generation Id";
                EditorGUILayout.LabelField(CODE_GEN_ID);
                string value = targetGONetParticipant.codeGenerationId == GONetParticipant.CodeGenerationId_Unset ? NOT_SET : targetGONetParticipant.codeGenerationId.ToString();
                EditorGUILayout.TextField(value);
                EditorGUILayout.EndHorizontal();
            }

            if (Application.isPlaying) // this value is only really relevant during play (not to mention, the way we determine this is faulty otherwise...false positives everywhere)
            { // design time?
                {
                    EditorGUILayout.BeginHorizontal();
                    const string INSTANTI = "Was Instantiated?";
                    EditorGUILayout.LabelField(INSTANTI);
                    EditorGUILayout.Toggle(targetGONetParticipant.WasInstantiated);
                    EditorGUILayout.EndHorizontal();
                }

                { // GoNetId
                    EditorGUILayout.BeginHorizontal();
                    const string GONET_ID = "GONetId";
                    EditorGUILayout.LabelField(GONET_ID);
                    string value = targetGONetParticipant.GONetId == GONetParticipant.GONetId_Unset ? NOT_SET : targetGONetParticipant.GONetId.ToString();
                    EditorGUILayout.TextField(value);
                    EditorGUILayout.EndHorizontal();
                }

                { // GoNetId RAW
                    EditorGUILayout.BeginHorizontal();
                    const string GONET_ID_RAW = "GONetId (RAW)";
                    EditorGUILayout.LabelField(GONET_ID_RAW);
                    string value = targetGONetParticipant.gonetId_raw == GONetParticipant.GONetId_Unset ? NOT_SET : targetGONetParticipant.gonetId_raw.ToString();
                    EditorGUILayout.TextField(value);
                    EditorGUILayout.EndHorizontal();
                }

                { // OwnerAuthorityId
                    EditorGUILayout.BeginHorizontal();
                    const string OWNER_AUTHORITY_ID = "OwnerAuthorityId";
                    EditorGUILayout.LabelField(OWNER_AUTHORITY_ID);
                    const string GONET_SERVER = "<GONet server>";
                    string value = targetGONetParticipant.OwnerAuthorityId == GONetMain.OwnerAuthorityId_Server
                        ? GONET_SERVER
                        : (targetGONetParticipant.OwnerAuthorityId == GONetMain.OwnerAuthorityId_Unset ? NOT_SET : targetGONetParticipant.OwnerAuthorityId.ToString());
                    EditorGUILayout.TextField(value);
                    EditorGUILayout.EndHorizontal();
                }
            }

            GUI.enabled = guiEnabledPrevious;

            const string ATTR = "[GONetAutoMagicalSync] Items to Sync";
            isSyncAttrSectionFolded = EditorGUILayout.Foldout(isSyncAttrSectionFolded, ATTR);
            if (isSyncAttrSectionFolded)
            { // Handle/draw all [GONetAutoMagicalSync] members
                EditorGUI.indentLevel++;

                foreach (var siblingMonoBehaviour in targetGONetParticipant.GetComponents<MonoBehaviour>())
                {
                    if (!(siblingMonoBehaviour is GONetParticipant))
                    {
                        var autoSyncMembersInSibling =
                            siblingMonoBehaviour
                                .GetType()
                                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                                .Where(member => (member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Field)
                                                && member.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true) != null);

                        if (autoSyncMembersInSibling.Count() > 0)
                        {
                            bool guiEnabledPrevious_inner = GUI.enabled;
                            GUI.enabled = false;

                            bool isfoldie;
                            isFoldedByTypeMemberNameMap.TryGetValue(siblingMonoBehaviour.GetType().Name, out isfoldie);
                            string ScriptName = siblingMonoBehaviour.GetType().Name;
                            isFoldedByTypeMemberNameMap[siblingMonoBehaviour.GetType().Name] = EditorGUILayout.Foldout(isfoldie, string.Concat(ScriptName, SCR));
                            if (isFoldedByTypeMemberNameMap[siblingMonoBehaviour.GetType().Name])
                            {
                                EditorGUI.indentLevel++;

                                EditorGUILayout.BeginHorizontal();
                                const string ScriptLabel = "Script";
                                EditorGUILayout.LabelField(ScriptLabel);

                                GUI.enabled = true;
                                if (GUILayout.Button(ScriptName, GetClickableDisabledLabelStyle()))
                                {
                                    var script = MonoScript.FromMonoBehaviour(siblingMonoBehaviour);
                                    if ((EditorApplication.timeSinceStartup - lastClickableLabelClickedTime) < CONSIDER_DOUBLE_CLICK_IF_WITHIN_TIME)
                                    {
                                        AssetDatabase.OpenAsset(script);
                                    }
                                    else
                                    {
                                        //Selection.SetActiveObjectWithContext(script, null); // this would be cool, but prevents the ability to double click since focus goes to this script and the thing to double click is no longer visible in inspector!!!
                                        EditorGUIUtility.PingObject(script);
                                    }

                                    lastClickableLabelClickedTime = EditorApplication.timeSinceStartup;
                                }
                                GUI.enabled = false;

                                EditorGUILayout.EndHorizontal();

                                foreach (var autoSyncMember in autoSyncMembersInSibling)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField(autoSyncMember.Name, GUILayout.MaxWidth(150));

                                    EditorGUILayout.TextField(autoSyncMember.MemberType == MemberTypes.Field ? ((FieldInfo)autoSyncMember).GetValue(siblingMonoBehaviour).ToString() : ((PropertyInfo)autoSyncMember).GetValue(siblingMonoBehaviour).ToString(),
                                        GUILayout.MinWidth(70), GUILayout.ExpandWidth(true));

                                    GONetAutoMagicalSyncAttribute autoSyncMember_SyncAttribute = (GONetAutoMagicalSyncAttribute)autoSyncMember.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true);
                                    DrawGONetSyncProfileTemplateButton_IfAppropriate(autoSyncMember_SyncAttribute.SettingsProfileTemplateName);

                                    EditorGUILayout.EndHorizontal();
                                }
                                EditorGUI.indentLevel--;
                            }

                            GUI.enabled = guiEnabledPrevious_inner;
                        }
                    }
                }


                serializedObject.Update();
                EditorGUI.BeginChangeCheck();
                serializedObject.UpdateIfRequiredOrScript();

                Animator animator = targetGONetParticipant.GetComponent<Animator>();
                if (animator != null && animator.parameterCount > 0)
                {
                    if (targetGONetParticipant.animatorSyncSupport == null)
                    {
                        targetGONetParticipant.animatorSyncSupport = new GONetParticipant.AnimatorControllerParameterMap();
                    }

                    bool isfoldie;
                    string animatorControllerName = animator.runtimeAnimatorController.name;
                    isFoldedByTypeMemberNameMap.TryGetValue(animatorControllerName, out isfoldie);

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.BeginHorizontal(GUILayout.MinWidth(140));
                    const string ANIMATOR_INTRINSICS = "Animator (Intrinsics)";
                    isFoldedByTypeMemberNameMap[animatorControllerName] = EditorGUILayout.Foldout(isfoldie, ANIMATOR_INTRINSICS);
                    EditorGUILayout.EndHorizontal();

                    DrawGONetSyncProfileTemplateButton_IfAppropriate(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___ANIMATOR_CONTROLLER_PARAMETERS);

                    EditorGUILayout.EndHorizontal();

                    if (isFoldedByTypeMemberNameMap[animatorControllerName])
                    {
                        EditorGUI.indentLevel++;

                        bool guiEnabledPrevious_inner = GUI.enabled;
                        GUI.enabled = false;

                        EditorGUILayout.BeginHorizontal();
                        const string ControllerLabel = "Controller";
                        EditorGUILayout.LabelField(ControllerLabel);
                        EditorGUILayout.TextField(animatorControllerName);
                        EditorGUILayout.EndHorizontal();

                        GUI.enabled = guiEnabledPrevious_inner;

                        if (Application.isPlaying)
                        {
                            guiEnabledPrevious_inner = GUI.enabled;
                            GUI.enabled = false;
                        }

                        for (int i = 0; i < animator.parameterCount; ++i)
                        {
                            AnimatorControllerParameter animatorControllerParameter = animator.parameters[i];
                            string parameterSyncMap_key = animatorControllerParameter.name;
                            if (!targetGONetParticipant.animatorSyncSupport.ContainsKey(parameterSyncMap_key))
                            {
                                targetGONetParticipant.animatorSyncSupport[parameterSyncMap_key] = new GONetParticipant.AnimatorControllerParameter()
                                {
                                    valueType = animatorControllerParameter.type,
                                    isSyncd = false
                                };
                            }
                            int parameterSyncMap_keyIndex = targetGONetParticipant.animatorSyncSupport.GetCustomKeyIndex(parameterSyncMap_key);

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(string.Concat("Is Syncd: ", parameterSyncMap_key));
                            SerializedProperty specificInnerMapValue_serializedProperty = serializedObject.FindProperty($"{nameof(GONetParticipant.animatorSyncSupport)}.values.Array.data[{parameterSyncMap_keyIndex}].{nameof(GONetParticipant.AnimatorControllerParameter.isSyncd)}");
                            EditorGUILayout.PropertyField(specificInnerMapValue_serializedProperty, GUIContent.none, false); // IMPORTANT: without this, editing prefabs would never save/persist changes!
                            EditorGUILayout.EndHorizontal();

                        }

                        GUI.enabled = guiEnabledPrevious_inner;

                        EditorGUI.indentLevel--;
                    }
                }

                { // what used to be DrawDefaultInspector():
                    const string GOIntrinsics = "Transform (Intrinsics)";
                    isMyScriptSectionUnfolded = EditorGUILayout.Foldout(isMyScriptSectionUnfolded, GOIntrinsics);// string.Concat(typeof(GONetParticipant).Name, SCR));
                    if (isMyScriptSectionUnfolded)
                    {
                        EditorGUI.indentLevel++;

                        bool guiEnabledPrevious_inner = GUI.enabled;
                        GUI.enabled = false;

                        EditorGUILayout.BeginHorizontal();
                        const string ScriptLabel = "Script";
                        EditorGUILayout.LabelField(ScriptLabel);
                        EditorGUILayout.TextField(nameof(Transform));
                        EditorGUILayout.EndHorizontal();

                        GUI.enabled = guiEnabledPrevious_inner;

                        { // IsPositionSyncd:
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(string.Concat("Is Position Syncd"), GUILayout.MaxWidth(150));
                            SerializedProperty serializedProperty = serializedObject.FindProperty($"{nameof(GONetParticipant.IsPositionSyncd)}");
                            EditorGUILayout.PropertyField(serializedProperty, GUIContent.none, false, GUILayout.MaxWidth(50)); // IMPORTANT: without this, editing would never save/persist changes!
                            DrawGONetSyncProfileTemplateButton_IfAppropriate(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___TRANSFORM_POSITION);
                            EditorGUILayout.EndHorizontal();
                        }
                        { // IsRotationSyncd:
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(string.Concat("Is Rotation Syncd"), GUILayout.MaxWidth(150));
                            SerializedProperty serializedProperty = serializedObject.FindProperty($"{nameof(GONetParticipant.IsRotationSyncd)}");
                            EditorGUILayout.PropertyField(serializedProperty, GUIContent.none, false, GUILayout.MaxWidth(50)); // IMPORTANT: without this, editing would never save/persist changes!
                            DrawGONetSyncProfileTemplateButton_IfAppropriate(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___TRANSFORM_ROTATION);
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUI.indentLevel--;
                    }
                }

                serializedObject.ApplyModifiedProperties();

                if (EditorGUI.EndChangeCheck())
                {
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(targetGONetParticipant);
                        EditorUtility.SetDirty(targetGONetParticipant.gameObject);

                        bool isPrefab = targetGONetParticipant.designTimeLocation.EndsWith(".prefab"); // TODO ensure we can count on this....or just use a sure fire way for unity to tell us the answer
                        if (!isPrefab)
                        {
                            EditorUtility.SetDirty(targetGONetParticipant);
                            EditorUtility.SetDirty(targetGONetParticipant.gameObject);
                            EditorSceneManager.MarkAllScenesDirty();
                        }
                    }
                }


                EditorGUI.indentLevel--;
            }
        }

        static double lastClickableLabelClickedTime;
        const double CONSIDER_DOUBLE_CLICK_IF_WITHIN_TIME = 0.3;

        static GUIStyle clickableDisabledLabelStyle;
        static GUIStyle GetClickableDisabledLabelStyle()
        {
            if (clickableDisabledLabelStyle == null)
            {
                clickableDisabledLabelStyle  = new GUIStyle(GUI.skin.textField);
                clickableDisabledLabelStyle.normal.textColor = Color.grey; // make it look disabled
            }
            return clickableDisabledLabelStyle;
        }

        private static void DrawGONetSyncProfileTemplateButton_IfAppropriate(string settingsProfileTemplateName)
        {
            if (!string.IsNullOrWhiteSpace(settingsProfileTemplateName))
            {
                const string PROFILE = "profile: ";
                const string TOOLTIP = "Click to select the corresponding GONet SyncSettingsProfile asset in Project view.\nOnce selected, you can view/edit the sync settings for this value.";

                bool superInnerPrev = GUI.enabled;
                GUI.enabled = true;
                GUIContent buttonTextWithTooltip = new GUIContent(string.Concat(PROFILE, settingsProfileTemplateName), TOOLTIP);
                if (GUILayout.Button(buttonTextWithTooltip))
                {
                    Object mainAsset = AssetDatabase.LoadMainAssetAtPath(string.Concat(
                        GONetEditorWindow.ASSETS_SYNC_SETTINGS_PROFILES_FOLDER_PATH,
                        settingsProfileTemplateName,
                        GONetEditorWindow.ASSET_FILE_EXTENSION));
                    if (mainAsset != null)
                    {
                        Selection.activeObject = mainAsset;
                    }
                    else
                    {
                        const string OOPS = "Oops.  The profile/template name used here (i.e., \"";
                        const string NAME = "\") does NOT match with any of the available entries in the folder: ";
                        const string INSTEAD = ".\nAt runtime, the following profile/template will be used instead: ";
                        Debug.LogWarning(string.Concat(OOPS, settingsProfileTemplateName ?? string.Empty, NAME, GONetEditorWindow.ASSETS_SYNC_SETTINGS_PROFILES_FOLDER_PATH, INSTEAD, GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___DEFAULT));
                    }
                }
                GUI.enabled = superInnerPrev;
            }
        }
    }
}
