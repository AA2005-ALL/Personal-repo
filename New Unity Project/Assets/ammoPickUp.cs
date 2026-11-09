using System;
using UnityEngine;
using Unity.Netcode;

public class ammoPickUp : NetworkBehaviour
{
    private int ammoToAdd = 5;

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

            if (playerAmmo.ammoInClip.Value == 30)
            {
                return;
            }

            //Calculates the players ammo count post pick up
            int ammoWaitingToAdd = playerAmmo.ammoInClip.Value + ammoToAdd;

            //Never allows the player to get an ammo value higher than the maximum 
            if (ammoWaitingToAdd > playerAmmo.maxAmmoInClip.Value)
            {
                playerAmmo.ammoInClip.Value = playerAmmo.maxAmmoInClip.Value;
            }

            //Simple ammo addition 
            else
            {
                playerAmmo.ammoInClip.Value += 5;
            }

            if (!IsServer)
            {
                return; 
            }

            NetworkObject.Despawn();
        }
    }
}
