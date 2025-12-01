using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class PlayerItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text readyStatusText;
    [SerializeField] private Image playerIconImage; // Nueva referencia
    [SerializeField] private Button kickButton;

    private Player player;

    public async void Initialize(Player player, bool isHost, string hostId, PlayerIcons playerIcons)
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
            UpdateReadyStatus(hostId);
        }

        // Cargar y mostrar el icono del jugador
        if (playerIconImage != null && playerIcons != null)
        {
            await LoadAndDisplayPlayerIcon(playerIcons);
        }

        // Solo mostrar botón de kick para host y no permitir kickearse a sí mismo
        bool isOwnPlayer = player.Id == AuthenticationService.Instance.PlayerId;
        if (kickButton != null)
        {
            kickButton.gameObject.SetActive(isHost && !isOwnPlayer && player.Id != hostId);
            kickButton.onClick.RemoveAllListeners();
            kickButton.onClick.AddListener(KickPlayer);
        }
    }

    private void UpdateReadyStatus(string hostId)
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

    private async Task LoadAndDisplayPlayerIcon(PlayerIcons playerIcons)
    {
        try
        {
            // Para el jugador local, cargar desde CloudSave
            if (player.Id == AuthenticationService.Instance.PlayerId)
            {
                var profileData = await CloudSaveManager.Instance.LoadPlayerProfileAsync();
                if (profileData != null)
                {
                    playerIconImage.sprite = playerIcons.GetIcon(profileData.iconIndex);
                    return;
                }
            }

            // Para otros jugadores, intentar obtener de los datos del lobby
            if (player.Data.TryGetValue("IconIndex", out PlayerDataObject iconData))
            {
                if (int.TryParse(iconData.Value, out int iconIndex))
                {
                    playerIconImage.sprite = playerIcons.GetIcon(iconIndex);
                    return;
                }
            }

            // Icono por defecto
            playerIconImage.sprite = playerIcons.GetIcon(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load player icon for {player.Id}: {ex.Message}");
            playerIconImage.sprite = playerIcons.GetIcon(0);
        }
    }

    private void KickPlayer()
    {
        if (player != null)
            LobbyServiceManager.Instance.KickPlayer(player.Id);
    }

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
}