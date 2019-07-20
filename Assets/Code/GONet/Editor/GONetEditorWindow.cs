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

        string NAMEO_DEFAULT = "<Enter Name>";
        string nameo;

        private void OnGUI()
        {
            nameo = EditorGUILayout.TextField("New Sync Settings Profile Template", nameo);
            if (GUILayout.Button("Create"))
            {
                CreateAsset<GONetAutoMagicalSyncSettings_ProfileTemplate>(nameo);
                nameo = NAMEO_DEFAULT;
            }
        }
        
        static void CreateAsset<T>(string assetName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string desiredPath = string.Concat("Assets/Resources/GONet/SyncSettingsProfiles/", assetName, ".asset");
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(desiredPath);

            //AssetDatabase.CreateAsset(asset, desiredPath);
            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    }
}
