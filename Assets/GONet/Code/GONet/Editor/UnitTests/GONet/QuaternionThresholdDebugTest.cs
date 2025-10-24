using NUnit.Framework;
using UnityEngine;

namespace GONet.Tests
{
    [TestFixture]
    public class QuaternionThresholdDebugTest
    {
        [Test]
        public void DebugActualDotProductFor0_133Degrees()
        {
            Quaternion q1 = Quaternion.Euler(0, 0, 0);
            Quaternion q2 = Quaternion.Euler(0, 0.133f, 0);

            float dot = Quaternion.Dot(q1, q2);
            float angle = Quaternion.Angle(q1, q2);

            Debug.Log($"[0.133°] Actual angle: {angle}°");
            Debug.Log($"[0.133°] Actual dot product: {dot:F10}");
            Debug.Log($"[0.133°] Threshold 0.9999999f (7 nines): {0.9999999f:F10}");
            Debug.Log($"[0.133°] dot > 0.9999999f? {dot > 0.9999999f}");
        }

        [Test]
        public void DebugActualDotProductFor0_027Degrees()
        {
            Quaternion q1 = Quaternion.Euler(0, 0, 0);
            Quaternion q2 = Quaternion.Euler(0, 0.027f, 0);

            float dot = Quaternion.Dot(q1, q2);
            float angle = Quaternion.Angle(q1, q2);

            Debug.Log($"[0.027°] Actual angle: {angle}°");
            Debug.Log($"[0.027°] Actual dot product: {dot:F10}");
            Debug.Log($"[0.027°] Threshold 0.9999999f (7 nines): {0.9999999f:F10}");
            Debug.Log($"[0.027°] dot > 0.9999999f? {dot > 0.9999999f}");
        }

        [Test]
        public void DebugActualDotProductFor0_026Degrees()
        {
            Quaternion q1 = Quaternion.Euler(0, 0, 0);
            Quaternion q2 = Quaternion.Euler(0, 0.026f, 0);

            float dot = Quaternion.Dot(q1, q2);
            float angle = Quaternion.Angle(q1, q2);

            Debug.Log($"[0.026°] Actual angle: {angle}°");
            Debug.Log($"[0.026°] Actual dot product: {dot:F10}");
            Debug.Log($"[0.026°] Threshold 0.9999999f (7 nines): {0.9999999f:F10}");
            Debug.Log($"[0.026°] dot > 0.9999999f? {dot > 0.9999999f}");
        }

        [Test]
        public void DebugActualDotProductFor0_05Degrees()
        {
            Quaternion q1 = Quaternion.Euler(0, 0, 0);
            Quaternion q2 = Quaternion.Euler(0, 0.05f, 0);

            float dot = Quaternion.Dot(q1, q2);
            float angle = Quaternion.Angle(q1, q2);

            Debug.Log($"[0.05°] Actual angle: {angle}°");
            Debug.Log($"[0.05°] Actual dot product: {dot:F10}");
            Debug.Log($"[0.05°] Threshold 0.9999999f (7 nines): {0.9999999f:F10}");
            Debug.Log($"[0.05°] dot > 0.9999999f? {dot > 0.9999999f}");
        }

        [Test]
        public void DebugDoubleCover()
        {
            Quaternion q1 = new Quaternion(0.707f, 0.707f, 0, 0);
            Quaternion q2 = new Quaternion(-0.707f, -0.707f, 0, 0);

            float dot = Quaternion.Dot(q1, q2);
            float angle = Quaternion.Angle(q1, q2);

            Debug.Log($"[DoubleCover] q1: {q1}");
            Debug.Log($"[DoubleCover] q2: {q2}");
            Debug.Log($"[DoubleCover] Angle: {angle}°");
            Debug.Log($"[DoubleCover] Dot product: {dot:F10}");
        }

        [Test]
        public void VerifyCrossPlatformSafeThreshold()
        {
            // Test with proposed safer threshold: 0.9999998f (6 nines + 8)
            float saferThreshold = 0.9999998f;

            // Our target: 0.133° (8°/s @ 60 FPS)
            Quaternion q1 = Quaternion.Euler(0, 0, 0);
            Quaternion q2 = Quaternion.Euler(0, 0.133f, 0);
            float dot_0_133 = Quaternion.Dot(q1, q2);

            Debug.Log($"[SaferThreshold] Proposed threshold: {saferThreshold:F10}");
            Debug.Log($"[SaferThreshold] 0.133° dot product: {dot_0_133:F10}");
            Debug.Log($"[SaferThreshold] 0.133° detected? {dot_0_133 <= saferThreshold}");
            Debug.Log($"[SaferThreshold] Margin: {saferThreshold - dot_0_133:F10}");
        }
    }
}
