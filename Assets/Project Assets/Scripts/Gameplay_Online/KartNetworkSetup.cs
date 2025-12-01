using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class KartNetworkSetup : NetworkBehaviour
{
    [Header("Componentes a Limpiar (Solo dejar al Owner)")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerHUD playerHUD;
    [SerializeField] private GameObject hudCanvas; // El objeto padre del UI

    [Header("Referencias Locales")]
    [SerializeField] private KartMovement kartMovement;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // --- SOY EL DUEÑO (JUGADOR LOCAL) ---

            // 1. Configurar cámara principal para seguirme a mí
            GameManager.Instance.SetLocalCameraTarget(this.transform);

            // 2. Asegurarnos que el HUD y el Input estén activos (aunque el input puede estar bloqueado por lógica)
            if (hudCanvas != null) hudCanvas.SetActive(true);
            if (playerInput != null) playerInput.enabled = true;
        }
        else
        {
            // --- NO SOY EL DUEÑO (ES OTRO JUGADOR ONLINE) ---

            // 1. Destruir/Desactivar componentes que solo el dueño debe tener
            if (playerInput != null) Destroy(playerInput);
            if (hudCanvas != null) Destroy(hudCanvas); // Adios HUD duplicado
            if (playerHUD != null) Destroy(playerHUD);
        }
    }

    // --- RPCs CONTROLADOS POR EL SERVER (GAMEMANAGER) ---

    [Rpc(SendTo.Owner)]
    public void UpdateCountdownRpc(string text)
    {
        // Este mensaje solo le llega al dueño de este kart
        if (playerHUD != null)
        {
            playerHUD.ShowCenterMessage(text, 0.8f);
        }
    }

    [Rpc(SendTo.Owner)]
    public void SetInputActiveRpc(bool active)
    {
        if (kartMovement != null)
        {
            if (active) kartMovement.EnableInput();
            else kartMovement.DisableInput();
        }
    }
}