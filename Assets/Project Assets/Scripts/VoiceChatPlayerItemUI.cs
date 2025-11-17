using TMPro;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Services.Authentication;

public class VoiceChatPlayerItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image playerIconImage;
    [SerializeField] private Slider volumeSlider;

    [Header("Service References")]
    [SerializeField] private PlayerIcons playerIcons;

    private VivoxParticipant participant;

    public async void Initialize(VivoxParticipant participant)
    {
        this.participant = participant;

        if (playerNameText != null)
            playerNameText.text = participant.DisplayName;

        // Cargar y mostrar el icono del jugador
        if (playerIconImage != null && playerIcons != null)
        {
            await LoadAndDisplayPlayerIcon();
        }

        // Configurar slider de volumen (0-1 mapeado a -50 a +50)
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            // Valor por defecto en el centro (0 dB)
            volumeSlider.value = 0;
        }
    }

    private async Task LoadAndDisplayPlayerIcon()
    {
        try
        {
            // Para el jugador local, cargar desde CloudSave
            if (participant.PlayerId == AuthenticationService.Instance.PlayerId)
            {
                var profileData = await CloudSaveManager.Instance.LoadPlayerProfileAsync();
                if (profileData != null)
                {
                    playerIconImage.sprite = playerIcons.GetIcon(profileData.iconIndex);
                    return;
                }
            }

            // Para otros jugadores, intentar obtener de los datos del lobby
            // Buscar en el lobby actual por el display name o playerId
            var lobby = LobbyServiceManager.Instance?.JoinedLobby;
            if (lobby?.Players != null)
            {
                foreach (var player in lobby.Players)
                {
                    if (player.Id == participant.PlayerId ||
                        (player.Data.ContainsKey("PlayerName") &&
                         player.Data["PlayerName"].Value == participant.DisplayName))
                    {
                        if (player.Data.TryGetValue("IconIndex", out var iconData))
                        {
                            if (int.TryParse(iconData.Value, out int iconIndex))
                            {
                                playerIconImage.sprite = playerIcons.GetIcon(iconIndex);
                                return;
                            }
                        }
                        break;
                    }
                }
            }

            // Icono por defecto
            playerIconImage.sprite = playerIcons.GetIcon(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load player icon for {participant.DisplayName}: {ex.Message}");
            playerIconImage.sprite = playerIcons.GetIcon(0);
        }
    }

    private void OnVolumeChanged(float value)
    {
        int volumeDb = Mathf.RoundToInt(value);
        VivoxLobbyManager.Instance.SetParticipantVolume(participant, volumeDb);
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }
}