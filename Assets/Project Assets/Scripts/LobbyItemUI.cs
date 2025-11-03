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
    [SerializeField] private Button joinButton;

    private Lobby lobby;

    private void OnEnable()
    {
        joinButton.onClick.AddListener(JoinLobby);
    }

    private void OnDisable()
    {
        joinButton.onClick.RemoveListener(JoinLobby);
    }

    public void Initialize(Lobby lobby)
    {
        this.lobby = lobby;

        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

        if (lobby.Data.ContainsKey(LobbyServiceManager.KEY_MAP))
        {
            mapText.text = lobby.Data[LobbyServiceManager.KEY_MAP].Value;
        }
    }

    private async void JoinLobby()
    {
        joinButton.interactable = false;

        bool success = await LobbyServiceManager.Instance.JoinLobbyById(lobby.Id);

        joinButton.interactable = true;

        if (!success)
        {
            Debug.LogError("Failed to join lobby");
        }
    }
}