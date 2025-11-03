using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class LobbyServiceManager : NonPersistentSingleton<LobbyServiceManager>
{
    [Header("Service References")]
    [SerializeField] private RelayServiceManager relayManager;
    [SerializeField] private NetworkSceneManager networkSceneManager;

    public Lobby HostLobby { get; private set; }
    public Lobby JoinedLobby { get; private set; }

    private float heartBeatTimer;
    private float lobbyUpdateTimer;

    // Events
    public event Action<List<Lobby>> OnLobbyListUpdated;
    public event Action<Lobby> OnLobbyUpdated;
    public event Action OnLobbyLeft;
    public event Action<string> OnLobbyJoinError;

    public const string KEY_RELAYCODE = "RelayCode";
    public const string KEY_MAP = "Map";

    private void Update()
    {
        HandleLobbyHeartBeat();
        HandleLobbyPollForUpdates();
    }

    private async void HandleLobbyHeartBeat()
    {
        if (HostLobby != null)
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer < 0)
            {
                float heartbeatTimerMax = 15;
                heartBeatTimer = heartbeatTimerMax;

                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(HostLobby.Id);
                }
                catch (LobbyServiceException ex)
                {
                    Debug.LogError($"Heartbeat failed: {ex.Message}");
                }
            }
        }
    }

    private async void HandleLobbyPollForUpdates()
    {
        if (JoinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0)
            {
                float LobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = LobbyUpdateTimerMax;

                try
                {
                    Lobby lobby = await LobbyService.Instance.GetLobbyAsync(JoinedLobby.Id);
                    JoinedLobby = lobby;
                    OnLobbyUpdated?.Invoke(lobby);

                    // Auto-join relay when host sets the code
                    if (lobby.Data.ContainsKey(KEY_RELAYCODE) &&
                        lobby.Data[KEY_RELAYCODE].Value != "0" &&
                        !IsLobbyHost())
                    {
                        relayManager.JoinRelay(lobby.Data[KEY_RELAYCODE].Value);
                    }
                }
                catch (LobbyServiceException ex)
                {
                    Debug.LogError($"Lobby update failed: {ex.Message}");
                }
            }
        }
    }

    public async Task<bool> CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, string map)
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                IsPrivate = isPrivate,
                Player = await GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAYCODE, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                    { KEY_MAP, new DataObject(DataObject.VisibilityOptions.Public, map) },
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            HostLobby = lobby;
            JoinedLobby = HostLobby;

            Debug.Log($"Lobby created: {lobby.Name}, Players: {lobby.Players.Count}/{lobby.MaxPlayers}");
            return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Create lobby failed: {ex.Message}");
            OnLobbyJoinError?.Invoke($"Create failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions()
            {
                Player = await GetPlayer()
            };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            JoinedLobby = lobby;

            Debug.Log($"Joined lobby: {lobby.Name}");
            return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Join lobby failed: {ex.Message}");
            OnLobbyJoinError?.Invoke($"Join failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> JoinLobbyById(string lobbyId)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
            {
                Player = await GetPlayer()
            };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            JoinedLobby = lobby;
            return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Join lobby by ID failed: {ex.Message}");
            OnLobbyJoinError?.Invoke($"Join failed: {ex.Message}");
            return false;
        }
    }

    public async void RefreshLobbyList()
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            OnLobbyListUpdated?.Invoke(queryResponse.Results);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Refresh lobbies failed: {ex.Message}");
        }
    }

    public async void StartGame()
    {
        if (!IsLobbyHost())
        {
            Debug.LogError("Only host can start the game");
            return;
        }

        try
        {
            string relayCode = await relayManager.CreateRelay();

            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(JoinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAYCODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            });

            JoinedLobby = lobby;
            networkSceneManager.LoadGameScene("GameScene");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Start game failed: {ex.Message}");
        }
    }

    public async void LeaveLobby()
    {
        if (JoinedLobby == null) return;

        try
        {
            if (IsLobbyHost())
            {
                await LobbyService.Instance.DeleteLobbyAsync(JoinedLobby.Id);
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, AuthenticationService.Instance.PlayerId);
            }

            HostLobby = null;
            JoinedLobby = null;
            OnLobbyLeft?.Invoke();
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Leave lobby failed: {ex.Message}");
        }
    }

    public async void UpdateLobbyMap(string map)
    {
        if (!IsLobbyHost()) return;

        try
        {
            HostLobby = await LobbyService.Instance.UpdateLobbyAsync(HostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_MAP, new DataObject(DataObject.VisibilityOptions.Public, map) }
                }
            });
            JoinedLobby = HostLobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Update lobby map failed: {ex.Message}");
        }
    }

    public async void UpdateMaxPlayers(int maxPlayers)
    {
        if (!IsLobbyHost()) return;

        try
        {
            HostLobby = await LobbyService.Instance.UpdateLobbyAsync(HostLobby.Id, new UpdateLobbyOptions
            {
                MaxPlayers = maxPlayers
            });
            JoinedLobby = HostLobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Update max players failed: {ex.Message}");
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (!IsLobbyHost()) return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, playerId);
            Debug.Log($"Player {playerId} kicked from lobby");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Kick player failed: {ex.Message}");
        }
    }

    public bool IsLobbyHost()
    {
        return HostLobby != null && JoinedLobby != null &&
               JoinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async Task<Player> GetPlayer()
    {
        string playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Player_" + UnityEngine.Random.Range(1000, 9999);
        }

        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }
}