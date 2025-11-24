using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class KartTackle : MonoBehaviour
{
    [Header("Configuración de Tackleada")]
    [SerializeField] private Entity targetEntity = Entity.Kart;
    [SerializeField] private Entity obstacleEntity = Entity.Wall;
    [SerializeField] private float tackleDistance = 50f;   // Distancia máxima si no choca
    [SerializeField] private float tackleSpeed = 40f;      // Velocidad del dash
    [SerializeField] private float tackleSteerSpeed = 10f; // Giro muy reducido
    [SerializeField] private float tackleCooldown = 2f;

    [Header("Configuración de Impacto (Knockback)")]
    [SerializeField] private float pushForceSpeed = 30f; // Velocidad a la que sale volando el rival
    [SerializeField] private float pushDistance = 15f;   // Distancia que recorre el rival

    [Header("Eventos de Control (Para el Inspector)")]
    [Tooltip("Invocado al iniciar tackleada o recibir golpe. Úsalo para DESACTIVAR KartMovement.")]
    public UnityEvent OnDisableControl;

    [Tooltip("Invocado al terminar tackleada o golpe. Úsalo para ACTIVAR KartMovement.")]
    public UnityEvent OnEnableControl;

    [Header("Eventos de Juego")]
    public UnityEvent OnTackleStart;     // Solo cuando TU haces la tackleada
    public UnityEvent OnHitTarget;       // Cuando chocas con un Kart
    public UnityEvent OnHitObstacle;     // Cuando chocas con una Pared

    // Referencias y Estado interno
    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isActionActive = false; // True si está tacleando o siendo empujado
    private bool isTackling = false;     // True solo si está atacando
    private bool canTackle = true;
    private Coroutine currentActionCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // --- INPUT METHODS (Conectar al PlayerInput) ---

    public void OnTackleInput(InputAction.CallbackContext context)
    {
        // Solo puede taclear si presionó el botón, tiene cooldown y no está haciendo otra acción
        if (context.performed && canTackle && !isActionActive)
        {
            StartCoroutine(PerformTackleRoutine());
        }
    }

    public void OnSteerInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // --- LÓGICA DE TACLEADA (ATACANTE) ---

    private IEnumerator PerformTackleRoutine()
    {
        isActionActive = true;
        isTackling = true;
        canTackle = false;

        // Eventos
        OnDisableControl?.Invoke(); // Desactiva movimiento normal
        OnTackleStart?.Invoke();

        Vector3 startPosition = transform.position;
        float traveledDistance = 0f;

        // Bucle físico de la tackleada
        while (traveledDistance < tackleDistance && isTackling)
        {
            // 1. Velocidad forzada hacia adelante
            Vector3 velocity = transform.forward * tackleSpeed;
            velocity.y = rb.linearVelocity.y; // Respetar gravedad
            rb.linearVelocity = velocity;

            // 2. Giro difícil (muy lento comparado al normal)
            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                float rotationAmount = moveInput.x * tackleSteerSpeed * Time.fixedDeltaTime;
                transform.Rotate(0, rotationAmount, 0);
            }

            // 3. Calcular distancia (solo horizontal)
            Vector3 currentPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 startPosFlat = new Vector3(startPosition.x, 0, startPosition.z);
            traveledDistance = Vector3.Distance(startPosFlat, currentPosFlat);

            yield return new WaitForFixedUpdate();
        }

        StopTackle();

        // Cooldown
        yield return new WaitForSeconds(tackleCooldown);
        canTackle = true;
    }

    private void StopTackle()
    {
        if (!isTackling) return;

        isTackling = false;
        isActionActive = false;
        rb.linearVelocity = Vector3.zero; // Frenado seco

        OnEnableControl?.Invoke(); // Devuelve el control al jugador
    }

    // --- LÓGICA DE COLISIONES ---

    private void OnCollisionEnter(Collision collision)
    {
        // Si no estamos haciendo nada especial, ignorar lógica compleja
        if (!isActionActive) return;

        EntityIdentifier otherEntity = collision.gameObject.GetComponent<EntityIdentifier>();

        // Si chocamos con algo sin identidad mientras tacleamos, tratarlo como pared
        if (otherEntity == null)
        {
            if (isTackling) HandleWallCollision();
            // Si estamos siendo empujados (knockback) y chocamos pared, también paramos
            else if (isActionActive) StopKnockback();
            return;
        }

        // SI YO ESTOY ATACANDO (TACKLE)
        if (isTackling)
        {
            // Caso A: Choqué con el OBJETIVO (Kart)
            if (otherEntity.Entity == targetEntity)
            {
                KartTackle otherTackleScript = collision.gameObject.GetComponent<KartTackle>();

                // Aplicar fuerza al otro
                if (otherTackleScript != null)
                {
                    otherTackleScript.ApplyKnockback(transform.forward, pushForceSpeed, pushDistance);
                }
                else
                {
                    // Fallback para objetos con física simple
                    Rigidbody otherRb = collision.rigidbody;
                    if (otherRb) otherRb.AddForce(transform.forward * pushForceSpeed, ForceMode.Impulse);
                }

                OnHitTarget?.Invoke();
                StopTackle(); // Yo me detengo al impactar
            }
            // Caso B: Choqué con una PARED (Obstáculo)
            else if (otherEntity.Entity == obstacleEntity)
            {
                HandleWallCollision();
            }
        }
        // SI YO ESTOY SIENDO EMPUJADO (KNOCKBACK)
        else
        {
            // Si choco pared mientras vuelo, me detengo
            if (otherEntity.Entity == obstacleEntity)
            {
                StopKnockback();
            }
        }
    }

    private void HandleWallCollision()
    {
        OnHitObstacle?.Invoke();
        StopTackle();
    }

    // --- LÓGICA DE RECIBIR GOLPE (VÍCTIMA) ---

    // Método público llamado por el atacante
    public void ApplyKnockback(Vector3 direction, float speed, float distance)
    {
        if (currentActionCoroutine != null) StopCoroutine(currentActionCoroutine);
        currentActionCoroutine = StartCoroutine(KnockbackRoutine(direction.normalized, speed, distance));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float speed, float distance)
    {
        isActionActive = true;
        isTackling = false; // Si estaba atacando, se cancela mi ataque

        OnDisableControl?.Invoke(); // Pierdo el control

        Vector3 startPosition = transform.position;
        float traveledDistance = 0f;

        while (traveledDistance < distance && isActionActive)
        {
            // Moverse forzosamente en dirección del golpe
            Vector3 velocity = direction * speed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;

            // Calcular distancia
            Vector3 currentPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 startPosFlat = new Vector3(startPosition.x, 0, startPosition.z);
            traveledDistance = Vector3.Distance(startPosFlat, currentPosFlat);

            yield return new WaitForFixedUpdate();
        }

        StopKnockback();
    }

    private void StopKnockback()
    {
        isActionActive = false;
        rb.linearVelocity = Vector3.zero;
        OnEnableControl?.Invoke(); // Recupero el control
    }

    // --- VISUALIZACIÓN EN EDITOR ---

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * tackleDistance);
        Gizmos.DrawWireSphere(transform.position + transform.forward * tackleDistance, 0.5f);
    }
}