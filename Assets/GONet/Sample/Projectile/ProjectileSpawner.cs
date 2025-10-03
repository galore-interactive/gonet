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
    private readonly List<Projectile> addressableProjectiles = new List<Projectile>(100);
    private float lastCheckTime = 0f;
    const float CHECK_INTERVAL = 1f;

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

        if (GONetMain.IsServer && gonetParticipant.IsMine && gonetParticipant.gameObject.name.StartsWith("Physics Cube Projectile"))
        {
            Projectile projectile = gonetParticipant.GetComponent<Projectile>();
            if (projectile != null)
            {
                addressableProjectiles.Add(projectile);
            }
        }
    }

    public override void OnGONetParticipantDisabled(GONetParticipant gonetParticipant)
    {
        base.OnGONetParticipantDisabled(gonetParticipant);
        Projectile projectile = gonetParticipant.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectiles.Remove(projectile);
            addressableProjectiles.Remove(projectile);
        }
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
                GONetParticipant gnp =
                    GONetMain.Client_InstantiateToBeRemotelyControlledByMe(projectilPrefab, transform.position, transform.rotation);
                GONetLog.Debug($"Spawned projectile for this client to remotely control, but server will own it. Is Mine? {gnp.IsMine} Is Mine To Remotely Control? {gnp.IsMine_ToRemotelyControl}");
                InstantiateAddressablesPrefab();
            }
        }
        foreach (var projectile in projectiles)
        {
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

    private void DestroyAddressableProjectilesOutOfView()
    {
        if (Camera.main == null) return;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        for (int i = addressableProjectiles.Count - 1; i >= 0; --i)
        {
            Projectile projectile = addressableProjectiles[i];
            Renderer renderer = projectile.GetComponent<Renderer>();
            if (renderer != null && !GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))
            {
                Destroy(projectile.gameObject);
            }
        }
    }
}