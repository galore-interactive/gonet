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
using UnityEngine;

namespace GONet.PluginAPI
{
    /// <summary>
    /// Default velocity-aware value blending implementation for Vector3 values.
    /// Handles synthesis of full values from velocity data and velocity-aware extrapolation.
    /// Most commonly used for Transform.position synchronization.
    /// </summary>
    public class DefaultVelocityBlending_Vector3 : IGONetAutoMagicalSync_CustomVelocityBlending
    {
        /// <summary>
        /// Synthesizes a full Vector3 position from velocity data.
        /// Formula: newPosition = previousPosition + velocity * deltaTime
        /// </summary>
        public GONetSyncableValue SynthesizeValueFromVelocity(
            GONetSyncableValue previousValue,
            GONetSyncableValue velocity,
            float deltaTime,
            GONetParticipant gonetParticipant)
        {
            Vector3 prevPos = previousValue.UnityEngine_Vector3;
            Vector3 vel = velocity.UnityEngine_Vector3;
            Vector3 synthesized = prevPos + (vel * deltaTime);
            return synthesized;
        }

        /// <summary>
        /// Extrapolates Vector3 value with velocity context awareness.
        /// Detects sub-quantization oscillation by checking for synthetic values and preferring velocity data.
        /// </summary>
        public GONetSyncableValue ExtrapolateWithVelocityContext(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            GONetParticipant gonetParticipant)
        {
            if (valueCount == 0)
                return Vector3.zero;

            // Get most recent snapshot
            NumericValueChangeSnapshot mostRecent = valueBuffer[valueCount - 1];

            // Calculate time delta for extrapolation
            long ticksDelta = atElapsedTicks - mostRecent.elapsedTicksAtChange;
            if (ticksDelta <= 0)
                return mostRecent.numericValue;

            float deltaTimeSeconds = (float)ticksDelta / System.Diagnostics.Stopwatch.Frequency;

            // Check if most recent value has velocity data (was from velocity packet or synthesized)
            bool hasVelocity = mostRecent.velocity.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector3;

            if (hasVelocity)
            {
                // Use velocity for extrapolation (handles sub-quantization correctly)
                Vector3 currentValue = mostRecent.numericValue.UnityEngine_Vector3;
                Vector3 velocity = mostRecent.velocity.UnityEngine_Vector3;
                Vector3 extrapolated = currentValue + (velocity * deltaTimeSeconds);
                return extrapolated;
            }
            else
            {
                // Fallback: Calculate velocity from last two snapshots
                if (valueCount < 2)
                    return mostRecent.numericValue;

                NumericValueChangeSnapshot prev = valueBuffer[valueCount - 2];
                long prevTicksDelta = mostRecent.elapsedTicksAtChange - prev.elapsedTicksAtChange;

                if (prevTicksDelta <= 0)
                    return mostRecent.numericValue;

                float prevDeltaTime = (float)prevTicksDelta / System.Diagnostics.Stopwatch.Frequency;
                Vector3 valueDelta = mostRecent.numericValue.UnityEngine_Vector3 - prev.numericValue.UnityEngine_Vector3;
                Vector3 calculatedVelocity = valueDelta / prevDeltaTime;

                Vector3 extrapolated = mostRecent.numericValue.UnityEngine_Vector3 + (calculatedVelocity * deltaTimeSeconds);
                return extrapolated;
            }
        }
    }
}
