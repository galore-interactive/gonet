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

namespace GONet.PluginAPI
{
    /// <summary>
    /// Interface for custom velocity-aware value blending and synthesis.
    /// Used when alternating value/velocity packets are enabled to:
    /// 1. Synthesize full values from received velocity data (populate value buffer)
    /// 2. Extrapolate with velocity context (handle sub-quantization oscillation)
    /// </summary>
    public interface IGONetAutoMagicalSync_CustomVelocityBlending
    {
        /// <summary>
        /// Synthesizes a full value from velocity data when velocity packet arrives.
        /// This ensures the value buffer always contains full values (never null).
        ///
        /// Called on non-authority when velocity packet received.
        /// </summary>
        /// <param name="previousValue">Most recent value in buffer (from previous position or synthetic)</param>
        /// <param name="velocity">Velocity/delta received in velocity packet</param>
        /// <param name="deltaTime">Time elapsed since previousValue (in seconds)</param>
        /// <param name="gonetParticipant">The GONetParticipant this value belongs to</param>
        /// <returns>Synthesized value to insert into value buffer</returns>
        GONetSyncableValue SynthesizeValueFromVelocity(
            GONetSyncableValue previousValue,
            GONetSyncableValue velocity,
            float deltaTime,
            GONetParticipant gonetParticipant);

        /// <summary>
        /// Extrapolates value with velocity context awareness.
        /// Handles sub-quantization oscillation by preferring velocity data over calculated deltas
        /// when consecutive values are "same" quantized value.
        ///
        /// Called during value blending extrapolation when synthetic values exist in buffer.
        /// </summary>
        /// <param name="valueBuffer">Ring buffer of recent value snapshots (positions + velocities)</param>
        /// <param name="valueCount">Number of valid entries in buffer</param>
        /// <param name="atElapsedTicks">Target time to extrapolate to</param>
        /// <param name="gonetParticipant">The GONetParticipant this value belongs to</param>
        /// <returns>Extrapolated value at target time</returns>
        GONetSyncableValue ExtrapolateWithVelocityContext(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            GONetParticipant gonetParticipant);
    }
}
