using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class OnlineKartProgress : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHUD hud; // Referencia local al HUD
    [SerializeField] private Rigidbody rb;

    [Header("Config")]
    [SerializeField] private Entity waypointEntity = Entity.Waypoint;
    [SerializeField] private Entity voidEntity = Entity.Void;

    // Estado Servidor
    public int LastPassedWaypointIndex { get; private set; } = -1;
    public int CurrentRank { get; set; } = 0;

    private Transform currentRespawnPoint;

    // Eventos (Sincronizados localmente si es necesario)
    public UnityEvent OnCheckpointCollected;
    public UnityEvent OnVoidFallDetected;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (OnlineRaceManager.Instance != null)
                OnlineRaceManager.Instance.RegisterRacer(this);
        }

        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && OnlineRaceManager.Instance != null)
        {
            OnlineRaceManager.Instance.UnregisterRacer(this);
        }
    }

    // LÓGICA DE SERVIDOR: Detectar colisiones
    private void OnTriggerEnter(Collider other)
    {
        // Solo el servidor procesa lógica de carrera para evitar trampas
        if (!IsServer) return;

        EntityIdentifier entityId = other.GetComponent<EntityIdentifier>();
        if (entityId == null) return;

        if (entityId.Entity == waypointEntity)
        {
            HandleWaypointPass(other.GetComponent<Waypoint>());
        }
        else if (entityId.Entity == voidEntity)
        {
            HandleVoidFall();
        }
    }

    private void HandleWaypointPass(Waypoint wp)
    {
        if (wp == null) return;

        int wpIndex = wp.Index;
        int totalWaypoints = OnlineRaceManager.Instance.TotalWaypoints;

        // Lógica simple de validación de dirección
        if (wpIndex > LastPassedWaypointIndex || (LastPassedWaypointIndex == totalWaypoints - 1 && wpIndex == 0))
        {
            LastPassedWaypointIndex = wpIndex;

            if (wp.HasCheckpoint)
            {
                currentRespawnPoint = wp.RespawnPoint;

                // Notificar al cliente dueño para feedback visual/sonoro
                PlayCheckpointFeedbackClientRpc();
            }
        }
    }

    private void HandleVoidFall()
    {
        // Respawn lógico en Servidor
        if (currentRespawnPoint != null)
        {
            // Resetear posición y físicas
            transform.position = currentRespawnPoint.position;
            transform.rotation = currentRespawnPoint.rotation;

            // Necesitamos resetear la velocidad del Rigidbody. 
            // Al ser NetworkRigidbody, esto debería propagarse, pero a veces es mejor forzarlo.
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Notificar al cliente
            PlayVoidFallFeedbackClientRpc();
        }
    }

    public void ForceUpdateRespawnPoint(Transform point)
    {
        currentRespawnPoint = point;
    }

    // --- RPCs DE CLIENTE (Feedback Visual/UI) ---

    [Rpc(SendTo.Owner)]
    public void UpdateRankClientRpc(int newRank)
    {
        // Solo actualizamos el HUD si somos el dueño local
        if (hud != null)
        {
            hud.UpdatePosition(newRank);
        }
    }

    [Rpc(SendTo.Owner)]
    private void PlayCheckpointFeedbackClientRpc()
    {
        OnCheckpointCollected?.Invoke();
    }

    [Rpc(SendTo.Owner)]
    private void PlayVoidFallFeedbackClientRpc()
    {
        OnVoidFallDetected?.Invoke();
    }
}