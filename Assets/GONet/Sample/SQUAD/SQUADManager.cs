/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
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
