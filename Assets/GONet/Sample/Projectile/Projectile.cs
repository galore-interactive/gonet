/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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
