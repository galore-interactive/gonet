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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GONet.Utils
{
    public sealed class Quantizer
    {
        public const uint MAX_QUANTIZED_BIT_COUNT = 32;

        static readonly float[] twoToPower = new float[MAX_QUANTIZED_BIT_COUNT + 1];
        static readonly uint[] maxValueForBits = new uint[MAX_QUANTIZED_BIT_COUNT + 1];

        static readonly Dictionary<QuantizerSettingsGroup, Quantizer> quantizersBySettingsMap = new Dictionary<QuantizerSettingsGroup, Quantizer>();

        static Quantizer()
        {
            int count = twoToPower.Length;
            for (int i = 0; i < count; ++i)
            {
                twoToPower[i] = (float)Math.Pow(2.0, i);

                if (i == 0)
                {
                    maxValueForBits[i] = 0;
                }
                else
                {
                    maxValueForBits[i] = (uint)((1 << i) - 1);
                }
            }
        }

        /// <summary>
        /// IMPORTANT: Can only be called from a single thread!
        /// </summary>
        internal static void EnsureQuantizerExistsForGroup(QuantizerSettingsGroup settings)
        {
            Quantizer quantizer;
            if (!quantizersBySettingsMap.TryGetValue(settings, out quantizer))
            {
                quantizer = new Quantizer(settings);
                quantizersBySettingsMap[settings] = quantizer;
            }
        }

        public static Quantizer LookupQuantizer(QuantizerSettingsGroup settings)
        {
            Quantizer quantizer;
            if (!quantizersBySettingsMap.TryGetValue(settings, out quantizer))
            {
                throw new ArgumentNullException("No Quantizer exists for settings"); // TODO log the settings
            }
            return quantizer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Quantize(float lowerBound, float upperBound, float unquantizedValue, uint quantizeToBitCount)
        {
            return Quantize(lowerBound, upperBound, unquantizedValue, quantizeToBitCount, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint QuantizeClamp(float lowerBound, float upperBound, float unquantizedValue, uint quantizeToBitCount)
        {
            return Quantize(lowerBound, upperBound, unquantizedValue, quantizeToBitCount, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Quantize(float lowerBound, float upperBound, float unquantizedValue, uint quantizeToBitCount, bool shouldClampValue)
        {
            if (upperBound <= lowerBound)
            {
                throw new ArgumentOutOfRangeException(nameof(upperBound));
            }
            if (quantizeToBitCount > MAX_QUANTIZED_BIT_COUNT || quantizeToBitCount == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantizeToBitCount));
            }
            if (unquantizedValue < lowerBound)
            {
                if (shouldClampValue)
                {
                    unquantizedValue = lowerBound;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(unquantizedValue));
                }
            }
            else if (unquantizedValue > upperBound)
            {
                if (shouldClampValue)
                {
                    unquantizedValue = upperBound;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(unquantizedValue));
                }
            }

            float boundRange = upperBound - lowerBound;
            float step = boundRange / (twoToPower[quantizeToBitCount] - 1f);
            float halfStep = step / 2f;
            float shifted = unquantizedValue - lowerBound + halfStep; // add half step for better accuracy considering we will be flooring (so, this accomplishes rounding)
            uint quantized = (uint)(shifted / step);
            return quantized;
        }

        static float s_fast_lowerBound;
        static float s_fast_upperBound;
        static uint  s_fast_quantizeToBitCount;
        static float s_fast_step;
        static float s_fast_halfStep;
        static bool s_fast_shouldClampValue;


        float m_fast_lowerBound;
        float m_fast_upperBound;
        uint m_fast_quantizeToBitCount;
        float m_fast_step;
        float m_fast_halfStep;
        bool m_fast_shouldClampValue;

        public Quantizer(QuantizerSettingsGroup settings)
            : this (settings.lowerBound, settings.upperBound, settings.quantizeToBitCount, settings.shouldClampValue) { }

        public Quantizer(float lowerBound, float upperBound, uint quantizeToBitCount, bool shouldClampValue)
        {
            if (upperBound <= lowerBound)
            {
                throw new ArgumentOutOfRangeException(nameof(upperBound));
            }
            if (quantizeToBitCount > MAX_QUANTIZED_BIT_COUNT || quantizeToBitCount == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantizeToBitCount));
            }

            m_fast_lowerBound = lowerBound;
            m_fast_upperBound = upperBound;
            m_fast_quantizeToBitCount = quantizeToBitCount;
            m_fast_shouldClampValue = shouldClampValue;

            double boundRange = upperBound - lowerBound; // IMPORTANT: have to use larger data type to hold difference between what could be float.MaxValue and float.MinValue, which in float terms is Infinity
            m_fast_step = (float)(boundRange / (double)(twoToPower[quantizeToBitCount] - 1f));
            m_fast_halfStep = m_fast_step / 2f;
        }

        public static void QuantizeFastPrep(float lowerBound, float upperBound, uint quantizeToBitCount, bool shouldClampValue)
        {
            if (upperBound <= lowerBound)
            {
                throw new ArgumentOutOfRangeException(nameof(upperBound));
            }
            if (quantizeToBitCount > MAX_QUANTIZED_BIT_COUNT || quantizeToBitCount == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantizeToBitCount));
            }

            s_fast_lowerBound = lowerBound;
            s_fast_upperBound = upperBound;
            s_fast_quantizeToBitCount = quantizeToBitCount;
            s_fast_shouldClampValue = shouldClampValue;

            float boundRange = upperBound - lowerBound;
            s_fast_step = boundRange / (twoToPower[quantizeToBitCount] - 1f);
            s_fast_halfStep = s_fast_step / 2f;
        }

        /// <summary>
        /// PRE: <see cref="QuantizeFastPrep(float, float, ref float, uint, bool, out float, out float)"/> was called and output from that is passed in here for faster execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint QuantizeFast(float unquantizedValue)
        {
            /*
            if (unquantizedValue < _fast_lowerBound)
            {
                if (_fast_shouldClampValue)
                {
                    unquantizedValue = _fast_lowerBound;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(unquantizedValue));
                }
            }
            else if (unquantizedValue > _fast_upperBound)
            {
                if (_fast_shouldClampValue)
                {
                    unquantizedValue = _fast_upperBound;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(unquantizedValue));
                }
            }
            */
            return (uint)((unquantizedValue - s_fast_lowerBound + s_fast_halfStep) / s_fast_step);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float UnquantizeFast(uint quantizedValue)
        {
            return (quantizedValue * s_fast_step) + s_fast_lowerBound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Quantize(float unquantizedValue)
        {
            /*
            if (unquantizedValue < _fast_lowerBound)
            {
                if (_fast_shouldClampValue)
                {
                    unquantizedValue = _fast_lowerBound;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(unquantizedValue));
                }
            }
            else if (unquantizedValue > _fast_upperBound)
            {
                if (_fast_shouldClampValue)
                {
                    unquantizedValue = _fast_upperBound;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(unquantizedValue));
                }
            }
            */
            return (uint)((unquantizedValue - m_fast_lowerBound + m_fast_halfStep) / m_fast_step);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Unquantize(uint quantizedValue)
        {
            return (quantizedValue * m_fast_step) + m_fast_lowerBound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Unquantize(float lowerBound, float upperBound, uint quantizedValue, uint quantizedToBitCount)
        {
            if (upperBound <= lowerBound)
            {
                throw new ArgumentOutOfRangeException(nameof(upperBound));
            }
            if (quantizedToBitCount > MAX_QUANTIZED_BIT_COUNT || quantizedToBitCount == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantizedToBitCount));
            }
            if (quantizedValue > maxValueForBits[quantizedToBitCount])
            {
                throw new ArgumentOutOfRangeException(nameof(quantizedValue));
            }

            float boundRange = upperBound - lowerBound;
            float step = boundRange / (twoToPower[quantizedToBitCount] - 1f);
            float shifted = quantizedValue * step;
            float unquantized = shifted + lowerBound;
            return unquantized;
        }
    }

    public struct QuantizerSettingsGroup : IEquatable<QuantizerSettingsGroup>
    {
        public float lowerBound;
        public float upperBound;
        public uint quantizeToBitCount;
        public bool shouldClampValue;

        public QuantizerSettingsGroup(float lowerBound, float upperBound, uint quantizeToBitCount, bool shouldClampValue)
        {
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
            this.quantizeToBitCount = quantizeToBitCount;
            this.shouldClampValue = shouldClampValue;
        }

        /// <summary>
        /// only return true if settings are such that a quantizer can even be used......<see cref="quantizeToBitCount"/> of 0 bits means no quantization will/should/can occur.
        /// </summary>
        public bool CanBeUsedForQuantization => quantizeToBitCount > 0;

        public override bool Equals(object obj)
        {
            if (!(obj is QuantizerSettingsGroup))
            {
                return false;
            }

            var group = (QuantizerSettingsGroup)obj;
            return lowerBound == group.lowerBound &&
                   upperBound == group.upperBound &&
                   quantizeToBitCount == group.quantizeToBitCount &&
                   shouldClampValue == group.shouldClampValue;
        }

        public bool Equals(QuantizerSettingsGroup other)
        {
            return lowerBound == other.lowerBound &&
                   upperBound == other.upperBound &&
                   quantizeToBitCount == other.quantizeToBitCount &&
                   shouldClampValue == other.shouldClampValue;
        }

        public override int GetHashCode()
        {
            var hashCode = -258498902;
            hashCode = hashCode * -1521134295 + lowerBound.GetHashCode();
            hashCode = hashCode * -1521134295 + upperBound.GetHashCode();
            hashCode = hashCode * -1521134295 + quantizeToBitCount.GetHashCode();
            hashCode = hashCode * -1521134295 + shouldClampValue.GetHashCode();
            return hashCode;
        }
    }
}
