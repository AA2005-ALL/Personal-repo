using System;
using UnityEngine;
using Unity.Netcode;

public class ammoPickUp : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            PlayerWeapon playerAmmo = other.gameObject.GetComponent<PlayerWeapon>();

            if (playerAmmo.ammoInClip.Value < playerAmmo.maxAmmoInClip.Value)
            {
                playerAmmo.ammoInClip.Value += 5;
            }

            if(!IsServer)
            {
                return; 
            }

            NetworkObject.Despawn();
        }
    }
}
