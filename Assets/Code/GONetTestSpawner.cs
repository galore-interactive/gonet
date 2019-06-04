using GONet;
using UnityEngine;

public class GONetTestSpawner : MonoBehaviour
{
    public Simpeesimul GONetServerPREFAB;
    public Simpeesimul GONetClientPREFAB;

    public GONetParticipant nonResourcePrefab;
    public GONetParticipant resourcePrefab;

    private bool hasServerSpawned;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Instantiate(GONetClientPREFAB);
        }

        if (Input.GetKeyDown(KeyCode.S) && !hasServerSpawned)
        {
            Instantiate(GONetServerPREFAB);
            hasServerSpawned = true;
        }

        if (GONetMain.IsServer)
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                GameObject cubeta = GameObject.Find("Cubetas");
                Instantiate(cubeta);
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                Instantiate(nonResourcePrefab);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Instantiate(resourcePrefab);
            }
        }
    }
}
