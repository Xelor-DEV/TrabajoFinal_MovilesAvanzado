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
        kickButton.onClick.AddListener(KickPlayer);
    }

    private void OnDisable()
    {
        kickButton.onClick.RemoveListener(KickPlayer);
    }

    // En PlayerItemUI.cs, mejora el método Initialize:

    public void Initialize(Player player, bool isHost)
    {
        this.player = player;

        if (player.Data.ContainsKey("PlayerName"))
        {
            playerNameText.text = player.Data["PlayerName"].Value;
        }
        else
        {
            playerNameText.text = "Unknown Player";
        }

        // Mostrar estado de ready
        if (player.Data.TryGetValue("IsReady", out PlayerDataObject isReadyData))
        {
            readyStatusText.text = isReadyData.Value == "true" ? "Ready" : "Not Ready";
        }
        else
        {
            readyStatusText.text = "Not Ready";
        }

        // Verificar si es el host y cambiar color del nombre
        var joinedLobby = LobbyServiceManager.Instance.JoinedLobby;
        if (joinedLobby != null && player.Id == joinedLobby.HostId)
        {
            playerNameText.color = Color.green;
            playerNameText.text += " (Host)";
        }
        else
        {
            playerNameText.color = Color.white;
        }

        // Solo mostrar botón de kick para host y no permitir kickearse a sí mismo
        bool isOwnPlayer = player.Id == AuthenticationService.Instance.PlayerId;
        if (kickButton != null)
        {
            kickButton.gameObject.SetActive(isHost && !isOwnPlayer);
            kickButton.onClick.RemoveAllListeners();
            kickButton.onClick.AddListener(KickPlayer);
        }
    }

    private void KickPlayer()
    {
        LobbyServiceManager.Instance.KickPlayer(player.Id);
    }
}