using System;
using System.Runtime.CompilerServices;

namespace GONet.Utils
{
    public sealed class Quantizer
    {
        public const uint MAX_QUANTIZED_BIT_COUNT = 32;

        static readonly float[] twoToPower = new float[MAX_QUANTIZED_BIT_COUNT + 1];
        static readonly uint[] maxValueForBits = new uint[MAX_QUANTIZED_BIT_COUNT + 1];

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

            float boundRange = upperBound - lowerBound;
            m_fast_step = boundRange / (twoToPower[quantizeToBitCount] - 1f);
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
}
