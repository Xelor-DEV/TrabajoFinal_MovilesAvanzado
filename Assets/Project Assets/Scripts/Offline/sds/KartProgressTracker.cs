using UnityEngine;
using UnityEngine.Events;

public class KartProgressTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHUD hud;
    [SerializeField] private Rigidbody rb;

    [Header("Config")]
    [SerializeField] private Entity waypointEntity = Entity.Waypoint;
    [SerializeField] private Entity voidEntity = Entity.Void;

    [Header("Events")]
    // [NUEVO] Eventos para que el SoundEffects se suscriba
    public UnityEvent OnCheckpointCollected;
    public UnityEvent OnVoidFallDetected;

    // Estado interno
    private int lastPassedWaypointIndex = -1;
    private Transform currentRespawnPoint;
    private int currentRank = 0;

    public int LastPassedWaypointIndex => lastPassedWaypointIndex;

    private void Start()
    {
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.RegisterRacer(this);
        }
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
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
        int totalWaypoints = RaceManager.Instance.TotalWaypoints;

        bool isMovingForward = (wpIndex > lastPassedWaypointIndex);
        bool isMovingBackward = (wpIndex < lastPassedWaypointIndex);
        int difference = Mathf.Abs(wpIndex - lastPassedWaypointIndex);
        bool isHugeJump = difference > (totalWaypoints / 2);

        if ((isMovingForward && !isHugeJump) || (isMovingBackward && !isHugeJump) || wpIndex == lastPassedWaypointIndex + 1)
        {
            lastPassedWaypointIndex = wpIndex;

            // [MODIFICADO] Solo si el waypoint realmente tiene checkpoint
            if (wp.HasCheckpoint)
            {
                currentRespawnPoint = wp.RespawnPoint;

                // Activar visuales
                wp.ActivatePortal();

                // [NUEVO] Invocar evento para audio u otros sistemas
                OnCheckpointCollected?.Invoke();

                Debug.Log($"Checkpoint alcanzado en Waypoint {wpIndex}.");
            }

            if (lastPassedWaypointIndex >= totalWaypoints - 1)
            {
                RaceManager.Instance.CheckRaceFinish(lastPassedWaypointIndex);
            }
        }
    }

    private void HandleVoidFall()
    {
        // [NUEVO] Invocar evento de caida
        OnVoidFallDetected?.Invoke();

        if (currentRespawnPoint != null)
        {
            Debug.Log("Reapareciendo en último Checkpoint.");
            transform.position = currentRespawnPoint.position;
            transform.rotation = currentRespawnPoint.rotation;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            Debug.LogError("¡ERROR CRÍTICO! Sin Checkpoint guardado.");
        }
    }

    public void ForceUpdateRespawnPoint(Transform newPoint)
    {
        currentRespawnPoint = newPoint;
    }

    public void UpdateRank(int newRank)
    {
        if (currentRank != newRank)
        {
            currentRank = newRank;
            if (hud != null) hud.UpdatePosition(currentRank);
        }
    }
}