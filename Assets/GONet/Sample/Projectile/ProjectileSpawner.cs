/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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
using UnityEngine;

public class ProjectileSpawner : GONetBehaviour
{
    public GONetParticipant projectilPrefab;

    private readonly List<Projectile> projectiles = new List<Projectile>(100);

    public override void OnGONetParticipantStarted(GONetParticipant gonetParticipant)
    {
        base.OnGONetParticipantStarted(gonetParticipant);

        if (gonetParticipant.GetComponent<Projectile>() != null)
        {
            Projectile projectile = gonetParticipant.GetComponent<Projectile>();
            projectiles.Add(projectile);

            if (GONetMain.IsServer && !projectile.GONetParticipant.IsMine)
            {
                GONetMain.Server_AssumeAuthorityOver(projectile.GONetParticipant);
            }
        }
    }

    public override void OnGONetParticipantDisabled(GONetParticipant gonetParticipant)
    {
        base.OnGONetParticipantDisabled(gonetParticipant);

        projectiles.Remove(gonetParticipant.GetComponent<Projectile>());
    }

    private void Update()
    {
        if (GONetMain.IsClient && Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.B) && projectilPrefab != null)
        {
            Instantiate(projectilPrefab, transform.position, transform.rotation);
        }

        foreach (var projectile in projectiles)
        {
            if (projectile.GONetParticipant.IsMine)
            {
                Vector3 previousPosition = projectile.transform.position;

                // option to use gonet time delta instead: projectile.transform.Translate(transform.forward * GONetMain.Time.DeltaTime * projectile.speed);
                projectile.transform.Translate(transform.forward * Time.deltaTime * projectile.speed);

                /*
                 * float magnitude = (projectile.transform.position - previousPosition).magnitude;
                double totalSeconds = Time.deltaTime;
                const string m = "ACTUHADI-------magnitude: ";
                const string s = " / ";
                const string e = " = ";
                GONetLog.Debug(string.Concat(m, magnitude, s, totalSeconds, e, magnitude / totalSeconds));
                */
            }
        }
    }
}
