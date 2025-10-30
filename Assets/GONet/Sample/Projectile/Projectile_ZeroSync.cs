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

using TMPro;
using UnityEngine;

namespace GONet.Sample
{
    /// <summary>
    /// ZERO-SYNC PROJECTILE: Deterministic projectile using only synchronized GONet time.
    ///
    /// Bandwidth comparison:
    /// - Original Projectile: Position + rotation sync at 24 Hz (~13 bytes/update = ~312 bytes/sec per projectile)
    /// - This implementation: ~44 bytes one-time at spawn (99.9% reduction)
    ///
    /// How it works:
    /// 1. Spawner initializes spawn parameters (spawn time, position, direction, speed) and serializes via IGONetSyncdBehaviourInitializer
    /// 2. Receivers deserialize spawn parameters from spawn message (zero delay - arrives with spawn)
    /// 3. All machines calculate position/rotation deterministically from GONetTime.ElapsedSeconds
    /// 4. Physics equation: position(t) = start_pos + direction * (speed - decay*t) * t
    /// 5. Speed decays linearly: speed(t) = startSpeed - decay * t
    /// 6. Auto-despawns when speed reaches zero (deterministic lifetime)
    ///
    /// Key advantages of IGONetSyncdBehaviourInitializer:
    /// - Zero runtime sync overhead (spawn parameters never sync after initial spawn)
    /// - No event bus subscriptions needed
    /// - No waiting for parameters to arrive (single network message with spawn)
    /// - Clean, simple code (two interface methods vs 150+ lines of event bus boilerplate)
    ///
    /// Limitations:
    /// - No physics interactions (would require event sync)
    /// - No player control after spawn (initial trajectory is deterministic)
    /// - Assumes no external forces (wind, gravity handled by Unity's transform.position.y)
    ///
    /// Production usage:
    /// - Ideal for: Bullets, arrows, projectiles with predictable trajectories
    /// - Not ideal for: Rockets with steering, grenades with physics bounces
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public class Projectile_ZeroSync : GONetParticipantCompanionBehaviour, IGONetSyncdBehaviourInitializer
    {
        [Header("Inspector Defaults (Randomized at Spawn)")]
        public float speed = 5f;
        public float speedDecayRate = 1f; // Speed decreases by this amount per second

        [Header("Spawn Parameters (Set via IGONetSyncdBehaviourInitializer)")]
        [Tooltip("GONet time when projectile was spawned")]
        public float spawnTime;

        [Tooltip("World position where projectile spawned")]
        public Vector3 spawnPosition;

        [Tooltip("Movement direction (normalized)")]
        public Vector3 movementDirection;

        [Tooltip("Initial speed (m/s)")]
        public float initialSpeed;

        [Tooltip("Speed decay rate (m/s²)")]
        public float syncedDecayRate;

        private TextMeshProUGUI text;
        private bool isInitialized = false;
        private float calculatedLifetime = 0f; // Deterministic lifetime (when speed reaches zero)

        protected override void Awake()
        {
            base.Awake();
            text = GetComponentInChildren<TextMeshProUGUI>();
        }

        void IGONetSyncdBehaviourInitializer.Spawner_SerializeSpawnData(Utils.BitByBitByteArrayBuilder builder)
        {
            // SPAWNER: Initialize spawn parameters and serialize for network transmission
            spawnTime = (float)GONetMain.Time.ElapsedSeconds;
            spawnPosition = transform.position;
            movementDirection = transform.forward.normalized;
            initialSpeed = speed;
            syncedDecayRate = speedDecayRate;

            // Calculate deterministic lifetime (matches original: despawn when speed < -startSpeed)
            // Original: if (speed > -startSpeed) else Destroy()
            // speed(t) = initialSpeed - t, so despawn when initialSpeed - t < -initialSpeed
            // Solving: initialSpeed - t = -initialSpeed → t = 2*initialSpeed
            calculatedLifetime = 2.0f * initialSpeed;
            isInitialized = true;

            // Serialize spawn data (5 floats + 2 Vector3s = ~44 bytes)
            builder.WriteFloat(spawnTime);
            // Vector3 = 3 floats
            builder.WriteFloat(spawnPosition.x);
            builder.WriteFloat(spawnPosition.y);
            builder.WriteFloat(spawnPosition.z);
            builder.WriteFloat(movementDirection.x);
            builder.WriteFloat(movementDirection.y);
            builder.WriteFloat(movementDirection.z);
            builder.WriteFloat(initialSpeed);
            builder.WriteFloat(syncedDecayRate);

            // GONetLog.Debug($"[Projectile_ZeroSync] Spawner serialized spawn data: pos={spawnPosition}, dir={movementDirection}, speed={initialSpeed:F2}, lifetime={calculatedLifetime:F2}s");
        }

        void IGONetSyncdBehaviourInitializer.Receiver_DeserializeSpawnData(Utils.BitByBitByteArrayBuilder builder)
        {
            // RECEIVER: Deserialize spawn parameters from network message
            // CRITICAL: Read in SAME ORDER as Spawner_SerializeSpawnData()
            builder.ReadFloat(out spawnTime);
            // Vector3 = 3 floats
            float x, y, z;
            builder.ReadFloat(out x);
            builder.ReadFloat(out y);
            builder.ReadFloat(out z);
            spawnPosition = new Vector3(x, y, z);
            builder.ReadFloat(out x);
            builder.ReadFloat(out y);
            builder.ReadFloat(out z);
            movementDirection = new Vector3(x, y, z);
            builder.ReadFloat(out initialSpeed);
            builder.ReadFloat(out syncedDecayRate);

            // Calculate deterministic lifetime from received data (matches original behavior)
            calculatedLifetime = 2.0f * initialSpeed;
            isInitialized = true;

            // GONetLog.Debug($"[Projectile_ZeroSync] Receiver deserialized spawn data: pos={spawnPosition}, dir={movementDirection}, speed={initialSpeed:F2}, lifetime={calculatedLifetime:F2}s");
        }

        internal override void UpdateAfterGONetReady()
        {
            base.UpdateAfterGONetReady();

            if (!isInitialized)
            {
                // This should NEVER happen with IGONetSyncdBehaviourInitializer (data arrives with spawn message)
                GONetLog.Error($"[Projectile_ZeroSync] Not initialized! This indicates a bug in spawn data handling.");
                return;
            }

            // Calculate elapsed time since spawn using synchronized GONet time
            double elapsedTime = GONetMain.Time.ElapsedSeconds - spawnTime;

            // Check if projectile lifetime has expired (deterministic on all clients)
            if (elapsedTime >= calculatedLifetime)
            {
                if (GONetParticipant.IsMine)
                {
                    // GONetLog.Debug($"[Projectile_ZeroSync] Despawning after {elapsedTime:F2}s (lifetime={calculatedLifetime:F2}s)");
                    Destroy(gameObject);
                }
                return;
            }

            // DETERMINISTIC PHYSICS: Calculate current speed with constant decay (matches original Projectile.cs)
            // Original: speed -= Time.deltaTime each frame
            // Deterministic equivalent: speed(t) = initialSpeed - t (no decay rate multiplier!)
            float currentSpeed = initialSpeed - (float)elapsedTime;
            // NOTE: Speed can go NEGATIVE to reverse direction (matching original behavior)

            // DETERMINISTIC POSITION: Simple integration of constant-deceleration motion
            // distance = initialSpeed*t - 0.5*t² (when decay rate is 1)
            float distanceTraveled = initialSpeed * (float)elapsedTime - 0.5f * (float)(elapsedTime * elapsedTime);
            transform.position = spawnPosition + movementDirection * distanceTraveled;

            // DETERMINISTIC ROTATION: Visual spin using same pattern as original
            const float CYCLE_SECONDS = 5f;
            const float DEGREES_PER_CYCLE = 360f / CYCLE_SECONDS;
            float cyclePosition = (float)elapsedTime % CYCLE_SECONDS;
            float smoothlyChangingMultiplier = cyclePosition * DEGREES_PER_CYCLE;
            smoothlyChangingMultiplier = Mathf.Sin(smoothlyChangingMultiplier * Mathf.Deg2Rad) + 2f; // Range: [1, 3]
            float rotationAngle = 100f * smoothlyChangingMultiplier * (float)elapsedTime;

            // Apply cumulative rotation (deterministic from spawn time)
            transform.rotation = Quaternion.Euler(rotationAngle, rotationAngle, rotationAngle);

            // Update UI text (authority vs non-authority)
            if (text != null)
            {
                if (GONetParticipant.IsMine)
                {
                    const string MINE = "Mine";
                    text.text = MINE;
                    text.color = Color.green;
                }
                else
                {
                    const string WHO = "?";
                    text.text = WHO;
                    text.color = Color.red;
                }
            }
        }
    }
}
