using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;

public class VivoxLobbyManager : NonPersistentSingleton<VivoxLobbyManager>
{
    private string currentLobbyChannelName;
    private bool isSubscribedToEvents = false;

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

            // Resuscribir eventos después del login por si acaso
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

            // Salir del canal anterior si existe
            if (!string.IsNullOrEmpty(currentLobbyChannelName))
            {
                await LeaveLobbyChannel();
            }

            currentLobbyChannelName = "lobby_" + lobbyId;
            await VivoxService.Instance.JoinGroupChannelAsync(currentLobbyChannelName, ChatCapability.TextOnly);

            // Asegurar suscripción a eventos después de unirse al canal
            SubscribeToVivoxEvents();

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
            Debug.Log($"Message sent to channel {currentLobbyChannelName}: {message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send lobby message failed: {ex.Message}");
        }
    }

    private void OnChannelMessageReceived(VivoxMessage message)
    {
        Debug.Log($"VivoxLobbyManager: Message received in channel {message.ChannelName} from {message.SenderDisplayName}: {message.MessageText}");

        // Verificar que el mensaje sea del canal actual
        if (message.ChannelName == currentLobbyChannelName)
        {
            Debug.Log($"VivoxLobbyManager: Dispatching message to UI - Channel: {message.ChannelName}, Sender: {message.SenderDisplayName}, Message: {message.MessageText}");

            // Disparar evento para que las UI lo capturen
            LobbyChatMessageReceived?.Invoke(message);
        }
        else
        {
            Debug.LogWarning($"VivoxLobbyManager: Message from different channel. Current: {currentLobbyChannelName}, Message Channel: {message.ChannelName}");
        }
    }

    // Eventos
    public event Action<VivoxMessage> LobbyChatMessageReceived;
    public event Action<string> OnLobbyChannelChanged;
    public event Action<string> OnLobbyChannelLeft;

    // Método para debuggear el estado de las suscripciones
    public void DebugSubscriptionStatus()
    {
        Debug.Log($"VivoxLobbyManager Debug - isSubscribedToEvents: {isSubscribedToEvents}, CurrentChannel: {currentLobbyChannelName}, IsLoggedIn: {VivoxService.Instance.IsLoggedIn}");
    }

    // En VivoxLobbyManager, añade este método temporal
    public async void TestMessageReception()
    {
        Debug.Log("=== Testing Message Reception ===");
        Debug.Log($"Current Channel: {currentLobbyChannelName}");
        Debug.Log($"Is Subscribed: {isSubscribedToEvents}");
        Debug.Log($"Is Logged In: {VivoxService.Instance.IsLoggedIn}");

        // Enviar un mensaje de prueba
        if (!string.IsNullOrEmpty(currentLobbyChannelName))
        {
            await SendLobbyMessage("TEST MESSAGE FROM HOST");
            Debug.Log("Test message sent");
        }
    }
}