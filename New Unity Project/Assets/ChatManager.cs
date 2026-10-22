using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Singleton;
    [SerializeField] ChatMessage chatMessagePrefab;
    [SerializeField] PlayerIconSelect playerIconScript;
    [SerializeField] CanvasGroup chatContent;
    [SerializeField] ScrollRect chatRenderer;
    [SerializeField] TMP_InputField playerNameInput;
    [SerializeField] TMP_InputField chatInput;
    [SerializeField] TextMeshProUGUI showHideButtonText;
    [SerializeField] Sprite[] playerIcons;
    public string playername;
    public int iconIndex = 0;

    void Awake()
    {
        Singleton = this;
    }

    // Update is called once per frame
    void Update()
    {
        iconIndex = playerIconScript.playerIconNo;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            playername = playerNameInput.text;
            SendChatMessage(iconIndex, chatInput.text, playername);
            chatInput.text = "";
        }
    }

    public void SendChatMessage(int playerIcon, string message, string fromWho = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        fromWho = playername;


        string s = fromWho + " > " + message;
        SendChatMessageServerRPC(s, playerIcon);
    }

    public void AddMessage(string message, int playerIcon)
    {
        ChatMessage chatMessage = Instantiate(chatMessagePrefab, chatContent.transform);
        chatMessage.transform.localScale = Vector3.one;
        Sprite playerSprite = playerIcons[Mathf.Clamp(playerIcon, 0, playerIcons.Length - 1)];

        UnityEngine.Debug.Log("I'm this: " + chatMessage.name + " This is my sprite " + playerSprite + " This is where I am " + chatMessage.gameObject.transform.position);
        chatMessage.SetChatText(message, playerSprite);
    }

    public void HideOrShowChat()
    {
        if(chatRenderer.gameObject.activeInHierarchy == true && chatInput.gameObject.activeInHierarchy == true)
        {
            showHideButtonText.text = "Show";
            chatRenderer.gameObject.SetActive(false);
            chatInput.gameObject.SetActive(false); 
        }

        else
        {
            showHideButtonText.text = "Hide";
            chatRenderer.gameObject.SetActive(true);
            chatInput.gameObject.SetActive(true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SendChatMessageServerRPC(string message, int playerIcon)
    {
        print("ServerRPC");
        ReceiveChatMessageClientRPC(message, playerIcon);
    }

    [ClientRpc]
    void ReceiveChatMessageClientRPC(string message, int playerIcon)
    {
        print("ClientRPC");
        ChatManager.Singleton.AddMessage(message, playerIcon);
    }
}
