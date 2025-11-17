using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

public class VoiceChatWindow : MonoBehaviour
{
    [Header("Window Controller")]
    [SerializeField] private WindowsController voiceChatWindowController;
    [SerializeField] private Button closeButton;

    [Header("Device Selection")]
    [SerializeField] private TMP_Text currentInputDeviceText;
    [SerializeField] private Button nextInputDeviceButton;
    [SerializeField] private Button previousInputDeviceButton;

    [SerializeField] private TMP_Text currentOutputDeviceText;
    [SerializeField] private Button nextOutputDeviceButton;
    [SerializeField] private Button previousOutputDeviceButton;

    [Header("Volume Settings")]
    [SerializeField] private Slider inputVolumeSlider;
    [SerializeField] private Toggle muteToggle;

    [Header("Player Info")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image playerIconImage;

    [Header("Other Players")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject playerVoiceItemPrefab;

    [Header("Service References")]
    [SerializeField] private PlayerIcons playerIcons;

    private List<VivoxInputDevice> inputDevices = new List<VivoxInputDevice>();
    private List<VivoxOutputDevice> outputDevices = new List<VivoxOutputDevice>();
    private int currentInputDeviceIndex = 0;
    private int currentOutputDeviceIndex = 0;
    private List<VoiceChatPlayerItemUI> playerItems = new List<VoiceChatPlayerItemUI>();

    private void Start()
    {
        InitializeDevices();
    }

    private void OnEnable()
    {
        // Configurar botones de dispositivos
        if (nextInputDeviceButton != null) nextInputDeviceButton.onClick.AddListener(NextInputDevice);
        if (previousInputDeviceButton != null) previousInputDeviceButton.onClick.AddListener(PreviousInputDevice);
        if (nextOutputDeviceButton != null) nextOutputDeviceButton.onClick.AddListener(NextOutputDevice);
        if (previousOutputDeviceButton != null) previousOutputDeviceButton.onClick.AddListener(PreviousOutputDevice);

        // Configurar botón de cerrar
        if (closeButton != null) closeButton.onClick.AddListener(CloseWindow);

        // Suscribirse a eventos de cambio de dispositivos
        VivoxService.Instance.AvailableInputDevicesChanged += OnAvailableInputDevicesChanged;
        VivoxService.Instance.AvailableOutputDevicesChanged += OnAvailableOutputDevicesChanged;
        VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemoved;

        // Inicializar dispositivos y UI
        InitializeDevices();
        UpdateDeviceDisplays();

        // Configurar slider y toggle
        if (inputVolumeSlider != null)
        {
            inputVolumeSlider.onValueChanged.AddListener(OnInputVolumeChanged);
            // Valor por defecto mapeado a 0 dB (centro del rango -50 a +50)
            inputVolumeSlider.value = 0.5f;
        }

        if (muteToggle != null)
        {
            muteToggle.onValueChanged.AddListener(OnMuteToggleChanged);
            muteToggle.isOn = false;
        }

        UpdatePlayerInfo();
        UpdateOtherPlayers();
    }

    private void OnDisable()
    {
        // Remover listeners
        if (nextInputDeviceButton != null) nextInputDeviceButton.onClick.RemoveListener(NextInputDevice);
        if (previousInputDeviceButton != null) previousInputDeviceButton.onClick.RemoveListener(PreviousInputDevice);
        if (nextOutputDeviceButton != null) nextOutputDeviceButton.onClick.RemoveListener(NextOutputDevice);
        if (previousOutputDeviceButton != null) previousOutputDeviceButton.onClick.RemoveListener(PreviousOutputDevice);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseWindow);

        // Desuscribirse de eventos
        VivoxService.Instance.AvailableInputDevicesChanged -= OnAvailableInputDevicesChanged;
        VivoxService.Instance.AvailableOutputDevicesChanged -= OnAvailableOutputDevicesChanged;
        VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel -= OnParticipantRemoved;

        if (inputVolumeSlider != null) inputVolumeSlider.onValueChanged.RemoveListener(OnInputVolumeChanged);
        if (muteToggle != null) muteToggle.onValueChanged.RemoveListener(OnMuteToggleChanged);
    }

    private void InitializeDevices()
    {
        inputDevices = VivoxLobbyManager.Instance.GetAvailableInputDevices().ToList();
        outputDevices = VivoxLobbyManager.Instance.GetAvailableOutputDevices().ToList();

        // Establecer los índices actuales basados en los dispositivos activos
        SetCurrentDeviceIndices();
    }

    private void SetCurrentDeviceIndices()
    {
        // Para dispositivos de entrada
        var activeInputDevice = VivoxService.Instance.ActiveInputDevice;
        if (activeInputDevice != null && inputDevices.Count > 0)
        {
            currentInputDeviceIndex = inputDevices.FindIndex(device => device.DeviceID == activeInputDevice.DeviceID);
            if (currentInputDeviceIndex == -1) currentInputDeviceIndex = 0;
        }

        // Para dispositivos de salida
        var activeOutputDevice = VivoxService.Instance.ActiveOutputDevice;
        if (activeOutputDevice != null && outputDevices.Count > 0)
        {
            currentOutputDeviceIndex = outputDevices.FindIndex(device => device.DeviceID == activeOutputDevice.DeviceID);
            if (currentOutputDeviceIndex == -1) currentOutputDeviceIndex = 0;
        }
    }

    private void OnAvailableInputDevicesChanged()
    {
        inputDevices = VivoxLobbyManager.Instance.GetAvailableInputDevices().ToList();
        SetCurrentDeviceIndices(); // Actualizar índices cuando cambien los dispositivos
        UpdateInputDeviceDisplay();
    }

    private void OnAvailableOutputDevicesChanged()
    {
        outputDevices = VivoxLobbyManager.Instance.GetAvailableOutputDevices().ToList();
        SetCurrentDeviceIndices(); // Actualizar índices cuando cambien los dispositivos
        UpdateOutputDeviceDisplay();
    }

    private void OnParticipantAdded(VivoxParticipant participant)
    {
        UpdateOtherPlayers();
    }

    private void OnParticipantRemoved(VivoxParticipant participant)
    {
        UpdateOtherPlayers();
    }

    private async void NextInputDevice()
    {
        if (inputDevices.Count == 0) return;
        currentInputDeviceIndex = (currentInputDeviceIndex + 1) % inputDevices.Count;
        await VivoxLobbyManager.Instance.SetInputDevice(inputDevices[currentInputDeviceIndex].DeviceID);
        UpdateInputDeviceDisplay();
    }

    private async void PreviousInputDevice()
    {
        if (inputDevices.Count == 0) return;
        currentInputDeviceIndex = (currentInputDeviceIndex - 1 + inputDevices.Count) % inputDevices.Count;
        await VivoxLobbyManager.Instance.SetInputDevice(inputDevices[currentInputDeviceIndex].DeviceID);
        UpdateInputDeviceDisplay();
    }

    private async void NextOutputDevice()
    {
        if (outputDevices.Count == 0) return;
        currentOutputDeviceIndex = (currentOutputDeviceIndex + 1) % outputDevices.Count;
        UpdateOutputDeviceDisplay();
        await VivoxLobbyManager.Instance.SetOutputDevice(outputDevices[currentOutputDeviceIndex].DeviceID);
    }

    private async void PreviousOutputDevice()
    {
        if (outputDevices.Count == 0) return;
        currentOutputDeviceIndex = (currentOutputDeviceIndex - 1 + outputDevices.Count) % outputDevices.Count;
        UpdateOutputDeviceDisplay();
        await VivoxLobbyManager.Instance.SetOutputDevice(outputDevices[currentOutputDeviceIndex].DeviceID);
    }

    private void UpdateDeviceDisplays()
    {
        UpdateInputDeviceDisplay();
        UpdateOutputDeviceDisplay();
    }

    private void UpdateInputDeviceDisplay()
    {
        if (currentInputDeviceText != null && inputDevices.Count > 0)
        {
            currentInputDeviceText.text = inputDevices[currentInputDeviceIndex].DeviceName;
        }
    }

    private void UpdateOutputDeviceDisplay()
    {
        if (currentOutputDeviceText != null && outputDevices.Count > 0)
        {
            currentOutputDeviceText.text = outputDevices[currentOutputDeviceIndex].DeviceName;
        }
    }

    private void OnInputVolumeChanged(float value)
    {
        // Mapear de 0-1 a -50 a +50
        int volumeDb = Mathf.RoundToInt(value * 100f - 50f);
        VivoxLobbyManager.Instance.SetInputDeviceVolume(volumeDb);
    }

    private void OnMuteToggleChanged(bool isMuted)
    {
        if (isMuted)
        {
            VivoxLobbyManager.Instance.MuteInputDevice();
        }
        else
        {
            VivoxLobbyManager.Instance.UnmuteInputDevice();
        }

        // Desactivar el slider si está muteado
        if (inputVolumeSlider != null) inputVolumeSlider.interactable = !isMuted;
    }

    private void CloseWindow()
    {
        if (voiceChatWindowController != null)
            voiceChatWindowController.HideWindow();
    }

    private async void UpdatePlayerInfo()
    {
        if (playerNameText != null)
        {
            playerNameText.text = await AuthenticationService.Instance.GetPlayerNameAsync();
        }

        // Cargar icono del perfil
        if (playerIconImage != null && playerIcons != null)
        {
            var profileData = await CloudSaveManager.Instance.LoadPlayerProfileAsync();
            if (profileData != null)
            {
                playerIconImage.sprite = playerIcons.GetIcon(profileData.iconIndex);
            }
        }
    }

    private void UpdateOtherPlayers()
    {
        // Limpiar items anteriores
        foreach (VoiceChatPlayerItemUI item in playerItems)
        {
            if (item != null && item.gameObject != null)
                Destroy(item.gameObject);
        }
        playerItems.Clear();

        // Obtener otros jugadores del canal de voz (excluyendo al local)
        var otherPlayers = VivoxLobbyManager.Instance.GetOtherVoiceParticipants();
        foreach (var player in otherPlayers)
        {
            GameObject playerItemObj = Instantiate(playerVoiceItemPrefab, content);
            VoiceChatPlayerItemUI playerItem = playerItemObj.GetComponent<VoiceChatPlayerItemUI>();
            if (playerItem != null)
            {

                playerItem.Initialize(player);
                playerItems.Add(playerItem);
            }
        }
    }
}