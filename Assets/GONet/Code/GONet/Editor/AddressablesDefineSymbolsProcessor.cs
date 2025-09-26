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

using UnityEditor;
using UnityEngine;
using System.Linq;

namespace GONet.Editor
{
    /// <summary>
    /// Automatically adds ADDRESSABLES_AVAILABLE define symbol when Unity Addressables package is installed.
    /// This allows GONet to use addressables functionality when available while maintaining backwards compatibility.
    /// </summary>
    [InitializeOnLoad]
    public class AddressablesDefineSymbolsProcessor
    {
        private const string ADDRESSABLES_DEFINE = "ADDRESSABLES_AVAILABLE";
        private const string ADDRESSABLES_PACKAGE_NAME = "com.unity.addressables";

        static AddressablesDefineSymbolsProcessor()
        {
            CheckAndUpdateAddressablesDefine();
        }

        private static void CheckAndUpdateAddressablesDefine()
        {
            try
            {
                var request = UnityEditor.PackageManager.Client.List();
                EditorApplication.CallbackFunction callback = null;
                callback = () =>
                {
                    if (request.IsCompleted)
                    {
                        EditorApplication.update -= callback;

                        bool hasAddressables = request.Result.Any(package => package.name == ADDRESSABLES_PACKAGE_NAME);
                        UpdateDefineSymbol(hasAddressables);
                    }
                };

                EditorApplication.update += callback;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"GONet: Failed to check Addressables package status: {ex.Message}");
            }
        }

        private static void UpdateDefineSymbol(bool hasAddressables)
        {
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var definesList = currentDefines.Split(';').ToList();

            bool hasDefine = definesList.Contains(ADDRESSABLES_DEFINE);

            if (hasAddressables && !hasDefine)
            {
                definesList.Add(ADDRESSABLES_DEFINE);
                string newDefines = string.Join(";", definesList.Where(d => !string.IsNullOrEmpty(d)));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newDefines);
                Debug.Log("GONet: Added ADDRESSABLES_AVAILABLE define symbol - Addressables support enabled");
            }
            else if (!hasAddressables && hasDefine)
            {
                definesList.Remove(ADDRESSABLES_DEFINE);
                string newDefines = string.Join(";", definesList.Where(d => !string.IsNullOrEmpty(d)));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newDefines);
                Debug.Log("GONet: Removed ADDRESSABLES_AVAILABLE define symbol - Addressables not available");
            }
        }
    }
}