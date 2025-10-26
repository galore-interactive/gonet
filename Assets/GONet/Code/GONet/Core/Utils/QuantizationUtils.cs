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

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GONet.Utils
{
    /// <summary>
    /// Utilities for calculating quantization error for different value types.
    /// Used by quantization-aware anchoring system to determine optimal times to send VALUE anchors.
    /// </summary>
    public static class QuantizationUtils
    {
        private static readonly float SQUARE_ROOT_OF_2 = Mathf.Sqrt(2.0f);
        private static readonly float QUAT_VALUE_MINIMUM = -1.0f / SQUARE_ROOT_OF_2;
        private static readonly float QUAT_VALUE_MAXIMUM = +1.0f / SQUARE_ROOT_OF_2;

        /// <summary>
        /// Calculate quantization error for Quaternion using Smallest3 encoding.
        /// Returns angular difference in degrees between actual and quantized values.
        /// </summary>
        /// <param name="actual">The actual quaternion value</param>
        /// <param name="bitsPerComponent">Number of bits per component (default: 9)</param>
        /// <returns>Angular difference in degrees (0° = perfect match, >0° = quantization error)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetQuaternionQuantizationError(Quaternion actual, byte bitsPerComponent = 9)
        {
            // Quantize the quaternion using same logic as QuaternionSerializer
            Quaternion quantized = QuantizeQuaternion(actual, bitsPerComponent);

            // Return angular difference (0° = perfect match, >0° = quantization error)
            return Quaternion.Angle(actual, quantized);
        }

        /// <summary>
        /// Quantizes a quaternion using Smallest3 encoding (same as QuaternionSerializer).
        /// Finds largest component (omit from transmission), quantizes 3 smallest components,
        /// then reconstructs the largest component from the unit length constraint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Quaternion QuantizeQuaternion(Quaternion q, byte bits)
        {
            // Find largest component (omit from transmission)
            int largestIndex = 0;
            float largestValue = Mathf.Abs(q.x);
            if (Mathf.Abs(q.y) > largestValue) { largestIndex = 1; largestValue = Mathf.Abs(q.y); }
            if (Mathf.Abs(q.z) > largestValue) { largestIndex = 2; largestValue = Mathf.Abs(q.z); }
            if (Mathf.Abs(q.w) > largestValue) { largestIndex = 3; }

            // Create quantizer for Smallest3 range
            Quantizer quantizer = new Quantizer(QUAT_VALUE_MINIMUM, QUAT_VALUE_MAXIMUM, bits, true);

            // Quantize 3 smallest components
            float[] components = new float[4] { q.x, q.y, q.z, q.w };
            for (int i = 0; i < 4; i++)
            {
                if (i == largestIndex) continue;

                // Quantize: float -> uint -> float
                uint quantized = quantizer.Quantize(components[i]);
                components[i] = quantizer.Unquantize(quantized);
            }

            // Reconstruct largest component from unit length constraint: x^2 + y^2 + z^2 + w^2 = 1
            float sumSq = 0f;
            for (int i = 0; i < 4; i++)
            {
                if (i != largestIndex) sumSq += components[i] * components[i];
            }

            // Clamp to avoid sqrt of negative due to floating point error
            float largestSq = Mathf.Max(0f, 1.0f - sumSq);
            components[largestIndex] = Mathf.Sqrt(largestSq);

            // Preserve sign of original largest component
            if (q[largestIndex] < 0) components[largestIndex] = -components[largestIndex];

            return new Quaternion(components[0], components[1], components[2], components[3]);
        }

        /// <summary>
        /// Calculate quantization error for Vector3 given quantization settings.
        /// Returns Euclidean distance between actual and quantized values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetVector3QuantizationError(
            Vector3 actual,
            float lowerBound,
            float upperBound,
            byte bitsPerComponent)
        {
            if (bitsPerComponent == 0) return 0f; // No quantization

            Quantizer quantizer = new Quantizer(lowerBound, upperBound, bitsPerComponent, true);

            // Quantize each component
            float qx = quantizer.Unquantize(quantizer.Quantize(actual.x));
            float qy = quantizer.Unquantize(quantizer.Quantize(actual.y));
            float qz = quantizer.Unquantize(quantizer.Quantize(actual.z));

            Vector3 quantized = new Vector3(qx, qy, qz);

            return Vector3.Distance(actual, quantized);
        }

        /// <summary>
        /// Calculate quantization error for Vector4 given quantization settings.
        /// Returns Euclidean distance between actual and quantized values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetVector4QuantizationError(
            Vector4 actual,
            float lowerBound,
            float upperBound,
            byte bitsPerComponent)
        {
            if (bitsPerComponent == 0) return 0f; // No quantization

            Quantizer quantizer = new Quantizer(lowerBound, upperBound, bitsPerComponent, true);

            // Quantize each component
            float qx = quantizer.Unquantize(quantizer.Quantize(actual.x));
            float qy = quantizer.Unquantize(quantizer.Quantize(actual.y));
            float qz = quantizer.Unquantize(quantizer.Quantize(actual.z));
            float qw = quantizer.Unquantize(quantizer.Quantize(actual.w));

            Vector4 quantized = new Vector4(qx, qy, qz, qw);

            return Vector4.Distance(actual, quantized);
        }

        /// <summary>
        /// Calculate quantization error for Vector2 given quantization settings.
        /// Returns Euclidean distance between actual and quantized values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetVector2QuantizationError(
            Vector2 actual,
            float lowerBound,
            float upperBound,
            byte bitsPerComponent)
        {
            if (bitsPerComponent == 0) return 0f; // No quantization

            Quantizer quantizer = new Quantizer(lowerBound, upperBound, bitsPerComponent, true);

            // Quantize each component
            float qx = quantizer.Unquantize(quantizer.Quantize(actual.x));
            float qy = quantizer.Unquantize(quantizer.Quantize(actual.y));

            Vector2 quantized = new Vector2(qx, qy);

            return Vector2.Distance(actual, quantized);
        }

        /// <summary>
        /// Calculate quantization error for float given quantization settings.
        /// Returns absolute difference between actual and quantized values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloatQuantizationError(
            float actual,
            float lowerBound,
            float upperBound,
            byte bits)
        {
            if (bits == 0) return 0f; // No quantization

            Quantizer quantizer = new Quantizer(lowerBound, upperBound, bits, true);

            uint quantized = quantizer.Quantize(actual);
            float dequantized = quantizer.Unquantize(quantized);

            return Mathf.Abs(actual - dequantized);
        }

        /// <summary>
        /// QUANTIZATION-AWARE ANCHORING: Check if Vector3 is near quantization boundaries.
        /// ALL components must be within threshold for clean VALUE anchor.
        /// Returns per-component errors and boundary check result.
        /// </summary>
        public static bool IsVector3NearQuantizationBoundary(
            Vector3 actual,
            float lowerBound,
            float upperBound,
            byte bitsPerComponent,
            float thresholdFraction,  // e.g., 0.30 for 30%
            out float errorX,
            out float errorY,
            out float errorZ,
            out float threshold)
        {
            errorX = errorY = errorZ = threshold = 0f;

            if (bitsPerComponent == 0) return false; // No quantization

            Quantizer quantizer = new Quantizer(lowerBound, upperBound, bitsPerComponent, true);

            // Quantize each component
            float qx = quantizer.Unquantize(quantizer.Quantize(actual.x));
            float qy = quantizer.Unquantize(quantizer.Quantize(actual.y));
            float qz = quantizer.Unquantize(quantizer.Quantize(actual.z));

            // Calculate per-component errors
            errorX = Mathf.Abs(actual.x - qx);
            errorY = Mathf.Abs(actual.y - qy);
            errorZ = Mathf.Abs(actual.z - qz);

            // Calculate threshold (30% of quantization step)
            float range = upperBound - lowerBound;
            float quantizationStep = range / (float)((1 << bitsPerComponent) - 1);
            threshold = quantizationStep * thresholdFraction;

            // ALL components must be within threshold
            return errorX < threshold &&
                   errorY < threshold &&
                   errorZ < threshold;
        }

        /// <summary>
        /// QUANTIZATION-AWARE ANCHORING: Check if Vector2 is near quantization boundaries.
        /// ALL components must be within threshold for clean VALUE anchor.
        /// </summary>
        public static bool IsVector2NearQuantizationBoundary(
            Vector2 actual,
            float lowerBound,
            float upperBound,
            byte bitsPerComponent,
            float thresholdFraction,
            out float errorX,
            out float errorY,
            out float threshold)
        {
            errorX = errorY = threshold = 0f;

            if (bitsPerComponent == 0) return false;

            Quantizer quantizer = new Quantizer(lowerBound, upperBound, bitsPerComponent, true);

            float qx = quantizer.Unquantize(quantizer.Quantize(actual.x));
            float qy = quantizer.Unquantize(quantizer.Quantize(actual.y));

            errorX = Mathf.Abs(actual.x - qx);
            errorY = Mathf.Abs(actual.y - qy);

            float range = upperBound - lowerBound;
            float quantizationStep = range / (float)((1 << bitsPerComponent) - 1);
            threshold = quantizationStep * thresholdFraction;

            return errorX < threshold && errorY < threshold;
        }

        /// <summary>
        /// QUANTIZATION-AWARE ANCHORING: Check if Vector4 is near quantization boundaries.
        /// ALL components must be within threshold for clean VALUE anchor.
        /// </summary>
        public static bool IsVector4NearQuantizationBoundary(
            Vector4 actual,
            float lowerBound,
            float upperBound,
            byte bitsPerComponent,
            float thresholdFraction,
            out float errorX,
            out float errorY,
            out float errorZ,
            out float errorW,
            out float threshold)
        {
            errorX = errorY = errorZ = errorW = threshold = 0f;

            if (bitsPerComponent == 0) return false;

            Quantizer quantizer = new Quantizer(lowerBound, upperBound, bitsPerComponent, true);

            float qx = quantizer.Unquantize(quantizer.Quantize(actual.x));
            float qy = quantizer.Unquantize(quantizer.Quantize(actual.y));
            float qz = quantizer.Unquantize(quantizer.Quantize(actual.z));
            float qw = quantizer.Unquantize(quantizer.Quantize(actual.w));

            errorX = Mathf.Abs(actual.x - qx);
            errorY = Mathf.Abs(actual.y - qy);
            errorZ = Mathf.Abs(actual.z - qz);
            errorW = Mathf.Abs(actual.w - qw);

            float range = upperBound - lowerBound;
            float quantizationStep = range / (float)((1 << bitsPerComponent) - 1);
            threshold = quantizationStep * thresholdFraction;

            return errorX < threshold &&
                   errorY < threshold &&
                   errorZ < threshold &&
                   errorW < threshold;
        }

        /// <summary>
        /// QUANTIZATION-AWARE ANCHORING: Check if float is near quantization boundary.
        /// </summary>
        public static bool IsFloatNearQuantizationBoundary(
            float actual,
            float lowerBound,
            float upperBound,
            byte bits,
            float thresholdFraction,
            out float error,
            out float threshold)
        {
            error = threshold = 0f;

            if (bits == 0) return false;

            Quantizer quantizer = new Quantizer(lowerBound, upperBound, bits, true);

            uint quantized = quantizer.Quantize(actual);
            float dequantized = quantizer.Unquantize(quantized);

            error = Mathf.Abs(actual - dequantized);

            float range = upperBound - lowerBound;
            float quantizationStep = range / (float)((1 << bits) - 1);
            threshold = quantizationStep * thresholdFraction;

            return error < threshold;
        }
    }
}
