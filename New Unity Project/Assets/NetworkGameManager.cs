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

public class NetworkGameManager : MonoBehaviour
{
    [SerializeField] TMP_InputField playerNameInput;
    [SerializeField] GameObject enemyPrefab;
    private Dictionary<ulong, PlayerData> connectedPlayers = new Dictionary<ulong, PlayerData>();
    private Dictionary<ulong, PlayerData> disconnectedPlayers = new Dictionary<ulong, PlayerData>();
    private Dictionary<string, PlayerData> savedSessions = new Dictionary<string, PlayerData>();
    [SerializeField] private float enemyRespawnDelay = 3f;
    [SerializeField] List<AudioSource> playerAudioManagers; 
    [SerializeField] ammoPickUp ammoPickUpPrefab;
    [SerializeField] Transform[] ammoSpawns;
    [SerializeField] TextMeshProUGUI[] nameTexts;
    [SerializeField] TextMeshProUGUI[] scoreTexts;
    private bool roundStarted = false;
    public bool RoundStarted => roundStarted;
    private float ammoCooldown; 

    public List<int> leaderboardScores = new List<int>();
    public List<string> leaderboardNames = new List<string>();

    private string sessionId;

    public bool TryGetSavedSession(string sessionId, out PlayerData player)
    {
        return savedSessions.TryGetValue(sessionId, out player);
    }

    private void Awake()
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
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
    }

    private void OnPlayerConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        PlayerData newPlayer = new PlayerData();
        newPlayer.PlayerName = $"Player {clientId}";
        newPlayer.Score = 0;
        newPlayer.Kills = 0;
        newPlayer.Deaths = 0;
        connectedPlayers.TryAdd(clientId, newPlayer);

        AddEntry(newPlayer.PlayerName, newPlayer.Score);

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
        if (!NetworkManager.Singleton.IsServer) return;

        if (connectedPlayers.TryGetValue(clientId, out PlayerData player))
        {
            if (!string.IsNullOrEmpty(player.SessionId))
            {
                savedSessions[player.SessionId] = player;
                Debug.Log($"Saved session for {player.PlayerName}, " + $"Score: {player.Score}");
            }

            connectedPlayers.Remove(clientId);
            Debug.Log($"Player {player.PlayerName} disconnected. " + $"Connected players: {connectedPlayers.Count}");
        }
    }

    public void UpdatePlayerRecord(ulong clientId, string playerName, int score)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (!connectedPlayers.TryGetValue(clientId, out PlayerData player))
        {
            player = new PlayerData();
            connectedPlayers.Add(clientId, player);
        }

        player.PlayerName = playerName;
        player.Score = score;

        UpdateLeaderboard();

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

        ammoCooldown -= Time.deltaTime;

        if(!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (ammoCooldown < 0)
        {
            ammoCooldown = 3;
            spawnAmmoPickUps();
        }
    }

    public void RegisterPlayerSession(ulong clientId, string sessionId)
    {
        if (!NetworkManager.Singleton.IsServer) 
            return;

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
        }

        else
        {
            Debug.Log($"New player session registered: {sessionId}");
        }

        //if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        //{
        //    PlayerNetworkData networkData = client.PlayerObject?.GetComponent<PlayerNetworkData>();

        //    if (networkData != null)
        //    {
        //        networkData.RestoreScore(savedPlayer.Score);
        //    }

        //    else
        //    {
        //        Debug.LogError("Can't find network data");
        //    }
        //}
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

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                Debug.LogError("NO CLIENT!!!");
                return;
            }

            PlayerNetworkData player = client.PlayerObject.GetComponent<PlayerNetworkData>();

            if (player != null)
            {
                player.SetPlaying();
            }
        }

        roundStarted = true;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            PlayerNetworkData player = client.PlayerObject.GetComponent<PlayerNetworkData>();

            if (player != null)
            {
                player.SetPlaying();
            }

            else
            {
                Debug.LogError("No player");
            }
        }

        Debug.Log("All players ready! Round starting...");
        SpawnEnemy();
    }

    // Adds a new entry to the leaderboard
    public void AddEntry(string name, int points)
    {
        leaderboardScores.Add(points);
        leaderboardNames.Add(name);

        //Sorts entries in the correct order by highest points tally
        leaderboardScores.Sort((x, y) => y.CompareTo(x));
    }

    public void DisplayLeaderboard()
    { 
        for(int i = 0; i < leaderboardScores.Count; i++)
        {
            Debug.Log("Displaying leaderboard");

            //Checks that the leaderboardScores and leaderboardNames array length hasn't been surpassed 
            if (i < nameTexts.Count() && i < scoreTexts.Count())
            {
                nameTexts[i].text = leaderboardNames[i];
                scoreTexts[i].text = leaderboardScores[i].ToString();

                Debug.Log(nameTexts[i].text);
                Debug.Log(scoreTexts[i].text);
            }
        }
    }

    public void UpdateLeaderboard()
    {
        Debug.Log("Leaderboard updating");

        //Removes all entries from the leaderboard
        leaderboardScores.Clear();
        leaderboardNames.Clear();

        //Sorts the leaderboard in order of highest to lowest score
        var newLeaderboardOrder = connectedPlayers.Values.OrderByDescending(player => player.Score);

        foreach (var player in newLeaderboardOrder)
        {
            leaderboardNames.Add(player.PlayerName);
            leaderboardScores.Add(player.Score);
        }

        //Displays the new order of players
        DisplayLeaderboard();
    }
}