using System.Net;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Security.Cryptography;

public class PlayerWeapon : NetworkBehaviour
{
    [SerializeField] NetworkObject projectilePrefab;
    [SerializeField] NetworkObject muzzleFlashPrefab;
    private PlayerNetworkData networkData;
    [SerializeField] Transform firePoint;
    [SerializeField] float fireInterval = 0.25f;
    [SerializeField] TMP_Text ammoDisplay;
    private int maxAmmoInClip = 30;
    public int ammoInClip;
    public int totalAmmoRemaining;
    public PlayerInputs controls;
    private float nextFireTime;
    public int currentWeapon; 

    private void Awake()
    {
        controls = new PlayerInputs();
        currentWeapon = 0;
        networkData = GetComponent<PlayerNetworkData>();
        ammoDisplay = transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>();
        Debug.Log("This is ammo Display " + transform.GetChild(0).GetChild(1).name);
        ammoInClip = 30;
        totalAmmoRemaining = 120;
        ammoDisplay.text = ammoInClip.ToString() + "/" + totalAmmoRemaining.ToString();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            controls.Player.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        { 
          controls.Player.Disable();
        }
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (!networkData.IsAlive.Value)
        {
            return;
        }

        if(controls.Player.SwitchWeapon.WasPressedThisFrame())
        {
            Debug.Log("F pressed");

            if(currentWeapon == 0)
            {
                currentWeapon = 1;
            }

            if(currentWeapon == 1)
            {
                currentWeapon = 0;
            }
        }

        if(currentWeapon == 0)
        {
            fireInterval = 0.25f;
        }

        if(currentWeapon == 1)
        {
            fireInterval = 0.05f;
        }

        if(controls.Player.Fire.WasPressedThisFrame())
        {
            RequestFireRpc(firePoint.position, firePoint.rotation);
        }

        if(controls.Player.Reload.WasPressedThisFrame())
        {
            ReloadRPC();
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestFireRpc(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (!networkData.IsAlive.Value)
        {
            return;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        if(ammoInClip <= 0)
        {
            return;
        }

        nextFireTime = Time.time + fireInterval;

        NetworkObject projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);

        NetworkObject muzzleFlash = Instantiate(muzzleFlashPrefab, spawnPosition, spawnRotation);

        projectile.Spawn();
        muzzleFlash.Spawn();

      

        NetwrokProjectile projectileData = projectile.GetComponent<NetwrokProjectile>();
        projectileData.Initialise(OwnerClientId);

        ammoInClip--;

        ammoDisplay.text = ammoInClip.ToString() + "/" + totalAmmoRemaining.ToString();

        muzzleFlash.Despawn();
    }

    [Rpc(SendTo.Server)]
    private void ReloadRPC()
    {
        if(ammoInClip == 30)
        {
            return;
        }

        int ammoToAddToClip = maxAmmoInClip - ammoInClip;
        
        totalAmmoRemaining -= ammoToAddToClip;

        ammoInClip = ammoInClip + ammoToAddToClip;

        ammoDisplay.text = ammoInClip.ToString() + "/" + totalAmmoRemaining.ToString();
    }
}