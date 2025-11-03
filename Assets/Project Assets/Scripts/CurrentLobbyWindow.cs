using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public class CurrentLobbyWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text maxPlayersText;
    [SerializeField] private Button increaseMaxPlayersButton;
    [SerializeField] private Button decreaseMaxPlayersButton;
    [SerializeField] private TMP_Text mapText;
    [SerializeField] private Button nextMapButton;
    [SerializeField] private Button previousMapButton;
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject playerItemPrefab;

    [Header("Window References")]
    [SerializeField] private WindowsController currentLobbyWindow;
    [SerializeField] private WindowsController otherModesWindow;

    private string[] maps = { "Forest", "Desert", "Ice" };
    private int currentMapIndex = 0;
    private List<PlayerItemUI> playerItems = new List<PlayerItemUI>();

    private void OnEnable()
    {
        increaseMaxPlayersButton.onClick.AddListener(IncreaseMaxPlayers);
        decreaseMaxPlayersButton.onClick.AddListener(DecreaseMaxPlayers);
        nextMapButton.onClick.AddListener(NextMap);
        previousMapButton.onClick.AddListener(PreviousMap);
        startGameButton.onClick.AddListener(StartGame);
        leaveLobbyButton.onClick.AddListener(LeaveLobby);

        LobbyServiceManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
        LobbyServiceManager.Instance.OnLobbyLeft += OnLobbyLeft;

        UpdateUI();
    }

    private void OnDisable()
    {
        increaseMaxPlayersButton.onClick.RemoveListener(IncreaseMaxPlayers);
        decreaseMaxPlayersButton.onClick.RemoveListener(DecreaseMaxPlayers);
        nextMapButton.onClick.RemoveListener(NextMap);
        previousMapButton.onClick.RemoveListener(PreviousMap);
        startGameButton.onClick.RemoveListener(StartGame);
        leaveLobbyButton.onClick.RemoveListener(LeaveLobby);

        if (LobbyServiceManager.Instance != null)
        {
            LobbyServiceManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
            LobbyServiceManager.Instance.OnLobbyLeft -= OnLobbyLeft;
        }

        ClearPlayerItems();
    }

    private void IncreaseMaxPlayers()
    {
        int newMaxPlayers = LobbyServiceManager.Instance.JoinedLobby.MaxPlayers + 1;
        newMaxPlayers = Mathf.Clamp(newMaxPlayers, 2, 8);
        LobbyServiceManager.Instance.UpdateMaxPlayers(newMaxPlayers);
    }

    private void DecreaseMaxPlayers()
    {
        int newMaxPlayers = LobbyServiceManager.Instance.JoinedLobby.MaxPlayers - 1;
        newMaxPlayers = Mathf.Clamp(newMaxPlayers, 2, 8);
        LobbyServiceManager.Instance.UpdateMaxPlayers(newMaxPlayers);
    }

    private void NextMap()
    {
        currentMapIndex = (currentMapIndex + 1) % maps.Length;
        LobbyServiceManager.Instance.UpdateLobbyMap(maps[currentMapIndex]);
    }

    private void PreviousMap()
    {
        currentMapIndex = (currentMapIndex - 1 + maps.Length) % maps.Length;
        LobbyServiceManager.Instance.UpdateLobbyMap(maps[currentMapIndex]);
    }

    private void UpdateUI()
    {
        var lobby = LobbyServiceManager.Instance.JoinedLobby;
        if (lobby == null) return;

        lobbyNameText.text = lobby.Name;
        maxPlayersText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
        lobbyCodeText.text = lobby.LobbyCode ?? "No Code";

        if (lobby.Data.ContainsKey(LobbyServiceManager.KEY_MAP))
        {
            string currentMap = lobby.Data[LobbyServiceManager.KEY_MAP].Value;
            mapText.text = currentMap;

            // Update current map index
            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i] == currentMap)
                {
                    currentMapIndex = i;
                    break;
                }
            }
        }

        bool isHost = LobbyServiceManager.Instance.IsLobbyHost();
        increaseMaxPlayersButton.gameObject.SetActive(isHost);
        decreaseMaxPlayersButton.gameObject.SetActive(isHost);
        nextMapButton.gameObject.SetActive(isHost);
        previousMapButton.gameObject.SetActive(isHost);
        startGameButton.gameObject.SetActive(isHost);

        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        ClearPlayerItems();

        var lobby = LobbyServiceManager.Instance.JoinedLobby;
        if (lobby == null) return;

        bool isHost = LobbyServiceManager.Instance.IsLobbyHost();

        foreach (Player player in lobby.Players)
        {
            GameObject playerItemObj = Instantiate(playerItemPrefab, content);
            PlayerItemUI playerItem = playerItemObj.GetComponent<PlayerItemUI>();
            playerItem.Initialize(player, isHost);
            playerItems.Add(playerItem);
        }
    }

    private void ClearPlayerItems()
    {
        foreach (PlayerItemUI item in playerItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        playerItems.Clear();
    }

    private void StartGame()
    {
        LobbyServiceManager.Instance.StartGame();
    }

    private void LeaveLobby()
    {
        LobbyServiceManager.Instance.LeaveLobby();
    }

    private void OnLobbyUpdated(Lobby lobby)
    {
        UpdateUI();
    }

    private void OnLobbyLeft()
    {
        currentLobbyWindow.HideWindow();
        otherModesWindow.ShowWindow();
    }
}