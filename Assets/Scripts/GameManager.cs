using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab;

    private void OnEnable()
    {
        // Si NetworkManager ya existe, suscribirse; si no, esperar a que esté listo
        if (NetworkManager.Singleton != null)
        {
            RegisterCallbacks();
        }
        else
        {
            StartCoroutine(WaitForNetworkManagerAndRegister());
        }
    }

    private IEnumerator WaitForNetworkManagerAndRegister()
    {
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }
        RegisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Si somos el servidor y ya hay clientes conectados (host), asegurarnos de crear sus players
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (var kv in NetworkManager.Singleton.ConnectedClients)
            {
                ulong clientId = kv.Key;
                var client = kv.Value;
                if (client.PlayerObject == null)
                {
                    SpawnPlayerForClient(clientId);
                }
            }
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        // Evitar doble spawn si PlayerObject ya existe
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null) return;
        }

        SpawnPlayerForClient(clientId);
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 0.5f, Random.Range(-3f, 3f));
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Si deseas limpiar algo al desconectarse, hazlo aquí.
    }
}