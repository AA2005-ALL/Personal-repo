using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class NetworkEnemy : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 58f;
    private Rigidbody2D rb;

    [SerializeField] NetworkObject projectilePrefab;

    [SerializeField] NetworkObject muzzleFlashPrefab;

    [SerializeField] Transform firePoint;

    [SerializeField] float fireInterval = 0.25f;
    public NetworkVariable<int> Health = new NetworkVariable<int>(30);

    public NetworkVariable<int> maxAmmoInClip = new NetworkVariable<int>(30);
    public NetworkVariable<int> ammoInClip = new NetworkVariable<int>(30);
    NetworkVariable<int> totalAmmoRemaining = new NetworkVariable<int>(120);
    private float nextFireTime;
    public int currentWeapon;
    private int nextShotId;
    private Dictionary<int, PredictedProjectile> predictedShots = new Dictionary<int, PredictedProjectile>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }

        PlayerNetworkData target = FindNearestPlayer();

        if (target == null)
        {
            return;
        }


        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target.playerVisual.rotation, 90);

        Vector2 direction = ((Vector2)target.transform.position - rb.position).normalized;

        float shootingDistance = 100f;

        if (Vector3.Distance(transform.position, target.transform.position) <= shootingDistance && ammoInClip.Value != 0 && totalAmmoRemaining.Value != 0)
        {
            //Debug.Log("Should shoot " + " This is my ammo in clip: " + ammoInClip.Value + " This is my total ammo remaining: " + totalAmmoRemaining.Value);
            int shotID = nextShotId++;

            requestFireRpc(firePoint.position, firePoint.rotation, shotID);        
        }

        if(ammoInClip.Value == 0)
        {
            //Debug.Log("Reloading");
            reloadRPC();
        }

        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    [Rpc(SendTo.Server)]
    private void requestFireRpc(Vector3 spawnPosition, Quaternion spawnRotation, int ShotID)
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        if (ammoInClip.Value <= 0)
        {
            return;
        }

        int shotId = nextShotId;

        nextFireTime = Time.time + fireInterval;

        NetworkObject projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);

        NetworkObject muzzleFlash = Instantiate(muzzleFlashPrefab, spawnPosition, spawnRotation);

        NetwrokProjectile projectileData = projectile.GetComponent<NetwrokProjectile>();
        projectileData.Initialise(NetworkObjectId, ShotID);

        projectile.Spawn();
        muzzleFlash.Spawn();

        ammoInClip.Value--;

        muzzleFlash.Despawn();
    }

    [Rpc(SendTo.Server)]
    private void reloadRPC()
    {
        if (ammoInClip.Value == 30)
        {
            return;
        }

        if (totalAmmoRemaining.Value <= 0)
        {
            totalAmmoRemaining.Value = 0;
            return;
        }

        int ammoToAddToClip = maxAmmoInClip.Value - ammoInClip.Value;

        totalAmmoRemaining.Value -= ammoToAddToClip;

        ammoInClip.Value = ammoInClip.Value + ammoToAddToClip;
    }

    private PlayerNetworkData FindNearestPlayer()
    {
        PlayerNetworkData nearest = null;

        float nearestDistance = float.MaxValue;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            PlayerNetworkData player = client.PlayerObject.GetComponent<PlayerNetworkData>();

            if (player == null || !player.IsAlive.Value)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = player;
            }
        }

        return nearest;
    }

    public bool TakeDamage(int damage)
    {
        if (!IsServer || Health.Value <= 0)
        {
            return false;
        }

        Health.Value = Mathf.Max(0, Health.Value - damage);

        if (Health.Value == 0)
        {
            NetworkGameManager gameManager = FindFirstObjectByType<NetworkGameManager>();

            if (gameManager != null)
            {
                gameManager.ScheduleEnemyRespawn();
            }

            NetworkObject.Despawn(true);

            return true;
        }

        return false;
    }
}