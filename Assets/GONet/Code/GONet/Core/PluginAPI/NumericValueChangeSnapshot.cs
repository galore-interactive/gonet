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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GONet.PluginAPI
{
    /// <summary>
    /// TODO replace this with <see cref="GONetSyncableValue"/>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct NumericValueChangeSnapshot // TODO : IEquatable<T>
    {
        [FieldOffset(0)]
        internal long elapsedTicksAtChange;

        [FieldOffset(8)]
        internal GONetSyncableValue numericValue;

        /// <summary>
        /// Velocity data when this snapshot came from a velocity packet.
        /// Null (GONetSyncType = None) when snapshot came from a position packet.
        /// Used by velocity-augmented sync system for sub-quantization handling.
        /// </summary>
        [FieldOffset(28)]
        internal GONetSyncableValue velocity;

        /// <summary>
        /// True if numericValue was synthesized from velocity data (not received directly).
        /// False if numericValue came from a position packet.
        /// Used to detect sub-quantization oscillation during extrapolation.
        /// </summary>
        [FieldOffset(48)]
        internal bool wasSynthesizedFromVelocity;

        NumericValueChangeSnapshot(long elapsedTicksAtChange, GONetSyncableValue value) : this()
        {
            this.elapsedTicksAtChange = elapsedTicksAtChange;
            numericValue = value;
        }

        /// <summary>
        /// Creates snapshot from a position packet (real value).
        /// </summary>
        NumericValueChangeSnapshot(long elapsedTicksAtChange, GONetSyncableValue value, bool wasSynthesized) : this()
        {
            this.elapsedTicksAtChange = elapsedTicksAtChange;
            numericValue = value;
            wasSynthesizedFromVelocity = wasSynthesized;
            // velocity remains default (GONetSyncType = None)
        }

        /// <summary>
        /// Creates snapshot from a velocity packet (synthesized value with velocity).
        /// </summary>
        NumericValueChangeSnapshot(long elapsedTicksAtChange, GONetSyncableValue synthesizedValue, GONetSyncableValue velocityValue) : this()
        {
            this.elapsedTicksAtChange = elapsedTicksAtChange;
            numericValue = synthesizedValue;
            velocity = velocityValue;
            wasSynthesizedFromVelocity = true;
        }

        internal static NumericValueChangeSnapshot Create(long elapsedTicksAtChange, GONetSyncableValue value)
        {
            if (value.GONetSyncType == GONetSyncableValueTypes.System_Single ||
                value.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector3 ||
                value.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector2 ||
                value.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector4 ||
                value.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Quaternion) // TODO move this to some public static list for folks to reference! e.g. Allowed Blendy Value Types
            {
                return new NumericValueChangeSnapshot(elapsedTicksAtChange, value);
            }

            throw new ArgumentException("Type not supported.", nameof(value.GONetSyncType));
        }

        /// <summary>
        /// Creates snapshot from a position packet (explicitly marking synthesis state).
        /// </summary>
        internal static NumericValueChangeSnapshot CreateFromPositionPacket(long elapsedTicksAtChange, GONetSyncableValue value, bool wasSynthesized = false)
        {
            if (value.GONetSyncType == GONetSyncableValueTypes.System_Single ||
                value.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector3 ||
                value.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector2 ||
                value.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector4 ||
                value.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Quaternion)
            {
                return new NumericValueChangeSnapshot(elapsedTicksAtChange, value, wasSynthesized);
            }

            throw new ArgumentException("Type not supported.", nameof(value.GONetSyncType));
        }

        /// <summary>
        /// Creates snapshot from a velocity packet (synthesized position with velocity data).
        /// </summary>
        internal static NumericValueChangeSnapshot CreateFromVelocityPacket(long elapsedTicksAtChange, GONetSyncableValue synthesizedValue, GONetSyncableValue velocityValue)
        {
            if (synthesizedValue.GONetSyncType == GONetSyncableValueTypes.System_Single ||
                synthesizedValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector3 ||
                synthesizedValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector2 ||
                synthesizedValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector4 ||
                synthesizedValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Quaternion)
            {
                // Validate velocity type matches position type
                if (velocityValue.GONetSyncType != synthesizedValue.GONetSyncType)
                {
                    throw new ArgumentException($"Velocity type ({velocityValue.GONetSyncType}) must match position type ({synthesizedValue.GONetSyncType})");
                }

                return new NumericValueChangeSnapshot(elapsedTicksAtChange, synthesizedValue, velocityValue);
            }

            throw new ArgumentException("Type not supported.", nameof(synthesizedValue.GONetSyncType));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is NumericValueChangeSnapshot))
            {
                return false;
            }

            var snapshot = (NumericValueChangeSnapshot)obj;
            return elapsedTicksAtChange == snapshot.elapsedTicksAtChange &&
                   EqualityComparer<GONetSyncableValue>.Default.Equals(numericValue, snapshot.numericValue) &&
                   EqualityComparer<GONetSyncableValue>.Default.Equals(velocity, snapshot.velocity) &&
                   wasSynthesizedFromVelocity == snapshot.wasSynthesizedFromVelocity;
        }

        public override int GetHashCode()
        {
            var hashCode = -1529925349;
            hashCode = hashCode * -1521134295 + elapsedTicksAtChange.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<GONetSyncableValue>.Default.GetHashCode(numericValue);
            hashCode = hashCode * -1521134295 + EqualityComparer<GONetSyncableValue>.Default.GetHashCode(velocity);
            hashCode = hashCode * -1521134295 + wasSynthesizedFromVelocity.GetHashCode();
            return hashCode;
        }
    }
}
