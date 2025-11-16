using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class JoinLobbyByCodeWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField lobbyCodeInput;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button closeButton;

    [Header("Window References")]
    [SerializeField] private WindowsController joinLobbyByCodeWindow;
    [SerializeField] private WindowsController currentLobbyWindow;
    [SerializeField] private WindowsController otherModesWindow;

    [Header("Service References")]
    [SerializeField] private FadeManager fadeManager;

    private bool isJoiningLobby = false;

    private void OnEnable()
    {
        if (!isJoiningLobby)
        {
            SetUIInteractable(true);
        }

        joinLobbyButton.onClick.AddListener(JoinLobby);
        closeButton.onClick.AddListener(CloseWindow);

        lobbyCodeInput.text = "";
    }

    private void OnDisable()
    {
        joinLobbyButton.onClick.RemoveListener(JoinLobby);
        closeButton.onClick.RemoveListener(CloseWindow);
    }

    private async void JoinLobby()
    {
        if (isJoiningLobby) return;

        string lobbyCode = lobbyCodeInput.text.Trim();
        if (string.IsNullOrEmpty(lobbyCode))
        {
            Debug.LogError("Lobby code is empty");
            return;
        }

        isJoiningLobby = true;
        SetUIInteractable(false);

        try
        {
            // Mostrar fade durante la unión al lobby
            if (fadeManager != null)
                fadeManager.Show();

            Debug.Log($"Joining lobby with code: {lobbyCode}");

            // Esperar a que se una al lobby
            bool success = await LobbyServiceManager.Instance.JoinLobbyByCode(lobbyCode);

            if (success)
            {
                // Transición a CurrentLobbyWindow antes de ocultar el fade
                if (currentLobbyWindow != null)
                {
                    joinLobbyByCodeWindow.HideWindow();
                    currentLobbyWindow.ShowWindow();
                }

                Debug.Log("Joined lobby successfully!");
            }
            else
            {
                Debug.LogError("Failed to join lobby");
                // Reactivar UI en caso de error
                SetUIInteractable(true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Join lobby exception: {ex.Message}");
            SetUIInteractable(true);
        }
        finally
        {
            isJoiningLobby = false;

            // Ocultar fade después de completar la transición
            if (fadeManager != null)
                fadeManager.Hide();
        }
    }

    private void SetUIInteractable(bool interactable)
    {
        lobbyCodeInput.interactable = interactable;
        joinLobbyButton.interactable = interactable;
        closeButton.interactable = interactable;
    }

    private void CloseWindow()
    {
        if (isJoiningLobby) return;

        joinLobbyByCodeWindow.HideWindow();

        if (otherModesWindow != null)
            otherModesWindow.ShowWindow();
    }
}