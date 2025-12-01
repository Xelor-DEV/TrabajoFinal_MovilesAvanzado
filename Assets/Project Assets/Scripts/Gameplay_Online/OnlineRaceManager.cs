using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class OnlineRaceManager : NetworkBehaviour
{
    public static OnlineRaceManager Instance { get; private set; }

    [Header("Track Configuration")]
    [SerializeField] private Waypoint[] waypoints;

    // Lista sincronizada de progresos
    private List<OnlineKartProgress> racers = new List<OnlineKartProgress>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Inicializar índices de waypoints
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null) waypoints[i].Index = i;
        }
    }

    public void RegisterRacer(OnlineKartProgress racer)
    {
        if (!racers.Contains(racer))
        {
            racers.Add(racer);
            // Asignar primer respawn point
            if (waypoints.Length > 0 && waypoints[0].RespawnPoint != null)
            {
                racer.ForceUpdateRespawnPoint(waypoints[0].RespawnPoint);
            }
        }
    }

    public void UnregisterRacer(OnlineKartProgress racer)
    {
        if (racers.Contains(racer)) racers.Remove(racer);
    }

    private void Update()
    {
        // Solo el servidor calcula los rankings
        if (IsServer)
        {
            CalculateRanking();
        }
    }

    private void CalculateRanking()
    {
        if (racers.Count < 1) return;

        // Ordenar lista
        racers.Sort((a, b) =>
        {
            // 1. Mayor índice de waypoint
            int wpCompare = b.LastPassedWaypointIndex.CompareTo(a.LastPassedWaypointIndex);
            if (wpCompare != 0) return wpCompare;

            // 2. Menor distancia al siguiente waypoint
            int nextWpIndex = (a.LastPassedWaypointIndex + 1) % waypoints.Length;
            Transform target = waypoints[nextWpIndex].transform;

            float distA = Vector3.SqrMagnitude(a.transform.position - target.position);
            float distB = Vector3.SqrMagnitude(b.transform.position - target.position);

            return distA.CompareTo(distB);
        });

        // Actualizar rangos y enviarlos a los clientes
        for (int i = 0; i < racers.Count; i++)
        {
            // Si el rango cambió, notificar al cliente dueño de ese kart
            if (racers[i].CurrentRank != (i + 1))
            {
                racers[i].CurrentRank = i + 1;
                racers[i].UpdateRankClientRpc(i + 1);
            }
        }
    }

    public int TotalWaypoints => waypoints.Length;
}