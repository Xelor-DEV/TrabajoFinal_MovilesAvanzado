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
    [SerializeField] private float movementSpeedMultiplier = 1.5f;

    [Header("Hitbox Settings")] // NUEVO: Configuración del área de golpe
    [SerializeField] private float tackleRadius = 2.0f; // Radio del "Sphere Overlap"
    [SerializeField] private LayerMask targetLayers;    // Capas a las que podemos golpear (ej: Players)
    [SerializeField] private Vector3 hitboxOffset = Vector3.zero; // Para centrar la esfera un poco adelante si quieres

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

    // State variables
    private bool isTackling = false;
    private bool canTackle = true;
    private Coroutine tackleCoroutine;
    private Coroutine cooldownCoroutine;
    
    // Optimization Buffer
    // Usamos un buffer fijo para no generar Garbage Collection (GC) en cada frame
    private readonly Collider[] hitBuffer = new Collider[10]; 

    // Properties
    public bool IsTackling => isTackling;
    public bool CanTackle => canTackle;
    public float TackleCooldown => tackleCooldown;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (entityIdentifier == null) entityIdentifier = GetComponent<EntityIdentifier>();
    }

    // NUEVO: Chequeo constante mientras tacleamos
    private void FixedUpdate()
    {
        if (isTackling)
        {
            CheckForTargets();
        }
    }

    private void CheckForTargets()
    {
        // Usamos NonAlloc para máxima optimización de memoria
        Vector3 center = transform.position + transform.TransformDirection(hitboxOffset);
        int numHits = Physics.OverlapSphereNonAlloc(center, tackleRadius, hitBuffer, targetLayers);

        for (int i = 0; i < numHits; i++)
        {
            Collider hit = hitBuffer[i];

            // 1. Ignorarnos a nosotros mismos
            if (hit.gameObject == gameObject) continue;

            // 2. Verificar si tiene EntityIdentifier
            EntityIdentifier otherEntity = hit.GetComponent<EntityIdentifier>();
            if (otherEntity != null && otherEntity.Entity == targetEntity)
            {
                // Encontramos una víctima válida
                HandleTargetHit(hit.gameObject);
                break; // Salimos del loop para no golpear a varios o al mismo dos veces en un frame
            }
        }
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
        isTackling = true;
        canTackle = false;

        // 1. Avisar que empezó el tackle (lógica general)
        OnTackleStarted?.Invoke();
        
        // 2. NUEVO: Enviar el multiplicador de velocidad al KartMovement (1.5x)
        OnSpeedMultiplierChanged?.Invoke(movementSpeedMultiplier);

        Vector3 tackleDirection = transform.forward;
        rb.AddForce(tackleDirection * tackleForce, ForceMode.Impulse);

        yield return new WaitForSeconds(tackleDuration);

        EndTackle();
    }

    private void EndTackle()
    {
        isTackling = false;
        
        OnTackleEnded?.Invoke();

        // 3. NUEVO: Restablecer el multiplicador a la normalidad (1.0x)
        OnSpeedMultiplierChanged?.Invoke(1.0f); 

        StartCooldown();
    }
    private void StartCooldown()
    {
        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        OnCooldownStarted?.Invoke();
        yield return new WaitForSeconds(tackleCooldown);
        canTackle = true;
        OnCooldownFinished?.Invoke();
    }

    // Mantenemos OnCollisionEnter SOLO para obstaculos (paredes)
    // Ya no necesitamos detectar Karts aquí porque el OverlapSphere se encarga de eso.
    private void OnCollisionEnter(Collision collision)
    {
        if (!isTackling) return;

        EntityIdentifier otherEntity = collision.gameObject.GetComponent<EntityIdentifier>();

        if (otherEntity != null)
        {
            if (otherEntity.Entity == obstacleEntity)
            {
                if (tackleCoroutine != null) StopCoroutine(tackleCoroutine);
                EndTackle();
            }
        }
    }

    // Simplificado: Ya no necesitamos el ContactPoint preciso de la colisión física
    private void HandleTargetHit(GameObject target)
    {
        if (tackleCoroutine != null) StopCoroutine(tackleCoroutine);

        KartTackleVictim victim = target.GetComponent<KartTackleVictim>();
        if (victim != null)
        {
            // Calculamos dirección: Desde mí hacia la víctima
            Vector3 pushDirection = (target.transform.position - transform.position).normalized;
            victim.ReceiveTackle(pushDirection, tackleForce * 0.7f, 8f, 1.5f);
        }

        OnTargetHit?.Invoke();
        EndTackle();
    }

    public void ForceEndCooldown()
    {
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }
        canTackle = true;
        OnCooldownFinished?.Invoke();
    }

    // NUEVO: Visualización en el editor para ajustar el radio perfecto
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f); // Rojo transparente
        Vector3 center = transform.position + transform.TransformDirection(hitboxOffset);
        Gizmos.DrawSphere(center, tackleRadius);
    }
}