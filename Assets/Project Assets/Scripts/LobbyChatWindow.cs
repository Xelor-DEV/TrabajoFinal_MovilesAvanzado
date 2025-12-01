using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

public class LobbyChatWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private TMP_InputField messageInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Animation Settings")]
    [SerializeField] private float messageAppearDuration = 0.3f;

    [Header("References")]
    [SerializeField] private WindowsController lobbyChatWindow;

    private List<LobbyChatMessageUI> messageInstances = new List<LobbyChatMessageUI>();

    private void Start()
    {
        Debug.Log("LobbyChatWindow: OnEnable - Subscribing to events");

        // Asegurar que VivoxLobbyManager esté inicializado
        if (VivoxLobbyManager.Instance == null)
        {
            Debug.LogError("LobbyChatWindow: VivoxLobbyManager instance is null!");
            return;
        }

        // Forzar suscripción a eventos en VivoxLobbyManager
        VivoxLobbyManager.Instance.DebugSubscriptionStatus();

        // Suscribirse a eventos de mensajes directos también
        VivoxLobbyManager.Instance.LobbyChatMessageReceived += OnLobbyChatMessageReceived;
        VivoxLobbyManager.Instance.DirectMessageReceived += OnDirectMessageReceived;
        VivoxLobbyManager.Instance.OnDirectMessageSent += OnDirectMessageSent;
        VivoxLobbyManager.Instance.OnLobbyChannelChanged += OnLobbyChannelChanged;
        VivoxLobbyManager.Instance.OnLobbyChannelLeft += OnLobbyChannelLeft;

        // Configurar botones
        sendButton.onClick.AddListener(SendMessage);
        closeButton.onClick.AddListener(CloseWindow);

        // Limpiar input field
        messageInputField.text = "";

        // Focus en el input field
        messageInputField.Select();
        messageInputField.ActivateInputField();
    }

    private void OnDisable()
    {
        Debug.Log("LobbyChatWindow: OnDisable - Unsubscribing from events");

        if (VivoxLobbyManager.Instance != null)
        {
            VivoxLobbyManager.Instance.LobbyChatMessageReceived -= OnLobbyChatMessageReceived;
            VivoxLobbyManager.Instance.DirectMessageReceived -= OnDirectMessageReceived;
            VivoxLobbyManager.Instance.OnDirectMessageSent -= OnDirectMessageSent;
            VivoxLobbyManager.Instance.OnLobbyChannelChanged -= OnLobbyChannelChanged;
            VivoxLobbyManager.Instance.OnLobbyChannelLeft -= OnLobbyChannelLeft;
        }

        sendButton.onClick.RemoveListener(SendMessage);
        closeButton.onClick.RemoveListener(CloseWindow);
    }

    private void OnLobbyChannelChanged(string newChannelName)
    {
        Debug.Log($"LobbyChatWindow: Channel changed to {newChannelName}");
        // Limpiar chat cuando se cambia a un nuevo canal
        ClearChat();
    }

    private void OnLobbyChannelLeft(string channelName)
    {
        Debug.Log($"LobbyChatWindow: Left channel {channelName}");
        // Limpiar chat cuando se sale del canal
        ClearChat();
    }

    private void OnLobbyChatMessageReceived(VivoxMessage message)
    {
        Debug.Log($"LobbyChatWindow: Received message from {message.SenderDisplayName}: {message.MessageText}");

        // Solo procesar mensajes del canal actual
        if (message.ChannelName == VivoxLobbyManager.Instance.CurrentLobbyChannel)
        {
            AddMessageToChat(message.SenderDisplayName, message.MessageText);
            ScrollToBottom();
        }
        else
        {
            Debug.LogWarning($"LobbyChatWindow: Ignoring message from different channel. Current: {VivoxLobbyManager.Instance.CurrentLobbyChannel}, Message Channel: {message.ChannelName}");
        }
    }

    private void AddMessageToChat(string senderName, string messageText)
    {
        Debug.Log($"LobbyChatWindow: Adding message to chat - {senderName}: {messageText}");

        if (chatMessagePrefab == null || content == null)
        {
            Debug.LogError("LobbyChatWindow: chatMessagePrefab or content is null!");
            return;
        }

        GameObject newMessage = Instantiate(chatMessagePrefab, content);
        LobbyChatMessageUI messageUI = newMessage.GetComponent<LobbyChatMessageUI>();

        if (messageUI != null)
        {
            messageUI.Initialize(senderName, messageText);
            messageInstances.Add(messageUI);

            // Animación de aparición
            newMessage.transform.localScale = Vector3.zero;
            newMessage.transform.DOScale(Vector3.one, messageAppearDuration).SetEase(Ease.OutBack);
        }
        else
        {
            Debug.LogError("LobbyChatWindow: LobbyChatMessageUI component not found on prefab!");
        }
    }

    private async void SendMessage()
    {
        string message = messageInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // Comando para mostrar jugadores disponibles
        if (message.Equals("/players", StringComparison.OrdinalIgnoreCase) ||
            message.Equals("/list", StringComparison.OrdinalIgnoreCase))
        {
            ShowAvailablePlayers();
            messageInputField.text = "";
            return;
        }

        Debug.Log($"LobbyChatWindow: Sending message: {message}");

        await VivoxLobbyManager.Instance.SendLobbyMessage(message);
        messageInputField.text = "";

        messageInputField.Select();
        messageInputField.ActivateInputField();
    }

    private void OnDirectMessageReceived(VivoxMessage message)
    {
        Debug.Log($"LobbyChatWindow: Received direct message from {message.SenderDisplayName}: {message.MessageText}");

        // Mostrar mensaje directo con formato especial
        AddDirectMessageToChat(message.SenderDisplayName, message.MessageText, false);
        ScrollToBottom();
    }

    private void OnDirectMessageSent(string targetPlayer, string message)
    {
        Debug.Log($"LobbyChatWindow: Sent direct message to {targetPlayer}: {message}");

        // Mostrar mensaje directo enviado con formato especial
        AddDirectMessageToChat(targetPlayer, message, true);
        ScrollToBottom();
    }

    private void AddDirectMessageToChat(string playerName, string messageText, bool isSentByMe)
    {
        Debug.Log($"LobbyChatWindow: Adding direct message to chat - {playerName}: {messageText}");

        if (chatMessagePrefab == null || content == null)
        {
            Debug.LogError("LobbyChatWindow: chatMessagePrefab or content is null!");
            return;
        }

        GameObject newMessage = Instantiate(chatMessagePrefab, content);
        LobbyChatMessageUI messageUI = newMessage.GetComponent<LobbyChatMessageUI>();

        if (messageUI != null)
        {
            string prefix = isSentByMe ? "[DM to " : "[DM from ";
            string suffix = "]";

            messageUI.Initialize($"{prefix}{playerName}{suffix}", messageText, Color.magenta);
            messageInstances.Add(messageUI);

            // Animación de aparición
            newMessage.transform.localScale = Vector3.zero;
            newMessage.transform.DOScale(Vector3.one, messageAppearDuration).SetEase(Ease.OutBack);
        }
        else
        {
            Debug.LogError("LobbyChatWindow: LobbyChatMessageUI component not found on prefab!");
        }
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void CloseWindow()
    {
        if (lobbyChatWindow != null)
        {
            lobbyChatWindow.HideWindow();
        }
    }

    public void ClearChat()
    {
        Debug.Log("LobbyChatWindow: Clearing chat");
        foreach (LobbyChatMessageUI message in messageInstances)
        {
            if (message != null && message.gameObject != null)
                Destroy(message.gameObject);
        }
        messageInstances.Clear();
    }

    private void AddSystemMessageToChat(string message, Color color)
    {
        if (chatMessagePrefab == null || content == null)
        {
            Debug.LogError("LobbyChatWindow: chatMessagePrefab or content is null!");
            return;
        }

        GameObject newMessage = Instantiate(chatMessagePrefab, content);
        LobbyChatMessageUI messageUI = newMessage.GetComponent<LobbyChatMessageUI>();

        if (messageUI != null)
        {
            messageUI.Initialize("[System]", message, color);
            messageInstances.Add(messageUI);

            newMessage.transform.localScale = Vector3.zero;
            newMessage.transform.DOScale(Vector3.one, messageAppearDuration).SetEase(Ease.OutBack);
        }
    }

    public void ShowAvailablePlayers()
    {
        var availablePlayers = VivoxLobbyManager.Instance.GetAvailablePlayerNames();
        if (availablePlayers != null && availablePlayers.Count > 0)
        {
            string playerList = "Available players: " + string.Join(", ", availablePlayers);
            AddSystemMessageToChat(playerList, Color.cyan);
            ScrollToBottom();
        }
    }
}