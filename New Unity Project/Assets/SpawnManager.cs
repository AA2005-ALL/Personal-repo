using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        int index = UnityEngine.Random.Range(0, spawnPoints.Length);

        PlayerNetworkData[] activePlayers = FindObjectsByType<PlayerNetworkData>(FindObjectsSortMode.None);

        for (int i = 0; i < activePlayers.Length; i++)
        {
            if (activePlayers[i].IsAlive.Value == true)
            {
                UnityEngine.Debug.LogWarning(Vector3.Distance(activePlayers[i].gameObject.transform.position, spawnPoints[index].position));

                if (Vector3.Distance(activePlayers[i].gameObject.transform.position, spawnPoints[index].position) < 30.0f)
                {
                    UnityEngine.Debug.LogWarning("Can't spawn here");
                    GetRandomSpawnPoint();
                    return null;
                }
            }
        }

        

        return spawnPoints[index];
    }
}