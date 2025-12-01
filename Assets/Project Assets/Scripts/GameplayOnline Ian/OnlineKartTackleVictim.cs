using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class OnlineKartTackleVictim : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private EntityIdentifier entityIdentifier;

    [Header("Events")]
    public UnityEvent OnPushStarted;
    public UnityEvent OnPushEnded;

    // State variables
    private bool isPushed = false;
    private Coroutine pushCoroutine;
    private bool isInvulnerable = false;

    public bool IsPushed => isPushed;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (entityIdentifier == null) entityIdentifier = GetComponent<EntityIdentifier>();
    }

    private IEnumerator PushRoutine(Vector3 pushDirection, float pushForce, float pushUpForce, float pushDuration)
    {
        isPushed = true;
        OnPushStarted?.Invoke();

        if (IsServer)
        {
            Vector3 finalPushDirection = new Vector3(pushDirection.x, 0, pushDirection.z).normalized;
            Vector3 force = finalPushDirection * pushForce + Vector3.up * pushUpForce;
            rb.AddForce(force, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(pushDuration);

        EndPushServer();
    }

    private void EndPushServer()
    {
        if (!IsServer) return;

        isPushed = false;
        EndPushRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void EndPushRpc()
    {
        isPushed = false;
        OnPushEnded?.Invoke();
    }

    public void SetInvulnerable(bool state)
    {
        isInvulnerable = state;
    }

    // Llamado por el server desde OnlineKartTackleAttacker
    [Rpc(SendTo.Everyone)]
    public void ReceiveTackleRpc(Vector3 pushDirection, float pushForce, float pushUpForce, float pushDuration)
    {
        if (isInvulnerable) return;

        if (pushCoroutine != null) StopCoroutine(pushCoroutine);
        pushCoroutine = StartCoroutine(PushRoutine(pushDirection, pushForce, pushUpForce, pushDuration));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (!isPushed) return;

        EntityIdentifier otherEntity = collision.gameObject.GetComponent<EntityIdentifier>();
        if (otherEntity != null && otherEntity.Entity == Entity.Wall)
        {
            if (pushCoroutine != null) StopCoroutine(pushCoroutine);
            EndPushServer();
        }
    }
}
