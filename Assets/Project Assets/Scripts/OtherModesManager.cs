using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class OtherModesManager : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button listLobbiesButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyByCodeButton;

    [Header("Window References")]
    [SerializeField] private WindowsController otherModesWindow;
    [SerializeField] private WindowsController listLobbiesWindow;
    [SerializeField] private WindowsController createLobbyWindow;
    [SerializeField] private WindowsController joinLobbyByCodeWindow;
    [SerializeField] private WindowsController currentLobbyWindow;

    private void OnEnable()
    {
        listLobbiesButton.onClick.AddListener(ShowListLobbiesWindow);
        createLobbyButton.onClick.AddListener(ShowCreateLobbyWindow);
        joinLobbyByCodeButton.onClick.AddListener(ShowJoinLobbyByCodeWindow);

        // Subscribe to lobby events
        LobbyServiceManager.Instance.OnLobbyUpdated += HandleLobbyJoined;
        LobbyServiceManager.Instance.OnLobbyLeft += HandleLobbyLeft;
    }

    private void OnDisable()
    {
        listLobbiesButton.onClick.RemoveListener(ShowListLobbiesWindow);
        createLobbyButton.onClick.RemoveListener(ShowCreateLobbyWindow);
        joinLobbyByCodeButton.onClick.RemoveListener(ShowJoinLobbyByCodeWindow);

        if (LobbyServiceManager.Instance != null)
        {
            LobbyServiceManager.Instance.OnLobbyUpdated -= HandleLobbyJoined;
            LobbyServiceManager.Instance.OnLobbyLeft -= HandleLobbyLeft;
        }
    }

    private void HandleLobbyLeft()
    {
        // Cuando se sale del lobby, mostrar la ventana de otros modos
        otherModesWindow.ShowWindow();
        listLobbiesWindow.HideWindow();
        createLobbyWindow.HideWindow();
        joinLobbyByCodeWindow.HideWindow();
        currentLobbyWindow.HideWindow();
    }

    private void ShowListLobbiesWindow()
    {
        otherModesWindow.HideWindow();
        listLobbiesWindow.ShowWindow();
    }

    private void ShowCreateLobbyWindow()
    {
        otherModesWindow.HideWindow();
        createLobbyWindow.ShowWindow();
    }

    private void ShowJoinLobbyByCodeWindow()
    {
        otherModesWindow.HideWindow();
        joinLobbyByCodeWindow.ShowWindow();
    }

    private void HandleLobbyJoined(Lobby lobby)
    {
        // When a lobby is joined, show the current lobby window
        if (lobby != null && lobby.Players.Count > 0)
        {
            otherModesWindow.HideWindow();
            listLobbiesWindow.HideWindow();
            createLobbyWindow.HideWindow();
            joinLobbyByCodeWindow.HideWindow();
            currentLobbyWindow.ShowWindow();
        }
    }
}