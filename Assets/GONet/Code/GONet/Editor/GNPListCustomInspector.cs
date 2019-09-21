
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
