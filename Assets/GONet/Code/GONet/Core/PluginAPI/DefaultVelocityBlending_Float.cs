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

namespace GONet.PluginAPI
{
    /// <summary>
    /// Default velocity-aware value blending implementation for float values.
    /// Handles synthesis of full values from velocity data and velocity-aware extrapolation.
    /// </summary>
    public class DefaultVelocityBlending_Float : IGONetAutoMagicalSync_CustomVelocityBlending
    {
        /// <summary>
        /// Synthesizes a full float value from velocity data.
        /// Formula: newValue = previousValue + velocity * deltaTime
        /// </summary>
        public GONetSyncableValue SynthesizeValueFromVelocity(
            GONetSyncableValue previousValue,
            GONetSyncableValue velocity,
            float deltaTime,
            GONetParticipant gonetParticipant)
        {
            float prevVal = previousValue.System_Single;
            float vel = velocity.System_Single;
            float synthesized = prevVal + (vel * deltaTime);
            return synthesized;
        }

        /// <summary>
        /// Extrapolates float value with velocity context awareness.
        /// Detects sub-quantization oscillation by checking for synthetic values and preferring velocity data.
        /// </summary>
        public GONetSyncableValue ExtrapolateWithVelocityContext(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            GONetParticipant gonetParticipant)
        {
            if (valueCount == 0)
                return 0f;

            // Get most recent snapshot
            NumericValueChangeSnapshot mostRecent = valueBuffer[valueCount - 1];

            // Calculate time delta for extrapolation
            long ticksDelta = atElapsedTicks - mostRecent.elapsedTicksAtChange;
            if (ticksDelta <= 0)
                return mostRecent.numericValue;

            float deltaTimeSeconds = (float)ticksDelta / System.Diagnostics.Stopwatch.Frequency;

            // Check if most recent value has velocity data (was from velocity packet or synthesized)
            bool hasVelocity = mostRecent.velocity.GONetSyncType == GONetSyncableValueTypes.System_Single;

            if (hasVelocity)
            {
                // Use velocity for extrapolation (handles sub-quantization correctly)
                float currentValue = mostRecent.numericValue.System_Single;
                float velocity = mostRecent.velocity.System_Single;
                float extrapolated = currentValue + (velocity * deltaTimeSeconds);
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
                float valueDelta = mostRecent.numericValue.System_Single - prev.numericValue.System_Single;
                float calculatedVelocity = valueDelta / prevDeltaTime;

                float extrapolated = mostRecent.numericValue.System_Single + (calculatedVelocity * deltaTimeSeconds);
                return extrapolated;
            }
        }
    }
}
