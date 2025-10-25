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
    /// Default velocity-aware value blending implementation for Quaternion values.
    /// Handles synthesis of full rotation from angular velocity (omega) and velocity-aware extrapolation.
    /// Used for Transform.rotation synchronization.
    /// </summary>
    public class DefaultVelocityBlending_Quaternion : IGONetAutoMagicalSync_CustomVelocityBlending
    {
        /// <summary>
        /// Synthesizes a full Quaternion rotation from angular velocity (omega).
        /// Formula: newRotation = previousRotation * exp(omega * deltaTime / 2)
        /// where omega is Vector3 (axis * radians/sec) and exp converts to quaternion.
        /// </summary>
        public GONetSyncableValue SynthesizeValueFromVelocity(
            GONetSyncableValue previousValue,
            GONetSyncableValue velocity,
            float deltaTime,
            GONetParticipant gonetParticipant)
        {
            Quaternion prevRot = previousValue.UnityEngine_Quaternion;
            Vector3 omega = velocity.UnityEngine_Vector3; // Angular velocity stored as Vector3

            // Integrate angular velocity to get rotation delta
            Quaternion deltaRotation = IntegrateAngularVelocity(omega, deltaTime);

            // Apply delta rotation
            Quaternion synthesized = prevRot * deltaRotation;

            return synthesized;
        }

        /// <summary>
        /// Extrapolates Quaternion rotation with angular velocity context awareness.
        /// Detects sub-quantization oscillation by checking for synthetic values and preferring omega data.
        /// </summary>
        public GONetSyncableValue ExtrapolateWithVelocityContext(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            GONetParticipant gonetParticipant)
        {
            if (valueCount == 0)
                return Quaternion.identity;

            // Get most recent snapshot
            NumericValueChangeSnapshot mostRecent = valueBuffer[valueCount - 1];

            // Calculate time delta for extrapolation
            long ticksDelta = atElapsedTicks - mostRecent.elapsedTicksAtChange;
            if (ticksDelta <= 0)
                return mostRecent.numericValue;

            float deltaTimeSeconds = (float)ticksDelta * (float)GONet.Utils.HighResolutionTimeUtils.TICKS_TO_SECONDS;

            // Check if most recent value has angular velocity data (omega stored as Vector3)
            bool hasAngularVelocity = mostRecent.velocity.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector3;

            if (hasAngularVelocity)
            {
                // Use angular velocity for extrapolation (handles sub-quantization correctly)
                Quaternion currentRotation = mostRecent.numericValue.UnityEngine_Quaternion;
                Vector3 omega = mostRecent.velocity.UnityEngine_Vector3;

                // DIAGNOSTIC: Log extrapolation inputs
                GONetLog.Info($"[CLIENT-EXTRAP] currentRot:{currentRotation} euler:{currentRotation.eulerAngles} omega:{omega} dt:{deltaTimeSeconds:F4}s");

                // Integrate angular velocity
                Quaternion deltaRotation = IntegrateAngularVelocity(omega, deltaTimeSeconds);
                Quaternion extrapolated = currentRotation * deltaRotation;

                // DIAGNOSTIC: Log extrapolation result
                GONetLog.Info($"[CLIENT-EXTRAP-RESULT] extrapolated:{extrapolated} euler:{extrapolated.eulerAngles}");

                return extrapolated;
            }
            else
            {
                // Fallback: Calculate angular velocity from last two snapshots
                if (valueCount < 2)
                    return mostRecent.numericValue;

                NumericValueChangeSnapshot prev = valueBuffer[valueCount - 2];
                long prevTicksDelta = mostRecent.elapsedTicksAtChange - prev.elapsedTicksAtChange;

                if (prevTicksDelta <= 0)
                    return mostRecent.numericValue;

                float prevDeltaTime = (float)prevTicksDelta * (float)GONet.Utils.HighResolutionTimeUtils.TICKS_TO_SECONDS;

                // Calculate angular velocity from quaternion delta
                Quaternion q0 = prev.numericValue.UnityEngine_Quaternion;
                Quaternion q1 = mostRecent.numericValue.UnityEngine_Quaternion;
                Vector3 calculatedOmega = CalculateAngularVelocity(q0, q1, prevDeltaTime);

                // Extrapolate using calculated omega
                Quaternion deltaRotation = IntegrateAngularVelocity(calculatedOmega, deltaTimeSeconds);
                Quaternion extrapolated = mostRecent.numericValue.UnityEngine_Quaternion * deltaRotation;

                return extrapolated;
            }
        }

        /// <summary>
        /// Integrates angular velocity (omega) into a quaternion rotation delta.
        /// Formula: q_delta = exp(omega * deltaTime / 2)
        /// where omega is axis * angular_speed (radians/sec)
        /// </summary>
        private Quaternion IntegrateAngularVelocity(Vector3 omega, float deltaTime)
        {
            // Half-angle for quaternion exponential
            Vector3 halfOmegaDt = omega * (deltaTime * 0.5f);
            float halfAngle = halfOmegaDt.magnitude;

            // Handle near-zero rotation
            if (halfAngle < 1e-6f)
                return Quaternion.identity;

            // Convert to quaternion: q = (cos(θ/2), sin(θ/2) * axis)
            float sinHalfAngle = Mathf.Sin(halfAngle);
            float cosHalfAngle = Mathf.Cos(halfAngle);
            Vector3 axis = halfOmegaDt / halfAngle; // Normalized axis

            Quaternion deltaRotation = new Quaternion(
                axis.x * sinHalfAngle,
                axis.y * sinHalfAngle,
                axis.z * sinHalfAngle,
                cosHalfAngle
            );

            return deltaRotation;
        }

        /// <summary>
        /// Calculates angular velocity (omega) from two quaternions.
        /// Same implementation as in GONetParticipant_AutoMagicalSyncCompanion_Generated.
        /// </summary>
        private Vector3 CalculateAngularVelocity(Quaternion q0, Quaternion q1, float deltaTime)
        {
            if (deltaTime <= 0f)
                return Vector3.zero;

            // Calculate relative rotation: q_delta = q1 * q0^-1
            Quaternion q0Inverse = Quaternion.Inverse(q0);
            Quaternion qDelta = q1 * q0Inverse;

            // Ensure shortest path (quaternion double-cover)
            if (qDelta.w < 0f)
            {
                qDelta.x = -qDelta.x;
                qDelta.y = -qDelta.y;
                qDelta.z = -qDelta.z;
                qDelta.w = -qDelta.w;
            }

            // Extract axis-angle
            float angle = 2f * Mathf.Acos(Mathf.Clamp(qDelta.w, -1f, 1f));
            float sinHalfAngle = Mathf.Sin(angle * 0.5f);

            // Handle near-zero rotation
            if (Mathf.Abs(sinHalfAngle) < 1e-6f)
                return Vector3.zero;

            // Extract axis
            Vector3 axis = new Vector3(
                qDelta.x / sinHalfAngle,
                qDelta.y / sinHalfAngle,
                qDelta.z / sinHalfAngle
            );

            // Angular velocity = axis * angle / deltaTime
            Vector3 omega = axis * (angle / deltaTime);

            return omega;
        }
    }
}
