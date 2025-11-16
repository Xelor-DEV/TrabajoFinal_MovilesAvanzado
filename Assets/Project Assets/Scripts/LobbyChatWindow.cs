using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Services.Vivox;

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

        // Suscribirse a eventos de mensajes y cambios de canal
        VivoxLobbyManager.Instance.LobbyChatMessageReceived += OnLobbyChatMessageReceived;
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

        Debug.Log($"LobbyChatWindow: Sending message: {message}");

        await VivoxLobbyManager.Instance.SendLobbyMessage(message);
        messageInputField.text = "";

        // Mantener focus en el input field
        messageInputField.Select();
        messageInputField.ActivateInputField();
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

    // Para enviar mensaje con Enter
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (messageInputField.isFocused && !string.IsNullOrEmpty(messageInputField.text.Trim()))
            {
                SendMessage();
            }
        }
    }
}