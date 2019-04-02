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
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
