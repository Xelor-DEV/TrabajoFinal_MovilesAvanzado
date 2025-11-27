using UnityEngine;
using System.Collections.Generic;

public class StartingGridManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Arrastra aquí los Transforms de las posiciones de salida en orden (1º, 2º, 3º...)")]
    [SerializeField] private Transform[] spawnPoints;

    public Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("StartingGridManager: No spawn points assigned!");
            return transform; // Fallback a la posición de este objeto
        }

        // Si hay más jugadores que puntos, usamos el operador módulo para evitar errores
        int safeIndex = index % spawnPoints.Length;
        return spawnPoints[safeIndex];
    }
}