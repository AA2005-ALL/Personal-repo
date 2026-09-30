using Unity.Netcode;
using UnityEngine;
public class PlayerWeapon : NetworkBehaviour
{
    [SerializeField] private NetworkObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireInterval = 0.25f;
    private PlayerInputs controls;
    private float nextFireTime;

    private void Awake()
    {
        controls = new PlayerInputs();
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

        if (controls.Player.Fire.WasPressedThisFrame())
        {
            RequestFireRpc(firePoint.position, firePoint.rotation);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestFireRpc(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireInterval;

        NetworkObject projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            spawnRotation
        );

        projectile.Spawn();

        NetwrokProjectile projectileData = projectile.GetComponent<NetwrokProjectile>();
        projectileData.Initialise(OwnerClientId);
    }
}