using GONet.Utils;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// TODO: add good description.
    /// 
    /// Do NOT add to <see cref="GameObject"/> instances yourself.  This is more of an internal to GONet managed concept.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(GONetParticipant))]
    public class GONetSessionContext : MonoBehaviour
    {
        private const string WORKAROUND = "Awake....side effect of getting GONetLog to do static initialization inside the Unity main thread...or else!";

        private void Awake()
        {
            GONetLog.Debug(WORKAROUND);

            DontDestroyOnLoad(gameObject);
        }
    }
}
