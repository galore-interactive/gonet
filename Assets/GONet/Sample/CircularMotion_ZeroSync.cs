using GONet;
using UnityEngine;

/// <summary>
/// Circular motion implementation using deterministic calculation from synchronized GONet time.
///
/// ZERO position/rotation sync required - all clients calculate identical motion from GONetMain.Time.ElapsedSeconds.
///
/// Bandwidth comparison:
/// - Original CircularMotion: ~6.2 KB/sec (11 positions + 6 rotations @ 24 Hz)
/// - This implementation: ~16 bytes one-time at spawn (99.97% reduction)
///
/// How it works:
/// 1. Spawner initializes random parameters (radius, speeds, start time) and serializes via IGONetSyncdBehaviourInitializer
/// 2. Receivers deserialize parameters from spawn message (zero delay - arrives with spawn)
/// 3. All clients calculate position/rotation deterministically: pos = f(GONetTime.ElapsedSeconds)
/// 4. GONet's NTP-style time sync ensures all clients use identical time values
///
/// Key advantages of IGONetSyncdBehaviourInitializer:
/// - Zero runtime sync overhead (parameters never sync after spawn)
/// - No event bus subscriptions needed
/// - Single network message (spawn + data in one packet)
///
/// Trade-offs:
/// - Works perfectly for deterministic motion (no physics, no input)
/// - If adding physics or player control, would need event-based sync
/// </summary>
public class CircularMotion_ZeroSync : GONetParticipantCompanionBehaviour, IGONetSyncdBehaviourInitializer
{
    [Header("Movement Settings (Inspector Defaults)")]
    [Tooltip("Base radius of circular path (randomized at spawn)")]
    public float radius = 5f;

    [Tooltip("Base angular speed in degrees per second (randomized at spawn)")]
    public float angularSpeed = 30f;

    [Header("Rotation Settings (Inspector Defaults)")]
    [Tooltip("Base rotation speed around Y-axis in degrees per second (randomized at spawn)")]
    public float rotationSpeed = 90f;

    [Header("Spawn Parameters (Set via IGONetSyncdBehaviourInitializer)")]
    [Tooltip("GONet time when this object started moving")]
    public float startTimeOffset;

    [Tooltip("Actual radius used (randomized by spawner)")]
    public float syncedRadius;

    [Tooltip("Actual angular speed used (randomized by spawner)")]
    public float syncedAngularSpeed;

    [Tooltip("Actual rotation speed used (randomized by spawner)")]
    public float syncedRotationSpeed;

    private bool isInitialized = false;

    void IGONetSyncdBehaviourInitializer.Spawner_SerializeSpawnData(GONet.Utils.BitByBitByteArrayBuilder builder)
    {
        // SPAWNER: Server initializes with randomization and serializes for network transmission
        //
        // CRITICAL: Because we use Random.Range(), we MUST use IGONetSyncdBehaviourInitializer!
        // - Without this interface: Each machine would generate different random values → desync
        // - With this interface: Server generates values once → all clients receive identical data → perfect sync
        //
        // This works for BOTH:
        // - Scene-defined objects: Server serializes during scene load, sends via RPC_SyncSceneDefinedObjectIds
        // - Runtime-spawned objects: Spawner serializes, sends via InstantiateGONetParticipantEvent.CustomSpawnData
        startTimeOffset = (float)GONetMain.Time.ElapsedSeconds;
        syncedRadius = radius * Random.Range(0.25f, 1.5f);
        syncedAngularSpeed = angularSpeed * Random.Range(0.25f, 1.5f);
        syncedRotationSpeed = rotationSpeed * Random.Range(0.25f, 1.5f);
        isInitialized = true;

        // Serialize spawn data (4 floats = ~16 bytes)
        int bytesBefore = builder.Length_WrittenBytes;
        builder.WriteFloat(startTimeOffset);
        builder.WriteFloat(syncedRadius);
        builder.WriteFloat(syncedAngularSpeed);
        builder.WriteFloat(syncedRotationSpeed);
        int bytesAfter = builder.Length_WrittenBytes;

        GONetLog.Debug($"[CircularMotion_ZeroSync] Spawner serialized: radius={syncedRadius:F2}, angularSpeed={syncedAngularSpeed:F2}, rotationSpeed={syncedRotationSpeed:F2}, startTime={startTimeOffset:F3}, bytesWritten={(bytesAfter - bytesBefore)}");
    }

    void IGONetSyncdBehaviourInitializer.Receiver_DeserializeSpawnData(GONet.Utils.BitByBitByteArrayBuilder builder)
    {
        // RECEIVER: Deserialize spawn parameters
        // CRITICAL: Read in SAME ORDER as Spawner_SerializeSpawnData()
        builder.ReadFloat(out startTimeOffset);
        builder.ReadFloat(out syncedRadius);
        builder.ReadFloat(out syncedAngularSpeed);
        builder.ReadFloat(out syncedRotationSpeed);
        isInitialized = true;

        GONetLog.Debug($"[CircularMotion_ZeroSync] Receiver deserialized: radius={syncedRadius:F2}, angularSpeed={syncedAngularSpeed:F2}, rotationSpeed={syncedRotationSpeed:F2}, startTime={startTimeOffset:F3}");
    }

    void Update()
    {
        if (!isInitialized)
        {
            // This should NEVER happen with IGONetSyncdBehaviourInitializer (data arrives with spawn message)
            GONetLog.Error($"[CircularMotion_ZeroSync] Not initialized! This indicates a bug in spawn data handling.");
            return;
        }

        // CRITICAL: Use synchronized GONet time - identical on all clients/server
        // This is the magic that makes zero-sync work!
        float elapsedTime = (float)GONetMain.Time.ElapsedSeconds - startTimeOffset;

        // Calculate position deterministically from synced time
        // All clients execute identical math with identical time → identical results
        float angle = (float)(elapsedTime * syncedAngularSpeed);
        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * syncedRadius;
        float z = Mathf.Sin(angle * Mathf.Deg2Rad) * syncedRadius;
        transform.position = new Vector3(x, transform.position.y, z);

        // Calculate rotation deterministically from synced time
        // No accumulated error like deltaTime-based approaches
        float rotationAngle = (float)(elapsedTime * syncedRotationSpeed);
        transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
    }
}
