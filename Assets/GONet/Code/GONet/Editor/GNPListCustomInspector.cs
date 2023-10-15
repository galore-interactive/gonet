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
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GONet.Editor
{
    public abstract class GNPListCustomInspector : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        protected void DrawGNPList(IEnumerable<GONetParticipant> gnpList, string customLabel, bool shouldShowGONetIdRAW)
        {
            if (gnpList != null && gnpList.Count() > 0)
            {
                EditorGUILayout.Separator();
                const string CLICK = " (click to select)";
                EditorGUILayout.LabelField(string.Concat(customLabel, CLICK));
                foreach (var gnp in gnpList.OrderBy(x => x.gameObject.name))
                {
                    const string RAW = ", GO Net Id (RAW): ";
                    const string GNId = ", GO Net Id: ";
                    string buttonLabel = string.Concat(gnp.gameObject.name, shouldShowGONetIdRAW ? RAW : GNId, shouldShowGONetIdRAW ? gnp.gonetId_raw : gnp.GONetId);
                    if (GUILayout.Button(buttonLabel))
                    {
                        Selection.activeGameObject = gnp.gameObject;
                    }
                }
            }
        }
    }
}
