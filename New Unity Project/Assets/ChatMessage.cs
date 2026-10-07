using UnityEngine;
using TMPro;
using System.Diagnostics;

public class ChatMessage : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI currentChatText; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetChatText(string chatText)
    {
        currentChatText.text = chatText;
        UnityEngine.Debug.Log(currentChatText.text);
    }
}
