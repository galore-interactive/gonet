using System.IO;
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

        private void OnGUI()
        {
            nameo = EditorGUILayout.TextField("New Sync Settings Profile Template", nameo);
            if (GUILayout.Button("Create"))
            {
                CreateSyncSettingsProfileAsset<GONetAutoMagicalSyncSettings_ProfileTemplate>(nameo);
                nameo = NAMEO_DEFAULT;
            }

            EditorGUILayout.Space();


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
