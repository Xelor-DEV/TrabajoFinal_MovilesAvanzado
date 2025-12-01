using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro; // Para TextMeshPro
using Unity.Services.Authentication;

public class KartNameTag : NetworkBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("El objeto GameObject padre que contiene el mesh renderer del texto")]
    public GameObject labelContainer;

    [Tooltip("El componente TextMeshPro (versión 3D, no UI)")]
    public TextMeshPro labelText;

    [Header("Estado (Sync)")]
    // Variable de red para sincronizar el nombre. WritePerm = Server (Autoritativo)
    public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // Suscribirse al cambio de nombre
        playerName.OnValueChanged += OnNameChanged;

        // Actualizar estado inicial
        UpdateVisuals();

        // LOGICA DE PROPIETARIO (Local Player)
        if (IsOwner)
        {
            // 1. Configurar mi propia cámara
            GameManager.Instance.SetLocalCameraTarget(this.transform);

            // 2. Solicitar al servidor que ponga mi nombre en la variable de red
            string myAuthName = GetMyAuthName();
            SetNameServerRpc(myAuthName);
        }
    }

    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged -= OnNameChanged;
    }

    private void Update()
    {
        // LOGICA DE BILLBOARD (Solo para jugadores remotos)
        // Si NO soy el dueño, significa que soy un "muñeco" en la pantalla de otro.
        // Debo mirar a la cámara DE ESE JUGADOR (la mainCamera local de su escena).
        if (!IsOwner && labelContainer != null && labelContainer.activeInHierarchy)
        {
            if (GameManager.Instance.mainCamera != null)
            {
                // Hacemos que el texto mire a la cámara principal de la escena
                Transform camTransform = GameManager.Instance.mainCamera.transform;

                // Rotación invertida para que el texto se lea correctamente (Billboard standard)
                labelContainer.transform.rotation = Quaternion.LookRotation(labelContainer.transform.position - camTransform.position);
            }
        }
    }

    // --- RPC para setear el nombre (Client -> Server) ---
    [Rpc(SendTo.Server)]
    private void SetNameServerRpc(string name)
    {
        // El servidor recibe el nombre y actualiza la NetworkVariable
        // Esto propaga el cambio a todos los clientes automáticamente
        playerName.Value = name;
    }

    // Evento al cambiar la variable
    private void OnNameChanged(FixedString64Bytes oldVal, FixedString64Bytes newVal)
    {
        if (labelText != null)
        {
            labelText.text = newVal.ToString();
        }
    }

    private void UpdateVisuals()
    {
        // Si soy el dueño, desactivo mi propio cartel para que no me estorbe
        if (IsOwner)
        {
            if (labelContainer != null) labelContainer.SetActive(false);
        }
        // Si es otro jugador, activo el cartel y pongo el texto actual
        else
        {
            if (labelContainer != null) labelContainer.SetActive(true);
            if (labelText != null) labelText.text = playerName.Value.ToString();
        }
    }

    // Utilidad para sacar el nombre del Auth Service
    private string GetMyAuthName()
    {
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                // Preferimos el nombre de perfil, si no existe, usamos el ID recortado
                string name = AuthenticationService.Instance.PlayerName;
                if (string.IsNullOrEmpty(name))
                {
                    name = "Player " + AuthenticationService.Instance.PlayerId.Substring(0, 4);
                }
                return name;
            }
        }
        catch { }
        return "Unknown";
    }
}