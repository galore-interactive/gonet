using GONet.Utils;
using UnityEngine;
using UnityEngine.UI;

public class SQUADManager : MonoBehaviour
{
    public Slider interpolationTimeSlider;

    public Transform q0;
    public Transform q1;

    public Transform result;

    public Transform q2;
    public Transform q3;

    private void Update()
    {
        result.position = q1.position + ((q2.position - q1.position) * interpolationTimeSlider.value);

        Quaternion 
            q0_ = q0.rotation,
            q1_ = q1.rotation,
            q2_ = q2.rotation,
            q3_ = q3.rotation;
        Quaternion q, a, b, p;
        QuaternionUtils.SquadSetup(ref q0_, ref q1_, ref q2_, ref q3_, out q, out a, out b, out p);
        result.rotation = QuaternionUtils.Squad(ref q, ref a, ref b, ref p, interpolationTimeSlider.value).normalized;

        result.rotation = 
            QuaternionUtils.SlerpUnclamped(
                ref q1_,
                ref q2_,
                interpolationTimeSlider.value * 2);
    }
}
