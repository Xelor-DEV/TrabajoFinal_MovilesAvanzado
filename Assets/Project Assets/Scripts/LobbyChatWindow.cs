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

    private void OnEnable()
    {
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
        // Limpiar chat cuando se cambia a un nuevo canal
        ClearChat();
        Debug.Log($"Chat window: Switched to new channel {newChannelName}");
    }

    private void OnLobbyChannelLeft(string channelName)
    {
        // Limpiar chat cuando se sale del canal
        ClearChat();
        Debug.Log($"Chat window: Left channel {channelName}");
    }

    private void OnLobbyChatMessageReceived(VivoxMessage message)
    {
        // Solo procesar mensajes del canal actual
        if (message.ChannelName == VivoxLobbyManager.Instance.CurrentLobbyChannel)
        {
            AddMessageToChat(message.SenderDisplayName, message.MessageText);
            ScrollToBottom();
        }
    }

    private void AddMessageToChat(string senderName, string messageText)
    {
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
    }

    private async void SendMessage()
    {
        string message = messageInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

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
        foreach (LobbyChatMessageUI message in messageInstances)
        {
            if (message != null && message.gameObject != null)
                Destroy(message.gameObject);
        }
        messageInstances.Clear();
    }
}