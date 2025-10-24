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

    [Header("Movement Settings")]
    public float radius = 5f; // The radius of the circular path
    public float angularSpeed = 30f; // The speed of the circular motion (degrees per second)

    [Header("Rotation Settings")]
    [Tooltip("SLOW (5°/s): Tests VELOCITY bundles - well within velocity bounds.\n" +
             "MEDIUM (25°/s): Tests threshold boundary - just above VELOCITY range.\n" +
             "FAST (180°/s): Tests VALUE bundles - typical fast physics rotation.\n" +
             "CUSTOM: Use the 'Custom Rotation Speed' field below.")]
    public RotationSpeedPreset rotationSpeedPreset = RotationSpeedPreset.Medium;

    [Tooltip("Custom rotation speed (degrees/sec). Only used when preset is set to 'Custom'.")]
    public float customRotationSpeed = 90f;

    private float angle = 0f; // Current angle in radians

    [GONetAutoMagicalSync]
    public float NettyWorkedFloat { get; set; }

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

    public override void OnGONetClientVsServerStatusKnown(bool isClient, bool isServer, ushort myAuthorityId)
    {
        base.OnGONetClientVsServerStatusKnown(isClient, isServer, myAuthorityId);

        if (GONetParticipant.IsMine)
        {
            angle = UnityEngine.Random.Range(0, Mathf.PI) * 2f;
            radius *= Random.Range(0.25f, 1.5f);
            angularSpeed *= Random.Range(0.25f, 1.5f);
            // Note: Don't randomize rotation speed - use preset for testing
        }
    }

    void Update()
    {
        if (GONetParticipant.IsMine)
        {
            // Calculate the new position in the circular path
            angle += angularSpeed * Time.deltaTime; // Update the angle based on angular speed
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            transform.position = new Vector3(x, transform.position.y, z);

            // Rotate the object around its own axis using preset speed
            float currentRotationSpeed = GetRotationSpeed();
            transform.Rotate(Vector3.up, currentRotationSpeed * Time.deltaTime);

            // just testing some network stuff
            NettyWorkedFloat += Time.deltaTime;
        }
    }
}
