using TMPro;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

public class VoiceChatPlayerItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image playerIconImage;
    [SerializeField] private Slider volumeSlider;

    private VivoxParticipant participant;

    public void Initialize(VivoxParticipant participant)
    {
        this.participant = participant;

        if (playerNameText != null)
            playerNameText.text = participant.DisplayName;

        // Configurar slider de volumen (0-1 mapeado a -50 a +50)
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            // Valor por defecto en el centro (0 dB)
            volumeSlider.value = 0.5f;
        }
    }

    private void OnVolumeChanged(float value)
    {
        // Mapear de 0-1 a -50 a +50
        int volume = Mathf.RoundToInt(value * 100f - 50f);
        VivoxLobbyManager.Instance.SetParticipantVolume(participant, volume);
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }
}