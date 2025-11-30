using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class KartTackleVictim : MonoBehaviour
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

    // Properties
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

        // Aplicar fuerza de empuje con componente diagonal/arriba
        Vector3 finalPushDirection = new Vector3(pushDirection.x, 0, pushDirection.z).normalized;
        Vector3 force = finalPushDirection * pushForce + Vector3.up * pushUpForce;
        rb.AddForce(force, ForceMode.Impulse);

        yield return new WaitForSeconds(pushDuration);

        EndPush();
    }

    private void EndPush()
    {
        isPushed = false;
        OnPushEnded?.Invoke();
    }

    public void SetInvulnerable(bool state)
    {
        isInvulnerable = state;
    }

    // MODIFICAR el método ReceiveTackle para chequear esto al principio:
    public void ReceiveTackle(Vector3 pushDirection, float pushForce, float pushUpForce, float pushDuration)
    {
        if (isInvulnerable) return; // <--- NUEVA LÍNEA: Si es invulnerable, ignoramos el golpe.

        if (pushCoroutine != null) StopCoroutine(pushCoroutine);
        pushCoroutine = StartCoroutine(PushRoutine(pushDirection, pushForce, pushUpForce, pushDuration));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isPushed) return;

        // Check if we hit a wall during push
        EntityIdentifier otherEntity = collision.gameObject.GetComponent<EntityIdentifier>();
        if (otherEntity != null && otherEntity.Entity == Entity.Wall)
        {
            // Stop push when hitting wall
            if (pushCoroutine != null) StopCoroutine(pushCoroutine);
            EndPush();
        }
    }
}