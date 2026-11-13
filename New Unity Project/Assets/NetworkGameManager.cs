using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Linq;
using NUnit.Framework.Interfaces;

public class NetworkGameManager : NetworkBehaviour
{
    [SerializeField] TMP_InputField playerNameInput;
    [SerializeField] GameObject enemyPrefab;
    private Dictionary<ulong, PlayerData> connectedPlayers = new Dictionary<ulong, PlayerData>();
    private Dictionary<ulong, PlayerData> disconnectedPlayers = new Dictionary<ulong, PlayerData>();
    private Dictionary<string, PlayerData> savedSessions = new Dictionary<string, PlayerData>();
    [SerializeField] float enemyRespawnDelay = 3f;
    [SerializeField] List<AudioSource> playerAudioManagers; 
    [SerializeField] ammoPickUp ammoPickUpPrefab;
    [SerializeField] Transform[] ammoSpawns;
    [SerializeField] TextMeshProUGUI[] nameTexts;
    [SerializeField] TextMeshProUGUI[] scoreTexts;
    private bool roundStarted = false;
    public bool RoundStarted => roundStarted;
    private float ammoCooldown; 

    public NetworkList<int> leaderboardScores = new NetworkList<int>();
    public NetworkList<FixedString64Bytes> leaderboardNames = new NetworkList<FixedString64Bytes>();

    private string sessionId;

    public bool TryGetSavedSession(string sessionId, out PlayerData player)
    {
        return savedSessions.TryGetValue(sessionId, out player);
    }

    public override void OnNetworkSpawn()
    {
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Name: " + device);
        }

        sessionId = Guid.NewGuid().ToString();
        ammoCooldown = 3f;
    }

    public string GetSessionId()
    {
        return sessionId;
    }

    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
    }
        
    private void OnDisable()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
    }

    private void OnPlayerConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        PlayerData newPlayer = new PlayerData();
        newPlayer.PlayerName = $"Player {clientId}";
        newPlayer.Score = 0;
        newPlayer.Kills = 0;
        newPlayer.Deaths = 0;
        connectedPlayers.TryAdd(clientId, newPlayer);

        if (roundStarted)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
            {
                if (client.PlayerObject != null)
                {
                    PlayerNetworkData networkData = client.PlayerObject.GetComponent<PlayerNetworkData>();

                    if (networkData != null)
                    {
                        networkData.SetPlaying();
                    }

                    else
                    {
                        Debug.LogError("Can't find network data from beginning");
                    }
                }
            }
        }

        PlayerData player = connectedPlayers[clientId];
        Debug.Log($"Player {player.PlayerName} registered. " + $"Total players: {connectedPlayers.Count}");
        CheckAllPlayersReady();
    }

    private void SpawnEnemy()
    {
        Debug.Log("Spawn enemy called");
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();

        if (spawnManager == null)
        {
            Debug.LogError("Can't find spawn manager");
            return;
        }

        Transform spawnPoint = spawnManager.GetRandomSpawnPoint();

        if (spawnPoint == null)
        {
            Debug.LogError("Can't find a spawn point");
            return;
        }

        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        if (enemy != null)
        {
            enemy.GetComponent<NetworkObject>().Spawn();
        }

        else
        {
            Debug.LogError("No enemies?");
        }
    }

    public void ScheduleEnemyRespawn()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        StartCoroutine(RespawnEnemyAfterDelay());
    }

    private IEnumerator RespawnEnemyAfterDelay()
    {
        yield return new WaitForSeconds(enemyRespawnDelay);

        if (!NetworkManager.Singleton.IsServer)
        {
            yield break;
        }

        SpawnEnemy();
    }


    private void OnPlayerDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if(connectedPlayers.TryGetValue(clientId, out PlayerData player))
        {
            disconnectedPlayers[clientId] = player;

            if (!string.IsNullOrEmpty(player.SessionId))
            {
                savedSessions[player.SessionId] = player;
                Debug.Log($"Saved session for {player.PlayerName}, " + $"Score: {player.Score}");
            }

            connectedPlayers.Remove(clientId);
            Debug.Log($"Saved disconnected player: {player.PlayerName}, " + $"Score: {player.Score}. Connected: " + $"{connectedPlayers.Count}, Disconnected: " + $"{disconnectedPlayers.Count}");
        }
    }

    public void UpdatePlayerRecord(ulong clientId, string playerName, int score)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (!connectedPlayers.TryGetValue(clientId, out PlayerData player))
        {
            player = new PlayerData();
            connectedPlayers.Add(clientId, player);
        }

        player.PlayerName = playerName;
        player.Score = score;

        Debug.Log($"Updated player {clientId}: " + $"{player.PlayerName}, Score: {player.Score}");
    }


    public void StartHost()
    {
        GetPlayerName();
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        GetPlayerName();
        NetworkManager.Singleton.StartClient();
    }

    public void StopConnection()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    public string GetPlayerName()
    {
        return playerNameInput.text;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }

        //if(Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        //{
            
        //}

        ammoCooldown -= Time.deltaTime;

        if(!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (ammoCooldown < 0 && roundStarted)
        {
            ammoCooldown = 3;
            spawnAmmoPickUps();
        }
    }

    public void RegisterPlayerSession(ulong clientId, string sessionId)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (!connectedPlayers.TryGetValue(clientId, out PlayerData player))
        {
            player = new PlayerData();
            connectedPlayers.Add(clientId, player);
        }

        player.SessionId = sessionId;

        if (savedSessions.TryGetValue(sessionId, out PlayerData savedPlayer))
        {
            player.PlayerName = savedPlayer.PlayerName;
            player.Score = savedPlayer.Score;
            player.Kills = savedPlayer.Kills;
            player.Deaths = savedPlayer.Deaths;
            Debug.Log($"Found returning player: {player.PlayerName}, " + $"Saved Score: {player.Score}");

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                PlayerNetworkData networkData = client.PlayerObject?.GetComponent<PlayerNetworkData>();

                if (networkData != null)
                {
                    networkData.RestoreScore(savedPlayer.Score);
                }

                else
                {
                    Debug.LogError("Can't find network data");
                }
            }
        }

        else
        {
            Debug.Log($"New player session registered: {sessionId}");
        }
    }

    public void spawnAmmoPickUps()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        ammoPickUp[] ammoPickups = FindObjectsByType<ammoPickUp>(FindObjectsSortMode.None);

        if (ammoPickups.Count() >= 5)
        {
            return;
        }

        else
        {
            int randomAmmoSpawn = UnityEngine.Random.Range(0, ammoSpawns.Count());

            ammoPickUp ammoP = Instantiate(ammoPickUpPrefab, ammoSpawns[randomAmmoSpawn].position, Quaternion.identity);

            ammoP.GetComponent<NetworkObject>().Spawn();
        }

    }

    public void CheckAllPlayersReady()
    {
        if (!NetworkManager.Singleton.IsServer || roundStarted)
        {
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            return;
        }

        List<PlayerNetworkData> readyPlayers = new List<PlayerNetworkData>();

        PlayerNetworkData[] players = FindObjectsByType<PlayerNetworkData>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].IsReady.Value == true)
            {
                readyPlayers.Add(players[i]);
            }

            else
            {
                continue; 
            }
        }

        if(readyPlayers.Count == players.Length)
        {
            Debug.Log("Round started set to true");
            roundStarted = true;
            for(int i = 0; i < readyPlayers.Count; i ++)
            {
                readyPlayers[i].SetPlaying();
            }
        }

        else
        {
            roundStarted = false;
        }

        if(roundStarted)
        {
            Debug.Log("All players ready! Round starting...");
            SpawnEnemy();
        }
    }

    public void UpdateLeaderboard()
    {
        var activePlayers = FindObjectsByType<PlayerNetworkData>(FindObjectsSortMode.None);
        var activePlayersInOrder = activePlayers.OrderByDescending(player => player.Score.Value).ToList();

        for(int i = 0; i < scoreTexts.Count(); i++)
        {
                if (i < activePlayersInOrder.Count())
                {
                  nameTexts[i].text = activePlayersInOrder[i].PlayerName.Value.ToString();
                  scoreTexts[i].text = activePlayersInOrder[i].Score.Value.ToString();
                }

                else
                {
                   nameTexts[i].text = "";
                   scoreTexts[i].text = "";
                }
        }
    }
}