using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
public class OnlineKartNetworkSetup : NetworkBehaviour
{
    [Header("Componentes a Limpiar (Solo dejar al Owner)")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerHUD playerHUD;
    [SerializeField] private GameObject hudCanvas;

    [Header("Referencias Locales")]
    [SerializeField] private OnlineKartMovement kartMovement;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameManager.Instance.SetLocalCameraTarget(this.transform);

            if (hudCanvas != null) hudCanvas.SetActive(true);
            if (playerInput != null) playerInput.enabled = true;
        }
        else
        {
            if (playerInput != null) Destroy(playerInput);
            if (hudCanvas != null) Destroy(hudCanvas);
            if (playerHUD != null) Destroy(playerHUD);
        }
    }

    [Rpc(SendTo.Owner)]
    public void UpdateCountdownRpc(string text)
    {
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

        if (playerInput != null)
        {
            playerInput.enabled = active;
        }
    }
}
