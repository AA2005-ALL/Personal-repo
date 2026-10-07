using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Singleton;
    [SerializeField] ChatMessage chatMessagePrefab;
    [SerializeField] CanvasGroup chatContent;
    [SerializeField] TMP_InputField chatInput;
    public string playername;

    void Awake()
    {
        Singleton = this;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return))
        {
            SendChatMessage(chatInput.text, playername);
            chatInput.text = "";
        }
    }

    public void SendChatMessage(string message, string fromWho = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string s = fromWho + " > " + message;
        SendChatMessageServerRPC(s);
    }

    public void AddMessage(string message)
    {
        ChatMessage chatMessage = Instantiate(chatMessagePrefab, chatContent.transform);
        UnityEngine.Debug.Log("I'm this: " + chatMessage.name + " This is where I am " + chatMessage.gameObject.transform.position);
        chatMessage.SetChatText(message);
    }

    [ServerRpc(RequireOwnership = false)]
    void SendChatMessageServerRPC(string message)
    {
        print("ServerRPC");
        ReceiveChatMessageClientRPC(message);
    }

    [ClientRpc]
    void ReceiveChatMessageClientRPC(string message)
    {
        print("ClientRPC");
        ChatManager.Singleton.AddMessage(message);
    }
}
