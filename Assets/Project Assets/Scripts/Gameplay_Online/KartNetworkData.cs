using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class KartNetworkData : NetworkBehaviour
{
    [Header("Network Data")]
    // Variable sincronizada para saber de quién es este Kart
    [SerializeField] private NetworkVariable<FixedString64Bytes> accountID = new NetworkVariable<FixedString64Bytes>();

    // Se llama desde el GameManager al instanciar el Kart
    public void SetData(PlayerData data)
    {
        accountID.Value = data.accountID;
        transform.position = data.position;
    }

    public override void OnNetworkDespawn()
    {
        // Solo el servidor guarda los datos al desaparecer el objeto
        if (IsServer)
        {
            // Guardamos la posición exacta donde se desconectó
            PlayerData dataToSave = new PlayerData(accountID.Value.ToString(), transform.position);

            // Le decimos al GameManager que actualice la "base de datos" en memoria
            GameManager.Instance.SavePlayerData(accountID.Value.ToString(), dataToSave);

            Debug.Log($"[KartNetworkData] Guardado: {accountID.Value} en {transform.position}");
        }
    }
}

[Serializable]
public class PlayerData
{
    public string accountID;
    public Vector3 position;

    public PlayerData(string id, Vector3 pos)
    {
        accountID = id;
        position = pos;
    }
}