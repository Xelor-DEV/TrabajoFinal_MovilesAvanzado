using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

public class PlayerItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text readyStatusText;
    [SerializeField] private Button kickButton;

    private Player player;

    private void OnEnable()
    {
        if (kickButton != null)
            kickButton.onClick.AddListener(KickPlayer);
    }

    private void OnDisable()
    {
        if (kickButton != null)
            kickButton.onClick.RemoveListener(KickPlayer);
    }

    public void Initialize(Player player, bool isHost, string hostId)
    {
        this.player = player;

        if (playerNameText != null)
        {
            if (player.Data.ContainsKey("PlayerName"))
            {
                playerNameText.text = player.Data["PlayerName"].Value;
            }
            else
            {
                playerNameText.text = "Unknown Player";
            }

            // Color verde para el host
            if (player.Id == hostId)
            {
                playerNameText.color = Color.green;
                playerNameText.text += " (Host)";
            }
        }

        // Mostrar estado de ready
        if (readyStatusText != null)
        {
            if (player.Data.TryGetValue("IsReady", out PlayerDataObject isReadyData))
            {
                readyStatusText.text = isReadyData.Value == "true" ? "Ready" : "Not Ready";
                readyStatusText.color = isReadyData.Value == "true" ? Color.green : Color.red;
            }
            else
            {
                readyStatusText.text = "Not Ready";
                readyStatusText.color = Color.red;
            }

            // El host no tiene estado de ready
            if (player.Id == hostId)
            {
                readyStatusText.text = "Host";
                readyStatusText.color = Color.blue;
            }
        }

        // Solo mostrar botón de kick para host y no permitir kickearse a sí mismo
        bool isOwnPlayer = player.Id == AuthenticationService.Instance.PlayerId;
        if (kickButton != null)
        {
            kickButton.gameObject.SetActive(isHost && !isOwnPlayer && player.Id != hostId);

            // Reconfigurar listener
            kickButton.onClick.RemoveAllListeners();
            kickButton.onClick.AddListener(KickPlayer);
        }
    }

    private void KickPlayer()
    {
        if (player != null)
            LobbyServiceManager.Instance.KickPlayer(player.Id);
    }
}