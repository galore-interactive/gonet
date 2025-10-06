/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 */

using UnityEngine;
using GONet;

/// <summary>
/// Controller for spawning test beacons to debug spawn/despawn propagation.
/// Press SPACE to spawn a beacon (client spawns, server takes ownership and despawns after lifetime).
/// </summary>
public class SpawnTestController : GONetBehaviour
{
    [Header("Beacon Prefab")]
    [Tooltip("Drag the SpawnTestBeacon prefab here")]
    public GONetParticipant beaconPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Minimum time between spawns (prevents accidental spam)")]
    public float spawnCooldown = 0.3f;
    private float lastSpawnTime = -999f;

    [Header("Debug Info")]
    [Tooltip("Show spawn instructions in console on start")]
    public bool showInstructions = true;

    protected override void Start()
    {
        base.Start();

        if (showInstructions)
        {
            GONetLog.Info("=== SPAWN TEST BEACON CONTROLS ===");
            GONetLog.Info("Press SPACE to spawn a test beacon");
            GONetLog.Info("Press R for RAPID-FIRE test (6 beacons instantly)");
            GONetLog.Info("Beacons will:");
            GONetLog.Info("  - Spawn at random position in view");
            GONetLog.Info("  - Show age via color: Green → Yellow → Red");
            GONetLog.Info("  - Despawn after ~3.5 seconds (server-side)");
            GONetLog.Info("  - Help debug spawn/despawn propagation issues");
            GONetLog.Info("==================================");
        }

        if (beaconPrefab == null)
        {
            GONetLog.Error("[SpawnTestController] beaconPrefab not assigned! Please assign in inspector.");
        }
    }

    void Update()
    {
        // Only clients can spawn (using Client_InstantiateToBeRemotelyControlledByMe pattern)
        if (!GONetMain.IsClient)
            return;

        if (beaconPrefab == null)
            return;

        // Check for spawn input with cooldown
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time - lastSpawnTime < spawnCooldown)
            {
                GONetLog.Debug($"[SpawnTestController] Spawn on cooldown (wait {spawnCooldown - (Time.time - lastSpawnTime):F1}s)");
                return;
            }

            SpawnBeacon();
            lastSpawnTime = Time.time;
        }

        // RAPID-FIRE TEST: Press R to spawn 6 beacons instantly (no delays)
        if (Input.GetKeyDown(KeyCode.R))
        {
            GONetLog.Info("[SpawnTestController] ========== RAPID-FIRE SPAWN TEST ==========");
            GONetLog.Info("[SpawnTestController] Spawning 6 beacons with ZERO delay...");

            for (int i = 0; i < 6; i++)
            {
                GONetLog.Info($"[SpawnTestController] Rapid spawn #{i + 1}/6...");
                SpawnBeacon();
            }

            GONetLog.Info("[SpawnTestController] ========== RAPID-FIRE COMPLETE ==========");
            GONetLog.Info("[SpawnTestController] Check server to see how many beacons appeared!");
            lastSpawnTime = Time.time; // Set cooldown after rapid-fire
        }
    }

    void SpawnBeacon()
    {
        // Determine spawn position on client BEFORE instantiating
        Vector3 spawnPosition = GetRandomFrustumPosition();

        // Use the Client_InstantiateToBeRemotelyControlledByMe pattern
        // Client spawns it with position already set, server takes ownership and will despawn it
        GONetParticipant beacon = GONetMain.Client_InstantiateToBeRemotelyControlledByMe(
            beaconPrefab,
            spawnPosition,       // Position set here so it's included in spawn message
            Quaternion.identity
        );

        if (beacon != null)
        {
            GONetLog.Info($"[SpawnTestController] Spawned beacon at {spawnPosition} - IsMine: {beacon.IsMine}, IsMine_ToRemotelyControl: {beacon.IsMine_ToRemotelyControl}, GONetId will be assigned soon");
        }
        else
        {
            GONetLog.Error("[SpawnTestController] Failed to spawn beacon - Client_InstantiateToBeRemotelyControlledByMe returned null");
        }
    }

    /// <summary>
    /// Gets a random position within the camera's view frustum, 8-12 units away.
    /// Avoids edges (20% margin) to keep beacons fully visible.
    /// </summary>
    Vector3 GetRandomFrustumPosition()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GONetLog.Warning("[SpawnTestController] No main camera found, spawning at origin");
            return Vector3.zero;
        }

        // Random distance from camera
        float distance = Random.Range(8f, 12f);

        // Random viewport position (avoid edges)
        float vpX = Random.Range(0.2f, 0.8f);
        float vpY = Random.Range(0.2f, 0.8f);

        Vector3 worldPos = cam.ViewportToWorldPoint(new Vector3(vpX, vpY, distance));

        GONetLog.Debug($"[SpawnTestController] Random spawn position: {worldPos} (distance: {distance:F1}, viewport: ({vpX:F2}, {vpY:F2}))");

        return worldPos;
    }
}
