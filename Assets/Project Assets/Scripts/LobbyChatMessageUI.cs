using UnityEngine;
using TMPro;

public class LobbyChatMessageUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text messageText;

    public void Initialize(string playerName, string message, Color? nameColor = null)
    {
        playerNameText.text = playerName;
        messageText.text = message;

        // Si se especifica un color, aplicarlo al nombre del jugador
        if (nameColor.HasValue)
        {
            playerNameText.color = nameColor.Value;
        }
    }
}