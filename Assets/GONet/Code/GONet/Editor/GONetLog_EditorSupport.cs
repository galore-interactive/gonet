/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
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
