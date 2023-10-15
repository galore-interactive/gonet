using GONet.Generation;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GONet.Editor
{
    public class GONetEditorWindow : EditorWindow
    {
        [MenuItem("GONet/GONet Editor Support")]
        public static void ShowWindow()
        {
            GONetEditorWindow editorWindow = EditorWindow.GetWindow<GONetEditorWindow>();
            const string GONET_EDITOR_SUPPORT = "GONet Editor Support";
            GUIContent titleContent = new GUIContent(GONET_EDITOR_SUPPORT);
            editorWindow.titleContent = titleContent;

            InitializeGUIStyles();
        }

        internal const string ASSETS_SYNC_SETTINGS_PROFILES_FOLDER_PATH = "Assets/GONet/Resources/GONet/SyncSettingsProfiles/";
        internal const string ASSET_FILE_EXTENSION = ".asset";

        string NAMEO_DEFAULT = "<Enter Name>";
        string nameo;

        string gonetIdText;
        string gonetIdText_raw;

        private static GUIStyle sectionHeaderGUIStyle = null;

        private static void InitializeGUIStyles()
        {
            sectionHeaderGUIStyle = new GUIStyle();
            sectionHeaderGUIStyle.alignment = TextAnchor.MiddleCenter;
            sectionHeaderGUIStyle.fontStyle = FontStyle.Normal;
            sectionHeaderGUIStyle.normal.textColor = Color.white;
            sectionHeaderGUIStyle.fontSize = 18;
            sectionHeaderGUIStyle.fontStyle = FontStyle.Bold;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                OnGUI_IsNotPlaying();
            }
            else // since GONetId is not assigned nor visible in inspector until playing, only show this then
            {
                OnGUI_IsPlaying();
            }
        }

        private void OnGUI_IsPlaying()
        {
            { // GONetId
                EditorGUILayout.Separator();

                EditorGUILayout.BeginHorizontal();
                const string GNId = "GONetId";
                EditorGUILayout.LabelField(GNId, GUILayout.MaxWidth(60));
                gonetIdText = EditorGUILayout.TextField(gonetIdText);
                const string NUMS = @"[^a-zA-Z0-9 ]";
                gonetIdText = gonetIdText == null ? gonetIdText : Regex.Replace(gonetIdText, NUMS, string.Empty);
                const string SEL = "Select in Hierarchy";
                const string TOOLIOUL = "Select the GameObject in the Hierarchy with GONetParticipant installed with GONetId value equal to input field value.";
                GUIContent buttonTextWithTooltip = new GUIContent(SEL, TOOLIOUL);
                if (GUILayout.Button(buttonTextWithTooltip))
                {
                    uint gonetIdSearch;
                    if (uint.TryParse(gonetIdText, out gonetIdSearch))
                    {
                        Component component = FindObjectsOfType<GONetParticipant>().FirstOrDefault(gnp => gnp.GONetId == gonetIdSearch);
                        if (component != null)
                        {
                            Selection.activeGameObject = component.gameObject;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            { // GONetId raw
                EditorGUILayout.Separator();

                EditorGUILayout.BeginHorizontal();
                const string GNId = "My GONetId (RAW)";
                EditorGUILayout.LabelField(GNId, GUILayout.MaxWidth(120));
                gonetIdText_raw = EditorGUILayout.TextField(gonetIdText_raw);
                const string NUMS = @"[^a-zA-Z0-9 ]";
                gonetIdText_raw = gonetIdText_raw == null ? gonetIdText_raw : Regex.Replace(gonetIdText_raw, NUMS, string.Empty);
                const string SEL = "Select in Hierarchy";
                const string TOOLIOUL = "Select the GameObject in the Hierarchy with GONetParticipant installed with GONetId (RAW) value equal to input field value -AND- Owner Authority Id value that matches \"mine.\"";
                GUIContent buttonTextWithTooltip = new GUIContent(SEL, TOOLIOUL);
                if (GUILayout.Button(buttonTextWithTooltip))
                {
                    uint gonetIdSearch;
                    if (uint.TryParse(gonetIdText_raw, out gonetIdSearch))
                    {
                        Component component = FindObjectsOfType<GONetParticipant>().FirstOrDefault(gnp => gnp.gonetId_raw == gonetIdSearch && gnp.OwnerAuthorityId == GONetMain.MyAuthorityId);
                        if (component != null)
                        {
                            Selection.activeGameObject = component.gameObject;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void OnGUI_IsNotPlaying()
        {
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            nameo = EditorGUILayout.TextField("New Sync Settings Profile", nameo);
            if (GUILayout.Button("Create"))
            {
                CreateSyncSettingsProfileAsset<GONetAutoMagicalSyncSettings_ProfileTemplate>(nameo);
                nameo = NAMEO_DEFAULT;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Refresh GONet's code generation", sectionHeaderGUIStyle);
            EditorGUILayout.Separator();

            const string REFRESH_TEXT = "GONet's code generation process is mostly automatic, triggered by specific Unity actions to ensure that GONet remains synchronized with the user's " +
                                        "networked code changes. However, there are situations where manual intervention is necessary to initiate this process.\nThere are two primary use " +
                                        "cases when users will need to manually refresh GONet's code generation:\n\n1. Before Entering Play Mode: It's essential to refresh GONet's code " +
                                        "generation manually if any changes (creation, modification, or deletion) related to GONet have been made since last manual refresh. This step " +
                                        "guarantees that the networked code is up-to-date and accurately reflects recent changes.\n\n2. When changing 'GONetAutoMagicalSync' fields: This " +
                                        "manual action is required when creating, modifying, or deleting a public field with the 'GONetAutoMagicalSync' attribute attached. Specially, when " +
                                        "the GameObject of that component also contains a 'GONetParticipant' component. By doing so, you ensure that synchronization is correctly established " +
                                        "between the components and their networked behavior. Also, by refreshing code generation, the user will have access to the related SyncEvent_GeneratedType " +
                                        "value in case the user wants to subscribe using GONetEventBus.Subscribe method.\n\nThese manual interventions ensure the integrity of your networked code in GONet, guaranteeing " +
                                        "that it remains synchronized with your Unity project's changes";
            EditorGUILayout.HelpBox(REFRESH_TEXT, MessageType.None);

            if (GUILayout.Button("Refresh GONet code generation"))
            {
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.UpdateAllUniqueSnaps();
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Fix GONet's code generation", sectionHeaderGUIStyle);
            EditorGUILayout.Separator();

            EditorStyles.label.wordWrap = true;
            const string FIX_TEXT = "Sometimes, code generation can get out of whack (i.e., when deleting/removing things in scene/project that are related to GONet) and the generated code will have " +
                                    "compilation errors as a result.  If you go to manually edit the generated code to fix the compilation errors, you will quickly see that the code generation " +
                                    "routine will come back and generate the code again and the compilation errors will return just as before.  The solution is to click this button to fix the issue " +
                                    "to cause GONet code generation to start all over again, forgetting what it had cached previously to aid in code generation and redo it all fresh. " +
                                    "This should fix things.  If not, please feel free to contact customer support by emailing contactus@galoreinteractive.com. " +
                                    "NOTE: After clicking this button you will have to focus away from the Unity Editor window and then bring focus back so Unity will recognize the changes and " +
                                    "recompile etc...";
            EditorGUILayout.HelpBox(FIX_TEXT, MessageType.Warning);
            if (GUILayout.Button("Fix GONet Generated Code"))
            {
                FixGONetGeneratedCode();
            }

            if (GUILayout.Button("Generate Runtime only scripts"))
            {
                GenerateRuntimeOnlyScripts();
            }
            if (GUILayout.Button("Delete Runtime only scripts"))
            {
                DeleteRuntimeOnlyScripts();
            }
        }

        private void FixGONetGeneratedCode()
        {
            if (File.Exists(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH))
            {
                File.Delete(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH);
            }

            if (File.Exists(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_IN_SCENE_UNIQUE_SNAPS_FILE_PATH))
            {
                File.Delete(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_IN_SCENE_UNIQUE_SNAPS_FILE_PATH);
            }

            if (File.Exists(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.ASSET_FOLDER_SNAPS_FILE))
            {
                File.Delete(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.ASSET_FOLDER_SNAPS_FILE);
            }

            const string UNITY_LIBRARY_SCRIPT_ASSEMBLIES = "Library/ScriptAssemblies";
            if (Directory.Exists(UNITY_LIBRARY_SCRIPT_ASSEMBLIES))
            {
                foreach (string filePath in Directory.GetFiles(UNITY_LIBRARY_SCRIPT_ASSEMBLIES))
                {
                    File.Delete(filePath);
                }
            }

            if (Directory.Exists(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_FILE_PATH))
            {
                foreach (string filePath in Directory.GetFiles(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_FILE_PATH))
                {
                    File.Delete(filePath);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.UpdateAllUniqueSnaps();
        }

        internal static T CreateSyncSettingsProfileAsset<T>(string assetName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string desiredPath = string.Concat(ASSETS_SYNC_SETTINGS_PROFILES_FOLDER_PATH, assetName, ASSET_FILE_EXTENSION);
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(desiredPath);

            //AssetDatabase.CreateAsset(asset, desiredPath);
            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            return asset;
        }

        private void GenerateRuntimeOnlyScripts()
        {
            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GenerateFiles();
        }

        private void DeleteRuntimeOnlyScripts()
        {
            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.DeleteGeneratedFiles();
        }
    }
}
