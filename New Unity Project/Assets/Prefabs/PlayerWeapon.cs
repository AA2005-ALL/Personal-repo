using System.Net;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Security.Cryptography;


public class PlayerWeapon : NetworkBehaviour
{
    [SerializeField] NetworkObject projectilePrefab;
    [SerializeField] NetworkObject muzzleFlashPrefab;
    [SerializeField] GameObject predictedProjectilePrefab;
    private PlayerNetworkData networkData;
    [SerializeField] Transform firePoint;
    [SerializeField] float fireInterval = 0.25f;
    [SerializeField] TMP_Text ammoDisplay;
    public NetworkVariable<int> maxAmmoInClip = new NetworkVariable<int>(30);
    public NetworkVariable<int> ammoInClip = new NetworkVariable<int>(30);
    NetworkVariable<int> totalAmmoRemaining = new NetworkVariable<int>(120);
    public PlayerInputs controls;
    private float nextFireTime;
    public int currentWeapon;

    private void Awake()
    {
        controls = new PlayerInputs();
        currentWeapon = 0;
        networkData = GetComponent<PlayerNetworkData>();
        ammoDisplay = transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>();
    }

    private void OnAmmoValueChanged(int previousValue, int newValue)
    {
        updateAmmoUI();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            controls.Player.Enable();

            ammoInClip.OnValueChanged += OnAmmoValueChanged;
            totalAmmoRemaining.OnValueChanged += OnAmmoValueChanged;
            maxAmmoInClip.OnValueChanged += OnAmmoValueChanged;

            updateAmmoUI();
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

        if (controls.Player.SwitchWeapon.WasPressedThisFrame())
        {
            Debug.Log(currentWeapon);

            if (currentWeapon == 0)
            {
                currentWeapon = 1;
            }

            else
            {
                currentWeapon = 0;
            }
        }

        if (ammoInClip.Value >= maxAmmoInClip.Value)
        {
            ammoInClip.Value = maxAmmoInClip.Value;
        }

        if (currentWeapon == 0)
        {
            fireInterval = 0.25f;
        }

        if (currentWeapon == 1)
        {
            fireInterval = 0.05f;
        }

        if (controls.Player.Fire.WasPressedThisFrame())
        {
            if (!IsServer)
            {
                Instantiate(predictedProjectilePrefab, firePoint.position, firePoint.rotation);
            }

            requestFireRpc(firePoint.position, firePoint.rotation);
        }

        if (controls.Player.Reload.WasPressedThisFrame())
        {
            reloadRPC();
        }
    }

    [Rpc(SendTo.Server)]
    private void requestFireRpc(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (!networkData.IsAlive.Value)
        {
            return;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        if (ammoInClip.Value <= 0)
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

        ammoInClip.Value--;

        updateAmmoUI();

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

        updateAmmoUI();
    }

    private void updateAmmoUI()
    {
        ammoDisplay.text = ammoInClip.Value.ToString() + "/" + totalAmmoRemaining.Value.ToString();
    }
}
