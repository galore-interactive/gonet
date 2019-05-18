using GONet;
using UnityEngine;

public class FieldChangeTest : MonoBehaviour
{
    [GONetAutoMagicalSync(Reliability = AutoMagicalSyncReliability.Unreliable, ShouldBlendBetweenValuesReceived = true, QuantizeDownToBitCount = 4, QuantizeLowerBound = -6.5f, QuantizeUpperBound = 6.5f)]
    public float someCoolGuyFloat;

    public float rottieTotty;

    Vector3 startPosition;
    Quaternion startRotation;

    float moveAmount = 5f;
    float moveAmount_rottieTotty = 0.15f;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void Update()
    {
        /* test with an ever-changing field value: */
        if (GONetMain.IsServer)
        {
            someCoolGuyFloat += moveAmount * Time.deltaTime;
            if (someCoolGuyFloat >= 6 || someCoolGuyFloat <= -6)
            {
                moveAmount *= -1;
            }

            rottieTotty += moveAmount_rottieTotty * Time.deltaTime;
            if (rottieTotty >= 2 || rottieTotty <= -1)
            {
                moveAmount_rottieTotty *= -1;
            }

            transform.rotation = Quaternion.Euler(startRotation.eulerAngles * rottieTotty);
        }
        /* */

        transform.position = startPosition + new Vector3(0, 0, someCoolGuyFloat);
    }
}
