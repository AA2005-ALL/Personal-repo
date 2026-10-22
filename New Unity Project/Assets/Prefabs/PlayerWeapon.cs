using System.Net;
using Unity.Netcode;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    [SerializeField] NetworkObject projectilePrefab;
    [SerializeField] NetworkObject muzzleFlashPrefab;
    private PlayerNetworkData networkData;
    [SerializeField] Transform firePoint;
    [SerializeField] float fireInterval = 0.25f;
    private PlayerInputs controls;
    private float nextFireTime;
    public int currentWeapon; 

    private void Awake()
    {
        controls = new PlayerInputs();
        currentWeapon = 0;
        networkData = GetComponent<PlayerNetworkData>();
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
            return;

        if (!networkData.IsAlive.Value)
            return;

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

        if (controls.Player.Fire.WasPressedThisFrame())
        {
            RequestFireRpc(firePoint.position, firePoint.rotation);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestFireRpc(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (!networkData.IsAlive.Value)
            return;

        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireInterval;

        NetworkObject projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);

        NetworkObject muzzleFlash = Instantiate(muzzleFlashPrefab, spawnPosition, spawnRotation);

        projectile.Spawn();
        muzzleFlash.Spawn();

        NetwrokProjectile projectileData = projectile.GetComponent<NetwrokProjectile>();
        projectileData.Initialise(OwnerClientId);
    }
}