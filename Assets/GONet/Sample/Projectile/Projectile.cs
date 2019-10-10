using TMPro;
using UnityEngine;

namespace GONet.Sample
{
    [RequireComponent(typeof(GONetParticipant))]
    public class Projectile : MonoBehaviour
    {
        public float speed = 2;

        public GONetParticipant GONetParticipant { get; private set; }

        TextMeshProUGUI text;

        private void Awake()
        {
            GONetParticipant = GetComponent<GONetParticipant>();
            text = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (GONetParticipant.IsMine)
            {
                text.text = "MINE";
                text.color = Color.green;
            }
            else
            {
                text.text = "?";
                text.color = Color.red;
            }
        }
    }
}
