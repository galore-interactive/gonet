using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GONet.Editor
{
    /// <summary>
    /// TODO draw properties if decorated with <see cref="GONetAutoMagicalSyncAttribute"/>.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class GONetAutoMagicalSyncMemberCustomInspector : UnityEditor.Editor
    {
        MonoBehaviour targetMonoBehaviour;

        private void OnEnable()
        {
            targetMonoBehaviour = (MonoBehaviour)target;
        }

        public override void OnInspectorGUI()
        {
            // TODO draw properties if decorated with <see cref="GONetAutoMagicalSyncAttribute"/>.

            DrawDefaultInspector();
        }
    }
}
