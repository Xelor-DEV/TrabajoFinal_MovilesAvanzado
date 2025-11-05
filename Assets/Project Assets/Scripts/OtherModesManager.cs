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

    private void HandleLobbyLeft()
    {
        Debug.Log("HandleLobbyLeft called in OtherModesManager");

        // Asegurarse de que estamos en la ventana correcta
        if (otherModesWindow != null && !otherModesWindow.gameObject.activeInHierarchy)
        {
            otherModesWindow.ShowWindow();
        }

        // Ocultar todas las demás ventanas
        if (listLobbiesWindow != null) listLobbiesWindow.HideWindow();
        if (createLobbyWindow != null) createLobbyWindow.HideWindow();
        if (joinLobbyByCodeWindow != null) joinLobbyByCodeWindow.HideWindow();
        if (currentLobbyWindow != null) currentLobbyWindow.HideWindow();
    }

    private void HandleLobbyJoined(Lobby lobby)
    {
        if (lobby != null && lobby.Players.Count > 0)
        {
            Debug.Log("Lobby joined, transitioning to current lobby window");

            // Ocultar todas las ventanas excepto currentLobbyWindow
            otherModesWindow.HideWindow();
            listLobbiesWindow.HideWindow();
            createLobbyWindow.HideWindow();
            joinLobbyByCodeWindow.HideWindow();

            // Mostrar currentLobbyWindow
            if (currentLobbyWindow != null)
                currentLobbyWindow.ShowWindow();
        }
    }
}