using System.Net;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Security.Cryptography;
using System.Collections;


public class PlayerWeapon : NetworkBehaviour
{
    [SerializeField] NetworkObject projectilePrefab;
    [SerializeField] NetworkObject muzzleFlashPrefab;
    [SerializeField] NetworkObject grenadePrefab;

    [SerializeField] GameObject predictedProjectilePrefab;
    private PlayerNetworkData networkData;
    [SerializeField] Transform firePoint;
    [SerializeField] float fireInterval = 0.25f;

    [SerializeField] TMP_Text ammoDisplay;
    public NetworkVariable<int> maxAmmoInClip = new NetworkVariable<int>(30);
    public NetworkVariable<int> ammoInClip = new NetworkVariable<int>(30);

    public NetworkVariable<int> grenadeAmount = new NetworkVariable<int>(1);

    NetworkVariable<int> maxGrenadeCount = new NetworkVariable<int>(2);
    NetworkVariable<int> totalAmmoRemaining = new NetworkVariable<int>(120);

    public NetworkVariable<int> damageDealt = new NetworkVariable<int>(10);

    public PlayerInputs controls;
    private float nextFireTime;
    public int currentWeapon;
    private int nextShotId;
    private Dictionary<int, PredictedProjectile> predictedShots = new Dictionary<int, PredictedProjectile>();

    private AudioSource reloadAudio;

    private void Awake()
    {
        controls = new PlayerInputs();
        currentWeapon = 0;
        networkData = GetComponent<PlayerNetworkData>();
        reloadAudio = GetComponent<AudioSource>();
        ammoDisplay = transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>();
    }

    private void OnAmmoValueChanged(int previousValue, int newValue)
    {
        updateAmmoUI();
    }


    public PredictedProjectile FindPredictedShot(int shotId)
    {
        predictedShots.TryGetValue(shotId, out PredictedProjectile shot);
        return shot;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            controls.Player.Enable();
        }

        ammoInClip.OnValueChanged += OnAmmoValueChanged;
        totalAmmoRemaining.OnValueChanged += OnAmmoValueChanged;
        maxAmmoInClip.OnValueChanged += OnAmmoValueChanged;

        updateAmmoUI();
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
                damageDealt.Value = 2;
                fireInterval = 0.0001f;
            }

            else
            {
                currentWeapon = 0;
                damageDealt.Value = 10;
                fireInterval = 0.25f;
            }
        }

        //if (currentWeapon == 0)
        //{
        //    damageDealt.Value = 10;
        //    fireInterval = 0.25f;
        //}

        //if (currentWeapon == 1)
        //{
        //    damageDealt.Value = 2;
        //    fireInterval = 0.0001f;
        //}

        if (controls.Player.Fire.WasPressedThisFrame())
        {
            int shotId = nextShotId++;

            if (!IsServer)
            {
                GameObject predicted = Instantiate(predictedProjectilePrefab,firePoint.position, firePoint.rotation);
                predictedShots[shotId] = predicted.GetComponent<PredictedProjectile>();
            }

            requestFireRpc(firePoint.position, firePoint.rotation, shotId);
        }

        if (controls.Player.ThrowGrenade.WasPressedThisFrame())
        {
            throwGrenadeRPC();
        }

        if (controls.Player.Reload.WasPressedThisFrame())
        {
            reloadRPC();
        }
    }

    [Rpc(SendTo.Server)]
    private void requestFireRpc(Vector3 spawnPosition, Quaternion spawnRotation, int ShotID)
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

        int shotId = nextShotId;

        nextFireTime = Time.time + fireInterval;

        NetworkObject projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);

        NetworkObject muzzleFlash = Instantiate(muzzleFlashPrefab, spawnPosition, spawnRotation);

        NetwrokProjectile projectileData = projectile.GetComponent<NetwrokProjectile>();
        projectileData.Initialise(NetworkObjectId, ShotID);

        projectile.Spawn();
        muzzleFlash.Spawn();

        ammoInClip.Value--;

        updateAmmoUI();
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

        reloadAudio.Play();

        int ammoToAddToClip = maxAmmoInClip.Value - ammoInClip.Value;

        totalAmmoRemaining.Value -= ammoToAddToClip;

        ammoInClip.Value = ammoInClip.Value + ammoToAddToClip;

        updateAmmoUI();
    }

    [Rpc(SendTo.Server)]
    private void throwGrenadeRPC()
    {
        if (grenadeAmount.Value <= 0)
        {
            grenadeAmount.Value = 0;
            return;
        }

        else
        {
            grenadeAmount.Value--;

            NetworkObject grenade = Instantiate(grenadePrefab, firePoint.transform.position, firePoint.transform.rotation);
            grenade.Spawn();
        }
    }

    private void updateAmmoUI()
    {
        ammoDisplay.text = ammoInClip.Value.ToString() + "/" + totalAmmoRemaining.Value.ToString();
    }
}
