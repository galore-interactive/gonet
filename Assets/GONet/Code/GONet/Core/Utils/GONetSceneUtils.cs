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

using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet.Utils
{
    /// <summary>
    /// Utilities for scene-related operations in GONet.
    /// </summary>
    public static class GONetSceneUtils
    {
        /// <summary>
        /// Reliably determines if a GameObject is in DontDestroyOnLoad.
        /// Works in both Editor and Build.
        /// In Editor: scene.name == "DontDestroyOnLoad"
        /// In Build: scene is null or invalid
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInDontDestroyOnLoad(GameObject gameObject)
        {
            if (gameObject == null) return false;

            Scene scene = gameObject.scene;

            // Fast path: Check validity first (fastest check)
            // Build: invalid scene = DDOL
            if (!scene.IsValid())
                return true;

            // Slower path: String comparison (only if scene is valid)
            // Editor: scene name is "DontDestroyOnLoad"
            return scene.name == HierarchyUtils.DONT_DESTROY_ON_LOAD_SCENE;
        }

        /// <summary>
        /// Gets scene identifier for networked scene association.
        /// Returns "DontDestroyOnLoad" for DDOL objects, scene name otherwise.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetSceneIdentifier(GameObject gameObject)
        {
            if (IsInDontDestroyOnLoad(gameObject))
                return HierarchyUtils.DONT_DESTROY_ON_LOAD_SCENE;

            Scene scene = gameObject.scene;
            return scene.IsValid() ? scene.name : string.Empty;
        }
    }
}