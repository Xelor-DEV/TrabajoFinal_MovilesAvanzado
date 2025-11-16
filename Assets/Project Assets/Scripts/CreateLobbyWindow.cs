using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;

public class CreateLobbyWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private TextMeshProUGUI maxPlayersText;
    [SerializeField] private Button increaseMaxPlayersButton;
    [SerializeField] private Button decreaseMaxPlayersButton;
    [SerializeField] private TextMeshProUGUI mapText;
    [SerializeField] private Button nextMapButton;
    [SerializeField] private Button previousMapButton;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private Button nextGameModeButton;
    [SerializeField] private Button previousGameModeButton;
    [SerializeField] private Toggle isPrivateToggle;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button closeButton;

    [Header("Window References")]
    [SerializeField] private WindowsController createLobbyWindow;
    [SerializeField] private WindowsController currentLobbyWindow;
    [SerializeField] private WindowsController otherModesWindow;

    [Header("Service References")]
    [SerializeField] private FadeManager fadeManager;
    [SerializeField] private GlobalGameSettings gameSettings;

    private string[] maps;
    private string[] gameModes;
    private int currentMapIndex = 0;
    private int currentGameModeIndex = 0;
    private int maxPlayers = 4;
    private bool isCreatingLobby = false;

    private void Start()
    {
        InitializeFromGameSettings();
    }

    private void OnEnable()
    {
        if (!isCreatingLobby)
        {
            SetUIInteractable(true);
        }

        increaseMaxPlayersButton.onClick.AddListener(IncreaseMaxPlayers);
        decreaseMaxPlayersButton.onClick.AddListener(DecreaseMaxPlayers);
        nextMapButton.onClick.AddListener(NextMap);
        previousMapButton.onClick.AddListener(PreviousMap);
        nextGameModeButton.onClick.AddListener(NextGameMode);
        previousGameModeButton.onClick.AddListener(PreviousGameMode);
        createLobbyButton.onClick.AddListener(CreateLobby);
        closeButton.onClick.AddListener(CloseWindow);

        UpdateUI();
    }

    private void OnDisable()
    {
        increaseMaxPlayersButton.onClick.RemoveListener(IncreaseMaxPlayers);
        decreaseMaxPlayersButton.onClick.RemoveListener(DecreaseMaxPlayers);
        nextMapButton.onClick.RemoveListener(NextMap);
        previousMapButton.onClick.RemoveListener(PreviousMap);
        nextGameModeButton.onClick.RemoveListener(NextGameMode);
        previousGameModeButton.onClick.RemoveListener(PreviousGameMode);
        createLobbyButton.onClick.RemoveListener(CreateLobby);
        closeButton.onClick.RemoveListener(CloseWindow);
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

        // Establecer máximo de jugadores desde GameSettings
        maxPlayers = gameSettings.maxGlobalPlayers;

        UpdateUI();
    }

    private void IncreaseMaxPlayers()
    {
        if (isCreatingLobby) return;
        maxPlayers = Mathf.Clamp(maxPlayers + 1, 2, gameSettings.maxGlobalPlayers);
        UpdateUI();
    }

    private void DecreaseMaxPlayers()
    {
        if (isCreatingLobby) return;
        maxPlayers = Mathf.Clamp(maxPlayers - 1, 2, gameSettings.maxGlobalPlayers);
        UpdateUI();
    }

    private void NextMap()
    {
        if (isCreatingLobby) return;
        currentMapIndex = (currentMapIndex + 1) % maps.Length;
        UpdateUI();
    }

    private void PreviousMap()
    {
        if (isCreatingLobby) return;
        currentMapIndex = (currentMapIndex - 1 + maps.Length) % maps.Length;
        UpdateUI();
    }

    private void NextGameMode()
    {
        if (isCreatingLobby) return;
        currentGameModeIndex = (currentGameModeIndex + 1) % gameModes.Length;
        UpdateUI();
    }

    private void PreviousGameMode()
    {
        if (isCreatingLobby) return;
        currentGameModeIndex = (currentGameModeIndex - 1 + gameModes.Length) % gameModes.Length;
        UpdateUI();
    }

    private void UpdateUI()
    {
        maxPlayersText.text = maxPlayers.ToString();

        if (maps != null && maps.Length > 0)
            mapText.text = maps[currentMapIndex];

        if (gameModes != null && gameModes.Length > 0)
            gameModeText.text = gameModes[currentGameModeIndex];
    }

    private async void CreateLobby()
    {
        if (isCreatingLobby) return;

        isCreatingLobby = true;
        SetUIInteractable(false);

        try
        {
            // Mostrar fade durante la creación del lobby
            if (fadeManager != null)
                fadeManager.Show();

            string lobbyName = string.IsNullOrEmpty(lobbyNameInput.text) ? "My Lobby" : lobbyNameInput.text;
            bool isPrivate = isPrivateToggle.isOn;
            string map = maps != null && maps.Length > 0 ? maps[currentMapIndex] : "Default";
            string gameMode = gameModes != null && gameModes.Length > 0 ? gameModes[currentGameModeIndex] : "Versus";

            Debug.Log($"Creating lobby: {lobbyName}, Players: {maxPlayers}, Map: {map}, GameMode: {gameMode}");

            // Esperar a que se cree el lobby
            bool success = await LobbyServiceManager.Instance.CreateLobby(lobbyName, maxPlayers, isPrivate, map, gameMode);

            if (success)
            {
                // Transición a CurrentLobbyWindow antes de ocultar el fade
                if (currentLobbyWindow != null)
                {
                    createLobbyWindow.HideWindow();
                    currentLobbyWindow.ShowWindow();
                }

                Debug.Log("Lobby created successfully!");
            }
            else
            {
                Debug.LogError("Failed to create lobby");
                // Reactivar UI en caso de error
                SetUIInteractable(true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Create lobby exception: {ex.Message}");
            SetUIInteractable(true);
        }
        finally
        {
            isCreatingLobby = false;

            // Ocultar fade después de completar la transición
            if (fadeManager != null)
                fadeManager.Hide();
        }
    }

    private void SetUIInteractable(bool interactable)
    {
        lobbyNameInput.interactable = interactable;
        increaseMaxPlayersButton.interactable = interactable;
        decreaseMaxPlayersButton.interactable = interactable;
        nextMapButton.interactable = interactable;
        previousMapButton.interactable = interactable;
        nextGameModeButton.interactable = interactable;
        previousGameModeButton.interactable = interactable;
        isPrivateToggle.interactable = interactable;
        createLobbyButton.interactable = interactable;
        closeButton.interactable = interactable;
    }

    private void CloseWindow()
    {
        if (isCreatingLobby) return;

        createLobbyWindow.HideWindow();

        if (otherModesWindow != null)
            otherModesWindow.ShowWindow();
    }
}