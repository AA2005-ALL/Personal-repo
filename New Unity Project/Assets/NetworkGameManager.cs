using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System;

public class NetworkGameManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private GameObject enemyPrefab;
    private Dictionary<ulong, PlayerData> connectedPlayers = new Dictionary<ulong, PlayerData>();
    private Dictionary<ulong, PlayerData> disconnectedPlayers = new Dictionary<ulong, PlayerData>();
    private Dictionary<string, PlayerData> savedSessions = new Dictionary<string, PlayerData>();
    [SerializeField] private float enemyRespawnDelay = 3f;
    private bool roundStarted = false;
    public bool RoundStarted => roundStarted;

    //public List<LeaderBoard> leaderboardEntries = new List<LeaderBoard>();

    private string sessionId;

    public bool TryGetSavedSession(string sessionId, out PlayerData player)
    {
        return savedSessions.TryGetValue(sessionId, out player);
    } 

    private void Awake()
    {
        sessionId = Guid.NewGuid().ToString();
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

            //if (player == null || !player.IsReady.Value)
            //{
            //    return;
            //}

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
                Debug.LogError("No player lol xoxo");
            }
        }

        Debug.Log("All players ready! Round starting...");
        SpawnEnemy();
    }

    //// Adds a new entry to the leaderboard
    //public void AddEntry(NetworkVariable<FixedString64Bytes> name, int points)
    //{
    //    leaderboardEntries.Add(new LeaderBoard(name, points));

    //    //Sorts entries in the correct order by highest points tally
    //    leaderboardEntries.Sort((x, y) => y.Points.CompareTo(x.Points));
    //}
}

//public class LeaderBoard
//{
//    //Variables
//    public NetworkVariable<FixedString64Bytes> Name;
//    public int Points;

//    //Creates a Leaderboard function and passes through a string and float variable
//    public LeaderBoard(NetworkVariable<FixedString64Bytes> name, int points)
//    {
//        //Sets the teamName to the name string variable that is passed through 
//        Name = name;

//        //Collects the points from the points float variable that is passed through 
//        Points = points;
//    }
//}