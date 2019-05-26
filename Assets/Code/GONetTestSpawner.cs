using GONet;
using UnityEngine;

public class GONetTestSpawner : MonoBehaviour
{
    public Simpeesimul GONetServerPREFAB;
    public Simpeesimul GONetClientPREFAB;

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

        if (Input.GetKeyDown(KeyCode.D))
        {
            GameObject cubeta = GameObject.Find("Cubeta");
            Instantiate(cubeta);
        }
    }
}
