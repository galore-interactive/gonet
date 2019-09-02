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
        }

        internal const string ASSETS_SYNC_SETTINGS_PROFILES_FOLDER_PATH = "Assets/GONet/Resources/GONet/SyncSettingsProfiles/";
        internal const string ASSET_FILE_EXTENSION = ".asset";

        string NAMEO_DEFAULT = "<Enter Name>";
        string nameo;

        string gonetIdText;
        string gonetIdText_raw;

        private void OnGUI()
        {
            if (!Application.isPlaying)
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
            }
            else // since GONetId is not assigned nor visible in inspector until playing, only show this then
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
    }
}
