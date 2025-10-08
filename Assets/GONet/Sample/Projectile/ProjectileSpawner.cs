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

using GONet;
using GONet.Sample;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ProjectileSpawner : GONetBehaviour
{
    public GONetParticipant projectilPrefab;
    private readonly List<Projectile> projectiles = new(100);
    private readonly List<GONetParticipant> addressableParticipants = new(100); // For Physics Cube Projectiles (no Projectile component)
    private float lastCheckTime = 0f;
    const float CHECK_INTERVAL = 1f;

    // Track last known positions to detect idle projectiles
    private readonly Dictionary<uint, Vector3> lastKnownPositions = new Dictionary<uint, Vector3>();
    private readonly Dictionary<uint, float> lastMovementTime = new Dictionary<uint, float>();

    public override void OnGONetReady(GONetParticipant gonetParticipant) // NOTE:  OnGONetReady is the recommended approach for v1.5+ (instead of OnGONetParticipantEnabled/Started/Etc..
    {
        base.OnGONetReady(gonetParticipant);

        if (gonetParticipant.GetComponent<Projectile>() != null)
        {
            Projectile projectile = gonetParticipant.GetComponent<Projectile>();
            projectiles.Add(projectile);

            /* This was replaced in v1.1.1 with use of GONetMain.Client_InstantiateToBeRemotelyControlledByMe():
            if (GONetMain.IsServer && !projectile.GONetParticipant.IsMine)
            {
                GONetMain.Server_AssumeAuthorityOver(projectile.GONetParticipant);
            }
            */
        }

        // Check for Physics Cube addressable projectiles (INDEPENDENT of Projectile component check)
        // Note: Physics Cube Projectiles don't have a Projectile component, so we track the GONetParticipant directly
        if (GONetMain.IsServer && gonetParticipant.IsMine && gonetParticipant.gameObject.name.StartsWith("Physics Cube Projectile"))
        {
            // Store the GONetParticipant in a separate tracking list since Physics Cube doesn't have Projectile component
            addressableParticipants.Add(gonetParticipant);
        }
    }

    public override void OnGONetParticipantDisabled(GONetParticipant gonetParticipant)
    {
        base.OnGONetParticipantDisabled(gonetParticipant);

        // Remove from projectiles list if it has a Projectile component
        Projectile projectile = gonetParticipant.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectiles.Remove(projectile);
        }

        // Also remove from addressableParticipants list (for Physics Cube Projectiles)
        addressableParticipants.Remove(gonetParticipant);

        // Clean up tracking dictionaries
        uint gonetId = gonetParticipant.GONetId;
        lastKnownPositions.Remove(gonetId);
        lastMovementTime.Remove(gonetId);
    }

    private void Update()
    {
        if (GONetMain.IsClient && projectilPrefab != null)
        {
            #region check keys and touches states
            bool shouldInstantiateBasedOnInput = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.B);
            if (!shouldInstantiateBasedOnInput)
            {
                shouldInstantiateBasedOnInput = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
                if (!shouldInstantiateBasedOnInput)
                {
                    foreach (Touch touch in Input.touches)
                    {
                        if (touch.phase == TouchPhase.Began)
                        {
                            shouldInstantiateBasedOnInput = true;
                            break;
                        }
                    }
                }
            }
            #endregion
            if (shouldInstantiateBasedOnInput)
            {
                // Spawn 9 projectiles in a spread pattern (160 degree arc)
                const int PROJECTILE_COUNT = 9;
                const float SPREAD_ANGLE = 160f; // Total spread in degrees
                const float ANGLE_INCREMENT = SPREAD_ANGLE / (PROJECTILE_COUNT - 1); // Evenly distributed
                const float START_ANGLE = -SPREAD_ANGLE / 2f; // Start at -80 degrees

                for (int i = 0; i < PROJECTILE_COUNT; i++)
                {
                    // Calculate angle for this projectile (relative to transform.forward)
                    float angleOffset = START_ANGLE + (i * ANGLE_INCREMENT);

                    // Create rotation: rotate around the Z-axis (up/down spread relative to camera)
                    // Since projectiles move in Vector3.forward (World space), we rotate the spawn rotation
                    Quaternion spreadRotation = transform.rotation * Quaternion.Euler(0f, 0f, angleOffset);

                    GONetParticipant gnp =
                        GONetMain.Client_InstantiateToBeRemotelyControlledByMe(projectilPrefab, transform.position, spreadRotation);
                    //GONetLog.Debug($"Spawned spread projectile #{i} at angle {angleOffset:F1}° - Is Mine? {gnp.IsMine} Is Mine To Remotely Control? {gnp.IsMine_ToRemotelyControl}");

                    // Spawn addressable for each projectile as well
                    InstantiateAddressablesPrefab(spreadRotation);
                }
            }
        }

        foreach (var projectile in projectiles)
        {
            // CRITICAL CHECK: Verify projectile is fully initialized before moving
            if (projectile == null || projectile.GONetParticipant == null)
            {
                continue;
            }

            uint gonetId = projectile.GONetParticipant.GONetId;

            if (projectile.GONetParticipant.IsMine)
            {
                // option to use gonet time delta instead: projectile.transform.Translate(transform.forward * GONetMain.Time.DeltaTime * projectile.speed);
                projectile.transform.Translate(Vector3.forward * Time.deltaTime * projectile.speed, Space.World);
                const float CYCLE_SECONDS = 5f;
                const float DECGREES_PER_CYCLE = 360f / CYCLE_SECONDS;
                var smoothlyChangingMultiplyFactor = Time.time % CYCLE_SECONDS;
                smoothlyChangingMultiplyFactor *= DECGREES_PER_CYCLE;
                smoothlyChangingMultiplyFactor = Mathf.Sin(smoothlyChangingMultiplyFactor * Mathf.Deg2Rad) + 2; // should be between 1 and 3 after this
                float rotationAngle = Time.deltaTime * 100 * smoothlyChangingMultiplyFactor;
                projectile.transform.Rotate(rotationAngle, rotationAngle, rotationAngle);
            }
            else
            {
                // Track movement to detect idle projectiles (sync stopped)
                Vector3 currentPos = projectile.transform.position;

                if (lastKnownPositions.TryGetValue(gonetId, out Vector3 lastPos))
                {
                    float distanceMoved = Vector3.Distance(currentPos, lastPos);
                    const float IDLE_THRESHOLD = 0.01f; // Consider idle if moved less than this

                    if (distanceMoved > IDLE_THRESHOLD)
                    {
                        // Moving - update last movement time
                        lastMovementTime[gonetId] = Time.time;
                        lastKnownPositions[gonetId] = currentPos;
                    }
                    else
                    {
                        // Not moving - check if idle for too long
                        if (lastMovementTime.TryGetValue(gonetId, out float lastMove))
                        {
                            float idleTime = Time.time - lastMove;
                            const float IDLE_WARNING_THRESHOLD = 3f; // Warn if idle for 3+ seconds

                            if (idleTime >= IDLE_WARNING_THRESHOLD && Time.frameCount % 180 == 0) // Log every 3 seconds
                            {
                                GONetLog.Warning($"[ProjectileSpawner] IDLE projectile '{projectile.name}' (GONetId: {gonetId}, Owner: {projectile.GONetParticipant.OwnerAuthorityId}) - IDLE for {idleTime:F1}s at Pos: {currentPos}");
                            }
                        }
                    }
                }
                else
                {
                    // First time seeing this projectile - initialize tracking
                    lastKnownPositions[gonetId] = currentPos;
                    lastMovementTime[gonetId] = Time.time;
                }
            }
        }

        if (GONetMain.IsServer && Time.time - lastCheckTime >= CHECK_INTERVAL)
        {
            lastCheckTime = Time.time;
            DestroyAddressableProjectilesOutOfView();
        }
    }
    private async Task InstantiateAddressablesPrefab(Quaternion rotation)
    {
        const string oohLaLa_addressablesPrefabPath = "Assets/GONet/Sample/Projectile/AddressablesOohLaLa/Physics Cube Projectile.prefab";
        GONetParticipant addressablePrefab = await GONetAddressablesHelper.LoadGONetPrefabAsync(oohLaLa_addressablesPrefabPath);
        // LoadGONetPrefabAsync guarantees we're back on Unity main thread after await
        // Safe to call Unity APIs now
        GONetParticipant addressableInstance =
            GONetMain.Client_InstantiateToBeRemotelyControlledByMe(addressablePrefab, transform.position, rotation);
    }

    /// <summary>
    /// Server-side cleanup: Destroys addressable projectiles that have traveled too far from spawn origin.
    /// Uses distance-based check instead of frustum culling because:
    /// 1. Server typically has no camera (headless)
    /// 2. We don't know which client camera to use
    /// 3. Distance check is simpler and more predictable
    /// </summary>
    private void DestroyAddressableProjectilesOutOfView()
    {
        const float MAX_DISTANCE = 50f; // Destroy projectiles beyond this distance from spawn origin
        Vector3 spawnOrigin = transform.position; // Spawner's position

        int destroyedCount = 0;
        for (int i = addressableParticipants.Count - 1; i >= 0; --i)
        {
            GONetParticipant participant = addressableParticipants[i];
            if (participant == null || participant.gameObject == null)
            {
                // Already destroyed, remove from list
                addressableParticipants.RemoveAt(i);
                continue;
            }

            float distanceFromSpawn = Vector3.Distance(participant.transform.position, spawnOrigin);

            if (distanceFromSpawn > MAX_DISTANCE)
            {
                Destroy(participant.gameObject);
                destroyedCount++;
            }
        }
    }
}
