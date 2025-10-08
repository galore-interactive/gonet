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

namespace GONet
{
    /// <summary>
    /// CLIENT ONLY: Behavior when GONetId batch is exhausted during spawn.
    ///
    /// IMPORTANT: Limbo is an EDGE CASE for extreme rapid spawning (100+ spawns/sec).
    /// Most games will NEVER encounter this - batches are designed to prevent it.
    /// Only configure if you're spawning projectiles/particles at massive rates.
    ///
    /// Limbo State: GameObject exists locally but has no GONetId (waiting for batch from server).
    /// Object is NOT networked, cannot sync values, cannot receive RPCs.
    /// Will "graduate" to networked when batch arrives.
    /// </summary>
    public enum Client_GONetIdBatchLimboMode
    {
        /// <summary>
        /// Don't spawn - TryInstantiate returns false.
        /// User is responsible for handling spawn failure (show message, queue action, etc.)
        ///
        /// Use when: Full control over spawn failure handling needed.
        ///
        /// Example: Show "Out of spawn capacity" message to player.
        /// </summary>
        ReturnFailure = 0,

        /// <summary>
        /// Spawn in "limbo" - ALL MonoBehaviours disabled (except GONetParticipant).
        /// Object is completely frozen until batch arrives.
        ///
        /// Use when: Object should be invisible/inactive until fully networked.
        ///
        /// Technical: Awake() runs, but Start() and Update() do NOT run (components disabled).
        /// When batch arrives, components re-enabled and Start() fires.
        /// </summary>
        InstantiateInLimboWithAutoDisableAll = 1,

        /// <summary>
        /// Spawn in "limbo" - ONLY renderers/colliders/physics disabled.
        /// MonoBehaviours still run (Start/Update) but object is invisible/non-physical.
        ///
        /// Use when: Logic can run but visuals/physics should wait.
        /// RECOMMENDED DEFAULT: Good balance of safety and flexibility.
        ///
        /// Technical: Disables Renderer, Collider, Collider2D, makes Rigidbody/Rigidbody2D kinematic.
        /// Your scripts run normally - check Client_IsInLimbo if needed.
        /// </summary>
        InstantiateInLimboWithAutoDisableRenderingAndPhysics = 2,

        /// <summary>
        /// Spawn in "limbo" - nothing disabled.
        /// Object runs normally, user must check Client_IsInLimbo themselves.
        ///
        /// Use when: Advanced users need full control, check IsInLimbo in game scripts.
        ///
        /// Example:
        /// <code>
        /// void Update() {
        ///     if (gonetParticipant.Client_IsInLimbo) {
        ///         // Don't process gameplay logic yet
        ///         return;
        ///     }
        ///     // Normal gameplay
        /// }
        /// </code>
        /// </summary>
        InstantiateInLimbo = 3,
    }
}
