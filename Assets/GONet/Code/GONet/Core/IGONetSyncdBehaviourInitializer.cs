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

namespace GONet
{
    /// <summary>
    /// <para>
    /// Implement this interface on any <see cref="GONetParticipantCompanionBehaviour"/> that needs to send
    /// custom initialization data with the spawn message.
    /// </para>
    ///
    /// <para>
    /// <b>CRITICAL RESTRICTION:</b> This interface ONLY works when implemented on classes that extend <see cref="GONetParticipantCompanionBehaviour"/>.
    /// GONet will NOT call these methods on regular MonoBehaviours or other types.
    /// </para>
    ///
    /// <para>
    /// <b>LIFECYCLE GUARANTEE:</b>
    /// - <b>SPAWNER:</b> <see cref="Spawner_SerializeSpawnData"/> is called AFTER Awake() and BEFORE OnGONetReady()
    /// - <b>RECEIVER (runtime spawns):</b> <see cref="Receiver_DeserializeSpawnData"/> is called DURING Instantiate() (before receiver's Awake() completes)
    /// - <b>RECEIVER (scene objects):</b> <see cref="Receiver_DeserializeSpawnData"/> is called AFTER Awake() but BEFORE OnGONetReady()
    /// - <b>ALL CASES:</b> Data is ALWAYS available before OnGONetReady() fires (guaranteed by GONet lifecycle gates)
    /// </para>
    ///
    /// <para>
    /// Common use cases:
    /// - Deterministic simulation (spawn time, initial position/velocity synchronized from server time)
    /// - Server-assigned initial state (loot contents, enemy stats, random seed)
    /// - Any data that should be set once at spawn and never change thereafter
    /// </para>
    ///
    /// <para>
    /// This is separate from <see cref="GONetAutoMagicalSyncAttribute"/> - spawn data is sent ONCE at instantiation,
    /// not continuously synced. This avoids runtime overhead for data that never changes after spawn.
    /// </para>
    ///
    /// <para>
    /// <b>Performance Benefits:</b>
    /// - Zero runtime sync overhead (no continuous change detection)
    /// - No event bus subscriptions needed
    /// - Single network message (spawn + data in one packet for runtime spawns, RPC for scene objects)
    /// - User-controlled serialization format (optimize byte layout as needed)
    /// </para>
    ///
    /// <para>
    /// <b>Example - Zero-Sync Projectile:</b>
    /// <code>
    /// public class Projectile_ZeroSync : GONetParticipantCompanionBehaviour, IGONetSyncdBehaviourInitializer
    /// {
    ///     // Spawn parameters (NOT [GONetAutoMagicalSync] - spawn-only!)
    ///     public float spawnTime;
    ///     public Vector3 spawnPosition;
    ///     public Vector3 movementDirection;
    ///
    ///     public void Spawner_SerializeSpawnData(BitByBitByteArrayBuilder builder)
    ///     {
    ///         // SPAWNER: Initialize and serialize
    ///         spawnTime = (float)GONetMain.Time.ElapsedSeconds;
    ///         spawnPosition = transform.position;
    ///         movementDirection = transform.forward.normalized;
    ///
    ///         builder.WriteFloat(spawnTime);
    ///         // Vector3 = 3 floats
    ///         builder.WriteFloat(spawnPosition.x);
    ///         builder.WriteFloat(spawnPosition.y);
    ///         builder.WriteFloat(spawnPosition.z);
    ///         builder.WriteFloat(movementDirection.x);
    ///         builder.WriteFloat(movementDirection.y);
    ///         builder.WriteFloat(movementDirection.z);
    ///     }
    ///
    ///     public void Receiver_DeserializeSpawnData(BitByBitByteArrayBuilder builder)
    ///     {
    ///         // RECEIVER: Deserialize in SAME ORDER
    ///         builder.ReadFloat(out spawnTime);
    ///         float x, y, z;
    ///         builder.ReadFloat(out x);
    ///         builder.ReadFloat(out y);
    ///         builder.ReadFloat(out z);
    ///         spawnPosition = new Vector3(x, y, z);
    ///         builder.ReadFloat(out x);
    ///         builder.ReadFloat(out y);
    ///         builder.ReadFloat(out z);
    ///         movementDirection = new Vector3(x, y, z);
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </summary>
    public interface IGONetSyncdBehaviourInitializer
    {
        /// <summary>
        /// <para>
        /// Called by GONet on the <b>SPAWNER</b> (machine that initialized this object).
        /// </para>
        ///
        /// <para>
        /// <b>TIMING GUARANTEE:</b> ALWAYS called AFTER Awake() completes and BEFORE OnGONetReady() fires.
        /// </para>
        ///
        /// <para>
        /// <b>For runtime-spawned objects:</b> Called during Start() on the spawner (after Awake, before first Update)
        /// </para>
        ///
        /// <para>
        /// <b>For scene-defined objects:</b> Called on server during scene load coroutine (after Awake, before OnGONetReady)
        /// </para>
        ///
        /// <para>
        /// Initialize spawn-time values (including randomization, server time, etc.) and serialize them into the builder.
        /// The spawner's instance is initialized by setting fields in this method, then the serialized data is sent to receivers.
        /// </para>
        ///
        /// <para>
        /// This could be called on:
        /// - Client (if client spawned via <see cref="UnityEngine.Object.Instantiate"/> or <see cref="GONetMain.Client_InstantiateToBeRemotelyControlledByMe"/>)
        /// - Server (if server spawned the object, or for scene-defined objects)
        /// </para>
        ///
        /// <para>
        /// <b>IMPORTANT:</b> On non-spawner machines, this is NOT called. Use <see cref="Receiver_DeserializeSpawnData"/> instead.
        /// </para>
        ///
        /// <para>
        /// <b>Serialization Tips:</b>
        /// - Use <c>builder.WriteFloat(value)</c> for floats
        /// - For Vector3: Write 3 floats separately (x, y, z)
        /// - Order matters! Deserialize in the SAME order in <see cref="Receiver_DeserializeSpawnData"/>
        /// - Consider quantization to save bandwidth (use WriteUInt with quantized values)
        /// </para>
        /// </summary>
        /// <param name="builder">Bit-by-bit builder for efficient serialization</param>
        void Spawner_SerializeSpawnData(BitByBitByteArrayBuilder builder);

        /// <summary>
        /// <para>
        /// Called by GONet on <b>RECEIVERS</b> (all machines that receive the spawn/initialization message).
        /// </para>
        ///
        /// <para>
        /// <b>TIMING GUARANTEE:</b> ALWAYS called BEFORE OnGONetReady() fires.
        /// </para>
        ///
        /// <para>
        /// <b>For runtime-spawned objects (receivers):</b> Called DURING Instantiate_Remote(), BEFORE the receiver's Awake() completes.
        /// Data is available immediately in receiver's Awake() and Start().
        /// </para>
        ///
        /// <para>
        /// <b>For scene-defined objects (receivers):</b> Called AFTER receiver's Awake() completes but BEFORE OnGONetReady().
        /// Data is available in Update() and OnGONetReady(), but NOT in Awake() or Start() (use Update check or OnGONetReady instead).
        /// </para>
        ///
        /// <para>
        /// Deserialize spawn data in the <b>SAME ORDER</b> as <see cref="Spawner_SerializeSpawnData"/>.
        /// Set your component's fields to match the spawner's initialized values.
        /// </para>
        ///
        /// <para>
        /// This is called on all non-spawner machines. Depending on who spawned the object:
        /// - If client spawned: Server and other clients receive
        /// - If server spawned: All clients receive
        /// - For scene objects: All clients receive (server is always spawner/authority)
        /// </para>
        ///
        /// <para>
        /// <b>CRITICAL:</b> GONet lifecycle gates ensure OnGONetReady() will NOT fire until this method completes.
        /// No need for event bus subscriptions or polling - data is guaranteed ready before OnGONetReady().
        /// </para>
        ///
        /// <para>
        /// <b>Deserialization Tips:</b>
        /// - Read in EXACT SAME ORDER as you wrote in <see cref="Spawner_SerializeSpawnData"/>
        /// - Use <c>builder.ReadFloat(out float value)</c> for floats (note: out parameter!)
        /// - For Vector3: Read 3 floats separately, then construct: <c>new Vector3(x, y, z)</c>
        /// - Builder automatically tracks position internally
        /// </para>
        /// </summary>
        /// <param name="builder">Bit-by-bit builder for efficient deserialization (same type as serialization for consistency)</param>
        void Receiver_DeserializeSpawnData(BitByBitByteArrayBuilder builder);
    }
}
