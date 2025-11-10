using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TeleportManager : NetworkBehaviour
{
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    private int nextSpawnIndex = 0;

    public static TeleportManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TeleportPlayer(NetworkObject player)
    {
        if (!IsServer) return;

        int index = nextSpawnIndex % spawnPoints.Count;
        nextSpawnIndex++;

        Vector3 targetPosition = spawnPoints[index].position;
        Quaternion targetRotation = spawnPoints[index].rotation;

        TeleportClientRpc(player.OwnerClientId, targetPosition, targetRotation);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TeleportClientRpc(ulong clientId, Vector3 position, Quaternion rotation)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            NetworkObject player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            player.transform.SetPositionAndRotation(position, rotation);
        }
    }
}