using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

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
    [SerializeField] private Toggle isPrivateToggle;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button closeButton;

    [Header("Window References")]
    [SerializeField] private WindowsController createLobbyWindow;
    [SerializeField] private WindowsController otherModesWindow;

    private string[] maps = { "Forest", "Desert", "Ice" };
    private int currentMapIndex = 0;
    private int maxPlayers = 4;

    private void OnEnable()
    {
        increaseMaxPlayersButton.onClick.AddListener(IncreaseMaxPlayers);
        decreaseMaxPlayersButton.onClick.AddListener(DecreaseMaxPlayers);
        nextMapButton.onClick.AddListener(NextMap);
        previousMapButton.onClick.AddListener(PreviousMap);
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
        createLobbyButton.onClick.RemoveListener(CreateLobby);
        closeButton.onClick.RemoveListener(CloseWindow);
    }

    private void IncreaseMaxPlayers()
    {
        maxPlayers = Mathf.Clamp(maxPlayers + 1, 2, 8);
        UpdateUI();
    }

    private void DecreaseMaxPlayers()
    {
        maxPlayers = Mathf.Clamp(maxPlayers - 1, 2, 8);
        UpdateUI();
    }

    private void NextMap()
    {
        currentMapIndex = (currentMapIndex + 1) % maps.Length;
        UpdateUI();
    }

    private void PreviousMap()
    {
        currentMapIndex = (currentMapIndex - 1 + maps.Length) % maps.Length;
        UpdateUI();
    }

    private void UpdateUI()
    {
        maxPlayersText.text = maxPlayers.ToString();
        mapText.text = maps[currentMapIndex];
    }

    private async void CreateLobby()
    {
        string lobbyName = string.IsNullOrEmpty(lobbyNameInput.text) ? "My Lobby" : lobbyNameInput.text;
        bool isPrivate = isPrivateToggle.isOn;
        string map = maps[currentMapIndex];

        createLobbyButton.interactable = false;

        bool success = await LobbyServiceManager.Instance.CreateLobby(lobbyName, maxPlayers, isPrivate, map);

        createLobbyButton.interactable = true;

        if (!success)
        {
            // Show error message
            Debug.LogError("Failed to create lobby");
        }
    }

    private void CloseWindow()
    {
        createLobbyWindow.HideWindow();
        otherModesWindow.ShowWindow();
    }
}