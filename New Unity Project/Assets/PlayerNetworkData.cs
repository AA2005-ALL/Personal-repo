using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Cryptography;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerNetworkData : NetworkBehaviour
{
    [SerializeField] TMP_Text playerInfoText;

    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>("Player");

    public NetworkVariable<float> FacingAngle = new NetworkVariable<float>(0f);

    [SerializeField] Transform playerVisual;

    [SerializeField] private GameObject invincibilityBubble;

    public NetworkVariable<int> Health = new NetworkVariable<int>(100);

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(true);

    public NetworkVariable<int> Score = new NetworkVariable<int>(0);

    public NetworkVariable<bool> justRespawned = new NetworkVariable<bool>(false);

    [SerializeField] private float respawnDelay = 3f;

    [SerializeField] private float spawnInvincibilityDelay = 3f;

    public NetworkVariable<float> RespawnTime = new NetworkVariable<float>(0f);

    public NetworkVariable<float> InvincibilityTimer = new NetworkVariable<float>(0f);

    private int playerCount; 

    public override void OnNetworkSpawn()
    {
        UnityEngine.Debug.Log($"Spawned {OwnerClientId} | " + $"Name: {PlayerName.Value} | " + $"Health: {Health.Value} | " + $"Score: {Score.Value}");

        playerInfoText = GetComponentInChildren<TMP_Text>();

        PlayerName.OnValueChanged += OnNameChanged;
        Health.OnValueChanged += OnIntValueChanged;
        IsAlive.OnValueChanged += OnAliveChanged;
        Score.OnValueChanged += OnIntValueChanged;
        FacingAngle.OnValueChanged += OnFacingAngleChanged;


        //AddEntry(PlayerName, Score);

        UpdatePlayerDisplay();

        ApplyFacingAngle(FacingAngle.Value);


        if (!IsOwner)
        {
            return;
        }

        NetworkGameManager gameManager = FindFirstObjectByType<NetworkGameManager>();

        string requestedName = gameManager.GetPlayerName();

        if (string.IsNullOrWhiteSpace(requestedName))
        {
            requestedName = $"Player {OwnerClientId}";
        }

        SetPlayerNameRpc(new FixedString64Bytes(requestedName));
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnNameChanged;
        Health.OnValueChanged -= OnIntValueChanged;
        IsAlive.OnValueChanged -= OnAliveChanged;
        Score.OnValueChanged -= OnIntValueChanged;
        FacingAngle.OnValueChanged -= OnFacingAngleChanged;
    }

    private void OnNameChanged(FixedString64Bytes previousValue,
                               FixedString64Bytes newValue)
    {
        UpdatePlayerDisplay();
    }

    private void OnIntValueChanged(int previousValue, int newValue)
    {
        UpdatePlayerDisplay();
    }

    private void OnFacingAngleChanged(float oldAngle, float newAngle)
    {
        //The owner already rotates immediately in PlayerController.
        
        if (IsOwner)
        {
            return;
        }

        ApplyFacingAngle(newAngle);
    }

    private void ApplyFacingAngle(float angle)
    {
        if (playerVisual == null)
        {
            return;
        }

        playerVisual.localRotation =
        Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdatePlayerDisplay()
    {
        if (playerInfoText == null)
        {
            return;
        }

        string status = "";

        if (!IsAlive.Value)
        {
            status = "\nDEAD";
        }

        playerInfoText.text = $"{PlayerName.Value}\n" + $"HP: {Health.Value}\n" + $"Score: {Score.Value}" + status;
    }

    private void OnAliveChanged(bool previousValue, bool newValue)
    {
        UpdatePlayerDisplay();
        UpdateAliveDisplay();

        if (!newValue)
        {
            UnityEngine.Debug.Log($"{PlayerName.Value} is now dead on this client.");
        }
    }

    [Rpc(SendTo.Server)]
    private void SetPlayerNameRpc(FixedString64Bytes newName)
    {
        string value = newName.ToString().Trim();

        if (string.IsNullOrEmpty(value))
        {
            value = $"Player {OwnerClientId}";
        }

        if (value.Length > 16)
        {
            value = value.Substring(0, 16);
        }

        PlayerName.Value = new FixedString64Bytes(value);
    }

    [Rpc(SendTo.Server)]
    public void SetFacingAngleRpc(float angle)
    {
        FacingAngle.Value = angle;
    }

    public bool TakeDamage(int damage)
    {
        if (!IsServer)
        {
            return false;
        }

        if (!IsAlive.Value || justRespawned.Value == true)
        {
            return false;
        }

        Health.Value = Mathf.Max(Health.Value - damage, 0);

        if (Health.Value == 0)
        {
            IsAlive.Value = false;
            RespawnTime.Value = Time.time + respawnDelay;
            UnityEngine.Debug.Log($"{PlayerName.Value} has died.");
            return true;
        }
        return false;
    }

    public void AddScore(int amount)
    {
        if (!IsServer)
            return;

        Score.Value += amount;
    }

    private void UpdateAliveDisplay()
    {
        if (playerVisual != null)
        {
            playerVisual.gameObject.SetActive(IsAlive.Value);
        }
    }

    [Rpc(SendTo.Owner)]
    private void TeleportPlayerRpc(Vector3 position)
    {
        transform.position = position;
    }

    private void Respawn()
    {
        SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();

        if (spawnManager == null)
        {
            return;
        }

        Transform spawnPoint = spawnManager.GetRandomSpawnPoint();

        if (spawnPoint == null)
        {
            return;
        }

        TeleportPlayerRpc(spawnPoint.position);

        Health.Value = 100;

        IsAlive.Value = true;

        justRespawned.Value = true;

        InvincibilityTimer.Value = Time.time + spawnInvincibilityDelay;

        invincibilityBubble.SetActive(true);
    }

    private void UpdateInvincibilityTimer()
    {
        float timeTillNotInvincible = InvincibilityTimer.Value - Time.time;

        UnityEngine.Debug.Log("time remaining for invincibility: " + timeTillNotInvincible);

        if(timeTillNotInvincible <= 0)
        {
            justRespawned.Value = false;
            invincibilityBubble.SetActive(false);
            InvincibilityTimer.Value = 0;
        }
    }

    private void UpdateRespawnDisplay()
    {
        if (playerInfoText == null)
        {
            return;
        }

        if (IsAlive.Value)
        {
            return;
        }

        float remainingTime = RespawnTime.Value - Time.time;
        int secondsRemaining = Mathf.CeilToInt(remainingTime);
        secondsRemaining = Mathf.Max(secondsRemaining, 0);
        playerInfoText.text = $"{PlayerName.Value}\n" + $"HP: {Health.Value}\n" + $"Score: {Score.Value}\n" + $"DEAD\n" + $"Respawning in {secondsRemaining}...";
    }

    private void Update()
    {
        if (!IsAlive.Value)
        {
            UpdateRespawnDisplay();
        }

        if (!IsServer)
        {
            return;
        }

        if (!IsAlive.Value && Time.time >= RespawnTime.Value)
        {
            Respawn();
        }

        if (justRespawned.Value == true)
        {
            UpdateInvincibilityTimer();
        }
    }
}
