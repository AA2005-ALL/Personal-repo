using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

public class ChatMessage : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI currentChatText;
    [SerializeField] Image playerImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetChatText(string chatText, Sprite playerIcon)
    {
        currentChatText.text = chatText;
        playerImage.sprite = playerIcon;
        UnityEngine.Debug.Log(currentChatText.text);
    }
}
