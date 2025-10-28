using GONet;
using UnityEngine;

public class CircularMotion : GONetParticipantCompanionBehaviour
{
    /// <summary>
    /// Rotation speed presets for testing quaternion velocity quantization:
    /// - SLOW: 5°/s (well within bounds → sends VELOCITY bundles, tests velocity-augmented sync)
    /// - MEDIUM: 25°/s (above ±19°/s threshold → sends VALUE bundles, slight quantization visible)
    /// - FAST: 180°/s (way above threshold → sends VALUE bundles, quantization acceptable due to motion blur)
    /// - CUSTOM: Use the 'Custom Rotation Speed' field below
    /// </summary>
    public enum RotationSpeedPreset
    {
        Slow,   // 5°/s - Tests VELOCITY bundle path (angular velocity synthesis)
        Medium, // 25°/s - Tests threshold boundary (just above VELOCITY range)
        Fast,   // 180°/s - Tests VALUE bundle path (typical physics simulation)
        Custom  // User-defined speed
    }

    /// <summary>
    /// Movement speed presets for testing position velocity quantization:
    /// - SLOW: 0.02 units/sec linear velocity (sub-quantization per sync @ 24 Hz → VELOCITY bundles, tests quantization-aware anchoring)
    /// - MEDIUM: 5 units/sec linear velocity (comfortable VELOCITY range, tests typical movement)
    /// - FAST: 15 units/sec linear velocity (near ±20 units/sec velocity bounds, more VALUE anchors expected)
    /// - CUSTOM: Use the 'Custom Angular Speed' field below
    ///
    /// Angular speed calculated dynamically based on radius to achieve target linear velocities.
    /// Position quantization step: 0.000954 units (0.95mm) from _GONet_Transform_Position profile.
    /// Sync rate: 24 Hz (0.04167s intervals) - SLOW ensures movement per sync &lt; quantization step.
    /// </summary>
    public enum MovementSpeedPreset
    {
        Slow,   // 0.02 units/sec linear - Sub-quantization per sync interval @ 24 Hz
        Medium, // 5 units/sec linear - Comfortable VELOCITY range
        Fast,   // 15 units/sec linear - Near velocity bounds (±20 units/sec)
        Custom  // User-defined angular speed
    }

    [Header("Movement Settings")]
    public float radius = 5f; // The radius of the circular path

    [Tooltip("SLOW (0.02 u/s): Sub-quantization per sync interval @ 24 Hz - tests quantization-aware anchoring.\n" +
             "MEDIUM (5 u/s): Comfortable VELOCITY range - typical movement testing.\n" +
             "FAST (15 u/s): Near velocity bounds (±20 u/s) - more VALUE anchors.\n" +
             "CUSTOM: Use the 'Custom Angular Speed' field below.\n\n" +
             "Angular speed calculated based on radius to achieve target linear velocities.")]
    public MovementSpeedPreset movementSpeedPreset = MovementSpeedPreset.Medium;

    [Tooltip("Custom angular speed (degrees/sec) for circular motion. Only used when preset is set to 'Custom'.")]
    public float customAngularSpeed = 30f;

    [Header("Rotation Settings")]
    [Tooltip("SLOW (5°/s): Tests VELOCITY bundles - well within velocity bounds.\n" +
             "MEDIUM (25°/s): Tests threshold boundary - just above VELOCITY range.\n" +
             "FAST (180°/s): Tests VALUE bundles - typical fast physics rotation.\n" +
             "CUSTOM: Use the 'Custom Rotation Speed' field below.")]
    public RotationSpeedPreset rotationSpeedPreset = RotationSpeedPreset.Medium;

    [Tooltip("Custom rotation speed (degrees/sec). Only used when preset is set to 'Custom'.")]
    public float customRotationSpeed = 90f;

    private float angle = 0f; // Current angle in radians

    /// <summary>
    /// Gets the actual rotation speed based on the selected preset.
    /// </summary>
    private float GetRotationSpeed()
    {
        switch (rotationSpeedPreset)
        {
            case RotationSpeedPreset.Slow:
                return 5f;      // 5°/s - Well within VELOCITY bounds, tests velocity-augmented sync
            case RotationSpeedPreset.Medium:
                return 25f;     // 25°/s - Just above ±19°/s threshold
            case RotationSpeedPreset.Fast:
                return 180f;    // 180°/s - Typical fast physics rotation
            case RotationSpeedPreset.Custom:
                return customRotationSpeed;
            default:
                return 25f;     // Default to Medium
        }
    }

    /// <summary>
    /// Gets the actual movement angular speed (degrees/sec) based on the selected preset.
    /// Converts desired linear velocity to angular speed based on current radius.
    /// Formula: angular_speed_deg = (linear_velocity / radius) * Rad2Deg
    /// </summary>
    private float GetMovementAngularSpeed()
    {
        float targetLinearVelocity;

        switch (movementSpeedPreset)
        {
            case MovementSpeedPreset.Slow:
                targetLinearVelocity = 0.02f;   // 0.02 units/sec - Sub-quantization per sync @ 24 Hz
                break;
            case MovementSpeedPreset.Medium:
                targetLinearVelocity = 5f;      // 5 units/sec - Comfortable VELOCITY range
                break;
            case MovementSpeedPreset.Fast:
                targetLinearVelocity = 15f;     // 15 units/sec - Near ±20 units/sec velocity bounds
                break;
            case MovementSpeedPreset.Custom:
                return customAngularSpeed;      // Use custom angular speed directly
            default:
                targetLinearVelocity = 5f;      // Default to Medium
                break;
        }

        // Convert linear velocity to angular speed (degrees/sec)
        // linear_velocity = angular_velocity * radius (where angular_velocity is in radians/sec)
        // angular_velocity_rad = linear_velocity / radius
        // angular_velocity_deg = angular_velocity_rad * Rad2Deg
        if (radius > 0.001f) // Avoid division by zero
        {
            return (targetLinearVelocity / radius) * Mathf.Rad2Deg;
        }

        return 30f; // Fallback if radius is too small
    }

    public override void OnGONetClientVsServerStatusKnown(bool isClient, bool isServer, ushort myAuthorityId)
    {
        base.OnGONetClientVsServerStatusKnown(isClient, isServer, myAuthorityId);

        if (GONetParticipant.IsMine)
        {
            angle = UnityEngine.Random.Range(0, Mathf.PI) * 2f;
            radius *= Random.Range(0.25f, 1.5f);
            // Note: Don't randomize speeds - use presets for controlled quantization testing
        }
    }

    void Update()
    {
        if (GONetParticipant.IsMine)
        {
            // Calculate the new position in the circular path using preset-based movement speed
            float currentMovementAngularSpeed = GetMovementAngularSpeed();
            angle += currentMovementAngularSpeed * Time.deltaTime;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            transform.position = new Vector3(x, transform.position.y, z);

            // Rotate the object around its own axis using preset rotation speed
            float currentRotationSpeed = GetRotationSpeed();
            transform.Rotate(Vector3.up, currentRotationSpeed * Time.deltaTime);
        }

        GONetLog.Debug($"GNP name: {name}, gonetid: {GONetParticipant.GONetId} (authorityId: {gonetParticipant.OwnerAuthorityId}), mine? {IsMine}");
    }
}
