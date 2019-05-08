using GONet;
using UnityEngine;

public class FieldChangeTest : MonoBehaviour
{
    //[GONetAutoMagicalSync(SyncChangesEverySeconds = 4.5f)]
    [GONetAutoMagicalSync(Reliability = AutoMagicalSyncReliability.Unreliable, ShouldBlendBetweenValuesReceived = true)]
    public float someCoolGuyFloat;

    //[GONetAutoMagicalSync(SyncChangesEverySeconds = 0.45f)] // NOTE: 10 times more frequent than someCoolGuyFloat
    [GONetAutoMagicalSync(Reliability = AutoMagicalSyncReliability.Unreliable, ShouldBlendBetweenValuesReceived = true)]
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
        }
        /* */

        transform.position = startPosition + new Vector3(0, 0, someCoolGuyFloat);
        transform.rotation = Quaternion.Euler(startRotation.eulerAngles * rottieTotty);
    }
}
