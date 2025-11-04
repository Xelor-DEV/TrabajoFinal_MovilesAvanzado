using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using System;

public class ListLobbiesWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject lobbyItemPrefab;

    [Header("Window References")]
    [SerializeField] private WindowsController listLobbiesWindow;
    [SerializeField] private WindowsController currentLobbyWindow;
    [SerializeField] private WindowsController otherModesWindow;

    [Header("Service References")]
    [SerializeField] private FadeManager fadeManager;

    private List<LobbyItemUI> lobbyItems = new List<LobbyItemUI>();
    private bool isRefreshing = false;
    private bool isJoiningLobby = false;

    private void OnEnable()
    {
        refreshButton.onClick.AddListener(RefreshLobbies);
        closeButton.onClick.AddListener(CloseWindow);

        LobbyServiceManager.Instance.OnLobbyListUpdated += OnLobbyListUpdated;

        // Cargar lobbies automáticamente al abrir la ventana
        RefreshLobbies();
    }

    private void OnDisable()
    {
        refreshButton.onClick.RemoveListener(RefreshLobbies);
        closeButton.onClick.RemoveListener(CloseWindow);

        if (LobbyServiceManager.Instance != null)
        {
            LobbyServiceManager.Instance.OnLobbyListUpdated -= OnLobbyListUpdated;
        }

        ClearLobbyItems();
    }

    private async void RefreshLobbies()
    {
        if (isRefreshing || isJoiningLobby) return;

        isRefreshing = true;
        refreshButton.interactable = false;

        Debug.Log("Refreshing lobbies...");

        try
        {
            // Mostrar fade durante la actualización
            if (fadeManager != null)
                fadeManager.Show();

            await LobbyServiceManager.Instance.RefreshLobbyList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Refresh lobbies failed: {ex.Message}");
        }
        finally
        {
            isRefreshing = false;

            // Ocultar fade después de completar
            if (fadeManager != null)
                fadeManager.Hide();
        }
    }

    private void OnLobbyListUpdated(List<Lobby> lobbies)
    {
        ClearLobbyItems();

        int publicLobbiesCount = 0;

        foreach (Lobby lobby in lobbies)
        {
            // Ignorar lobbies privados
            if (lobby.IsPrivate) continue;

            publicLobbiesCount++;

            GameObject lobbyItemObj = Instantiate(lobbyItemPrefab, content);
            LobbyItemUI lobbyItem = lobbyItemObj.GetComponent<LobbyItemUI>();
            lobbyItem.Initialize(lobby, this);
            lobbyItems.Add(lobbyItem);
        }

        Debug.Log(publicLobbiesCount > 0
            ? $"Found {publicLobbiesCount} public lobbies"
            : "No public lobbies found");

        // Reactivar botón de refresh
        refreshButton.interactable = true;
    }

    public async void JoinLobby(string lobbyId)
    {
        if (isJoiningLobby) return;

        isJoiningLobby = true;
        SetAllJoinButtonsInteractable(false);
        refreshButton.interactable = false;

        try
        {
            // Mostrar fade durante la unión al lobby
            if (fadeManager != null)
                fadeManager.Show();

            Debug.Log("Joining lobby...");

            bool success = await LobbyServiceManager.Instance.JoinLobbyById(lobbyId);

            if (success)
            {
                // Transición a CurrentLobbyWindow antes de ocultar el fade
                if (currentLobbyWindow != null)
                {
                    listLobbiesWindow.HideWindow();
                    currentLobbyWindow.ShowWindow();
                }

                Debug.Log("Joined lobby successfully!");
            }
            else
            {
                Debug.LogError("Failed to join lobby");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Join lobby exception: {ex.Message}");
            Debug.Log("Error joining lobby");
        }
        finally
        {
            isJoiningLobby = false;
            refreshButton.interactable = true;
            SetAllJoinButtonsInteractable(true);

            // Ocultar fade después de completar
            if (fadeManager != null)
                fadeManager.Hide();
        }
    }

    private void SetAllJoinButtonsInteractable(bool interactable)
    {
        foreach (LobbyItemUI item in lobbyItems)
        {
            if (item != null)
                item.SetJoinButtonInteractable(interactable);
        }
    }

    private void ClearLobbyItems()
    {
        foreach (LobbyItemUI item in lobbyItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        lobbyItems.Clear();
    }

    private void CloseWindow()
    {
        if (isRefreshing || isJoiningLobby) return;

        listLobbiesWindow.HideWindow();

        if (otherModesWindow != null)
            otherModesWindow.ShowWindow();
    }
}