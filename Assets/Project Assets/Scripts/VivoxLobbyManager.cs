using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;

public class VivoxLobbyManager : NonPersistentSingleton<VivoxLobbyManager>
{
    private string currentLobbyChannelName;

    public bool IsInLobbyChannel => !string.IsNullOrEmpty(currentLobbyChannelName);
    public string CurrentLobbyChannel => currentLobbyChannelName;

    private void Start() 
    {
        if (VivoxService.Instance != null)
        {
            VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;
        }
    }

    private void OnDestroy()
    {
        if (VivoxService.Instance != null)
        {
            VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
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

            // Salir del canal anterior si existe
            if (!string.IsNullOrEmpty(currentLobbyChannelName))
            {
                await LeaveLobbyChannel();
            }

            currentLobbyChannelName = "lobby_" + lobbyId;
            await VivoxService.Instance.JoinGroupChannelAsync(currentLobbyChannelName, ChatCapability.TextOnly);

            // Disparar evento de cambio de canal
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
            Debug.Log($"Left lobby text channel: {currentLobbyChannelName}");
            currentLobbyChannelName = null;

            // Disparar evento de salida del canal
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

        try
        {
            MessageOptions messageOptions = new MessageOptions
            {
                Metadata = JsonUtility.ToJson(new Dictionary<string, string> { { "Region", "Kalindor" } })
            };

            await VivoxService.Instance.SendChannelTextMessageAsync(currentLobbyChannelName, message, messageOptions);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send lobby message failed: {ex.Message}");
        }
    }

    private void OnChannelMessageReceived(VivoxMessage message)
    {
        if (message.ChannelName == currentLobbyChannelName)
        {
            // Disparar evento para que las UI lo capturen
            LobbyChatMessageReceived?.Invoke(message);
        }
    }

    // Eventos
    public event Action<VivoxMessage> LobbyChatMessageReceived;
    public event Action<string> OnLobbyChannelChanged; // Nuevo canal
    public event Action<string> OnLobbyChannelLeft; // Canal abandonado
}