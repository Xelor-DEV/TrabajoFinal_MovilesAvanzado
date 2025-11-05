using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Button closeLobbyButton; // Solo para host - cierra el lobby completo
    [SerializeField] private Button exitLobbyButton;  // Solo para clientes - sale del lobby
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
        // Pequeño delay para asegurar que todo esté inicializado
        StartCoroutine(DelayedInitialization());
    }

    private System.Collections.IEnumerator DelayedInitialization()
    {
        yield return null; // Esperar un frame

        // Suscribir eventos primero
        if (LobbyServiceManager.Instance != null)
        {
            LobbyServiceManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
            LobbyServiceManager.Instance.OnLobbyLeft += OnLobbyLeft;
            LobbyServiceManager.Instance.OnLobbyJoinError += OnLobbyJoinError;
        }

        // Luego configurar UI listeners
        if (nextMapButton != null) nextMapButton.onClick.AddListener(NextMap);
        if (previousMapButton != null) previousMapButton.onClick.AddListener(PreviousMap);
        if (nextGameModeButton != null) nextGameModeButton.onClick.AddListener(NextGameMode);
        if (previousGameModeButton != null) previousGameModeButton.onClick.AddListener(PreviousGameMode);
        if (startGameButton != null) startGameButton.onClick.AddListener(StartGame);
        if (readyButton != null) readyButton.onClick.AddListener(ToggleReady);

        if (closeLobbyButton != null) closeLobbyButton.onClick.AddListener(CloseLobby);
        if (exitLobbyButton != null) exitLobbyButton.onClick.AddListener(LeaveLobby);

        if (isPrivateToggle != null) isPrivateToggle.onValueChanged.AddListener(OnPrivacyChanged);

        // Reinicializar estado
        isReady = false;
        isStartingGame = false;

        // Forzar actualización de UI
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

        // Remover listeners de los nuevos botones
        if (closeLobbyButton != null) closeLobbyButton.onClick.RemoveListener(CloseLobby);
        if (exitLobbyButton != null) exitLobbyButton.onClick.RemoveListener(LeaveLobby);

        isPrivateToggle.onValueChanged.RemoveListener(OnPrivacyChanged);

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
    private void OnLobbyJoinError(string errorMessage)
    {
        Debug.LogError($"Lobby join error: {errorMessage}");
        // Forzar salida del lobby en caso de error
        OnLobbyLeft();
    }

    private void CheckIfStillInLobby(Lobby lobby)
    {
        if (lobby == null) return;

        string currentPlayerId = AuthenticationService.Instance.PlayerId;
        bool isInLobby = lobby.Players.Any(p => p.Id == currentPlayerId);

        if (!isInLobby)
        {
            Debug.Log("Player is no longer in the lobby");
            OnLobbyLeft();
        }
    }

    // Modifica el método UpdateUI para una mejor gestión de permisos de host:
    private void UpdateUI()
    {
        // Verificar que el LobbyServiceManager esté inicializado
        if (LobbyServiceManager.Instance == null)
        {
            Debug.LogError("LobbyServiceManager instance is null");
            return;
        }

        var lobby = LobbyServiceManager.Instance.JoinedLobby;
        if (lobby == null)
        {
            Debug.LogError("JoinedLobby is null in UpdateUI");
            return;
        }

        try
        {
            // Comprobaciones de nulidad para cada elemento de UI
            if (lobbyNameText != null)
                lobbyNameText.text = lobby.Name;

            if (playerCountText != null)
                playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

            if (lobbyCodeText != null)
                lobbyCodeText.text = lobby.LobbyCode ?? "No Code";

            // Toggle de privacidad solo para mostrar estado, no interactivo
            if (isPrivateToggle != null)
            {
                isPrivateToggle.isOn = lobby.IsPrivate;
                isPrivateToggle.interactable = false;
            }

            // Actualizar mapa
            if (mapText != null && lobby.Data.ContainsKey(LobbyServiceManager.KEY_MAP))
            {
                string currentMap = lobby.Data[LobbyServiceManager.KEY_MAP].Value;
                mapText.text = currentMap;

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
            if (gameModeText != null && lobby.Data.ContainsKey(LobbyServiceManager.KEY_GAMEMODE))
            {
                string currentGameMode = lobby.Data[LobbyServiceManager.KEY_GAMEMODE].Value;
                gameModeText.text = currentGameMode;

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

            // Configurar visibilidad de controles según si es host o no - DESACTIVAR GameObject
            if (nextMapButton != null) nextMapButton.gameObject.SetActive(isHost);
            if (previousMapButton != null) previousMapButton.gameObject.SetActive(isHost);
            if (nextGameModeButton != null) nextGameModeButton.gameObject.SetActive(isHost);
            if (previousGameModeButton != null) previousGameModeButton.gameObject.SetActive(isHost);

            // Configurar botones de cerrar/salir del lobby
            if (closeLobbyButton != null)
                closeLobbyButton.gameObject.SetActive(isHost);
            if (exitLobbyButton != null)
                exitLobbyButton.gameObject.SetActive(!isHost);

            // Configurar botones de start y ready
            if (startGameButton != null) startGameButton.gameObject.SetActive(isHost);
            if (readyButton != null) readyButton.gameObject.SetActive(!isHost);

            // Actualizar estado del botón de start (solo interactuable si todos están listos)
            if (startGameButton != null)
                startGameButton.interactable = AreAllPlayersReady();

            // Actualizar botón de ready
            if (readyButtonText != null)
                readyButtonText.text = isReady ? "Ready" : "Not Ready";

            UpdatePlayerList();

            // Verificar si el jugador actual sigue en el lobby
            CheckIfStillInLobby(lobby);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in UpdateUI: {ex.Message}");
        }
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

    private void CloseLobby()
    {
        if (!LobbyServiceManager.Instance.IsLobbyHost()) return;
        LobbyServiceManager.Instance.LeaveLobby();
    }

    // Método existente para salir del lobby (clientes)
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

        string hostId = lobby.HostId;
        bool allPlayersReady = true;

        foreach (Player player in lobby.Players)
        {
            // El host no necesita estar "ready", solo los clientes
            if (player.Id == hostId) continue;

            if (player.Data.TryGetValue("IsReady", out PlayerDataObject isReadyData))
            {
                if (isReadyData.Value != "true")
                {
                    allPlayersReady = false;
                    break;
                }
            }
            else
            {
                allPlayersReady = false;
                break;
            }
        }

        Debug.Log($"AreAllPlayersReady: {allPlayersReady} (Host: {hostId}, Players: {lobby.Players.Count})");
        return allPlayersReady;
    }
}