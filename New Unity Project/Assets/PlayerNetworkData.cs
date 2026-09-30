using System;
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

    public NetworkVariable<int> Health = new NetworkVariable<int>(100);

    public NetworkVariable<int> Score = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        UnityEngine.Debug.Log( $"Spawned {OwnerClientId} | " + $"Name: {PlayerName.Value} | " + $"Health: {Health.Value} | " + $"Score: {Score.Value}");

        playerInfoText = GetComponentInChildren<TMP_Text>();

        PlayerName.OnValueChanged += OnNameChanged;
        Health.OnValueChanged += OnIntValueChanged;
        Score.OnValueChanged += OnIntValueChanged;
        FacingAngle.OnValueChanged += OnFacingAngleChanged;


        UpdatePlayerDisplay();

        ApplyFacingAngle(FacingAngle.Value);


        if (!IsOwner)
        {
            return;
        }

        NetworkGameManager gameManager =
            FindFirstObjectByType<NetworkGameManager>();

        string requestedName = gameManager.GetPlayerName();

        if (string.IsNullOrWhiteSpace(requestedName))
            requestedName = $"Player {OwnerClientId}";

        SetPlayerNameRpc(new FixedString64Bytes(requestedName));
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnNameChanged;
        Health.OnValueChanged -= OnIntValueChanged;
        Score.OnValueChanged -= OnIntValueChanged;
        FacingAngle.OnValueChanged -= OnFacingAngleChanged;
    }

    private void OnNameChanged(FixedString64Bytes previousValue,
                               FixedString64Bytes newValue)
    {
        UnityEngine.Debug.Log("OnNameChanged called");
        UpdatePlayerDisplay();
    }

    private void OnIntValueChanged(int previousValue, int newValue)
    {
        UnityEngine.Debug.Log("OnIntValueChanged called");
        UpdatePlayerDisplay();
    }

    private void OnFacingAngleChanged(float oldAngle, float newAngle)
    {
        // The owner already rotates immediately in PlayerController. 
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
        UnityEngine.Debug.Log("UpdatePlayerDisplay called");

        if (playerInfoText == null)
        {
            return;
        }

        playerInfoText.text = $"{PlayerName.Value}\n" + $"HP: {Health.Value}\n" + $"Score: {Score.Value}";
    }

    [Rpc(SendTo.Server)]
    private void SetPlayerNameRpc(FixedString64Bytes newName)
    {
        UnityEngine.Debug.Log("SetPlayerNameRpc called");
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

    public void TakeDamage(int damage)
    {
        if (!IsServer)
            return;

        Health.Value = Mathf.Max(Health.Value - damage, 0);
    }

    public void AddScore(int amount)
    {
        if (!IsServer)
            return;

        Score.Value += amount;
    }

    private void Update()
    {
        // TEMPORARY LAB TEST CODE 
        if (!IsServer)
            return;

        if (Keyboard.current.hKey.wasPressedThisFrame)
            TakeDamage(10);

        if (Keyboard.current.pKey.wasPressedThisFrame)
            AddScore(1);
    }
}