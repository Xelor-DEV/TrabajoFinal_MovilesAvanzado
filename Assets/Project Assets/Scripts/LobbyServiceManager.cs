using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

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
    public const string KEY_GAMEMODE = "GameMode";

    private string currentPlayerId => AuthenticationService.Instance.PlayerId;

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
                    // Si falla el heartbeat, el lobby podría haber sido eliminado
                    if (ex.Message.Contains("NotFound"))
                    {
                        ForceLeaveLobby();
                    }
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
                float LobbyUpdateTimerMax = 2.0f;
                lobbyUpdateTimer = LobbyUpdateTimerMax;

                try
                {
                    Lobby lobby = await LobbyService.Instance.GetLobbyAsync(JoinedLobby.Id);
                    JoinedLobby = lobby;

                    // Verificar SIEMPRE si el jugador actual sigue en el lobby
                    CheckIfPlayerStillInLobby(lobby);

                    // Solo invocar OnLobbyUpdated si el jugador sigue en el lobby
                    if (IsPlayerInLobby(lobby))
                    {
                        OnLobbyUpdated?.Invoke(lobby);
                    }

                    // Auto-join relay cuando el host establece el código
                    if (lobby.Data.ContainsKey(KEY_RELAYCODE) &&
                        lobby.Data[KEY_RELAYCODE].Value != "0" &&
                        !IsLobbyHost() &&
                        !NetworkManager.Singleton.IsClient)
                    {
                        relayManager.JoinRelay(lobby.Data[KEY_RELAYCODE].Value);
                    }
                }
                catch (LobbyServiceException ex)
                {
                    Debug.LogError($"Lobby update failed: {ex.Message}");

                    if (ex.Message.Contains("Too Many Requests"))
                    {
                        lobbyUpdateTimer = 5.0f;
                        Debug.LogWarning("Rate limit hit, increasing poll interval");
                    }
                    else if (ex.Message.Contains("NotFound") || ex.Message.Contains("Not Found") || ex.Message.Contains("lobby not found"))
                    {
                        // El lobby ya no existe - fue eliminado por el host
                        Debug.Log("Lobby was deleted by host, forcing all players to leave");
                        ForceLeaveLobby();
                    }
                    else if (ex.Message.Contains("Forbidden"))
                    {
                        // No tenemos acceso al lobby
                        Debug.Log("Access to lobby forbidden, forcing leave");
                        ForceLeaveLobby();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unexpected error in lobby update: {ex.Message}");
                    // En caso de error inesperado, forzar salida para evitar estado inconsistente
                    ForceLeaveLobby();
                }
            }
        }
    }

    private void CheckIfPlayerStillInLobby(Lobby lobby)
    {
        if (!IsPlayerInLobby(lobby))
        {
            Debug.Log($"Player {currentPlayerId} is no longer in lobby {lobby.Id}. Triggering leave.");
            ForceLeaveLobby();
        }
    }

    private bool IsPlayerInLobby(Lobby lobby)
    {
        return lobby?.Players != null && lobby.Players.Any(p => p.Id == currentPlayerId);
    }

    private void ForceLeaveLobby()
    {
        Debug.Log("ForceLeaveLobby called - Lobby no longer exists or player was removed");

        // Limpiar referencias primero
        var wasInLobby = JoinedLobby != null;
        HostLobby = null;
        JoinedLobby = null;

        // Solo invocar el evento si realmente estábamos en un lobby
        if (wasInLobby)
        {
            OnLobbyLeft?.Invoke();
        }
    }

    public async Task<bool> CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, string map, string gameMode)
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
                    { KEY_GAMEMODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode) }
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

    public async Task RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0")
                }
            };

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);
            OnLobbyListUpdated?.Invoke(queryResponse.Results);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Refresh lobbies failed: {ex.Message}");
            OnLobbyListUpdated?.Invoke(new List<Lobby>());
        }
    }

    public async Task StartGame()
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
                // Host: eliminar el lobby completo
                await LobbyService.Instance.DeleteLobbyAsync(JoinedLobby.Id);
                Debug.Log("Host deleted the lobby");
            }
            else
            {
                // Cliente: solo salir del lobby
                await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, currentPlayerId);
                Debug.Log("Client left the lobby");
            }

            ForceLeaveLobby();
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Leave lobby failed: {ex.Message}");
            // Forzar salida incluso si hay error
            ForceLeaveLobby();
        }
    }

    // Resto de métodos permanecen igual...
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

    public async void UpdateLobbyGameMode(string gameMode)
    {
        if (!IsLobbyHost()) return;

        try
        {
            HostLobby = await LobbyService.Instance.UpdateLobbyAsync(HostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_GAMEMODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode) }
                }
            });
            JoinedLobby = HostLobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Update lobby game mode failed: {ex.Message}");
        }
    }

    public async void UpdatePlayerReady(string playerId, bool isReady)
    {
        try
        {
            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isReady.ToString().ToLower()) }
                }
            };

            await LobbyService.Instance.UpdatePlayerAsync(JoinedLobby.Id, playerId, options);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Update player ready failed: {ex.Message}");
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

            // Actualizar el lobby después de kickear
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(JoinedLobby.Id);
            JoinedLobby = lobby;
            OnLobbyUpdated?.Invoke(lobby);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Kick player failed: {ex.Message}");
        }
    }

    public bool IsLobbyHost()
    {
        return HostLobby != null && JoinedLobby != null &&
               JoinedLobby.HostId == currentPlayerId;
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
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false") }
            }
        };
    }
}