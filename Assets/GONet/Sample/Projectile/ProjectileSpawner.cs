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
    private readonly List<Projectile> projectiles = new List<Projectile>(100);
    private readonly List<Projectile> addressableProjectiles = new List<Projectile>(100); // Legacy - not used for Physics Cube
    private readonly List<GONetParticipant> addressableParticipants = new List<GONetParticipant>(100); // For Physics Cube Projectiles (no Projectile component)
    private float lastCheckTime = 0f;
    const float CHECK_INTERVAL = 1f;

    protected override void Awake()
    {
        base.Awake();
        GONetLog.Debug($"[ProjectileSpawner] Awake() called - registering with GONetBehaviours system. GameObject: '{gameObject.name}', Scene: '{gameObject.scene.name}'");
    }

    protected override void OnDestroy()
    {
        GONetLog.Debug($"[ProjectileSpawner] OnDestroy() called - unregistering from GONetBehaviours system. GameObject: '{gameObject.name}', projectiles.Count: {projectiles.Count}");
        base.OnDestroy();
    }

    public override void OnGONetReady(GONetParticipant gonetParticipant) // NOTE:  OnGONetReady is the recommended approach for v1.5+ (instead of OnGONetParticipantEnabled/Started/Etc..
    {
        base.OnGONetReady(gonetParticipant);

        // Debug: Log ALL OnGONetReady calls to see what's being registered
        GONetLog.Debug($"[ProjectileSpawner] OnGONetReady called for '{gonetParticipant.name}' (GONetId: {gonetParticipant.GONetId}, IsMine: {gonetParticipant.IsMine}, IsServer: {GONetMain.IsServer})");

        if (gonetParticipant.GetComponent<Projectile>() != null)
        {
            Projectile projectile = gonetParticipant.GetComponent<Projectile>();
            GONetLog.Info($"[ProjectileSpawner] OnGONetReady called for projectile '{gonetParticipant.name}' (GONetId: {gonetParticipant.GONetId}, IsMine: {gonetParticipant.IsMine}, OwnerAuthorityId: {gonetParticipant.OwnerAuthorityId}, MyAuthorityId: {GONetMain.MyAuthorityId}) - adding to projectiles list (count will be: {projectiles.Count + 1})");
            projectiles.Add(projectile);

            /* This was replaced in v1.1.1 with use of GONetMain.Client_InstantiateToBeRemotelyControlledByMe():
            if (GONetMain.IsServer && !projectile.GONetParticipant.IsMine)
            {
                GONetMain.Server_AssumeAuthorityOver(projectile.GONetParticipant);
            }
            */
        }
        else
        {
            // Debug: Log when Projectile component is missing
            GONetLog.Debug($"[ProjectileSpawner] OnGONetReady: '{gonetParticipant.name}' has no Projectile component - skipping");
        }

        // Check for Physics Cube addressable projectiles (INDEPENDENT of Projectile component check)
        // Note: Physics Cube Projectiles don't have a Projectile component, so we track the GONetParticipant directly
        if (GONetMain.IsServer && gonetParticipant.IsMine && gonetParticipant.gameObject.name.StartsWith("Physics Cube Projectile"))
        {
            // Store the GONetParticipant in a separate tracking list since Physics Cube doesn't have Projectile component
            addressableParticipants.Add(gonetParticipant);
            GONetLog.Debug($"[ProjectileSpawner] Added addressable Physics Cube to cleanup list: '{gonetParticipant.name}' (GONetId: {gonetParticipant.GONetId}) - List count now: {addressableParticipants.Count}");
        }
    }

    public override void OnGONetParticipantDisabled(GONetParticipant gonetParticipant)
    {
        base.OnGONetParticipantDisabled(gonetParticipant);
        Projectile projectile = gonetParticipant.GetComponent<Projectile>();
        if (projectile != null)
        {
            bool removed = projectiles.Remove(projectile);
            addressableProjectiles.Remove(projectile);
            //GONetLog.Debug($"[ProjectileSpawner] OnGONetParticipantDisabled: Removed projectile '{gonetParticipant.name}' (GONetId: {gonetParticipant.GONetId}, IsMine: {gonetParticipant.IsMine}) from list. Was in list: {removed}, new count: {projectiles.Count}");
        }

        // Also remove from addressableParticipants list (for Physics Cube Projectiles)
        addressableParticipants.Remove(gonetParticipant);
    }
    private void Update()
    {
        // Log projectiles list size periodically for debugging
        if (Time.frameCount % 300 == 0 && projectiles.Count > 0)
        {
            //GONetLog.Debug($"[ProjectileSpawner] Update: projectiles.Count = {projectiles.Count}, addressableProjectiles.Count = {addressableProjectiles.Count}");

            // Check for duplicate GONetIds in the list (this would cause faster movement!)
            var seenIds = new HashSet<uint>();
            var duplicates = new List<uint>();
            foreach (var p in projectiles)
            {
                if (p != null && p.GONetParticipant != null)
                {
                    uint id = p.GONetParticipant.GONetId;
                    if (seenIds.Contains(id))
                    {
                        duplicates.Add(id);
                    }
                    else
                    {
                        seenIds.Add(id);
                    }
                }
            }
            if (duplicates.Count > 0)
            {
                GONetLog.Warning($"[ProjectileSpawner] DUPLICATE GONETIDS IN LIST: {string.Join(", ", duplicates)} - This would cause faster movement!");
            }
        }

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
                GONetParticipant gnp =
                    GONetMain.Client_InstantiateToBeRemotelyControlledByMe(projectilPrefab, transform.position, transform.rotation);
                //GONetLog.Debug($"Spawned projectile for this client to remotely control, but server will own it. Is Mine? {gnp.IsMine} Is Mine To Remotely Control? {gnp.IsMine_ToRemotelyControl}");
                InstantiateAddressablesPrefab();
            }
        }
        foreach (var projectile in projectiles)
        {
            // CRITICAL CHECK: Verify projectile is fully initialized before moving
            if (projectile == null || projectile.GONetParticipant == null)
            {
                if (Time.frameCount % 300 == 0)
                {
                    GONetLog.Warning($"[ProjectileSpawner] Update: Found null projectile or GONetParticipant in list! Skipping...");
                }
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
        }

        if (GONetMain.IsServer && Time.time - lastCheckTime >= CHECK_INTERVAL)
        {
            lastCheckTime = Time.time;
            DestroyAddressableProjectilesOutOfView();
        }
    }
    private async Task InstantiateAddressablesPrefab()
    {
        const string oohLaLa_addressablesPrefabPath = "Assets/GONet/Sample/Projectile/AddressablesOohLaLa/Physics Cube Projectile.prefab";
        GONetParticipant addressablePrefab = await GONetAddressablesHelper.LoadGONetPrefabAsync(oohLaLa_addressablesPrefabPath);
        // LoadGONetPrefabAsync guarantees we're back on Unity main thread after await
        // Safe to call Unity APIs now
        GONetParticipant addressableInstance =
            GONetMain.Client_InstantiateToBeRemotelyControlledByMe(addressablePrefab, transform.position, transform.rotation);
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

        // Debug: Log cleanup check
        if (addressableParticipants.Count > 0)
        {
            GONetLog.Debug($"[ProjectileSpawner] Cleanup check: {addressableParticipants.Count} addressable Physics Cube projectiles in list, spawn origin: {spawnOrigin}");
        }

        int destroyedCount = 0;
        for (int i = addressableParticipants.Count - 1; i >= 0; --i)
        {
            GONetParticipant participant = addressableParticipants[i];
            if (participant == null || participant.gameObject == null)
            {
                // Already destroyed, remove from list
                addressableParticipants.RemoveAt(i);
                GONetLog.Debug($"[ProjectileSpawner] Removed null participant from list at index {i}");
                continue;
            }

            float distanceFromSpawn = Vector3.Distance(participant.transform.position, spawnOrigin);

            // Debug: Log each projectile's distance
            GONetLog.Debug($"[ProjectileSpawner] Physics Cube '{participant.name}' (GONetId: {participant.GONetId}) - Position: {participant.transform.position}, Distance: {distanceFromSpawn:F1}m");

            if (distanceFromSpawn > MAX_DISTANCE)
            {
                GONetLog.Debug($"[ProjectileSpawner] Destroying Physics Cube '{participant.name}' - too far from spawn ({distanceFromSpawn:F1}m > {MAX_DISTANCE}m)");
                Destroy(participant.gameObject);
                destroyedCount++;
            }
        }

        if (destroyedCount > 0)
        {
            GONetLog.Debug($"[ProjectileSpawner] Destroyed {destroyedCount} Physics Cube projectiles this cleanup cycle");
        }
    }
}
