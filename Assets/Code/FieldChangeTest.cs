using GONet;
using UnityEngine;

public class FieldChangeTest : MonoBehaviour
{
    [GONetAutoMagicalSync]
    public float someCoolGuyFloat;

    Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
    }

    /* test with an ever-changing field value:
    private void Update()
    {
        if (GONetMain.IsServer)
        {
            someCoolGuyFloat += 0.0001f;
        }
    }
    */

    private void Update()
    {
        transform.position = startPosition + new Vector3(0, 0, someCoolGuyFloat);
    }
}
