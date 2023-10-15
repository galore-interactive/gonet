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
using System;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// TODO document this for people to understand.  For now, check out the member comments/summaries.
    /// </summary>
    public interface IGONetAutoMagicalSync_CustomSerializer
    {
        /* TODO Although this would break some things in an update, we should consider adding this code in to be more like IGONetAutoMagicalSync_CustomValueBlending:
            /// <summary>
            /// An instance of this class will only be able to blend values for a single GONet supported value type.  This is it.
            /// </summary>
            GONetSyncableValueTypes AppliesOnlyToGONetType { get; }

            /// <summary>
            /// Let the world know how this class is different than others and unique in its way of arriving at a smoothed "estimated" value given a history of actual values.
            /// </summary>
            string Description { get; }
         */

        void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound);

        /// <summary>
        /// Since the way this custom serializer may or may not perform quantization during <see cref="Serialize(BitByBitByteArrayBuilder, GONetParticipant, GONetSyncableValue)"/>, 
        /// this method is helpful to know if two values are considered the same *IF* quantization is part of the equation.
        /// </summary>
        bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB);

        /// <param name="gonetParticipant">here for reference in case that helps to serialize properly</param>
        void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value);

        GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom);
    }

    #region default serializer implementations

    public class Vector2Serializer : IGONetAutoMagicalSync_CustomSerializer
    {
        public const byte DEFAULT_BITS_PER_COMPONENT = 32;
        public const float DEFAULT_MAX_VALUE = 10000f;
        public const float DEFAULT_MIN_VALUE = -DEFAULT_MAX_VALUE;

        bool isQuantizationInitialized = false;
        Quantizer quantizer;
        byte bitsPerComponent = DEFAULT_BITS_PER_COMPONENT;

        public Vector2Serializer() { }

        public void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound)
        {
            if (quantizeDownToBitCount > 0)
            {
                if (isQuantizationInitialized)
                {
                    throw new InvalidOperationException("Quantization is already initialized for this custom serializer.");
                }

                bitsPerComponent = quantizeDownToBitCount;
                quantizer = new Quantizer(quantizeLowerBound, quantizeUpperBound, bitsPerComponent, true);

                isQuantizationInitialized = true;
            }
        }

        public GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom)
        {
            if(bitsPerComponent == DEFAULT_BITS_PER_COMPONENT)
            {
                float x;
                bitStream_readFrom.ReadFloat(out x);
                float y;
                bitStream_readFrom.ReadFloat(out y);

                return new Vector2(x, y);
            }
            else
            {
                uint x;
                bitStream_readFrom.ReadUInt(out x, bitsPerComponent);
                uint y;
                bitStream_readFrom.ReadUInt(out y, bitsPerComponent);

                return new Vector2(quantizer.Unquantize(x), quantizer.Unquantize(y));
            }
        }

        public void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value)
        {
            Vector2 vector2 = value.UnityEngine_Vector2;

            if(bitsPerComponent == DEFAULT_BITS_PER_COMPONENT)
            {
                bitStream_appendTo.WriteFloat(vector2.x);
                bitStream_appendTo.WriteFloat(vector2.y);
            }
            else
            {
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector2.x), bitsPerComponent);
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector2.y), bitsPerComponent);
            }
        }

        public bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB)
        {
            if(!isQuantizationInitialized)
            {
                return valueA == valueB;
            }

            Vector2 vector2A = valueA.UnityEngine_Vector2;
            Vector2 vector2B = valueB.UnityEngine_Vector2;

            return
                quantizer.Quantize(vector2A.x) == quantizer.Quantize(vector2B.x) &&
                quantizer.Quantize(vector2A.y) == quantizer.Quantize(vector2B.y);
        }
    }

    public class Vector3Serializer : IGONetAutoMagicalSync_CustomSerializer
    {
        public const byte DEFAULT_BITS_PER_COMPONENT = 32;
        public const float DEFAULT_MAX_VALUE = 10000f;
        public const float DEFAULT_MIN_VALUE = -DEFAULT_MAX_VALUE;

        bool isQuantizationInitialized = false;
        Quantizer quantizer;
        byte bitsPerComponent = DEFAULT_BITS_PER_COMPONENT;

        public Vector3Serializer() { }

        public void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound)
        {
            if (quantizeDownToBitCount > 0)
            {
                if (isQuantizationInitialized)
                {
                    throw new InvalidOperationException("Quantization is already initialized for this custom serializer.");
                }

                bitsPerComponent = quantizeDownToBitCount;
                quantizer = new Quantizer(quantizeLowerBound, quantizeUpperBound, bitsPerComponent, true);

                isQuantizationInitialized = true;
            }
        }

        public GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom)
        {
            bool areFloatsFullSized = bitsPerComponent == DEFAULT_BITS_PER_COMPONENT;
            if (areFloatsFullSized) // i.e., nothing to unquantize
            {
                float x;
                bitStream_readFrom.ReadFloat(out x);
                float y;
                bitStream_readFrom.ReadFloat(out y);
                float z;
                bitStream_readFrom.ReadFloat(out z);

                return new Vector3(x, y, z);
            }
            else
            {
                uint x;
                bitStream_readFrom.ReadUInt(out x, bitsPerComponent);
                uint y;
                bitStream_readFrom.ReadUInt(out y, bitsPerComponent);
                uint z;
                bitStream_readFrom.ReadUInt(out z, bitsPerComponent);

                return new Vector3(quantizer.Unquantize(x), quantizer.Unquantize(y), quantizer.Unquantize(z));
            }
        }

        public void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value)
        {
            Vector3 vector3 = value.UnityEngine_Vector3;

            bool areFloatsFullSized = bitsPerComponent == DEFAULT_BITS_PER_COMPONENT;
            if (areFloatsFullSized) // i.e., nothing to quantize
            {
                bitStream_appendTo.WriteFloat(vector3.x);
                bitStream_appendTo.WriteFloat(vector3.y);
                bitStream_appendTo.WriteFloat(vector3.z);
            }
            else
            {
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector3.x), bitsPerComponent);
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector3.y), bitsPerComponent);
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector3.z), bitsPerComponent);
            }
        }

        public bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB)
        {
            if(!isQuantizationInitialized)
            {
                return valueA == valueB;
            }

            Vector3 vector3A = valueA.UnityEngine_Vector3;
            Vector3 vector3B = valueB.UnityEngine_Vector3;

            return
                quantizer.Quantize(vector3A.x) == quantizer.Quantize(vector3B.x) &&
                quantizer.Quantize(vector3A.y) == quantizer.Quantize(vector3B.y) &&
                quantizer.Quantize(vector3A.z) == quantizer.Quantize(vector3B.z);
        }
    }

    public class Vector4Serializer : IGONetAutoMagicalSync_CustomSerializer
    {
        public const byte DEFAULT_BITS_PER_COMPONENT = 32;
        public const float DEFAULT_MAX_VALUE = 10000f;
        public const float DEFAULT_MIN_VALUE = -DEFAULT_MAX_VALUE;

        bool isQuantizationInitialized = false;
        Quantizer quantizer;
        byte bitsPerComponent = DEFAULT_BITS_PER_COMPONENT;

        public Vector4Serializer() { }

        public void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound)
        {
            if (quantizeDownToBitCount > 0)
            {
                if (isQuantizationInitialized)
                {
                    throw new InvalidOperationException("Quantization is already initialized for this custom serializer.");
                }

                bitsPerComponent = quantizeDownToBitCount;
                quantizer = new Quantizer(quantizeLowerBound, quantizeUpperBound, bitsPerComponent, true);

                isQuantizationInitialized = true;
            }
        }

        public GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom)
        {
            if (bitsPerComponent == DEFAULT_BITS_PER_COMPONENT)
            {
                float x;
                bitStream_readFrom.ReadFloat(out x);
                float y;
                bitStream_readFrom.ReadFloat(out y);
                float z;
                bitStream_readFrom.ReadFloat(out z);
                float w;
                bitStream_readFrom.ReadFloat(out w);

                return new Vector4(x, y, z, w);
            }
            else
            {
                uint x;
                bitStream_readFrom.ReadUInt(out x, bitsPerComponent);
                uint y;
                bitStream_readFrom.ReadUInt(out y, bitsPerComponent);
                uint z;
                bitStream_readFrom.ReadUInt(out z, bitsPerComponent);
                uint w;
                bitStream_readFrom.ReadUInt(out w, bitsPerComponent);

                return new Vector4(quantizer.Unquantize(x), quantizer.Unquantize(y), quantizer.Unquantize(z), quantizer.Unquantize(w));
            }
        }

        public void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value)
        {
            Vector4 vector4 = value.UnityEngine_Vector4;

            if(bitsPerComponent == DEFAULT_BITS_PER_COMPONENT)
            {
                bitStream_appendTo.WriteFloat(vector4.x);
                bitStream_appendTo.WriteFloat(vector4.y);
                bitStream_appendTo.WriteFloat(vector4.z);
                bitStream_appendTo.WriteFloat(vector4.w);
            }
            else
            {
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector4.x), bitsPerComponent);
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector4.y), bitsPerComponent);
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector4.z), bitsPerComponent);
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector4.w), bitsPerComponent);
            }
        }

        public bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB)
        {
            if(!isQuantizationInitialized)
            {
                return valueA == valueB;
            }

            Vector4 vector4A = valueA.UnityEngine_Vector4;
            Vector4 vector4B = valueB.UnityEngine_Vector4;

            return
                quantizer.Quantize(vector4A.x) == quantizer.Quantize(vector4B.x) &&
                quantizer.Quantize(vector4A.y) == quantizer.Quantize(vector4B.y) &&
                quantizer.Quantize(vector4A.z) == quantizer.Quantize(vector4B.z) &&
                quantizer.Quantize(vector4A.w) == quantizer.Quantize(vector4B.w);
        }
    }

    public class QuaternionSerializer : IGONetAutoMagicalSync_CustomSerializer
    {
        static readonly float SQUARE_ROOT_OF_2 = Mathf.Sqrt(2.0f);
        static readonly float QuatValueMinimum = -1.0f / SQUARE_ROOT_OF_2;
        static readonly float QuatValueMaximum = +1.0f / SQUARE_ROOT_OF_2;

        bool isQuantizationInitialized = false;
        Quantizer quantizer;
        byte bitsPerSmallestThreeItem = DEFAULT_BITS_PER_SMALLEST_THREE;

        public const byte DEFAULT_BITS_PER_SMALLEST_THREE = 9;

        public QuaternionSerializer() : this(DEFAULT_BITS_PER_SMALLEST_THREE) { }

        public QuaternionSerializer(byte bitsPerSmallestThreeItem)
        {
            this.bitsPerSmallestThreeItem = bitsPerSmallestThreeItem;
            quantizer = new Quantizer(QuatValueMinimum, QuatValueMaximum, bitsPerSmallestThreeItem, true);
        }

        public void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound)
        {
            if (quantizeDownToBitCount > 0)
            {
                if (isQuantizationInitialized)
                {
                    throw new InvalidOperationException("Quantization is already initialized for this custom serializer.");
                }

                bitsPerSmallestThreeItem = quantizeDownToBitCount;
                quantizer = new Quantizer(quantizeLowerBound, quantizeUpperBound, bitsPerSmallestThreeItem, true);

                isQuantizationInitialized = true;
            }
        }

        /// <returns>a <see cref="Quaternion"/></returns>
        public GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom)
        {
            uint LargestIndex;
            bitStream_readFrom.ReadUInt(out LargestIndex, 2);
            uint SmallestA;
            bitStream_readFrom.ReadUInt(out SmallestA, bitsPerSmallestThreeItem);
            uint SmallestB;
            bitStream_readFrom.ReadUInt(out SmallestB, bitsPerSmallestThreeItem);
            uint SmallestC;
            bitStream_readFrom.ReadUInt(out SmallestC, bitsPerSmallestThreeItem);

            float a = quantizer.Unquantize(SmallestA);
            float b = quantizer.Unquantize(SmallestB);
            float c = quantizer.Unquantize(SmallestC);

            float x, y, z, w;

            switch (LargestIndex)
            {
                case 0:
                    {
                        x = (float)Math.Sqrt(1f - a * a - b * b - c * c); // calculated the largest value based on the smallest 3 values
                        y = a;
                        z = b;
                        w = c;
                    }
                    break;

                case 1:
                    {
                        x = a;
                        y = (float)Math.Sqrt(1f - a * a - b * b - c * c); // calculated the largest value based on the smallest 3 values
                        z = b;
                        w = c;
                    }
                    break;

                case 2:
                    {
                        x = a;
                        y = b;
                        z = (float)Math.Sqrt(1f - a * a - b * b - c * c); // calculated the largest value based on the smallest 3 values
                        w = c;
                    }
                    break;

                case 3:
                    {
                        x = a;
                        y = b;
                        z = c;
                        w = (float)Math.Sqrt(1f - a * a - b * b - c * c); // calculated the largest value based on the smallest 3 values
                    }
                    break;

                default:
                    {
                        UnityEngine.Debug.Assert(false);
                        x = 0F;
                        y = 0F;
                        z = 0F;
                        w = 1F;
                    }
                    break;
            }

            // IMPORTANT: normalizing here is important since the quantization process will lose precision naturally, which will potentially
            //            cause the resultant unquantized value to be unnormalized and Unity will not like dealing with unnormalized.
            return new Quaternion(x, y, z, w).normalized;
        }

        public void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value)
        {
            uint LargestIndex;
            uint SmallestA;
            uint SmallestB;
            uint SmallestC;

            Quantize(value, out LargestIndex, out SmallestA, out SmallestB, out SmallestC);

            bitStream_appendTo.WriteUInt(LargestIndex, 2);
            bitStream_appendTo.WriteUInt(SmallestA, bitsPerSmallestThreeItem);
            bitStream_appendTo.WriteUInt(SmallestB, bitsPerSmallestThreeItem);
            bitStream_appendTo.WriteUInt(SmallestC, bitsPerSmallestThreeItem);
        }

        private void Quantize(GONetSyncableValue value, out uint largestIndex, out uint smallestA, out uint smallestB, out uint smallestC)
        {
            Quaternion quattie = value.UnityEngine_Quaternion;
            float x = quattie.x;
            float y = quattie.y;
            float z = quattie.z;
            float w = quattie.w;

            float xABS = x < 0 ? -x : x;
            float yABS = y < 0 ? -y : y;
            float zABS = z < 0 ? -z : z;
            float wABS = w < 0 ? -w : w;

            largestIndex = 0;
            float largestValue = xABS;

            if (yABS > largestValue)
            {
                largestIndex = 1;
                largestValue = yABS;
            }

            if (zABS > largestValue)
            {
                largestIndex = 2;
                largestValue = zABS;
            }

            if (wABS > largestValue)
            {
                largestIndex = 3;
                largestValue = wABS;
            }

            float a = 0f;
            float b = 0f;
            float c = 0f;

            switch (largestIndex)
            {
                case 0:
                    if (x >= 0)
                    {
                        a = y;
                        b = z;
                        c = w;
                    }
                    else
                    {
                        a = -y;
                        b = -z;
                        c = -w;
                    }
                    break;

                case 1:
                    if (y >= 0)
                    {
                        a = x;
                        b = z;
                        c = w;
                    }
                    else
                    {
                        a = -x;
                        b = -z;
                        c = -w;
                    }
                    break;

                case 2:
                    if (z >= 0)
                    {
                        a = x;
                        b = y;
                        c = w;
                    }
                    else
                    {
                        a = -x;
                        b = -y;
                        c = -w;
                    }
                    break;

                case 3:
                    if (w >= 0)
                    {
                        a = x;
                        b = y;
                        c = z;
                    }
                    else
                    {
                        a = -x;
                        b = -y;
                        c = -z;
                    }
                    break;

                default:
                    UnityEngine.Debug.Assert(false);
                    break;
            }


            smallestA = quantizer.Quantize(a);
            smallestB = quantizer.Quantize(b);
            smallestC = quantizer.Quantize(c);
        }

        public bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB)
        {
            uint LargestIndex_A, LargestIndex_B;
            uint SmallestA_A, SmallestA_B;
            uint SmallestB_A, SmallestB_B;
            uint SmallestC_A, SmallestC_B;

            Quantize(valueA, out LargestIndex_A, out SmallestA_A, out SmallestB_A, out SmallestC_A);
            Quantize(valueB, out LargestIndex_B, out SmallestA_B, out SmallestB_B, out SmallestC_B);

            return
                LargestIndex_A == LargestIndex_B &&
                SmallestA_A == SmallestA_B &&
                SmallestB_A == SmallestB_B &&
                SmallestC_A == SmallestC_B;
        }
    }

    #endregion
}
