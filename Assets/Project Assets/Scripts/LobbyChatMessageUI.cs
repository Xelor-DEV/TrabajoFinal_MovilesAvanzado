using UnityEngine;
using TMPro;

public class LobbyChatMessageUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text messageText;

    public void Initialize(string playerName, string message)
    {
        playerNameText.text = playerName;
        messageText.text = message;
    }
}