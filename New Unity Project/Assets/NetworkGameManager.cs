using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkGameManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField playerNameInput;

    //public List<LeaderBoard> leaderboardEntries = new List<LeaderBoard>();

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