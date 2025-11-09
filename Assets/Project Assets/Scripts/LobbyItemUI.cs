using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

public class LobbyItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text mapText;
    [SerializeField] private TMP_Text gameModeText;
    [SerializeField] private Button joinButton;

    private Lobby lobby;
    private ListLobbiesWindow listLobbiesWindow;

    private void OnEnable()
    {
        joinButton.onClick.AddListener(JoinLobby);
    }

    private void OnDisable()
    {
        joinButton.onClick.RemoveListener(JoinLobby);
    }

    public void Initialize(Lobby lobby, ListLobbiesWindow parentWindow)
    {
        this.lobby = lobby;
        this.listLobbiesWindow = parentWindow;

        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

        if (lobby.Data.ContainsKey(LobbyServiceManager.KEY_MAP))
        {
            mapText.text = $"Map: {lobby.Data[LobbyServiceManager.KEY_MAP].Value}";
        }

        if (lobby.Data.ContainsKey(LobbyServiceManager.KEY_GAMEMODE))
        {
            gameModeText.text = $"Mode: {lobby.Data[LobbyServiceManager.KEY_GAMEMODE].Value}";
        }

        bool isLobbyFull = lobby.Players.Count >= lobby.MaxPlayers;

        joinButton.interactable = !isLobbyFull;
    }

    public void SetJoinButtonInteractable(bool interactable)
    {
        bool isLobbyFull = lobby.Players.Count >= lobby.MaxPlayers;
        joinButton.interactable = interactable && !isLobbyFull;
    }

    private void JoinLobby()
    {
        if (listLobbiesWindow != null)
        {
            listLobbiesWindow.JoinLobby(lobby.Id);
        }
    }
}