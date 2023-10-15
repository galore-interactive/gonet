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
using GONet.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
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

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();

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

            { // IsRigidBodyOwnerOnlyControlled
                var pre = GUI.enabled;
                GUI.enabled = !Application.isPlaying;
                EditorGUILayout.BeginHorizontal();
                SerializedProperty serializedProperty = serializedObject.FindProperty(nameof(GONetParticipant.IsRigidBodyOwnerOnlyControlled));
                const string TT = @"The expectation on setting this to true is the values for <see cref=""IsPositionSyncd""/> and <see cref=""IsRotationSyncd""/> are true
and the associated <see cref=""GameObject""/> has a <see cref=""Rigidbody""/> installed on it as well 
and <see cref=""Rigidbody.isKinematic""/> is false and if using gravity, <see cref=""Rigidbody.useGravity""/> is true.

If all that applies, then non-owners (i.e., <see cref=""IsMine""/> is false) will have <see cref=""Rigidbody.isKinematic""/> set to true and <see cref=""Rigidbody.useGravity""/> set to false
so the auto magically sync'd values for position and rotation come from owner controlled actions only.

IMPORTANT: This is not going have an effect if/when changed during a running game.  This needs to be set during design time.  Maybe a future release will decorate it with <see cref=""GONetAutoMagicalSyncAttribute""/>, if people need it.";
                GUIContent tooltip = new GUIContent(StringUtils.AddSpacesBeforeUppercase(nameof(GONetParticipant.IsRigidBodyOwnerOnlyControlled), 1), TT);
                if (EditorGUILayout.PropertyField(serializedProperty, tooltip))
                {
                    // TODO why does this method return bool?  do we need to do something!
                }
                EditorGUILayout.EndHorizontal();
                GUI.enabled = pre;
            }

            { // ShouldHideDuringRemoteInstantiate
                var pre = GUI.enabled;
                GUI.enabled = !Application.isPlaying;
                EditorGUILayout.BeginHorizontal();
                SerializedProperty serializedProperty = serializedObject.FindProperty(nameof(GONetParticipant.ShouldHideDuringRemoteInstantiate));
                const string TT = @"This is an option (good for projectiles) to deal with there being an inherent delay of <see cref=""GONetMain.valueBlendingBufferLeadSeconds""/> from the time a
remote instantiation of this <see cref=""GONetParticipant""/> (and <see cref=""IsMine""/> is false) occurs and the time auto-magical sync data starts processing for value blending 
(i.e., <see cref=""GONetAutoMagicalSyncSettings_ProfileTemplate.ShouldBlendBetweenValuesReceived""/> and <see cref=""GONetAutoMagicalSyncAttribute.ShouldBlendBetweenValuesReceived""/>).

When this option is set to true, all <see cref=""Renderer""/> components on this (including children) are turned off during the buffer lead time delay and then turned back on.

If this option does not exactly suit your needs and you want something similar, then just subscribe using <see cref=""GONetMain.EventBus""/> to the <see cref=""GONetParticipantStartedEvent""/>
and check if that event's envelope has <see cref=""GONetEventEnvelope.IsSourceRemote""/> set to true and you can implement your own option to deal with this situation.";
                GUIContent tooltip = new GUIContent(StringUtils.AddSpacesBeforeUppercase(nameof(GONetParticipant.ShouldHideDuringRemoteInstantiate), 1), TT);
                if (EditorGUILayout.PropertyField(serializedProperty, tooltip))
                {
                    // TODO why does this method return bool?  do we need to do something!
                }
                EditorGUILayout.EndHorizontal();
                GUI.enabled = pre;
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
                    const string GONET_ID = "GO Net Id";
                    const string TT = "This is a combination of GO Net Id (RAW) and Owner Authority Id";
                    GUIContent tooltip = new GUIContent(GONET_ID, TT);
                    EditorGUILayout.LabelField(tooltip);
                    string value = targetGONetParticipant.GONetId == GONetParticipant.GONetId_Unset ? NOT_SET : targetGONetParticipant.GONetId.ToString();
                    EditorGUILayout.TextField(value);
                    EditorGUILayout.EndHorizontal();
                }

                if (targetGONetParticipant.GONetIdAtInstantiation != targetGONetParticipant.GONetId)
                { // GoNetIdAtInstantiation
                    EditorGUILayout.BeginHorizontal();
                    const string GONET_ID = "GO Net Id (At Instantiation)";
                    const string TT = "This is the original/first GONetId assigned, but it has changed due to someone else assuming authority over it (e.g., the server via GONetMain.Server_AssumeAuthorityOver()).";
                    GUIContent tooltip = new GUIContent(GONET_ID, TT);
                    EditorGUILayout.LabelField(tooltip);
                    EditorGUILayout.TextField(targetGONetParticipant.GONetIdAtInstantiation.ToString());
                    EditorGUILayout.EndHorizontal();
                }

                { // GoNetId RAW
                    EditorGUILayout.BeginHorizontal();
                    const string GONET_ID_RAW = "GO Net Id (RAW)";
                    EditorGUILayout.LabelField(GONET_ID_RAW);
                    string value = targetGONetParticipant.gonetId_raw == GONetParticipant.GONetId_Unset ? NOT_SET : targetGONetParticipant.gonetId_raw.ToString();
                    EditorGUILayout.TextField(value);
                    EditorGUILayout.EndHorizontal();
                }

                { // OwnerAuthorityId
                    EditorGUILayout.BeginHorizontal();
                    const string OWNER_AUTHORITY_ID = "Owner Authority Id";
                    EditorGUILayout.LabelField(OWNER_AUTHORITY_ID);
                    const string GONET_SERVER = "<GONet server>";
                    string value = targetGONetParticipant.OwnerAuthorityId == GONetMain.OwnerAuthorityId_Server
                        ? GONET_SERVER
                        : (targetGONetParticipant.OwnerAuthorityId == GONetMain.OwnerAuthorityId_Unset ? NOT_SET : targetGONetParticipant.OwnerAuthorityId.ToString());
                    EditorGUILayout.TextField(value);
                    EditorGUILayout.EndHorizontal();
                }

                { // IsMine
                    EditorGUILayout.BeginHorizontal();
                    const string IS_MINE = "Is Mine?";
                    EditorGUILayout.LabelField(IS_MINE);
                    EditorGUILayout.Toggle(GONetMain.IsMine(targetGONetParticipant));
                    EditorGUILayout.EndHorizontal();
                }

                if (targetGONetParticipant.RemotelyControlledByAuthorityId != GONetMain.OwnerAuthorityId_Unset)
                { // RemotelyControlledByAuthorityId && IsMine_ToRemotelyControl
                    EditorGUILayout.BeginHorizontal();
                    const string REMOTELY_CONTROLLED_BY_AUTHORITY_ID = "Remotely Controlled by Authority Id";
                    EditorGUILayout.LabelField(REMOTELY_CONTROLLED_BY_AUTHORITY_ID);
                    string value = targetGONetParticipant.RemotelyControlledByAuthorityId.ToString();
                    EditorGUILayout.TextField(value);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    const string IS_REMOTELY_CONTROLLED_BY_ME = "Is Mine (for Remote Control)?";
                    EditorGUILayout.LabelField(IS_REMOTELY_CONTROLLED_BY_ME);
                    EditorGUILayout.Toggle(targetGONetParticipant.IsMine_ToRemotelyControl);
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

                                    { // is at rest?
                                        GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = GONetMain.GetSyncCompanionByGNP(targetGONetParticipant);

                                        byte index = 0;
                                        if (syncCompanion != null && syncCompanion.TryGetIndexByMemberName(autoSyncMember.Name, out index))
                                        {
                                            bool isAtRest = syncCompanion != null ? syncCompanion.IsValueAtRest(index) : false;
                                            EditorGUILayout.LabelField("At_Rest?");
                                            EditorGUILayout.Toggle(isAtRest);
                                        }
                                    }

                                    DrawGONetSyncProfileTemplateButton(autoSyncMember_SyncAttribute.SettingsProfileTemplateName, siblingMonoBehaviour);

                                    EditorGUILayout.EndHorizontal();
                                }
                                EditorGUI.indentLevel--;
                            }

                            GUI.enabled = guiEnabledPrevious_inner;
                        }
                    }
                }


                Animator animator = targetGONetParticipant.GetComponent<Animator>();
                if (AnimationEditorUtils.TryGetAnimatorControllerParameters(animator, out var parameters))
                {
                    if (parameters != null && parameters.Length > 0)
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

                        DrawGONetSyncProfileTemplateButton(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___ANIMATOR_CONTROLLER_PARAMETERS);

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

                            for (int i = 0; i < parameters.Length; ++i)
                            {
                                if (!StringUtils.IsStringValidForCSharpNamingConventions(parameters[i].name))
                                {
                                    GONetLog.Error($"The animation parameter name '{parameters[i].name}' is not valid. Skipping this parameter. Please, check the rules that a string must follow in order to be valid. You can find them within the class StringUtils.IsStringValidForCSharpNamingConventions");
                                    Debug.LogError($"The animation parameter name '{parameters[i].name}' is not valid. Skipping this parameter. Please, check the rules that a string must follow in order to be valid. You can find them within the class StringUtils.IsStringValidForCSharpNamingConventions");
                                    continue;
                                }

                                AnimatorControllerParameter animatorControllerParameter = parameters[i];
                                bool isAnimParamTypeSupportedInGONet = animatorControllerParameter.type != AnimatorControllerParameterType.Trigger;
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

                                bool guiItemPrior = GUI.enabled;
                                if (!isAnimParamTypeSupportedInGONet)
                                {
                                    // Currently trigger type is not supported as we do not know how to monitor and network when trigger occurs...still thinking, but until then we do NOT want to give the appearance that we can allow it, so do not show it in UI as editable, but at least show it with a note so users know what is going on
                                    GUI.enabled = false;
                                }
                                EditorGUILayout.BeginHorizontal();
                                string labelString = string.Concat("Is Syncd: ", parameterSyncMap_key);
                                GUIContent labelContent = new GUIContent(labelString, string.Empty);
                                if (!isAnimParamTypeSupportedInGONet)
                                {
                                    labelContent.tooltip = "Currently, the trigger type is not supported as we do not know how to monitor and network when trigger occurs...still thinking, but until then, we do NOT want to give the appearance that we can allow it, so only show it in UI as readonly.  At least users can see it greyed out, see this tooltip and know what is going on.";
                                }
                                EditorGUILayout.LabelField(labelContent);
                                SerializedProperty specificInnerMapValue_serializedProperty = serializedObject.FindProperty($"{nameof(GONetParticipant.animatorSyncSupport)}.values.Array.data[{parameterSyncMap_keyIndex}].{nameof(GONetParticipant.AnimatorControllerParameter.isSyncd)}");
                                EditorGUILayout.PropertyField(specificInnerMapValue_serializedProperty, GUIContent.none, false); // IMPORTANT: without this, editing prefabs would never save/persist changes!
                                EditorGUILayout.EndHorizontal();
                                GUI.enabled = guiItemPrior;
                            }

                            GUI.enabled = guiEnabledPrevious_inner;

                            EditorGUI.indentLevel--;
                        }
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
                            DrawGONetSyncProfileTemplateButton(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___TRANSFORM_POSITION);
                            EditorGUILayout.EndHorizontal();
                        }
                        { // IsRotationSyncd:
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(string.Concat("Is Rotation Syncd"), GUILayout.MaxWidth(150));
                            SerializedProperty serializedProperty = serializedObject.FindProperty($"{nameof(GONetParticipant.IsRotationSyncd)}");
                            EditorGUILayout.PropertyField(serializedProperty, GUIContent.none, false, GUILayout.MaxWidth(50)); // IMPORTANT: without this, editing would never save/persist changes!
                            DrawGONetSyncProfileTemplateButton(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___TRANSFORM_ROTATION);
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
                clickableDisabledLabelStyle = new GUIStyle(GUI.skin.textField);
                clickableDisabledLabelStyle.normal.textColor = Color.grey; // make it look disabled
            }
            return clickableDisabledLabelStyle;
        }

        private static void DrawGONetSyncProfileTemplateButton(string settingsProfileTemplateName, MonoBehaviour siblingMonoBehaviour = null)
        {
            const string PROFILE = "profile: ";
            const string TOOLTIP_PROFILE = "Click to select the corresponding GONet SyncSettingsProfile asset in Project view.\nOnce selected, you can view/edit the sync settings for all values using this profile.";
            const string TOOLTIP_ATTR = "No profile identified in [GONetAutoMagicalSync(SettingsProfileTemplateName=\"<profile name here>\")].\nClick to open the C# class with the [GONetAutoMagicalSync] attribute for this field/property.\nOnce open, you can view/edit the sync settings for this value directly in the C# Attribute -OR- set the name of the profile you want to use.";

            string tooltip = TOOLTIP_PROFILE;
            string profileName = settingsProfileTemplateName;

            if (string.IsNullOrWhiteSpace(settingsProfileTemplateName))
            {
                if (siblingMonoBehaviour == null)
                {
                    throw new System.Exception("not supported");
                }

                tooltip = TOOLTIP_ATTR;
                profileName = "N/A (uses C# Attribute settings)";
            }

            bool superInnerPrev = GUI.enabled;
            GUI.enabled = true;
            GUIContent buttonTextWithTooltip = new GUIContent(string.Concat(PROFILE, profileName), tooltip);
            if (GUILayout.Button(buttonTextWithTooltip))
            {
                if (tooltip == TOOLTIP_PROFILE)
                {
                    UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(string.Concat(
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
                        const string NEW = "\nTo create a new sync settings profile/template, open the GONet Editor Support window (see File menu named GONet), enter the name of the new profile/temple, click Create and edit the settings to your liking.";
                        Debug.LogWarning(string.Concat(OOPS, settingsProfileTemplateName ?? string.Empty, NAME, GONetEditorWindow.ASSETS_SYNC_SETTINGS_PROFILES_FOLDER_PATH, INSTEAD, GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___DEFAULT, NEW));
                    }
                }
                else if (tooltip == TOOLTIP_ATTR)
                {
                    var script = MonoScript.FromMonoBehaviour(siblingMonoBehaviour);
                    AssetDatabase.OpenAsset(script);
                }
            }
            GUI.enabled = superInnerPrev;
        }
    }
}
