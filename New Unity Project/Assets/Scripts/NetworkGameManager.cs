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

    [SerializeField] TextMeshProUGUI roundTimer;

    [SerializeField] TextMeshProUGUI roundCountUI;

    [SerializeField] TextMeshProUGUI enemiesLeftToClearRoundUI;
    private bool matchStarted = false;
    public bool MatchStarted => matchStarted;
    private float ammoCooldown;

    NetworkVariable<int> maxRoundTimerMinutes = new NetworkVariable<int>(1);
    NetworkVariable<int> roundTimerMinutes = new NetworkVariable<int>(1);
    NetworkVariable<float> roundTimerSeconds = new NetworkVariable<float>(0f);

    NetworkVariable<int> roundCount = new NetworkVariable<int>(0);

    NetworkVariable<int> maxEnemiesLeftToClear = new NetworkVariable<int>(3);

    NetworkVariable<int> enemiesLeftToClearRound = new NetworkVariable<int>(0);

    NetworkVariable<bool> enemiesCleared = new NetworkVariable<bool>(false); 

    public NetworkList<int> leaderboardScores = new NetworkList<int>();
    public NetworkList<FixedString64Bytes> leaderboardNames = new NetworkList<FixedString64Bytes>();

    private List<NetworkEnemy> livingEnemies = new List<NetworkEnemy>();

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
        roundTimerMinutes.OnValueChanged += OnMinutesChanged;
        roundTimerSeconds.OnValueChanged += OnSecondsChanged;
        roundCount.OnValueChanged += OnRoundsChanged;
        enemiesCleared.OnValueChanged += OnEnemiesCleared;
        enemiesLeftToClearRound.OnValueChanged += OnEnemiesLeftToClearChanged;
    }

    private void OnMinutesChanged(int previous, int current)
    {
        UpdateTimerUI();
    }

    private void OnSecondsChanged(float previous, float current)
    {
        UpdateTimerUI();
    }

    private void OnRoundsChanged(int previous, int current)
    {
        UpdateRoundUI();
    }

    private void OnEnemiesCleared(bool previous, bool current)
    {
        UpdateRoundUI();
    }

    private void OnEnemiesLeftToClearChanged(int previous, int current)
    {
        UpdateEnemiesLeftUI();
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

        if (matchStarted)
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
            livingEnemies.Add(enemy.GetComponent<NetworkEnemy>());
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

        enemiesLeftToClearRound.Value--;

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

        if (connectedPlayers.TryGetValue(clientId, out PlayerData player))
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

        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (matchStarted)
        {
            if(livingEnemies.Count < enemiesLeftToClearRound.Value && ammoCooldown < 0)
            {
                SpawnEnemy();
            }

            roundTimerSeconds.Value -= Time.deltaTime;

            if (roundTimerSeconds.Value <= 0 && roundTimerMinutes.Value > 0)
            {
                roundTimerMinutes.Value--;
                roundTimerSeconds.Value = 59;
            }

            if(roundTimerMinutes.Value <= 0 && roundTimerSeconds.Value <= 0 && enemiesCleared.Value == false)
            {
                //Loss conditions
                Debug.Log("Players have lost");

                matchStarted = false;

                roundCount.Value = 0;

                PlayerNetworkData[] players = FindObjectsByType<PlayerNetworkData>(FindObjectsSortMode.None);

                for (int i = 0; i < livingEnemies.Count; i++)
                {
                    livingEnemies[i].GetComponent<NetworkObject>().Despawn();
                }

                for (int i = 0; i < players.Length; i++)
                {
                    players[i].IsReady.Value = false;
                    players[i].IsPlaying.Value = false;
                }
            }
            
            //Win conditions and round difficulty increases
            if(enemiesLeftToClearRound.Value <= 0)
            {
                enemiesCleared.Value = true;
            }

            if(enemiesCleared.Value == true)
            {
                roundCount.Value++;
                maxEnemiesLeftToClear.Value++;
                enemiesLeftToClearRound.Value = maxEnemiesLeftToClear.Value;
                roundTimerMinutes.Value = maxRoundTimerMinutes.Value;
                roundTimerSeconds.Value = 59;
                enemiesCleared.Value = false;
            }

            if (roundCount.Value == roundCount.Value % 5 && enemiesCleared.Value == true)
            {
                Debug.Log("Extra minute gained");
                maxRoundTimerMinutes.Value++;
            }
        }

        if (ammoCooldown < 0 && matchStarted)
        {
            ammoCooldown = 3;
            spawnAmmoPickUps();
        }
    }

    private void UpdateTimerUI()
    {
        if (roundTimerSeconds.Value < 10)
        {
            roundTimer.text = roundTimerMinutes.Value.ToString() + ":" + "0" + Math.Round(roundTimerSeconds.Value).ToString();
        }

        else
        {
            roundTimer.text = roundTimerMinutes.Value.ToString() + ":" + Math.Round(roundTimerSeconds.Value).ToString();
        }
    }

    private void UpdateRoundUI()
    {
        roundCountUI.text = "Round: " + roundCount.Value.ToString();
    }

    private void UpdateEnemiesLeftUI()
    {
        enemiesLeftToClearRoundUI.text = "Enemies left: " + enemiesLeftToClearRound.Value.ToString();
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
        //Ensures this function isn't called at unneccesary moments such as the round already being started or there only being one player
        if (!NetworkManager.Singleton.IsServer || matchStarted)
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
            //Only adds a player to the list if they are ready 
            if (players[i].IsReady.Value == true)
            {
                readyPlayers.Add(players[i]);
            }

            //Ignores players that aren't ready 
            else
            {
                continue; 
            }
        }

        //Ensures rounds cannot start unless all players are ready
        if(readyPlayers.Count == players.Length)
        {
            Debug.Log("Round started set to true");
            matchStarted = true;

            for(int i = 0; i < readyPlayers.Count; i ++)
            {
                readyPlayers[i].SetPlaying();
            }
        }

        else
        {
            matchStarted = false;
        }

        if(matchStarted)
        {
            Debug.Log("All players ready! Round starting...");
            maxEnemiesLeftToClear.Value = 3;
            maxRoundTimerMinutes.Value = 2; 
            enemiesLeftToClearRound.Value = maxEnemiesLeftToClear.Value;
            roundTimerMinutes.Value = maxRoundTimerMinutes.Value;
            roundCount.Value++;
            SpawnEnemy();
        }
    }

    public void UpdateLeaderboard()
    {
        var activePlayers = FindObjectsByType<PlayerNetworkData>(FindObjectsSortMode.None);

        //Converted to List to make searching through the leaderbaord easier. 
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