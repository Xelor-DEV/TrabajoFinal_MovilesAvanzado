using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Services.Vivox;

public class LobbyChatNotification : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform notificationPanel;
    [SerializeField] private TMP_Text senderNameText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button openChatButton;

    [Header("Animation Settings")]
    [SerializeField] private float popInDuration = 0.3f;
    [SerializeField] private float popOutDuration = 0.2f;
    [SerializeField] private float displayDuration = 4f;

    [Header("Chat Window Reference")]
    [SerializeField] private WindowsController chatWindowController;

    private Sequence currentAnimationSequence;
    private bool isNotificationActive = false;

    private void Start()
    {
        // Inicialmente desactivado
        notificationPanel.gameObject.SetActive(false);

        // Configurar botón
        openChatButton.onClick.AddListener(OpenChatWindow);
    }

    private void OnEnable()
    {
        VivoxLobbyManager.Instance.LobbyChatMessageReceived += OnNewMessageReceived;
        VivoxLobbyManager.Instance.OnLobbyChannelChanged += OnLobbyChannelChanged;
        VivoxLobbyManager.Instance.OnLobbyChannelLeft += OnLobbyChannelLeft;
    }

    private void OnDisable()
    {
        if (VivoxLobbyManager.Instance != null)
        {
            VivoxLobbyManager.Instance.LobbyChatMessageReceived -= OnNewMessageReceived;
            VivoxLobbyManager.Instance.OnLobbyChannelChanged -= OnLobbyChannelChanged;
            VivoxLobbyManager.Instance.OnLobbyChannelLeft -= OnLobbyChannelLeft;
        }

        currentAnimationSequence?.Kill();
    }

    private void OnLobbyChannelChanged(string newChannelName)
    {
        // Ocultar notificación cuando se cambia de canal
        HideNotificationImmediate();
        Debug.Log($"Chat notification: Switched to new channel {newChannelName}");
    }

    private void OnLobbyChannelLeft(string channelName)
    {
        // Ocultar notificación cuando se sale del canal
        HideNotificationImmediate();
        Debug.Log($"Chat notification: Left channel {channelName}");
    }

    private void OnNewMessageReceived(VivoxMessage message)
    {
        // Solo mostrar notificaciones del canal actual
        if (message.ChannelName == VivoxLobbyManager.Instance.CurrentLobbyChannel)
        {
            ShowMessageNotification(message.SenderDisplayName, message.MessageText);
        }
    }

    public void ShowMessageNotification(string senderName, string message)
    {
        // Actualizar textos
        senderNameText.text = senderName;
        messageText.text = message;

        // Cancelar animación anterior
        currentAnimationSequence?.Kill();

        // Configurar nueva animación
        currentAnimationSequence = DOTween.Sequence();

        if (!isNotificationActive)
        {
            // Primera vez: Pop In
            notificationPanel.gameObject.SetActive(true);
            notificationPanel.localScale = Vector3.zero;
            isNotificationActive = true;

            currentAnimationSequence
                .Append(notificationPanel.DOScale(Vector3.one, popInDuration).SetEase(Ease.OutBack))
                .AppendInterval(displayDuration)
                .Append(notificationPanel.DOScale(Vector3.zero, popOutDuration).SetEase(Ease.InBack))
                .OnComplete(() => {
                    notificationPanel.gameObject.SetActive(false);
                    isNotificationActive = false;
                });
        }
        else
        {
            // Ya está activo: Pop Out -> Cambiar texto -> Pop In
            currentAnimationSequence
                .Append(notificationPanel.DOScale(Vector3.zero, popOutDuration).SetEase(Ease.InBack))
                .AppendCallback(() => {
                    // Actualizar textos durante la animación
                    senderNameText.text = senderName;
                    messageText.text = message;
                })
                .Append(notificationPanel.DOScale(Vector3.one, popInDuration).SetEase(Ease.OutBack))
                .AppendInterval(displayDuration)
                .Append(notificationPanel.DOScale(Vector3.zero, popOutDuration).SetEase(Ease.InBack))
                .OnComplete(() => {
                    notificationPanel.gameObject.SetActive(false);
                    isNotificationActive = false;
                });
        }
    }

    private void OpenChatWindow()
    {
        if (chatWindowController != null)
        {
            chatWindowController.ShowWindow();
        }

        // Ocultar notificación inmediatamente
        HideNotificationImmediate();
    }

    private void HideNotificationImmediate()
    {
        currentAnimationSequence?.Kill();
        notificationPanel.gameObject.SetActive(false);
        isNotificationActive = false;
    }

    // Método público para ocultar desde otros scripts si es necesario
    public void HideNotification()
    {
        HideNotificationImmediate();
    }
}