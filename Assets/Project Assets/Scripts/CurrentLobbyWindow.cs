using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

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
    [SerializeField] private Button exitLobbyButton; // Para clientes
    [SerializeField] private Button closeLobbyButton; // Para host - cerrar lobby completo
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

    private string currentPlayerId => AuthenticationService.Instance.PlayerId;

    private void Start()
    {
        InitializeFromGameSettings();
    }

    private void OnEnable()
    {
        // Suscribir eventos primero
        if (LobbyServiceManager.Instance != null)
        {
            LobbyServiceManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
            LobbyServiceManager.Instance.OnLobbyLeft += OnLobbyLeft;
            LobbyServiceManager.Instance.OnLobbyJoinError += OnLobbyJoinError;
        }

        // Configurar listeners de botones
        if (nextMapButton != null) nextMapButton.onClick.AddListener(NextMap);
        if (previousMapButton != null) previousMapButton.onClick.AddListener(PreviousMap);
        if (nextGameModeButton != null) nextGameModeButton.onClick.AddListener(NextGameMode);
        if (previousGameModeButton != null) previousGameModeButton.onClick.AddListener(PreviousGameMode);
        if (startGameButton != null) startGameButton.onClick.AddListener(StartGame);
        if (readyButton != null) readyButton.onClick.AddListener(ToggleReady);
        if (exitLobbyButton != null) exitLobbyButton.onClick.AddListener(LeaveLobby);
        if (closeLobbyButton != null) closeLobbyButton.onClick.AddListener(CloseLobby);
        if (isPrivateToggle != null) isPrivateToggle.onValueChanged.AddListener(OnPrivacyChanged);

        // Reinicializar estado
        isReady = false;
        isStartingGame = false;

        // Usar Coroutine para esperar un frame antes de actualizar UI
        StartCoroutine(DelayedUIUpdate());
    }

    private IEnumerator DelayedUIUpdate()
    {
        yield return null; // Esperar un frame
        UpdateUI();
    }

    private void OnDisable()
    {
        // Remover listeners
        if (nextMapButton != null) nextMapButton.onClick.RemoveListener(NextMap);
        if (previousMapButton != null) previousMapButton.onClick.RemoveListener(PreviousMap);
        if (nextGameModeButton != null) nextGameModeButton.onClick.RemoveListener(NextGameMode);
        if (previousGameModeButton != null) previousGameModeButton.onClick.RemoveListener(PreviousGameMode);
        if (startGameButton != null) startGameButton.onClick.RemoveListener(StartGame);
        if (readyButton != null) readyButton.onClick.RemoveListener(ToggleReady);
        if (exitLobbyButton != null) exitLobbyButton.onClick.RemoveListener(LeaveLobby);
        if (closeLobbyButton != null) closeLobbyButton.onClick.RemoveListener(CloseLobby);
        if (isPrivateToggle != null) isPrivateToggle.onValueChanged.RemoveListener(OnPrivacyChanged);

        // Desuscribir eventos
        if (LobbyServiceManager.Instance != null)
        {
            LobbyServiceManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
            LobbyServiceManager.Instance.OnLobbyLeft -= OnLobbyLeft;
            LobbyServiceManager.Instance.OnLobbyJoinError -= OnLobbyJoinError;
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
        Debug.Log($"Lobby privacy: {(isPrivate ? "Private" : "Public")}");
    }

    private void OnLobbyJoinError(string errorMessage)
    {
        Debug.LogError($"Lobby join error: {errorMessage}");
        OnLobbyLeft();
    }

    private void UpdateUI()
    {
        var lobby = LobbyServiceManager.Instance?.JoinedLobby;
        if (lobby == null)
        {
            Debug.LogError("JoinedLobby is null in UpdateUI");
            return;
        }

        try
        {
            // Comprobaciones de nulidad seguras
            if (lobbyNameText != null) lobbyNameText.text = lobby.Name ?? "Unnamed Lobby";
            if (playerCountText != null) playerCountText.text = $"{lobby.Players?.Count ?? 0}/{lobby.MaxPlayers}";
            if (lobbyCodeText != null) lobbyCodeText.text = lobby.LobbyCode ?? "No Code";

            // Toggle de privacidad
            if (isPrivateToggle != null)
            {
                isPrivateToggle.isOn = lobby.IsPrivate;
                isPrivateToggle.interactable = false;
            }

            // Actualizar mapa
            if (mapText != null && lobby.Data != null && lobby.Data.ContainsKey(LobbyServiceManager.KEY_MAP))
            {
                string currentMap = lobby.Data[LobbyServiceManager.KEY_MAP].Value;
                mapText.text = currentMap ?? "Unknown";

                for (int i = 0; i < maps.Length; i++)
                {
                    if (maps[i] == currentMap)
                    {
                        currentMapIndex = i;
                        break;
                    }
                }
            }

            // Actualizar modo de juego
            if (gameModeText != null && lobby.Data != null && lobby.Data.ContainsKey(LobbyServiceManager.KEY_GAMEMODE))
            {
                string currentGameMode = lobby.Data[LobbyServiceManager.KEY_GAMEMODE].Value;
                gameModeText.text = currentGameMode ?? "Unknown";

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

            // Configurar visibilidad de controles
            if (nextMapButton != null) nextMapButton.gameObject.SetActive(isHost);
            if (previousMapButton != null) previousMapButton.gameObject.SetActive(isHost);
            if (nextGameModeButton != null) nextGameModeButton.gameObject.SetActive(isHost);
            if (previousGameModeButton != null) previousGameModeButton.gameObject.SetActive(isHost);

            // Configurar botones de start y ready
            if (startGameButton != null) startGameButton.gameObject.SetActive(isHost);
            if (readyButton != null) readyButton.gameObject.SetActive(!isHost);

            // Configurar botones de salida
            if (exitLobbyButton != null) exitLobbyButton.gameObject.SetActive(!isHost);
            if (closeLobbyButton != null) closeLobbyButton.gameObject.SetActive(isHost);

            // Actualizar estado del botón de start - considerar mínimo de jugadores
            if (startGameButton != null)
            {
                bool hasEnoughPlayers = HasEnoughPlayersToStart();
                bool allClientsReady = AreAllClientsReady();
                startGameButton.interactable = hasEnoughPlayers && allClientsReady;

                // Mostrar tooltip o mensaje si no hay suficientes jugadores
                if (!hasEnoughPlayers)
                {
                    // Podrías añadir un texto de ayuda aquí
                    Debug.Log($"Not enough players to start. Need at least {gameSettings.minPlayersToStart} players.");
                }
            }

            // Actualizar botón de ready
            if (readyButtonText != null)
                readyButtonText.text = isReady ? "Ready" : "Not Ready";

            UpdatePlayerList();

            // Verificar si el jugador actual sigue en el lobby
            CheckIfStillInLobby(lobby);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in UpdateUI: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void UpdatePlayerList()
    {
        ClearPlayerItems();

        var lobby = LobbyServiceManager.Instance?.JoinedLobby;
        if (lobby == null || lobby.Players == null) return;

        bool isHost = LobbyServiceManager.Instance.IsLobbyHost();

        foreach (Player player in lobby.Players)
        {
            if (player == null) continue;

            GameObject playerItemObj = Instantiate(playerItemPrefab, content);
            PlayerItemUI playerItem = playerItemObj.GetComponent<PlayerItemUI>();
            if (playerItem != null)
            {
                playerItem.Initialize(player, isHost, lobby.HostId);
                playerItems.Add(playerItem);
            }
        }
    }

    private void ClearPlayerItems()
    {
        foreach (PlayerItemUI item in playerItems)
        {
            if (item != null && item.gameObject != null)
                Destroy(item.gameObject);
        }
        playerItems.Clear();
    }

    private async void StartGame()
    {
        if (isStartingGame) return;
        if (!LobbyServiceManager.Instance.IsLobbyHost()) return;
        if (!AreAllClientsReady()) return;

        isStartingGame = true;
        SetUIInteractable(false);

        try
        {
            if (fadeManager != null) fadeManager.Show();
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
            if (fadeManager != null) fadeManager.Hide();
        }
    }

    private void ToggleReady()
    {
        if (LobbyServiceManager.Instance.IsLobbyHost()) return;

        isReady = !isReady;
        LobbyServiceManager.Instance.UpdatePlayerReady(currentPlayerId, isReady);

        if (readyButtonText != null)
            readyButtonText.text = isReady ? "Ready" : "Not Ready";

        if (readyButton != null)
        {
            readyButton.interactable = false;
            Invoke(nameof(EnableReadyButton), 1f);
        }
    }

    private void EnableReadyButton()
    {
        if (readyButton != null) readyButton.interactable = true;
    }

    private void LeaveLobby()
    {
        LobbyServiceManager.Instance.LeaveLobby();
    }

    private void CloseLobby()
    {
        if (LobbyServiceManager.Instance.IsLobbyHost())
        {
            LobbyServiceManager.Instance.LeaveLobby();
        }
    }

    private void SetUIInteractable(bool interactable)
    {
        if (nextMapButton != null) nextMapButton.interactable = interactable;
        if (previousMapButton != null) previousMapButton.interactable = interactable;
        if (nextGameModeButton != null) nextGameModeButton.interactable = interactable;
        if (previousGameModeButton != null) previousGameModeButton.interactable = interactable;
        if (startGameButton != null) startGameButton.interactable = interactable;
        if (readyButton != null) readyButton.interactable = interactable;
        if (exitLobbyButton != null) exitLobbyButton.interactable = interactable;
        if (closeLobbyButton != null) closeLobbyButton.interactable = interactable;
        if (isPrivateToggle != null) isPrivateToggle.interactable = interactable;
    }

    private void OnLobbyUpdated(Lobby lobby)
    {
        UpdateUI();
    }

    private void OnLobbyLeft()
    {
        Debug.Log("CurrentLobbyWindow: OnLobbyLeft called");

        if (currentLobbyWindow != null)
            currentLobbyWindow.HideWindow();

        if (otherModesWindow != null)
            otherModesWindow.ShowWindow();
    }

    private void CheckIfStillInLobby(Lobby lobby)
    {
        if (lobby?.Players == null) return;

        bool isInLobby = lobby.Players.Any(p => p.Id == currentPlayerId);

        if (!isInLobby)
        {
            Debug.Log("Player is no longer in the lobby");
            OnLobbyLeft();
        }
    }

    private bool HasEnoughPlayersToStart()
    {
        var lobby = LobbyServiceManager.Instance?.JoinedLobby;
        if (lobby?.Players == null) return false;

        // Verificar si hay al menos el mínimo de jugadores requeridos
        return lobby.Players.Count >= gameSettings.minPlayersToStart;
    }


    private bool AreAllClientsReady()
    {
        var lobby = LobbyServiceManager.Instance?.JoinedLobby;
        if (lobby?.Players == null) return false;

        // Primero verificar que hay suficientes jugadores
        if (!HasEnoughPlayersToStart()) return false;

        foreach (Player player in lobby.Players)
        {
            // El host no necesita estar ready
            if (player.Id == lobby.HostId) continue;

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