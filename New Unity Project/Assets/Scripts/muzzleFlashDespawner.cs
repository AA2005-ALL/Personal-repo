using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class muzzleFlashDespawner : NetworkBehaviour
{
    NetworkVariable<float> despawnTime = new NetworkVariable<float>(0.2f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(waitToDespawn());
        }
    }

    public override void OnNetworkDespawn()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator waitToDespawn()
    {
        yield return new WaitForSeconds(despawnTime.Value);
        NetworkObject.Despawn();
    }
}
