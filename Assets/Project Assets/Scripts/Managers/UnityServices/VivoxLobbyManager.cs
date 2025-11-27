using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Lobbies.Models;

public class VivoxLobbyManager : NonPersistentSingleton<VivoxLobbyManager>
{
    private string currentLobbyChannelName;
    private bool isSubscribedToEvents = false;

    // Diccionario para mapear display names a playerIds
    private Dictionary<string, string> playerDisplayNameToIdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool IsInLobbyChannel => !string.IsNullOrEmpty(currentLobbyChannelName);
    public string CurrentLobbyChannel => currentLobbyChannelName;

    private void Start()
    {
        SubscribeToVivoxEvents();
    }

    private void SubscribeToVivoxEvents()
    {
        if (isSubscribedToEvents) return;

        try
        {
            VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;
            VivoxService.Instance.DirectedMessageReceived += OnDirectedMessageReceived;
            isSubscribedToEvents = true;
            Debug.Log("VivoxLobbyManager: Successfully subscribed to Vivox events");
        }
        catch (Exception ex)
        {
            Debug.LogError($"VivoxLobbyManager: Failed to subscribe to Vivox events: {ex.Message}");
        }
    }

    private void UnsubscribeFromVivoxEvents()
    {
        if (!isSubscribedToEvents) return;

        try
        {
            VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
            VivoxService.Instance.DirectedMessageReceived -= OnDirectedMessageReceived;
            isSubscribedToEvents = false;
            Debug.Log("VivoxLobbyManager: Unsubscribed from Vivox events");
        }
        catch (Exception ex)
        {
            Debug.LogError($"VivoxLobbyManager: Failed to unsubscribe from Vivox events: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromVivoxEvents();
        ClearPlayerMap();
    }

    // Método para actualizar el mapeo de jugadores
    public void UpdatePlayerMap(Lobby lobby)
    {
        if (lobby?.Players == null) return;

        lock (playerDisplayNameToIdMap)
        {
            playerDisplayNameToIdMap.Clear();

            foreach (var player in lobby.Players)
            {
                if (player.Data != null && player.Data.ContainsKey("PlayerName"))
                {
                    string displayName = player.Data["PlayerName"].Value;
                    if (!string.IsNullOrEmpty(displayName) && !playerDisplayNameToIdMap.ContainsKey(displayName))
                    {
                        playerDisplayNameToIdMap[displayName] = player.Id;
                        Debug.Log($"Mapped player: {displayName} -> {player.Id}");

                        // Actualizar también el icono si está disponible
                        if (player.Data.TryGetValue("IconIndex", out var iconData))
                        {
                            Debug.Log($"Player {displayName} has icon index: {iconData.Value}");
                        }
                    }
                }
            }

            Debug.Log($"Player map updated with {playerDisplayNameToIdMap.Count} players");
        }
    }

    public void ClearPlayerMap()
    {
        lock (playerDisplayNameToIdMap)
        {
            playerDisplayNameToIdMap.Clear();
        }
    }

    private string GetPlayerIdByDisplayName(string displayName)
    {
        lock (playerDisplayNameToIdMap)
        {
            if (playerDisplayNameToIdMap.TryGetValue(displayName, out string playerId))
            {
                return playerId;
            }
        }

        Debug.LogWarning($"Player with display name '{displayName}' not found in map");
        return null;
    }

    public List<string> GetAvailablePlayerNames()
    {
        lock (playerDisplayNameToIdMap)
        {
            return playerDisplayNameToIdMap.Keys.ToList();
        }
    }

    public async Task<bool> LoginVivox()
    {
        try
        {
            if (VivoxService.Instance.IsLoggedIn)
                return true;

            string nickName = await AuthenticationService.Instance.GetPlayerNameAsync();
            if (string.IsNullOrEmpty(nickName))
            {
                nickName = "Player_" + UnityEngine.Random.Range(1000, 9999);
            }

            LoginOptions loginOptions = new LoginOptions
            {
                DisplayName = nickName
            };

            await VivoxService.Instance.LoginAsync(loginOptions);
            Debug.Log("Vivox login successful: " + nickName);

            SubscribeToVivoxEvents();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Vivox login failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateAndJoinLobbyChannel(string lobbyId)
    {
        try
        {
            if (!VivoxService.Instance.IsLoggedIn)
            {
                bool loginSuccess = await LoginVivox();
                if (!loginSuccess) return false;
            }

            if (!string.IsNullOrEmpty(currentLobbyChannelName))
            {
                await LeaveLobbyChannel();
            }

            currentLobbyChannelName = "lobby_" + lobbyId;
            await VivoxService.Instance.JoinGroupChannelAsync(currentLobbyChannelName, ChatCapability.TextOnly);

            await JoinLobbyVoiceChannel(lobbyId);

            SubscribeToVivoxEvents();
            OnLobbyChannelChanged?.Invoke(currentLobbyChannelName);

            Debug.Log($"Joined lobby text channel: {currentLobbyChannelName}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Create/Join lobby channel failed: {ex.Message}");
            currentLobbyChannelName = null;
            return false;
        }
    }

    public async Task LeaveLobbyChannel()
    {
        if (string.IsNullOrEmpty(currentLobbyChannelName)) return;

        try
        {
            string oldChannel = currentLobbyChannelName;
            await VivoxService.Instance.LeaveChannelAsync(currentLobbyChannelName);

            // Salir del canal de voz
            await LeaveLobbyVoiceChannel();

            Debug.Log($"Left lobby text channel: {currentLobbyChannelName}");
            currentLobbyChannelName = null;

            ClearPlayerMap();
            OnLobbyChannelLeft?.Invoke(oldChannel);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Leave lobby channel failed: {ex.Message}");
        }
    }

    public async Task SendLobbyMessage(string message)
    {
        if (string.IsNullOrEmpty(currentLobbyChannelName) || !VivoxService.Instance.IsLoggedIn) return;

        if (IsDirectMessageCommand(message))
        {
            await ProcessDirectMessageCommand(message);
            return;
        }

        try
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(currentLobbyChannelName, message);
            Debug.Log($"Message sent to channel {currentLobbyChannelName}: {message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send lobby message failed: {ex.Message}");
        }
    }

    public async Task SendDirectMessage(string message, string targetDisplayName)
    {
        if (!VivoxService.Instance.IsLoggedIn || string.IsNullOrEmpty(message)) return;

        try
        {
            // Obtener el PlayerId del display name
            string targetPlayerId = GetPlayerIdByDisplayName(targetDisplayName);
            if (string.IsNullOrEmpty(targetPlayerId))
            {
                Debug.LogError($"Cannot send direct message: Player '{targetDisplayName}' not found in lobby");
                return;
            }

            // Verificar que no estamos enviando un mensaje a nosotros mismos
            string currentPlayerId = AuthenticationService.Instance.PlayerId;
            if (targetPlayerId == currentPlayerId)
            {
                Debug.LogError("Cannot send direct message to yourself");
                return;
            }

            await VivoxService.Instance.SendDirectTextMessageAsync(targetPlayerId, message);
            Debug.Log($"Direct message sent to {targetDisplayName} ({targetPlayerId}): {message}");

            // Mostrar el mensaje directo localmente para el remitente
            OnDirectMessageSent?.Invoke(targetDisplayName, message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send direct message failed: {ex.Message}");
        }
    }

    private bool IsDirectMessageCommand(string message)
    {
        if (string.IsNullOrEmpty(message)) return false;

        string trimmedMessage = message.Trim();
        return trimmedMessage.StartsWith("/directmessage", StringComparison.OrdinalIgnoreCase) ||
               trimmedMessage.StartsWith("/dm", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ProcessDirectMessageCommand(string command)
    {
        try
        {
            // Formato: /DirectMessage "playername" "message"
            // O formato corto: /dm "playername" "message"

            string[] parts = command.Split('"');
            if (parts.Length < 4)
            {
                Debug.LogError("Invalid direct message format. Use: /DirectMessage \"playername\" \"message\"");
                return;
            }

            string targetDisplayName = parts[1].Trim();
            string directMessage = parts[3].Trim();

            if (string.IsNullOrEmpty(targetDisplayName) || string.IsNullOrEmpty(directMessage))
            {
                Debug.LogError("Player name or message cannot be empty");
                return;
            }

            await SendDirectMessage(directMessage, targetDisplayName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to process direct message command: {ex.Message}");
        }
    }

    private void OnChannelMessageReceived(VivoxMessage message)
    {
        Debug.Log($"VivoxLobbyManager: Message received in channel {message.ChannelName} from {message.SenderDisplayName}: {message.MessageText}");

        if (message.ChannelName == currentLobbyChannelName)
        {
            Debug.Log($"VivoxLobbyManager: Dispatching message to UI - Channel: {message.ChannelName}, Sender: {message.SenderDisplayName}, Message: {message.MessageText}");
            LobbyChatMessageReceived?.Invoke(message);
        }
        else
        {
            Debug.LogWarning($"VivoxLobbyManager: Message from different channel. Current: {currentLobbyChannelName}, Message Channel: {message.ChannelName}");
        }
    }

    private void OnDirectedMessageReceived(VivoxMessage message)
    {
        Debug.Log($"VivoxLobbyManager: Direct message received from {message.SenderDisplayName}: {message.MessageText}");
        DirectMessageReceived?.Invoke(message);
    }

    // Eventos
    public event Action<VivoxMessage> LobbyChatMessageReceived;
    public event Action<VivoxMessage> DirectMessageReceived;
    public event Action<string, string> OnDirectMessageSent;
    public event Action<string> OnLobbyChannelChanged;
    public event Action<string> OnLobbyChannelLeft;

    private async Task JoinLobbyVoiceChannel(string lobbyId)
    {
        try
        {
            string voiceChannelName = "voice_lobby_" + lobbyId;
            await VivoxService.Instance.JoinGroupChannelAsync(voiceChannelName, ChatCapability.AudioOnly);
            Debug.Log($"Joined lobby voice channel: {voiceChannelName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Join lobby voice channel failed: {ex.Message}");
        }
    }

    private async Task LeaveLobbyVoiceChannel()
    {
        try
        {
            if (string.IsNullOrEmpty(currentLobbyChannelName)) return;

            string voiceChannelName = "voice_lobby_" + currentLobbyChannelName.Replace("lobby_", "");
            await VivoxService.Instance.LeaveChannelAsync(voiceChannelName);
            Debug.Log($"Left lobby voice channel: {voiceChannelName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Leave lobby voice channel failed: {ex.Message}");
        }
    }

    // Métodos para dispositivos y volumen
    public IEnumerable<VivoxInputDevice> GetAvailableInputDevices()
    {
        return VivoxService.Instance.AvailableInputDevices;
    }

    public IEnumerable<VivoxOutputDevice> GetAvailableOutputDevices()
    {
        return VivoxService.Instance.AvailableOutputDevices;
    }


    // Métodos de dispositivos y volumen corregidos
    public async Task SetInputDevice(string deviceId)
    {
        try
        {
            var inputDevice = VivoxService.Instance.AvailableInputDevices.FirstOrDefault(x => x.DeviceID.Equals(deviceId));
            if (inputDevice != null)
            {
                await VivoxService.Instance.SetActiveInputDeviceAsync(inputDevice);
                Debug.Log($"Input device set to: {inputDevice.DeviceName}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SetInputDevice failed: {ex.Message}");
        }
    }

    public async Task SetOutputDevice(string deviceId)
    {
        try
        {
            var outputDevice = VivoxService.Instance.AvailableOutputDevices.FirstOrDefault(x => x.DeviceID.Equals(deviceId));
            if (outputDevice != null)
            {
                await VivoxService.Instance.SetActiveOutputDeviceAsync(outputDevice);
                Debug.Log($"Output device set to: {outputDevice.DeviceName}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SetOutputDevice failed: {ex.Message}");
        }
    }

    public void SetInputDeviceVolume(int volumeDb)
    {
        try
        {
            VivoxService.Instance.SetInputDeviceVolume(volumeDb);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SetInputDeviceVolume failed: {ex.Message}");
        }
    }

    public void SetOutputDeviceVolume(int volumeDb)
    {
        try
        {
            VivoxService.Instance.SetOutputDeviceVolume(volumeDb);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SetOutputDeviceVolume failed: {ex.Message}");
        }
    }

    public void SetParticipantVolume(VivoxParticipant participant, int volume)
    {
        try
        {
            participant.SetLocalVolume(volume);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SetParticipantVolume failed: {ex.Message}");
        }
    }

    public void MuteInputDevice()
    {
        try
        {
            VivoxService.Instance.MuteInputDevice();
            Debug.Log("Input device muted");
        }
        catch (Exception ex)
        {
            Debug.LogError($"MuteInputDevice failed: {ex.Message}");
        }
    }

    public void UnmuteInputDevice()
    {
        try
        {
            VivoxService.Instance.UnmuteInputDevice();
            Debug.Log("Input device unmuted");
        }
        catch (Exception ex)
        {
            Debug.LogError($"UnmuteInputDevice failed: {ex.Message}");
        }
    }

    public List<VivoxParticipant> GetOtherVoiceParticipants()
    {
        var otherParticipants = new List<VivoxParticipant>();

        foreach (var channel in VivoxService.Instance.ActiveChannels)
        {
            if (channel.Key.Contains("voice_lobby_"))
            {
                foreach (var participant in channel.Value)
                {
                    // Excluir al jugador local
                    if (participant.PlayerId != AuthenticationService.Instance.PlayerId)
                    {
                        otherParticipants.Add(participant);
                    }
                }
            }
        }

        return otherParticipants;
    }

    public void DebugSubscriptionStatus()
    {
        Debug.Log($"VivoxLobbyManager Debug - isSubscribedToEvents: {isSubscribedToEvents}, CurrentChannel: {currentLobbyChannelName}, IsLoggedIn: {VivoxService.Instance.IsLoggedIn}");
    }
}