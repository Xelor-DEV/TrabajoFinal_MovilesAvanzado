using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using System;

public class CurrentLobbyWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text mapText;
    [SerializeField] private Button nextMapButton;
    [SerializeField] private Button previousMapButton;
    [SerializeField] private TMP_Text gameModeText;
    [SerializeField] private Button nextGameModeButton;
    [SerializeField] private Button previousGameModeButton;
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Button exitLobbyButton;
    [SerializeField] private Toggle isPrivateToggle;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject playerItemPrefab;

    [Header("Window References")]
    [SerializeField] private WindowsController currentLobbyWindow;
    [SerializeField] private WindowsController otherModesWindow;

    [Header("Service References")]
    [SerializeField] private FadeManager fadeManager;
    [SerializeField] private GlobalGameSettings gameSettings;

    private string[] maps;
    private string[] gameModes;
    private int currentMapIndex = 0;
    private int currentGameModeIndex = 0;
    private List<PlayerItemUI> playerItems = new List<PlayerItemUI>();
    private bool isReady = false;
    private bool isStartingGame = false;

    private void Start()
    {
        InitializeFromGameSettings();
    }

    private void OnEnable()
    {
        nextMapButton.onClick.AddListener(NextMap);
        previousMapButton.onClick.AddListener(PreviousMap);
        nextGameModeButton.onClick.AddListener(NextGameMode);
        previousGameModeButton.onClick.AddListener(PreviousGameMode);
        startGameButton.onClick.AddListener(StartGame);
        readyButton.onClick.AddListener(ToggleReady);
        exitLobbyButton.onClick.AddListener(LeaveLobby);
        isPrivateToggle.onValueChanged.AddListener(OnPrivacyChanged);

        LobbyServiceManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
        LobbyServiceManager.Instance.OnLobbyLeft += OnLobbyLeft;

        UpdateUI();
    }

    private void OnDisable()
    {
        nextMapButton.onClick.RemoveListener(NextMap);
        previousMapButton.onClick.RemoveListener(PreviousMap);
        nextGameModeButton.onClick.RemoveListener(NextGameMode);
        previousGameModeButton.onClick.RemoveListener(PreviousGameMode);
        startGameButton.onClick.RemoveListener(StartGame);
        readyButton.onClick.RemoveListener(ToggleReady);
        exitLobbyButton.onClick.RemoveListener(LeaveLobby);
        isPrivateToggle.onValueChanged.RemoveListener(OnPrivacyChanged);

        if (LobbyServiceManager.Instance != null)
        {
            LobbyServiceManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
            LobbyServiceManager.Instance.OnLobbyLeft -= OnLobbyLeft;
        }

        ClearPlayerItems();
    }

    private void InitializeFromGameSettings()
    {
        if (gameSettings == null)
        {
            Debug.LogError("GlobalGameSettings not assigned!");
            return;
        }

        // Inicializar mapas desde GameSettings
        maps = new string[gameSettings.selectableMaps.Length];
        for (int i = 0; i < gameSettings.selectableMaps.Length; i++)
        {
            maps[i] = gameSettings.selectableMaps[i].ToString();
        }

        // Inicializar modos de juego desde GameSettings
        gameModes = new string[gameSettings.availableGameModes.Length];
        for (int i = 0; i < gameSettings.availableGameModes.Length; i++)
        {
            gameModes[i] = gameSettings.availableGameModes[i].ToString();
        }

        UpdateUI();
    }

    private void NextMap()
    {
        if (!LobbyServiceManager.Instance.IsLobbyHost()) return;

        currentMapIndex = (currentMapIndex + 1) % maps.Length;
        LobbyServiceManager.Instance.UpdateLobbyMap(maps[currentMapIndex]);
    }

    private void PreviousMap()
    {
        if (!LobbyServiceManager.Instance.IsLobbyHost()) return;

        currentMapIndex = (currentMapIndex - 1 + maps.Length) % maps.Length;
        LobbyServiceManager.Instance.UpdateLobbyMap(maps[currentMapIndex]);
    }

    private void NextGameMode()
    {
        if (!LobbyServiceManager.Instance.IsLobbyHost()) return;

        currentGameModeIndex = (currentGameModeIndex + 1) % gameModes.Length;
        LobbyServiceManager.Instance.UpdateLobbyGameMode(gameModes[currentGameModeIndex]);
    }

    private void PreviousGameMode()
    {
        if (!LobbyServiceManager.Instance.IsLobbyHost()) return;

        currentGameModeIndex = (currentGameModeIndex - 1 + gameModes.Length) % gameModes.Length;
        LobbyServiceManager.Instance.UpdateLobbyGameMode(gameModes[currentGameModeIndex]);
    }

    private void OnPrivacyChanged(bool isPrivate)
    {
        if (!LobbyServiceManager.Instance.IsLobbyHost()) return;

        // Actualizar privacidad del lobby
        // Nota: Unity Lobbies no permite cambiar la privacidad después de crear el lobby
        // Esto es solo para mostrar el estado actual
        Debug.Log($"Lobby privacy: {(isPrivate ? "Private" : "Public")}");
    }

    private void UpdateUI()
    {
        var lobby = LobbyServiceManager.Instance.JoinedLobby;
        if (lobby == null) return;

        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
        lobbyCodeText.text = lobby.LobbyCode ?? "No Code";
        isPrivateToggle.isOn = lobby.IsPrivate;

        if (lobby.Data.ContainsKey(LobbyServiceManager.KEY_MAP))
        {
            string currentMap = lobby.Data[LobbyServiceManager.KEY_MAP].Value;
            mapText.text = currentMap;

            // Actualizar índice del mapa actual
            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i] == currentMap)
                {
                    currentMapIndex = i;
                    break;
                }
            }
        }

        if (lobby.Data.ContainsKey(LobbyServiceManager.KEY_GAMEMODE))
        {
            string currentGameMode = lobby.Data[LobbyServiceManager.KEY_GAMEMODE].Value;
            gameModeText.text = currentGameMode;

            // Actualizar índice del modo de juego actual
            for (int i = 0; i < gameModes.Length; i++)
            {
                if (gameModes[i] == currentGameMode)
                {
                    currentGameModeIndex = i;
                    break;
                }
            }
        }

        bool isHost = LobbyServiceManager.Instance.IsLobbyHost();

        // Configurar visibilidad de controles según si es host o no
        nextMapButton.gameObject.SetActive(isHost);
        previousMapButton.gameObject.SetActive(isHost);
        nextGameModeButton.gameObject.SetActive(isHost);
        previousGameModeButton.gameObject.SetActive(isHost);
        isPrivateToggle.interactable = isHost;

        // Configurar botones de start y ready
        startGameButton.gameObject.SetActive(isHost);
        readyButton.gameObject.SetActive(!isHost);

        // Actualizar estado del botón de start
        startGameButton.interactable = AreAllPlayersReady();

        // Actualizar botón de ready
        readyButtonText.text = isReady ? "Ready" : "Not Ready";

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

    private async void StartGame()
    {
        if (isStartingGame) return;
        if (!LobbyServiceManager.Instance.IsLobbyHost()) return;
        if (!AreAllPlayersReady()) return;

        isStartingGame = true;
        SetUIInteractable(false);

        try
        {
            // Mostrar fade durante el inicio del juego
            if (fadeManager != null)
                fadeManager.Show();

            await LobbyServiceManager.Instance.StartGame();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Start game failed: {ex.Message}");
            SetUIInteractable(true);
        }
        finally
        {
            isStartingGame = false;

            // Ocultar fade después de completar
            if (fadeManager != null)
                fadeManager.Hide();
        }
    }

    private void ToggleReady()
    {
        isReady = !isReady;
        LobbyServiceManager.Instance.UpdatePlayerReady(AuthenticationService.Instance.PlayerId, isReady);
        readyButtonText.text = isReady ? "Ready" : "Not Ready";

        // Desactivar botón ready temporalmente para evitar spam
        readyButton.interactable = false;
        Invoke(nameof(EnableReadyButton), 1f);
    }

    private void EnableReadyButton()
    {
        readyButton.interactable = true;
    }

    private void LeaveLobby()
    {
        LobbyServiceManager.Instance.LeaveLobby();
    }

    private void SetUIInteractable(bool interactable)
    {
        nextMapButton.interactable = interactable;
        previousMapButton.interactable = interactable;
        nextGameModeButton.interactable = interactable;
        previousGameModeButton.interactable = interactable;
        startGameButton.interactable = interactable;
        readyButton.interactable = interactable;
        exitLobbyButton.interactable = interactable;
        isPrivateToggle.interactable = interactable;
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

    private bool AreAllPlayersReady()
    {
        var lobby = LobbyServiceManager.Instance.JoinedLobby;
        if (lobby == null) return false;

        foreach (Player player in lobby.Players)
        {
            if (player.Data.TryGetValue("IsReady", out PlayerDataObject isReadyData))
            {
                if (isReadyData.Value != "true")
                    return false;
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}