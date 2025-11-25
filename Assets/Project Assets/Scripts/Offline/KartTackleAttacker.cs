using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Events;

public class KartTackleAttacker : MonoBehaviour
{
    [Header("Tackle Settings")]
    [SerializeField] private float tackleDuration = 1.5f;
    [SerializeField] private float tackleCooldown = 3f;
    [SerializeField] private float tackleForce = 35f;

    [Header("Target Settings")]
    [SerializeField] private Entity targetEntity = Entity.Kart;
    [SerializeField] private Entity obstacleEntity = Entity.Wall;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private EntityIdentifier entityIdentifier;

    [Header("Events")]
    public UnityEvent OnTackleStarted;
    public UnityEvent OnTackleEnded;

    // State variables
    private bool isTackling = false;
    private bool canTackle = true;
    private Coroutine tackleCoroutine;

    // Properties
    public bool IsTackling => isTackling;
    public bool CanTackle => canTackle;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (entityIdentifier == null) entityIdentifier = GetComponent<EntityIdentifier>();
    }

    public void OnTackle(InputAction.CallbackContext context)
    {
        if (context.performed && canTackle && !isTackling && entityIdentifier.Entity == Entity.Kart)
        {
            StartTackle();
        }
    }

    private void StartTackle()
    {
        if (tackleCoroutine != null) StopCoroutine(tackleCoroutine);
        tackleCoroutine = StartCoroutine(TackleRoutine());
    }

    private IEnumerator TackleRoutine()
    {
        // Setup tackle state
        isTackling = true;
        canTackle = false;

        // Activar eventos - estos se conectarán en el inspector
        OnTackleStarted?.Invoke();

        // Aplicar fuerza inicial de tackleada
        Vector3 tackleDirection = transform.forward;
        rb.AddForce(tackleDirection * tackleForce, ForceMode.Impulse);

        // Mantener el estado por la duración
        yield return new WaitForSeconds(tackleDuration);

        EndTackle();
    }

    private void EndTackle()
    {
        isTackling = false;

        // Activar eventos - estos se conectarán en el inspector
        OnTackleEnded?.Invoke();

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(tackleCooldown);
        canTackle = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isTackling) return;

        EntityIdentifier otherEntity = collision.gameObject.GetComponent<EntityIdentifier>();

        if (otherEntity != null)
        {
            // Check if we hit a target entity
            if (otherEntity.Entity == targetEntity)
            {
                HandleTargetHit(collision.gameObject, collision.contacts[0].point);
                return;
            }

            // Check if we hit an obstacle
            if (otherEntity.Entity == obstacleEntity)
            {
                // Stop tackle when hitting obstacle
                if (tackleCoroutine != null) StopCoroutine(tackleCoroutine);
                EndTackle();
                return;
            }
        }
    }

    private void HandleTargetHit(GameObject target, Vector3 contactPoint)
    {
        // Stop our tackle
        if (tackleCoroutine != null) StopCoroutine(tackleCoroutine);

        // Buscar el componente KartTackleVictim en el objetivo
        KartTackleVictim victim = target.GetComponent<KartTackleVictim>();
        if (victim != null)
        {
            // Calcular dirección del empuje (desde nuestro centro al punto de contacto)
            Vector3 pushDirection = (target.transform.position - transform.position).normalized;

            // Llamar directamente al método del víctima
            victim.ReceiveTackle(pushDirection, tackleForce * 0.7f, 8f, 1.5f);
        }

        EndTackle();
    }
}