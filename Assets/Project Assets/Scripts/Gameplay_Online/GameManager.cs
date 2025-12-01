using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Cinemachine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias Generales")]
    public GameObject playerPrefab;
    public StartingGridManager gridManager;
    public OnlineRaceManager onlineRaceManager; // Referencia al nuevo manager

    [Header("Referencias de Cámara")]
    public CinemachineCamera cinemachineCamera;
    public Camera mainCamera;

    [Header("Configuración de Carrera")]
    [SerializeField] private float graceTimeDuration = 3f; // Tiempo antes de contar 3, 2, 1...

    // Base de datos temporal
    private Dictionary<string, PlayerData> playerStatesByAccountID = new Dictionary<string, PlayerData>();
    private bool hasRequestedSpawn = false;

    // Estado de la carrera
    public bool IsRacing { get; private set; } = false;
    private List<KartNetworkSetup> spawnedKarts = new List<KartNetworkSetup>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient && IsSpawned)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                TryRequestSpawn(NetworkManager.Singleton.LocalClientId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        hasRequestedSpawn = false;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    // --- LOGICA DE SPAWN (Igual que antes pero registrando el kart) ---

    private void OnClientConnected(ulong clientId) => TryRequestSpawn(clientId);

    private void TryRequestSpawn(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;
        if (hasRequestedSpawn) return;
        hasRequestedSpawn = true;
        string myAuthID = AuthenticationService.Instance.PlayerId;
        RegisterPlayerRpc(myAuthID, clientId);
    }

    [Rpc(SendTo.Server)]
    private void RegisterPlayerRpc(string accountID, ulong rpcClientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(rpcClientId, out var client))
        {
            if (client.PlayerObject != null) return;
        }

        PlayerData finalData;
        if (playerStatesByAccountID.TryGetValue(accountID, out PlayerData savedData))
        {
            finalData = savedData;
        }
        else
        {
            // Nueva sesión
            int randomIndex = Random.Range(0, gridManager.SpawnPoints.Length);
            Transform spawnPoint = gridManager.GetSpawnPoint(randomIndex);
            finalData = new PlayerData(accountID, spawnPoint.position);
            playerStatesByAccountID[accountID] = finalData;
        }

        SpawnPlayerInternal(rpcClientId, finalData);
    }

    private void SpawnPlayerInternal(ulong clientId, PlayerData data)
    {
        GameObject kartObj = Instantiate(playerPrefab, data.position, Quaternion.identity);
        var netObj = kartObj.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, true);

        if (kartObj.TryGetComponent(out KartNetworkData kartData))
        {
            kartData.SetData(data);
        }

        // Registramos el setup para poder enviarle mensajes luego
        if (kartObj.TryGetComponent(out KartNetworkSetup setup))
        {
            spawnedKarts.Add(setup);

            // IMPORTANTE: Bloquear input al nacer hasta que empiece la carrera
            setup.SetInputActiveRpc(false);
        }

        // Si tenemos suficientes jugadores (o por debug), iniciamos la rutina de carrera
        // Aquí podrías poner una condición: if (spawnedKarts.Count >= 2) ...
        // Por ahora, iniciamos la cuenta regresiva si es el primer jugador, solo para probar.
        if (spawnedKarts.Count == 1)
        {
            StartCoroutine(ServerRaceSequence());
        }
    }

    // --- LOGICA DE SECUENCIA DE CARRERA (Reemplaza GameInitializer) ---

    private IEnumerator ServerRaceSequence()
    {
        // 1. Esperar tiempo de gracia (lobby o preparación)
        yield return new WaitForSeconds(graceTimeDuration);

        // 2. Cuenta regresiva 3, 2, 1
        int countdown = 3;
        while (countdown > 0)
        {
            // Enviar mensaje a TODOS los clientes
            foreach (var kart in spawnedKarts)
            {
                if (kart != null) kart.UpdateCountdownRpc(countdown.ToString());
            }
            yield return new WaitForSeconds(1f);
            countdown--;
        }

        // 3. GO!
        foreach (var kart in spawnedKarts)
        {
            if (kart != null)
            {
                kart.UpdateCountdownRpc("GO!");
                kart.SetInputActiveRpc(true); // DESBLOQUEAR CONTROLES
            }
        }

        IsRacing = true;
    }

    // --- UTILIDADES ---

    public void SetLocalCameraTarget(Transform target)
    {
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = target;
            cinemachineCamera.LookAt = target;
        }
    }

    public void SavePlayerData(string accountID, PlayerData data)
    {
        if (playerStatesByAccountID.ContainsKey(accountID))
            playerStatesByAccountID[accountID] = data;
        else
            playerStatesByAccountID.Add(accountID, data);
    }
}