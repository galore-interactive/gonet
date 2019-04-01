using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
