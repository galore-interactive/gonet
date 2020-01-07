using GONet;
using GONet.Sample;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public GONetParticipant projectilPrefab;

    private readonly List<Projectile> projectiles = new List<Projectile>(100);

    private void Awake()
    {
        GONetMain.EventBus.Subscribe<GONetParticipantStartedEvent>(envelope => {
            if (envelope.GONetParticipant && envelope.GONetParticipant.GetComponent<Projectile>() != null)
            {
                Projectile projectile = envelope.GONetParticipant.GetComponent<Projectile>();
                projectiles.Add(projectile);

                if (GONetMain.IsServer && !projectile.GONetParticipant.IsMine)
                {
                    // example option (wait a bit before server assumes authority): StartCoroutine(Server_AssumeOwnershipAfterSeconds(projectile.GONetParticipant, GONetMain.valueBlendingBufferLeadSeconds));
                    GONetMain.Server_AssumeAuthorityOver(projectile.GONetParticipant);
                }
            }
        });
    }

    private IEnumerator Server_AssumeOwnershipAfterSeconds(GONetParticipant gonetParticipant, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        GONetMain.Server_AssumeAuthorityOver(gonetParticipant);
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
                projectile.transform.Translate(transform.forward * Time.deltaTime * projectile.speed);
            }
        }
    }
}
