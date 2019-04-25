using GONet;
using UnityEngine;

public class FieldChangeTest : MonoBehaviour
{
    [GONetAutoMagicalSync(SyncChangesEverySeconds = 4.5f)]
    public float someCoolGuyFloat;

    Vector3 startPosition;

    float moveAmount = 1f;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        /* test with an ever-changing field value: */
        if (GONetMain.IsServer)
        {
            someCoolGuyFloat += moveAmount * Time.deltaTime;
            if (someCoolGuyFloat >= 5 || someCoolGuyFloat <= -5)
            {
                moveAmount *= -1;
            }
        }
        /* */

        transform.position = startPosition + new Vector3(0, 0, someCoolGuyFloat);
    }
}
