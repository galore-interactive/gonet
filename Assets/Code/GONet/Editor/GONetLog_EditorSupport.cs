using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using GONet.Utils;

namespace GONet.Editor
{
    public class GONetLog_EditorSupport
    {
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            string configFolder = Path.Combine(Application.dataPath, "../configs");
            string dataFolder = Path.Combine(pathToBuiltProject.Replace(".exe", "_Data"), "configs");

            FileUtils.Copy(configFolder, dataFolder);
        }
    }
}
