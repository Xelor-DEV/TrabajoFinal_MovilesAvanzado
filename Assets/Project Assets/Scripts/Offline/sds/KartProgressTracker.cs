using UnityEngine;

public class KartProgressTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHUD hud;
    [SerializeField] private Rigidbody rb;

    [Header("Config")]
    [SerializeField] private Entity waypointEntity = Entity.Waypoint;
    [SerializeField] private Entity voidEntity = Entity.Void;

    // Estado interno
    private int lastPassedWaypointIndex = -1;
    private Transform currentRespawnPoint; // El último lugar seguro conocido
    private int currentRank = 0;

    public int LastPassedWaypointIndex => lastPassedWaypointIndex;

    private void Start()
    {
        // Registrarse en la carrera
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.RegisterRacer(this);
        }

        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificar identidad
        EntityIdentifier entityId = other.GetComponent<EntityIdentifier>();
        if (entityId == null) return;

        if (entityId.Entity == waypointEntity)
        {
            // Buscamos el componente Waypoint en el objeto chocado
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

        // LÓGICA DE PROGRESO:
        // Solo validamos si es el siguiente waypoint en la lista (o el primero 0)
        // Esto evita que si retrocedes y tocas uno viejo, se rompa la lógica.
        if (wpIndex > lastPassedWaypointIndex)
        {
            // 1. Actualizamos el índice para saber quién va primero (Ranking)
            lastPassedWaypointIndex = wpIndex;

            // 2. LÓGICA DE CHECKPOINT (El Balance):
            // Solo actualizamos el punto de respawn SI este waypoint tiene uno asignado.
            // Si wp.RespawnPoint es null, mantenemos el 'currentRespawnPoint' anterior.
            if (wp.RespawnPoint != null)
            {
                currentRespawnPoint = wp.RespawnPoint;
                Debug.Log($"Checkpoint alcanzado en Waypoint {wpIndex}. Nuevo Respawn guardado.");
            }
            else
            {
                // Solo log para debug, no cambiamos el respawn
                // Debug.Log($"Waypoint {wpIndex} cruzado (Sin Checkpoint).");
            }

            // 3. Revisar si terminamos la carrera
            if (lastPassedWaypointIndex >= RaceManager.Instance.TotalWaypoints - 1)
            {
                RaceManager.Instance.CheckRaceFinish(lastPassedWaypointIndex);
            }
        }
    }

    private void HandleVoidFall()
    {
        if (currentRespawnPoint != null)
        {
            Debug.Log("Jugador cayó al vacío. Reapareciendo en último Checkpoint.");

            // Resetear posición
            transform.position = currentRespawnPoint.position;
            transform.rotation = currentRespawnPoint.rotation;

            // IMPORTANTE: Matar la inercia. Si te caes rápido y reapareces,
            // no quieres salir disparado con la velocidad que tenías al caer.
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero; // Unity 6 (o rb.velocity en versiones viejas)
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            // Fallback de emergencia por si el Waypoint 0 no tenía respawn configurado
            Debug.LogError("¡ERROR CRÍTICO! El jugador cayó y no hay ningún Checkpoint guardado.");
        }
    }

    // Llamado por RaceManager al inicio para dar el primer punto seguro (Salida)
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