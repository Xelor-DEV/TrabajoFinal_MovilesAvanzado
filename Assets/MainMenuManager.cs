using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button otherOptionsButton;

    [Header("Window References")]
    [SerializeField] private WindowsController createLobbyWindow;
    [SerializeField] private WindowsController otherModesWindow;
    [SerializeField] private WindowsController currentLobbyWindow;

    [Header("Service References")]
    [SerializeField] private FadeManager fadeManager;

    private bool isQuickJoining = false;

    private void OnEnable()
    {
        playButton.onClick.AddListener(HandlePlayButton);
        otherOptionsButton.onClick.AddListener(HandleOtherOptionsButton);
    }

    private void OnDisable()
    {
        playButton.onClick.RemoveListener(HandlePlayButton);
        otherOptionsButton.onClick.RemoveListener(HandleOtherOptionsButton);
    }

    private async void HandlePlayButton()
    {
        if (isQuickJoining) return;

        isQuickJoining = true;
        playButton.interactable = false;
        otherOptionsButton.interactable = false;

        try
        {
            // Mostrar fade durante la búsqueda
            if (fadeManager != null)
                fadeManager.Show();

            Debug.Log("Starting quick join...");

            // Intentar unirse rápidamente a un lobby
            bool success = await LobbyServiceManager.Instance.QuickJoinLobby();

            if (success)
            {
                Debug.Log("Quick join successful!");
                // Si nos unimos exitosamente, mostrar la ventana del lobby actual
                if (currentLobbyWindow != null)
                {
                    currentLobbyWindow.ShowWindow();
                }
            }
            else
            {
                Debug.Log("Quick join failed, opening create lobby window");
                // Si no encontramos lobby, abrir ventana de creación
                if (createLobbyWindow != null)
                {
                    createLobbyWindow.ShowWindow();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Quick join process failed: {ex.Message}");
            // En caso de error, también abrir ventana de creación
            if (createLobbyWindow != null)
            {
                createLobbyWindow.ShowWindow();
            }
        }
        finally
        {
            isQuickJoining = false;

            // Ocultar fade después de completar
            if (fadeManager != null)
                fadeManager.Hide();

            // Reactivar botones
            playButton.interactable = true;
            otherOptionsButton.interactable = true;
        }
    }

    private void HandleOtherOptionsButton()
    {
        if (otherModesWindow != null)
        {
            otherModesWindow.ShowWindow();
        }
    }
}