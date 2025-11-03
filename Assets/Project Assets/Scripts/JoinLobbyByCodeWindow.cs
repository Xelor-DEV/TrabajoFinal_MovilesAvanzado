using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class JoinLobbyByCodeWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField lobbyCodeInput;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button closeButton;

    [Header("Window References")]
    [SerializeField] private WindowsController joinLobbyByCodeWindow;
    [SerializeField] private WindowsController otherModesWindow;

    private void OnEnable()
    {
        joinLobbyButton.onClick.AddListener(JoinLobby);
        closeButton.onClick.AddListener(CloseWindow);

        lobbyCodeInput.text = "";
        joinLobbyButton.interactable = true;
    }

    private void OnDisable()
    {
        joinLobbyButton.onClick.RemoveListener(JoinLobby);
        closeButton.onClick.RemoveListener(CloseWindow);
    }

    private async void JoinLobby()
    {
        string lobbyCode = lobbyCodeInput.text.Trim();
        if (string.IsNullOrEmpty(lobbyCode))
        {
            Debug.LogError("Lobby code is empty");
            return;
        }

        joinLobbyButton.interactable = false;

        bool success = await LobbyServiceManager.Instance.JoinLobbyByCode(lobbyCode);

        joinLobbyButton.interactable = true;

        if (!success)
        {
            // Show error message
            Debug.LogError("Failed to join lobby");
        }
    }

    private void CloseWindow()
    {
        joinLobbyByCodeWindow.HideWindow();
        otherModesWindow.ShowWindow();
    }
}