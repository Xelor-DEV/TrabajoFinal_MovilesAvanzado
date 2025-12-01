using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class RaceManager : NonPersistentSingleton<RaceManager>
{
    [Header("Track Configuration")]
    [Tooltip("Arrastra todos los Waypoints en orden aqu�.")]
    [SerializeField] private Waypoint[] waypoints;
    [SerializeField] private bool drawPathGizmos = true;
    [SerializeField] private Color pathColor = Color.cyan;

    [Header("Ranking Settings")]
    [SerializeField] private float rankingUpdateInterval = 0.1f;

    // Lista de jugadores registrados
    private List<KartProgressTracker> racers = new List<KartProgressTracker>();
    private bool raceFinished = false;

    public UnityEvent<int> OnRaceEnded;

    public List<KartProgressTracker> Racers => racers;

    private void Awake()
    {
        // Inicializar �ndices de waypoints
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null) waypoints[i].Index = i;
        }
    }

    private void Start()
    {
        StartCoroutine(RankingRoutine());
    }

    public void NotifyRaceFinish(int winningPlayerID)
    {
        if (raceFinished) return;
        
        raceFinished = true;
        Debug.Log($"¡VICTORIA! El Ganador es el Player {winningPlayerID + 1}");

        // Lanzamos el evento para que lo escuche el RaceCompletionHandler
        OnRaceEnded?.Invoke(winningPlayerID);
    }

    // M�todo llamado por el GameInitializer para registrar jugadores
    public void RegisterRacer(KartProgressTracker racer)
    {
        if (!racers.Contains(racer))
        {
            racers.Add(racer);

            // SEGURIDAD: Intentamos asignar el primer punto de respawn disponible.
            // Lo ideal es que el Waypoint[0] SIEMPRE tenga un RespawnPoint asignado en el inspector.
            if (waypoints.Length > 0 && waypoints[0].RespawnPoint != null)
            {
                racer.ForceUpdateRespawnPoint(waypoints[0].RespawnPoint);
            }
            else
            {
                Debug.LogWarning("RaceManager: �Cuidado! El Waypoint 0 no tiene RespawnPoint. Si se caen al inicio, habr� error.");
            }
        }
    }

    public Waypoint GetWaypoint(int index)
    {
        if (index >= 0 && index < waypoints.Length)
            return waypoints[index];
        return null;
    }

    public int TotalWaypoints => waypoints.Length;

    private IEnumerator RankingRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(rankingUpdateInterval);

        while (!raceFinished)
        {
            CalculateRanking();
            yield return wait;
        }
    }

    private void CalculateRanking()
    {
        if (racers.Count < 2) return;

        // ALGORITMO DE ORDENAMIENTO
        // Ordenamos la lista 'racers' bas�ndonos en:
        // 1. Mayor �ndice de Waypoint completado.
        // 2. Menor distancia al SIGUIENTE waypoint.

        racers.Sort((a, b) =>
        {
            // Comparar �ndice de waypoint (Mayor es mejor)
            int waypointCompare = b.LastPassedWaypointIndex.CompareTo(a.LastPassedWaypointIndex);

            if (waypointCompare != 0) return waypointCompare;

            // Si est�n en el mismo waypoint, calculamos distancia al siguiente
            int nextWpIndex = a.LastPassedWaypointIndex + 1;

            // Si ya terminaron, da igual (o podriamos usar distancia a meta)
            if (nextWpIndex >= waypoints.Length) return 0;

            Transform target = waypoints[nextWpIndex].transform;

            float distA = Vector3.SqrMagnitude(a.transform.position - target.position);
            float distB = Vector3.SqrMagnitude(b.transform.position - target.position);

            // Menor distancia es mejor
            return distA.CompareTo(distB);
        });

        // Actualizar HUDs con la nueva posici�n
        for (int i = 0; i < racers.Count; i++)
        {
            // i + 1 es la posici�n (1�, 2�, etc.)
            racers[i].UpdateRank(i + 1);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawPathGizmos || waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Vector3 start = waypoints[i].transform.position;
                Vector3 end = waypoints[i + 1].transform.position;

                Gizmos.DrawLine(start, end);

                // Dibujar flecha simple
                Vector3 dir = (end - start).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

                Vector3 arrowHead = end - (dir * 2f);
                Gizmos.DrawLine(end, arrowHead + (right * 1f));
                Gizmos.DrawLine(end, arrowHead - (right * 1f));
            }
        }
    }
}