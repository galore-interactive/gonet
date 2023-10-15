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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet.Utils
{
    public static class HierarchyUtils
    {
        const string PATH_SEPARATOR = "/";
        const char PATH_SEPARATOR_CHAR = '/';

        const string SAME_NAME_SIBLING_PREFIX = "_+3";
        static readonly int SAME_NAME_SIBLING_PREFIX_LENGTH = SAME_NAME_SIBLING_PREFIX.Length;
        const string SAME_NAME_SIBLING_SUFFIX = "N06";

        const string GONETID_PREFIX = "~3y";
        static readonly int GONETID_PREFIX_LENGTH = GONETID_PREFIX.Length;
        const string GONETID_SUFFIX = "3|]";
        static readonly int GONETID_SUFFIX_LENGTH = GONETID_SUFFIX.Length;

        private const string DONT_DESTROY_ON_LOAD_SCENE = "DontDestroyOnLoad";
        static readonly ArrayPool<GameObject> goArrayPool = new ArrayPool<GameObject>(100, 5, 2, 50);

        /// <summary>
        /// Example return values:
        /// ---"MyAwesomeSceneName/SomeRootObjectName/SomeChildName/SomeLeafName"
        /// ---"MyAwesomeSceneName/SomeRootObjectName/SomeChildName_+34N06/SomeLeafName" // multiple at same level with same name SomeChildName and the correct one is the 4th one
        /// ---"MyAwesomeSceneName/SomeRootObjectName/SomeChildName~3y1280013|]_+34N06/SomeLeafName" // multiple at same level with same name SomeChildName and the correct one is the 4th one AND it is a GNP with GONetId (at instantiation) of 128001
        /// ---"MyAwesomeSceneName/SomeRootObjectName/SomeChildName/SomeLeafName~3y1034253|]" // there is only one at this level with name SomeLeafName and it is a GNP with GONetId (at instantiation) of 103425
        /// 
        /// This is useful for uniquely identifying <see cref="GameObject"/>s with the same value across the network, because <see cref="UnityEngine.Object.GetInstanceID"/> will NOT be same across network.
        /// IMPORTANT: The return value from this method will ONLY work the reverse way when passing it as an arg to <see cref="FindByFullUniquePath(string)"/>!
        /// </summary>
        public static string GetFullUniquePath(GameObject gameObject)
        {
            if ((object)gameObject == null)
            {
                return string.Empty;
            }

            string path = string.Concat(PATH_SEPARATOR, gameObject.name);

            GONetParticipant gonetParticipant = gameObject.GetComponent<GONetParticipant>();
            if ((object)gonetParticipant != null && gonetParticipant.DoesGONetIdContainAllComponents())
            {
                path = string.Concat(path, GONETID_PREFIX, gonetParticipant.GONetIdAtInstantiation, GONETID_SUFFIX);
            }

            int siblingCount, mySiblingIndex;
            if (DoIHaveSiblingsOfSameName(gameObject, out siblingCount, out mySiblingIndex))
            {
                path = string.Concat(path, SAME_NAME_SIBLING_PREFIX, mySiblingIndex, SAME_NAME_SIBLING_SUFFIX);
            }

            while ((object)gameObject.transform.parent != null)
            {
                gameObject = gameObject.transform.parent.gameObject;

                string myUniqueName = gameObject.name;

                if (DoIHaveSiblingsOfSameName(gameObject, out siblingCount, out mySiblingIndex))
                {
                    myUniqueName = string.Concat(myUniqueName, SAME_NAME_SIBLING_PREFIX, mySiblingIndex, SAME_NAME_SIBLING_SUFFIX);
                }
                path = string.Concat(PATH_SEPARATOR, myUniqueName, path);
            }

            // now prefix with scene name:
            string sceneName = gameObject.scene == null /* this will happen in build for DontDestroyOnLoad */ ? DONT_DESTROY_ON_LOAD_SCENE : gameObject.scene.name;
            path = string.Concat(sceneName, path);

            return path;
        }

        /// <summary>
        /// PRE: <paramref name="gameObject"/> is NOT null...Unity null or otherwise!
        /// </summary>
        private static bool DoIHaveSiblingsOfSameName(GameObject gameObject, out int sameNameSiblingCount, out int mySameNameSiblingIndex)
        {
            mySameNameSiblingIndex = -1;

            int actualSiblingCount_consideringOversizedPool;
            GameObject[] siblings;
            int overallCount;
            bool areSiblingsFromPool = false;
            if ((object)gameObject.transform.parent == null)
            {
                siblings = gameObject.scene.GetRootGameObjects();
                overallCount = siblings.Length;
            }
            else
            {
                siblings = GetSiblings_NonRoot(gameObject, out actualSiblingCount_consideringOversizedPool);
                overallCount = actualSiblingCount_consideringOversizedPool;
                areSiblingsFromPool = true;
            }

            sameNameSiblingCount = 0;
            for (int i = 0; i < overallCount; ++i)
            {
                if (siblings[i].name == gameObject.name)
                {
                    if ((object)siblings[i] == (object)gameObject)
                    {
                        mySameNameSiblingIndex = sameNameSiblingCount;
                    }
                    sameNameSiblingCount++;
                }
            }

            if (areSiblingsFromPool)
            {
                goArrayPool.Return(siblings);
            }

            return sameNameSiblingCount > 1;
        }

        /// <summary>
        /// PRE: <paramref name="gameObject"/> has transform.parent that is not null (i.e., not a root level object).
        /// IMPORTANT: Caller is responsible for returning return value to <see cref="goArrayPool"/>!!!
        /// </summary>
        private static GameObject[] GetSiblings_NonRoot(GameObject gameObject, out int actualSiblingCount_consideringOversizedPool)
        {
            return GetChildren(gameObject.transform.parent.gameObject, out actualSiblingCount_consideringOversizedPool);
        }

        /// <summary>
        /// PRE: <paramref name="parentGameObject"/> is not null (i.e., not a root level object).
        /// IMPORTANT: Caller is responsible for returning return value to <see cref="goArrayPool"/>!!!
        /// </summary>
        private static GameObject[] GetChildren(GameObject parentGameObject, out int actualSiblingCount_consideringOversizedPool)
        {
            Transform parent = parentGameObject.transform;
            actualSiblingCount_consideringOversizedPool = parent.childCount;
            GameObject[] siblings = goArrayPool.Borrow(actualSiblingCount_consideringOversizedPool);
            for (int i = 0; i < actualSiblingCount_consideringOversizedPool; ++i)
            {
                siblings[i] = parent.GetChild(i).gameObject;
            }
            return siblings;
        }

        /// <summary>
        /// PRE: <paramref name="uniqueFullPath"/> was created by calling <see cref="GetFullUniquePath(GameObject)"/>!
        /// 
        /// TODO Use <see cref="GONetMain.recentlyDisabledGONetId_to_GONetIdAtInstantiation_Map"/> to help find any GNPs referenced that were here just a minute ago, but not now
        /// </summary>
        public static GameObject FindByFullUniquePath(string uniqueFullPath)
        {
            if (string.IsNullOrWhiteSpace(uniqueFullPath))
            {
                return null;
            }

            GameObject gameObject = null;
            string[] uniquePathParts = uniqueFullPath.Split(PATH_SEPARATOR_CHAR);
            int count = uniquePathParts.Length;
            string sceneName = uniquePathParts[0]; // scene name is always first!
            for (int iUniquePart = 1; iUniquePart < count; ++iUniquePart)
            {
                string uniquePathPart = uniquePathParts[iUniquePart];

                bool hasGONetIdAtInstantiation = false;
                uint gonetIdAtInstantiation = GONetParticipant.GONetId_Unset;
                int idPrefxIndex = uniquePathPart.LastIndexOf(GONETID_PREFIX);
                int idSuffixIndex = uniquePathPart.LastIndexOf(GONETID_SUFFIX);
                if (idPrefxIndex != -1 && idSuffixIndex > idPrefxIndex)
                {
                    int iStart = idPrefxIndex + GONETID_PREFIX_LENGTH;
                    hasGONetIdAtInstantiation = uint.TryParse(uniquePathPart.Substring(iStart, idSuffixIndex - iStart), out gonetIdAtInstantiation);

                    uniquePathPart = uniquePathPart.Remove(idPrefxIndex, (idSuffixIndex + GONETID_SUFFIX_LENGTH) - idPrefxIndex);
                }

                if (uniquePathPart.EndsWith(SAME_NAME_SIBLING_SUFFIX))
                {
                    int iPrefixStart = uniquePathPart.LastIndexOf(SAME_NAME_SIBLING_PREFIX);
                    int iSuffixStart = uniquePathPart.LastIndexOf(SAME_NAME_SIBLING_SUFFIX);
                    int indexLength = iSuffixStart - iPrefixStart - SAME_NAME_SIBLING_PREFIX_LENGTH;
                    int uniqueSiblingIndex = int.Parse(uniquePathPart.Substring(iPrefixStart + SAME_NAME_SIBLING_PREFIX_LENGTH, indexLength));
                    string gameObjectNameOriginal = uniquePathPart.Substring(0, iPrefixStart);

                    gameObject = GetUniqueSibling(sceneName, gameObject, gameObjectNameOriginal, uniqueSiblingIndex, hasGONetIdAtInstantiation, gonetIdAtInstantiation);
                }
                else
                {
                    if ((object)gameObject == null)
                    {
                        if (sceneName == DONT_DESTROY_ON_LOAD_SCENE)
                        {
                            GameObject crossOurFingersHopeToDie_thisBetterBeTheOnlyOne = GameObject.Find(uniquePathPart);
                            gameObject = crossOurFingersHopeToDie_thisBetterBeTheOnlyOne;
                        }
                        else
                        {
                            GameObject[] rootGOs = SceneManager.GetSceneByName(sceneName).GetRootGameObjects();
                            int childCount = rootGOs.Length;
                            for (int iRootGO = 0; iRootGO < childCount; ++iRootGO)
                            {
                                GameObject rootGO = rootGOs[iRootGO];
                                if (rootGO.name == uniquePathPart)
                                {
                                    gameObject = rootGO;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        gameObject = gameObject.transform.Find(uniquePathPart).gameObject;
                    }
                }

                if (hasGONetIdAtInstantiation && gameObject != null)
                {
                    GONetParticipant gonetParticipant = gameObject.GetComponent<GONetParticipant>();
                    if ((object)gonetParticipant == null || (gonetParticipant.GONetIdAtInstantiation != gonetIdAtInstantiation && !GONetMain.WasDefinedInScene(gonetParticipant)))
                    {
                        GONetLog.Warning("We found the wrong GNP or did not find one at all.  uniqueFullPath: " + uniqueFullPath + " gonetParticipant.GONetIdAtInstantiation: " + ((object)gonetParticipant == null ? "<null>" : gonetParticipant.GONetIdAtInstantiation.ToString()));
                    }
                }
            }

            return gameObject;
        }

        private static readonly List<GameObject> rootGOs = new List<GameObject>(100);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="gameObjectNameOriginal">does NOT contain <see cref="SAME_NAME_SIBLING_PREFIX"/> or <see cref="SAME_NAME_SIBLING_SUFFIX"/></param>
        /// <param name="uniqueSiblingIndex"></param>
        /// <returns></returns>
        private static GameObject GetUniqueSibling(string sceneName, GameObject parent, string gameObjectNameOriginal, int uniqueSiblingIndex, bool hasGONetIdAtInstantiation, uint gonetIdAtInstantiation)
        {
            int actualSiblingCount_consideringOversizedPool;

            GONetParticipant gnpWithMatchingIdAtInstantiation = null;
            GameObject[] siblings;
            int overallCount;
            bool areSiblingsFromPool = false;
            if ((object)parent == null)
            {
                if (sceneName == DONT_DESTROY_ON_LOAD_SCENE)
                {
                    GameObject crossOurFingersHopeToDie_thisBetterBeTheOnlyOne = GameObject.Find(gameObjectNameOriginal);
                    siblings = new GameObject[1] { crossOurFingersHopeToDie_thisBetterBeTheOnlyOne }; // TODO do not new up the array here!
                }
                else
                {
                    siblings = SceneManager.GetSceneByName(sceneName).GetRootGameObjects();
                }
                overallCount = siblings.Length;
            }
            else
            {
                siblings = GetChildren(parent, out actualSiblingCount_consideringOversizedPool);
                overallCount = actualSiblingCount_consideringOversizedPool;
                areSiblingsFromPool = true;
            }

            GameObject uniqueSibling = null;
            int sameNameSiblingCount = 0;
            for (int i = 0; i < overallCount; ++i)
            {
                GameObject sibling = siblings[i];

                if (hasGONetIdAtInstantiation && (object)gnpWithMatchingIdAtInstantiation == null)
                {
                    GONetParticipant gnp = sibling.GetComponent<GONetParticipant>();
                    if ((object)gnp != null && gnp.GONetIdAtInstantiation == gonetIdAtInstantiation)
                    {
                        gnpWithMatchingIdAtInstantiation = gnp;
                    }
                }

                if (siblings[i].name == gameObjectNameOriginal)
                {
                    if (sameNameSiblingCount == uniqueSiblingIndex)
                    {
                        uniqueSibling = sibling;
                        break;
                    }

                    ++sameNameSiblingCount;
                }
            }

            if ((object)uniqueSibling == null)
            {
                if (hasGONetIdAtInstantiation && (object)gnpWithMatchingIdAtInstantiation != null)
                {
                    uniqueSibling = gnpWithMatchingIdAtInstantiation.gameObject;
                }

                if ((object)uniqueSibling == null)
                {
                    const string NOT = "Sibling not found. gameObjectNameOriginal: ";
                    const string IDX = " uniqueSiblingIndex: ";
                    const string GNID = " gonetIdAtInstantiation: ";
                    GONetLog.Warning(string.Concat(NOT, gameObjectNameOriginal, IDX, uniqueSiblingIndex, GNID, gonetIdAtInstantiation));
                }
            }

            if (areSiblingsFromPool)
            {
                goArrayPool.Return(siblings);
            }

            if (hasGONetIdAtInstantiation && (object)uniqueSibling != null && (object)gnpWithMatchingIdAtInstantiation != null)
            {
                GONetParticipant uniqueSiblingGNP = uniqueSibling.GetComponent<GONetParticipant>();
                if ((object)uniqueSiblingGNP == null)
                {
                    const string WRONG = "Well, our matching did not add up and the data we were given did not get accounted for correctly.  We identified a matching sibling that is not a GONetParticipant and we were expecting to match with GONetIdAtInstantiation!";
                    GONetLog.Error(WRONG);
                }
                else if (uniqueSiblingGNP.GONetIdAtInstantiation != gnpWithMatchingIdAtInstantiation.GONetIdAtInstantiation)
                {
                    const string FIX = "We force fixed a mismatch of sibling.  This mismatch likely occurred due to destroying of a GameObject in the hierarchy that was key to uniquely identifying this thing; however, luckily, we had enough additiona information with the GONetIdAtInstantiation to deal with it and we are all good now!";
                    GONetLog.Info(FIX);

                    uniqueSibling = gnpWithMatchingIdAtInstantiation.gameObject;
                }
            }

            return uniqueSibling;
        }

        public static T GetComponentInImmediateChildrenOnly<T>(GameObject gameObject) where T : Component
        {
            if ((object)gameObject != null)
            {
                Transform parent = gameObject.transform;
                int childCount = parent.childCount;
                for (int i = 0; i < childCount; ++i)
                {
                    Transform child = parent.GetChild(i);
                    T componentInImmediateChildren = child.gameObject.GetComponent<T>();
                    if ((object)componentInImmediateChildren != null)
                    {
                        return componentInImmediateChildren;
                    }
                }
            }

            return null;
        }
    }
}
