using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public class ListLobbiesWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject lobbyItemPrefab;

    [Header("Window References")]
    [SerializeField] private WindowsController listLobbiesWindow;
    [SerializeField] private WindowsController otherModesWindow;

    private List<LobbyItemUI> lobbyItems = new List<LobbyItemUI>();

    private void OnEnable()
    {
        refreshButton.onClick.AddListener(RefreshLobbies);
        closeButton.onClick.AddListener(CloseWindow);

        LobbyServiceManager.Instance.OnLobbyListUpdated += OnLobbyListUpdated;

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

    private void RefreshLobbies()
    {
        refreshButton.interactable = false;
        LobbyServiceManager.Instance.RefreshLobbyList();

        // Re-enable button after a short delay
        Invoke(nameof(EnableRefreshButton), 2f);
    }

    private void EnableRefreshButton()
    {
        refreshButton.interactable = true;
    }

    private void OnLobbyListUpdated(List<Lobby> lobbies)
    {
        ClearLobbyItems();

        foreach (Lobby lobby in lobbies)
        {
            GameObject lobbyItemObj = Instantiate(lobbyItemPrefab, content);
            LobbyItemUI lobbyItem = lobbyItemObj.GetComponent<LobbyItemUI>();
            lobbyItem.Initialize(lobby);
            lobbyItems.Add(lobbyItem);
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
        listLobbiesWindow.HideWindow();
        otherModesWindow.ShowWindow();
    }
}