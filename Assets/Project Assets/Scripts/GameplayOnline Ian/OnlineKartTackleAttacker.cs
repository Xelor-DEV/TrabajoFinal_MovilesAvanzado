using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Events;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class OnlineKartTackleAttacker : NetworkBehaviour
{
    [Header("Tackle Settings")]
    [SerializeField] private float tackleDuration = 1.5f;
    [SerializeField] private float tackleCooldown = 3f;
    [SerializeField] private float tackleForce = 35f;
    [SerializeField] private float movementSpeedMultiplier = 1.5f;

    [Header("Hitbox Settings")]
    [SerializeField] private float tackleRadius = 2.0f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private Vector3 hitboxOffset = Vector3.zero;

    [Header("Target Settings")]
    [SerializeField] private Entity targetEntity = Entity.Kart;
    [SerializeField] private Entity obstacleEntity = Entity.Wall;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private EntityIdentifier entityIdentifier;

    [Header("Events")]
    public UnityEvent OnTackleStarted;
    public UnityEvent OnTackleEnded;
    public UnityEvent OnTargetHit;
    public UnityEvent OnCooldownStarted;
    public UnityEvent OnCooldownFinished;

    public UnityEvent<float> OnSpeedMultiplierChanged;

    private bool isTackling = false;
    private bool canTackle = true;
    private Coroutine tackleCoroutine;
    private Coroutine cooldownCoroutine;

    private readonly Collider[] hitBuffer = new Collider[10];

    public bool IsTackling => isTackling;
    public bool CanTackle => canTackle;
    public float TackleCooldown => tackleCooldown;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (entityIdentifier == null) entityIdentifier = GetComponent<EntityIdentifier>();
    }

    private void FixedUpdate()
    {
        // Solo el server chequea impactos
        if (!IsServer) return;

        if (isTackling)
        {
            CheckForTargets();
        }
    }

    private void CheckForTargets()
    {
        Vector3 center = transform.position + transform.TransformDirection(hitboxOffset);
        int numHits = Physics.OverlapSphereNonAlloc(center, tackleRadius, hitBuffer, targetLayers);

        for (int i = 0; i < numHits; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit.gameObject == gameObject) continue;

            EntityIdentifier otherEntity = hit.GetComponent<EntityIdentifier>();
            if (otherEntity != null && otherEntity.Entity == targetEntity)
            {
                HandleTargetHit(hit.gameObject);
                break;
            }
        }
    }

    public void OnTackle(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.performed && canTackle && !isTackling && entityIdentifier.Entity == Entity.Kart)
        {
            RequestTackleRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestTackleRpc()
    {
        if (!canTackle || isTackling) return;
        if (entityIdentifier == null || entityIdentifier.Entity != Entity.Kart) return;

        if (tackleCoroutine != null) StopCoroutine(tackleCoroutine);
        tackleCoroutine = StartCoroutine(TackleRoutineServer());
    }

    private IEnumerator TackleRoutineServer()
    {
        SetTackleStateServer(true, movementSpeedMultiplier);

        Vector3 tackleDirection = transform.forward;
        rb.AddForce(tackleDirection * tackleForce, ForceMode.Impulse);

        yield return new WaitForSeconds(tackleDuration);

        EndTackleServer();
    }

    private void EndTackleServer()
    {
        SetTackleStateServer(false, 1.0f);
        StartCooldownServer();
    }

    private void StartCooldownServer()
    {
        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(CooldownRoutineServer());
    }

    private IEnumerator CooldownRoutineServer()
    {
        canTackle = false;
        SyncCooldownStateRpc(false);
        yield return new WaitForSeconds(tackleCooldown);
        canTackle = true;
        SyncCooldownStateRpc(true);
    }

    private void SetTackleStateServer(bool tackling, float speedMultiplier)
    {
        isTackling = tackling;
        SyncTackleStateRpc(tackling, speedMultiplier);

        if (tackling)
        {
            OnTackleStarted?.Invoke();
            OnSpeedMultiplierChanged?.Invoke(speedMultiplier);
        }
        else
        {
            OnTackleEnded?.Invoke();
            OnSpeedMultiplierChanged?.Invoke(1.0f);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SyncTackleStateRpc(bool tackling, float speedMultiplier)
    {
        isTackling = tackling;
        OnSpeedMultiplierChanged?.Invoke(speedMultiplier);

        if (tackling)
        {
            OnTackleStarted?.Invoke();
        }
        else
        {
            OnTackleEnded?.Invoke();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SyncCooldownStateRpc(bool canUseTackle)
    {
        if (canUseTackle)
        {
            canTackle = true;
            OnCooldownFinished?.Invoke();
        }
        else
        {
            canTackle = false;
            OnCooldownStarted?.Invoke();
        }
    }

    private void HandleTargetHit(GameObject target)
    {
        OnlineKartTackleVictim victim = target.GetComponent<OnlineKartTackleVictim>();
        if (victim != null)
        {
            Vector3 pushDirection = (target.transform.position - transform.position).normalized;
            victim.ReceiveTackleRpc(pushDirection, tackleForce * 0.7f, 8f, 1.5f);
        }

        NotifyTargetHitRpc();
        EndTackleServer();
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyTargetHitRpc()
    {
        OnTargetHit?.Invoke();
    }

    public void ForceEndCooldown()
    {
        if (!IsServer) return;

        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }
        canTackle = true;
        SyncCooldownStateRpc(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (!isTackling) return;

        EntityIdentifier otherEntity = collision.gameObject.GetComponent<EntityIdentifier>();

        if (otherEntity != null && otherEntity.Entity == obstacleEntity)
        {
            if (tackleCoroutine != null) StopCoroutine(tackleCoroutine);
            EndTackleServer();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Vector3 center = transform.position + transform.TransformDirection(hitboxOffset);
        Gizmos.DrawSphere(center, tackleRadius);
    }
}
