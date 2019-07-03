﻿/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

            EditorGUI.indentLevel++;
            const string GOIntrinsics = "Transform (Instrinsics)";
            isMyScriptSectionUnfolded = EditorGUILayout.Foldout(isMyScriptSectionUnfolded, GOIntrinsics);// string.Concat(typeof(GONetParticipant).Name, SCR));
            if (isMyScriptSectionUnfolded)
            {
                EditorGUI.indentLevel++;
                DrawDefaultInspector();
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
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
                                EditorGUILayout.TextField(ScriptName);
                                EditorGUILayout.EndHorizontal();

                                foreach (var autoSyncMember in autoSyncMembersInSibling)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField(autoSyncMember.Name);
                                    EditorGUILayout.TextField(autoSyncMember.MemberType == MemberTypes.Field ? ((FieldInfo)autoSyncMember).GetValue(siblingMonoBehaviour).ToString() : ((PropertyInfo)autoSyncMember).GetValue(siblingMonoBehaviour).ToString());
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

                foreach (var animator in targetGONetParticipant.GetComponentsInChildren<Animator>())
                {
                    if (animator.parameterCount > 0)
                    {
                        if (targetGONetParticipant.animatorSyncSupport == null)
                        {
                            targetGONetParticipant.animatorSyncSupport = new GONetParticipant.StringToStringToBoolDictionaryDictionary();
                        }

                        bool isfoldie;
                        string animatorSyncSupport_key = animator.runtimeAnimatorController.name;
                        isFoldedByTypeMemberNameMap.TryGetValue(animatorSyncSupport_key, out isfoldie);
                        const string ANIMATOR_INTRINSICS = "Animator (Intrinsics)";
                        isFoldedByTypeMemberNameMap[animatorSyncSupport_key] = EditorGUILayout.Foldout(isfoldie, ANIMATOR_INTRINSICS);
                        if (isFoldedByTypeMemberNameMap[animatorSyncSupport_key])
                        {
                            GONetParticipant.StringToBoolDictionary parameterSyncMap;
                            if (!targetGONetParticipant.animatorSyncSupport.TryGetValue(animatorSyncSupport_key, out parameterSyncMap))
                            {
                                parameterSyncMap = new GONetParticipant.StringToBoolDictionary();
                                targetGONetParticipant.animatorSyncSupport[animatorSyncSupport_key] = parameterSyncMap;
                            }
                            int animatorSyncSupport_keyIndex = targetGONetParticipant.animatorSyncSupport.GetCustomKeyIndex(animatorSyncSupport_key);

                            EditorGUI.indentLevel++;

                            bool guiEnabledPrevious_inner = GUI.enabled;
                            GUI.enabled = false;

                            EditorGUILayout.BeginHorizontal();
                            const string ControllerLabel = "Controller";
                            EditorGUILayout.LabelField(ControllerLabel);
                            EditorGUILayout.TextField(animatorSyncSupport_key);
                            EditorGUILayout.EndHorizontal();

                            GUI.enabled = guiEnabledPrevious_inner;

                            for (int i = 0; i < animator.parameterCount; ++i)
                            {
                                string parameterSyncMap_key = animator.parameters[i].name;
                                if (!parameterSyncMap.ContainsKey(parameterSyncMap_key))
                                {
                                    parameterSyncMap[parameterSyncMap_key] = false;
                                }
                                int parameterSyncMap_keyIndex = parameterSyncMap.GetCustomKeyIndex(parameterSyncMap_key);

                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(string.Concat("Is Syncd: ", parameterSyncMap_key));
                                SerializedProperty specificInnerMapValue_serializedProperty = serializedObject.FindProperty($"animatorSyncSupport.values.Array.data[{animatorSyncSupport_keyIndex}].values.Array.data[{parameterSyncMap_keyIndex}]");
                                EditorGUILayout.PropertyField(specificInnerMapValue_serializedProperty, GUIContent.none, false); // IMPORTANT: without this, editing prefabs would never save/persist changes!
                                EditorGUILayout.EndHorizontal();

                            }

                            EditorGUI.indentLevel--;
                        }
                    }
                }

                serializedObject.ApplyModifiedProperties();

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(targetGONetParticipant);
                    EditorUtility.SetDirty(targetGONetParticipant.gameObject);

                    bool isPrefab = targetGONetParticipant.designTimeLocation.EndsWith(".prefab");
                    if (!isPrefab)
                    {
                        EditorUtility.SetDirty(targetGONetParticipant);
                        EditorUtility.SetDirty(targetGONetParticipant.gameObject);
                        EditorSceneManager.MarkAllScenesDirty();
                    }
                }


                EditorGUI.indentLevel--;
            }
        }
    }
}
