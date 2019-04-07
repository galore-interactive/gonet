using GONet;
using UnityEngine;

public class FieldChangeTest : MonoBehaviour
{
    [GONetAutoMagicalSync]
    public float someCoolGuyFloat;

    /* test with an ever-changing field value:
    private void Update()
    {
        if (GONetMain.IsServer)
        {
            someCoolGuyFloat += 0.0001f;
        }
    }
    */
}
