using UnityEngine;

namespace GONet
{
    [RequireComponent(typeof(GONetParticipant))]
    public class DestroyIfMineOnKeyPress : MonoBehaviour
    {
        GONetParticipant gnp;

        [GONetAutoMagicalSync(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___EMPTY_USE_ATTRIBUTE_PROPERTIES_DIRECTLY, ProcessingPriority = 3)]
        public float willHeUpdate;

        private void Awake()
        {
            gnp = GetComponent<GONetParticipant>();
        }

        private void Update()
        {
            if (gnp != null && (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.D)))
            {
                if (GONetMain.IsMine(gnp))
                {
                    Destroy(gameObject); // this should auto-propagate to all
                }
            }
        }
    }
}
