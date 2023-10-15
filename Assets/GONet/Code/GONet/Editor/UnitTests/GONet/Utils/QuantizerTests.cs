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

using NUnit.Framework;
using System;
using UnityEngine;

namespace GONet.Utils
{
    [TestFixture]
    public class QuantizerTests
    {
        [Test]
        public void QuaternionOrientationEquality_PerformanceComparison()
        {
            Quaternion rotation1 = Quaternion.Euler(123.456f, -324.654f, 34.001f);
            Quaternion rotation2 = Quaternion.Euler(-123.456f, 324.654f, -34.001f);

            long ticksAtStart;
            long durationTicks;

            ticksAtStart = DateTime.UtcNow.Ticks;
            QuaternionOrientationEquality(rotation1, rotation2, false, 100_000, true);
            durationTicks = DateTime.UtcNow.Ticks - ticksAtStart;
            Debug.Log("duration (ms): " + TimeSpan.FromTicks(durationTicks).TotalMilliseconds);

            ticksAtStart = DateTime.UtcNow.Ticks;
            QuaternionOrientationEquality(rotation1, rotation2, false, 100_000, false);
            durationTicks = DateTime.UtcNow.Ticks - ticksAtStart;
            Debug.Log("duration (ms): " + TimeSpan.FromTicks(durationTicks).TotalMilliseconds);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            rotation1 = rotation2;

            ticksAtStart = DateTime.UtcNow.Ticks;
            QuaternionOrientationEquality(rotation1, rotation2, true, 100_000, true);
            durationTicks = DateTime.UtcNow.Ticks - ticksAtStart;
            Debug.Log("duration (ms): " + TimeSpan.FromTicks(durationTicks).TotalMilliseconds);

            ticksAtStart = DateTime.UtcNow.Ticks;
            QuaternionOrientationEquality(rotation1, rotation2, true, 100_000, false);
            durationTicks = DateTime.UtcNow.Ticks - ticksAtStart;
            Debug.Log("duration (ms): " + TimeSpan.FromTicks(durationTicks).TotalMilliseconds);


            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            rotation1 = Quaternion.Euler(0f, 180f, 0f);
            rotation2 = Quaternion.Euler(180f, 0f, -180f);

            ticksAtStart = DateTime.UtcNow.Ticks;
            QuaternionOrientationEquality(rotation1, rotation2, true, 100_000, true);
            durationTicks = DateTime.UtcNow.Ticks - ticksAtStart;
            Debug.Log("duration (ms): " + TimeSpan.FromTicks(durationTicks).TotalMilliseconds);

            ticksAtStart = DateTime.UtcNow.Ticks;
            QuaternionOrientationEquality(rotation1, rotation2, true, 100_000, false);
            durationTicks = DateTime.UtcNow.Ticks - ticksAtStart;
            Debug.Log("duration (ms): " + TimeSpan.FromTicks(durationTicks).TotalMilliseconds);
        }

        private void QuaternionOrientationEquality(Quaternion rotation1, Quaternion rotation2, bool shouldBeEqual, int iterations, bool useEulers)
        {
            for (int i = 0; i < iterations; ++i)
            {
                if (useEulers)
                {
                    bool isEqual = rotation1.eulerAngles == rotation2.eulerAngles;
                    Assert.AreEqual(shouldBeEqual, isEqual);
                }
                else
                {
                    float angleDiff = Quaternion.Angle(rotation1, rotation2);
                    bool isEqual = angleDiff < 1e-3f;
                    Assert.AreEqual(shouldBeEqual, isEqual);
                }
            }
        }


        [Test]
        public void ShaunTest()
        {
            const float LOWER = -31.999f;
            const float UPPER = 31.999f;
            const uint QUANTIZED_BITS = 16;

            uint max = (uint)Mathf.Pow(2, QUANTIZED_BITS);
            for (uint i = 0; i < 100; ++i)
            {
                float unquantized_calculated = Quantizer.Unquantize(LOWER, UPPER, i, QUANTIZED_BITS);
                GONetLog.Debug(string.Concat("quantized: ", i, " unquantized: ", unquantized_calculated));
            }
        }

        [Test]
        public void CompareWithHalfFloat()
        {
            const int ITERATIONS = 100000;

            var runtime = CompareWithHalfFloat(false, false, false, true, ITERATIONS); // 6.9ms
            GONetLog.Debug(string.Concat(ITERATIONS, " iterations of Mathf took \t", runtime.TotalMilliseconds, " milliseconds."));

            runtime = CompareWithHalfFloat(false, false, true, false, ITERATIONS); // ?ms
            GONetLog.Debug(string.Concat(ITERATIONS, " iterations of QuantizerFast instance took \t", runtime.TotalMilliseconds, " milliseconds."));

            runtime = CompareWithHalfFloat(false, true, false, false, ITERATIONS); // ?ms
            GONetLog.Debug(string.Concat(ITERATIONS, " iterations of QuantizerFast took \t", runtime.TotalMilliseconds, " milliseconds."));

            runtime = CompareWithHalfFloat(true, false, false, false, ITERATIONS); // 12.6ms
            GONetLog.Debug(string.Concat(ITERATIONS, " iterations of Quantizer took \t", runtime.TotalMilliseconds, " milliseconds."));
        }

        TimeSpan CompareWithHalfFloat(bool shouldRunQuantizer, bool shouldRunQuantizerFast, bool shouldRunQuantizerFastInstance, bool shouldRunMathf, int iterations)
        {
            const float LOWER = -3.2768f;
            const float UPPER = 3.2767f;
            //const float LOWER = -1000f;
            //const float UPPER = 1000f;
            const uint QUANTIZED_BITS = 8;

            double diff_unquantized_total = 0;
            double diff_unquantizedFast_total = 0;
            double diff_unquantizedFastInstance_total = 0;
            double diff_unhalfied_total = 0;

            Quantizer instance = new Quantizer(LOWER, UPPER, QUANTIZED_BITS, false);

            Quantizer.QuantizeFastPrep(LOWER, UPPER, QUANTIZED_BITS, false);

            DateTime start = HighResolutionTimeUtils.Now;
            for (int i = 0; i < iterations; ++i)
            {
                float original = UnityEngine.Random.Range(LOWER, UPPER);

                if (shouldRunQuantizer)
                {
                    uint quantized = Quantizer.Quantize(LOWER, UPPER, original, QUANTIZED_BITS);
                    float unquantized = Quantizer.Unquantize(LOWER, UPPER, quantized, QUANTIZED_BITS);
                    float diff_unquantized = Math.Abs(original - unquantized);
                    diff_unquantized_total += diff_unquantized;
                }

                if (shouldRunQuantizerFast)
                {
                    uint quantized = Quantizer.QuantizeFast(original);
                    float unquantized = Quantizer.UnquantizeFast(quantized);
                    float diff_unquantizedFast = Math.Abs(original - unquantized);
                    diff_unquantizedFast_total += diff_unquantizedFast;
                }

                if (shouldRunQuantizerFastInstance)
                {
                    uint quantized = instance.Quantize(original);
                    float unquantized = instance.Unquantize(quantized);
                    float diff_unquantizedFastInstance = Math.Abs(original - unquantized);
                    diff_unquantizedFastInstance_total += diff_unquantizedFastInstance;
                }

                if (shouldRunMathf)
                {
                    ushort halfie = Mathf.FloatToHalf(original);
                    float unhalfied = Mathf.HalfToFloat(halfie);
                    float diff_unhalfied = Math.Abs(original - unhalfied);
                    diff_unhalfied_total += diff_unhalfied;
                }

                //Log.Debug(string.Concat("original: ", original, " unquantized: ", unquantized, " unhalfied: ", unhalfied));
                //Log.Debug(string.Concat("WINNER: ", diff_unhalfied < diff_unquantized ? "Mathf.Half" : "Quantizer"));
            }
            DateTime end = HighResolutionTimeUtils.Now;

            double diff_unquantizedFast_average = diff_unquantizedFast_total / iterations;
            double diff_unquantizedFastInstance_average = diff_unquantizedFastInstance_total / iterations;
            double diff_unquantized_average = diff_unquantized_total / iterations;
            double diff_unhalfied_average = diff_unhalfied_total / iterations;
            GONetLog.Debug(string.Concat("After ", iterations, " iterations, \ndiff_unquantized_average: \t\t", diff_unquantized_average, "\ndiff_unquantizedFast_average: \t\t", diff_unquantizedFast_average, "\ndiff_unquantizedFastInstance_average: \t\t", diff_unquantizedFastInstance_average, "\ndiff_unhalfied_average: \t\t", diff_unhalfied_average, "\nOverall WINNER: ", diff_unhalfied_total < diff_unquantized_total ? "Mathf.Half" : "Quantizer"));

            return new TimeSpan(end.Ticks - start.Ticks);
        }

        [Test]
        public void PositiveRange()
        {
            const float LOWER = 0.00037f;
            const float UPPER = 0.015f;
            const float UNQUANTIZED_ORIGINAL = 0.0056732248f;
            const uint QUANTIZED_BITS = 3;

            uint quantized = Quantizer.Quantize(LOWER, UPPER, UNQUANTIZED_ORIGINAL, QUANTIZED_BITS);
            Assert.AreEqual(3, quantized);

            float unquantized_calculated = Quantizer.Unquantize(LOWER, UPPER, quantized, QUANTIZED_BITS);
            Assert.GreaterOrEqual(unquantized_calculated, LOWER);
            Assert.LessOrEqual(unquantized_calculated, UPPER);
            GONetLog.Debug(string.Concat("Unquantized..... original: ", UNQUANTIZED_ORIGINAL, " calculated: ", unquantized_calculated));


            for (uint i = 0; i < Mathf.Pow(2, QUANTIZED_BITS); ++i)
            {
                unquantized_calculated = Quantizer.Unquantize(LOWER, UPPER, i, QUANTIZED_BITS);
                GONetLog.Debug(string.Concat("quantized: ", i, " unquantized: ", unquantized_calculated));
            }
        }

        [Test]
        public void NegativeRange()
        {
            const float LOWER = -0.015f;
            const float UPPER = -0.00037f;
            const float UNQUANTIZED_ORIGINAL = -0.0056732248f;
            const uint QUANTIZED_BITS = 3;

            uint quantized = Quantizer.Quantize(LOWER, UPPER, UNQUANTIZED_ORIGINAL, QUANTIZED_BITS);
            Assert.AreEqual(4, quantized);

            float unquantized_calculated = Quantizer.Unquantize(LOWER, UPPER, quantized, QUANTIZED_BITS);
            Assert.GreaterOrEqual(unquantized_calculated, LOWER);
            Assert.LessOrEqual(unquantized_calculated, UPPER);
            GONetLog.Debug(string.Concat("Unquantized..... original: ", UNQUANTIZED_ORIGINAL, " calculated: ", unquantized_calculated));


            for (uint i = 0; i < Mathf.Pow(2, QUANTIZED_BITS); ++i)
            {
                unquantized_calculated = Quantizer.Unquantize(LOWER, UPPER, i, QUANTIZED_BITS);
                GONetLog.Debug(string.Concat("quantized: ", i, " unquantized: ", unquantized_calculated));
            }
        }


        [Test]
        public void NegativeToPositiveRange()
        {
            const float LOWER = -0.00037f;
            const float UPPER = 0.00037f;
            const float UNQUANTIZED_ORIGINAL = 0;
            const uint QUANTIZED_BITS = 3;

            uint quantized = Quantizer.Quantize(LOWER, UPPER, UNQUANTIZED_ORIGINAL, QUANTIZED_BITS);
            Assert.AreEqual(4, quantized);

            float unquantized_calculated = Quantizer.Unquantize(LOWER, UPPER, quantized, QUANTIZED_BITS);
            Assert.GreaterOrEqual(unquantized_calculated, LOWER);
            Assert.LessOrEqual(unquantized_calculated, UPPER);
            GONetLog.Debug(string.Concat("Unquantized..... original: ", UNQUANTIZED_ORIGINAL, " calculated: ", unquantized_calculated));

            for (uint i = 0; i < Mathf.Pow(2, QUANTIZED_BITS); ++i)
            {
                unquantized_calculated = Quantizer.Unquantize(LOWER, UPPER, i, QUANTIZED_BITS);
                GONetLog.Debug(string.Concat("quantized: ", i, " unquantized: ", unquantized_calculated));
            }
        }
    }
}
