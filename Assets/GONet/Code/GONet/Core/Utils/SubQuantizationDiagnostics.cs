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

using GONet.PluginAPI;
using System;
using UnityEngine;

namespace GONet.Utils
{
    /// <summary>
    /// Diagnostic utility for detecting and logging sub-quantization value changes.
    /// Logs when the actual delta-from-baseline is smaller than the quantization step,
    /// which can cause visual jitter in value blending on non-authority clients.
    /// </summary>
    public static class SubQuantizationDiagnostics
    {
        /// <summary>
        /// Checks if the delta being serialized is sub-quantization (smaller than quantization step)
        /// and logs diagnostic information if so.
        /// </summary>
        public static void CheckAndLogIfSubQuantization<T>(
            uint gonetId,
            string memberName,
            T deltaFromBaseline,
            QuantizerSettingsGroup quantizationSettings,
            IGONetAutoMagicalSync_CustomSerializer customSerializer)
        {
            // TEMPORARY DEBUG: Always log to verify this code path is being hit
            float deltaMagnitude = CalculateMagnitude(deltaFromBaseline);
            float quantizationStep = CalculateQuantizationStep(quantizationSettings, customSerializer);

            GONetLog.Info($"[SUB-QUANT-CHECK] GONetId={gonetId}, Member={memberName}, DeltaMag={deltaMagnitude:F6}, QuantStep={quantizationStep:F6}, CanQuantize={quantizationSettings.CanBeUsedForQuantization}, IsSubQuant={deltaMagnitude > 0f && deltaMagnitude < quantizationStep}");

            // Only check if quantization is actually being used
            if (!quantizationSettings.CanBeUsedForQuantization)
                return;

            // Check if this is sub-quantization movement
            if (deltaMagnitude > 0f && deltaMagnitude < quantizationStep)
            {
                float ratio = deltaMagnitude / quantizationStep;
                string formattedDelta = FormatValue(deltaFromBaseline);

                GONetLog.Info($"[SUB-QUANTIZATION] GONetId={gonetId}, Member={memberName}, DeltaMagnitude={deltaMagnitude:F6}, QuantizationStep={quantizationStep:F6}, Ratio={ratio:F3}, Delta={formattedDelta}");
            }
        }

        private static float CalculateMagnitude<T>(T value)
        {
            if (value is float f)
            {
                return Math.Abs(f);
            }
            else if (value is Vector3 v3)
            {
                return v3.magnitude;
            }
            else if (value is Vector2 v2)
            {
                return v2.magnitude;
            }
            else if (value is Quaternion q)
            {
                // For quaternions, we can't easily calculate a "delta magnitude"
                // because they don't use baseline subtraction (serialized directly)
                // This will be handled separately in the Quaternion serializer check
                return 0f;
            }

            return 0f;
        }

        private static float CalculateQuantizationStep(QuantizerSettingsGroup settings, IGONetAutoMagicalSync_CustomSerializer customSerializer)
        {
            // Special handling for QuaternionSerializer (Smallest3 encoding)
            if (customSerializer is QuaternionSerializer quatSerializer)
            {
                // Quaternion uses Smallest3 encoding - each component quantized to [-1/sqrt(2), +1/sqrt(2)]
                // The quantization step for each component is:
                const float SMALLEST3_RANGE = 1.41421356f; // sqrt(2)
                float maxQuantizedValueSmallest3 = (float)Math.Pow(2.0, settings.quantizeToBitCount) - 1f;
                return SMALLEST3_RANGE / maxQuantizedValueSmallest3;
            }

            // Standard quantization step calculation
            if (settings.quantizeToBitCount == 0)
                return float.MaxValue;

            float boundRange = settings.upperBound - settings.lowerBound;
            float maxQuantizedValue = (float)Math.Pow(2.0, settings.quantizeToBitCount) - 1f;
            return boundRange / maxQuantizedValue;
        }

        private static string FormatValue<T>(T value)
        {
            if (value is float f)
            {
                return $"{f:F6}";
            }
            else if (value is Vector3 v3)
            {
                return $"({v3.x:F4},{v3.y:F4},{v3.z:F4})";
            }
            else if (value is Vector2 v2)
            {
                return $"({v2.x:F4},{v2.y:F4})";
            }
            else if (value is Quaternion q)
            {
                return $"({q.x:F4},{q.y:F4},{q.z:F4},{q.w:F4})";
            }

            return value?.ToString() ?? "null";
        }
    }
}
