using UnityEngine;

namespace GONet.Sample
{
    [RequireComponent(typeof(Camera))]
    public class SimpleCameraMovement : MonoBehaviour
    {
        public float speed = 5;

        private void Update()
        {
            if (Input.GetKey(KeyCode.DownArrow))
            {
                transform.Translate(new Vector3(0, -speed * Time.deltaTime), Space.Self);
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                transform.Translate(new Vector3(0, speed * Time.deltaTime), Space.Self);
            }

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                transform.Translate(new Vector3(-speed * Time.deltaTime, 0), Space.Self);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                transform.Translate(new Vector3(speed * Time.deltaTime, 0), Space.Self);
            }
        }
    }
}
