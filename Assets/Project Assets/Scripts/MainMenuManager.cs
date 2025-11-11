using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button otherOptionsButton;
    [SerializeField] private Button profileButton;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image playerIconImage;

    [Header("Window References")]
    [SerializeField] private WindowsController createLobbyWindow;
    [SerializeField] private WindowsController otherModesWindow;
    [SerializeField] private WindowsController currentLobbyWindow;
    [SerializeField] private WindowsController profileWindow;

    [Header("Data References")]
    [SerializeField] private PlayerIcons playerIcons;

    [Header("Service References")]
    [SerializeField] private FadeManager fadeManager;

    private bool isQuickJoining = false;

    private async void OnEnable()
    {
        playButton.onClick.AddListener(HandlePlayButton);
        otherOptionsButton.onClick.AddListener(HandleOtherOptionsButton);
        profileButton.onClick.AddListener(HandleProfileButton);

        // Suscribirse a eventos del CloudSaveManager
        CloudSaveManager.Instance.OnProfileDataLoaded += OnProfileDataUpdated;
        CloudSaveManager.Instance.OnProfileDataSaved += OnProfileDataUpdated;

        // Cargar perfil inicial
        await LoadPlayerProfile();
    }

    private void OnDisable()
    {
        playButton.onClick.RemoveListener(HandlePlayButton);
        otherOptionsButton.onClick.RemoveListener(HandleOtherOptionsButton);
        profileButton.onClick.RemoveListener(HandleProfileButton);

        // Desuscribirse de eventos
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnProfileDataLoaded -= OnProfileDataUpdated;
            CloudSaveManager.Instance.OnProfileDataSaved -= OnProfileDataUpdated;
        }
    }

    private async Task LoadPlayerProfile()
    {
        try
        {
            await CloudSaveManager.Instance.LoadPlayerProfileAsync();
            // La UI se actualizará a través del evento OnProfileDataLoaded
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load player profile in main menu: {ex.Message}");
        }
    }

    private async void OnProfileDataUpdated(PlayerProfileData profileData)
    {
        await UpdatePlayerDisplay(profileData);
    }

    private async Task UpdatePlayerDisplay(PlayerProfileData profileData)
    {
        try
        {
            // Obtener nombre del jugador desde Authentication
            string displayName = await CloudSaveManager.Instance.GetPlayerDisplayNameAsync();
            playerNameText.text = displayName;

            if (playerIcons != null && profileData != null)
            {
                playerIconImage.sprite = playerIcons.GetIcon(profileData.iconIndex);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update player display: {ex.Message}");
        }
    }

    private async void HandlePlayButton()
    {
        if (isQuickJoining) return;

        isQuickJoining = true;
        playButton.interactable = false;
        otherOptionsButton.interactable = false;
        profileButton.interactable = false;

        try
        {
            if (fadeManager != null)
                fadeManager.Show();

            Debug.Log("Starting quick join...");
            bool success = await LobbyServiceManager.Instance.QuickJoinLobby();

            if (success)
            {
                Debug.Log("Quick join successful!");
                if (currentLobbyWindow != null)
                {
                    currentLobbyWindow.ShowWindow();
                }
            }
            else
            {
                Debug.Log("Quick join failed, opening create lobby window");
                if (createLobbyWindow != null)
                {
                    createLobbyWindow.ShowWindow();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Quick join process failed: {ex.Message}");
            if (createLobbyWindow != null)
            {
                createLobbyWindow.ShowWindow();
            }
        }
        finally
        {
            isQuickJoining = false;
            if (fadeManager != null)
                fadeManager.Hide();

            playButton.interactable = true;
            otherOptionsButton.interactable = true;
            profileButton.interactable = true;
        }
    }

    private void HandleOtherOptionsButton()
    {
        if (otherModesWindow != null)
        {
            otherModesWindow.ShowWindow();
        }
    }

    private void HandleProfileButton()
    {
        if (profileWindow != null)
        {
            profileWindow.ShowWindow();
        }
    }
}