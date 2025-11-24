using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EntityIdentifier))]
public class KartTackle : MonoBehaviour
{
    [Header("Configuración de Tackleada")]
    [SerializeField] private Entity targetEntity = Entity.Kart;
    [SerializeField] private Entity obstacleEntity = Entity.Wall;
    [SerializeField] private float tackleDistance = 50f; // Distancia a recorrer
    [SerializeField] private float tackleSpeed = 40f;    // Velocidad durante tackle
    [SerializeField] private float tackleSteerSpeed = 10f; // Giro reducido (difícil girar)
    [SerializeField] private float tackleCooldown = 2f;

    [Header("Configuración de Impacto (Knockback)")]
    [SerializeField] private float pushForceSpeed = 30f; // Velocidad a la que sale despedido el objetivo
    [SerializeField] private float pushDistance = 15f;   // Distancia que recorrerá el objetivo empujado

    [Header("Eventos")]
    public UnityEvent OnTackleStart;
    public UnityEvent OnTackleEnd;
    public UnityEvent OnHitTarget; // Golpeó un Kart
    public UnityEvent OnHitObstacle; // Golpeó una Pared
    public UnityEvent<bool> OnTackleStateChanged; // Para desactivar otros scripts (True=Tackling, False=Normal)

    // Referencias y Estado
    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isTackling = false;
    private bool isKnockedBack = false;
    private bool canTackle = true;
    private Coroutine currentActionCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // --- INPUT METHODS ---

    // Asigna esto en tu PlayerInput (Button South / X)
    public void OnTackleInput(InputAction.CallbackContext context)
    {
        if (context.performed && canTackle && !isTackling && !isKnockedBack)
        {
            StartCoroutine(PerformTackleRoutine());
        }
    }

    // Asigna esto en tu PlayerInput (Stick Left / WASD) - Necesitamos saber si intenta girar
    public void OnSteerInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // --- LOGIC: PERFORM TACKLE ---

    private IEnumerator PerformTackleRoutine()
    {
        isTackling = true;
        canTackle = false;

        // Notificar a otros sistemas (ej: apagar KartMovement normal)
        OnTackleStateChanged?.Invoke(true);
        OnTackleStart?.Invoke();

        Vector3 startPosition = transform.position;
        float traveledDistance = 0f;

        // Bucle de movimiento de Tackleada
        while (traveledDistance < tackleDistance && isTackling)
        {
            // 1. Movimiento forzado hacia adelante
            Vector3 velocity = transform.forward * tackleSpeed;
            // Mantener la velocidad Y actual (gravedad) para no volar si cae
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;

            // 2. Giro dificultoso (Steering limitado)
            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                float rotationAmount = moveInput.x * tackleSteerSpeed * Time.fixedDeltaTime;
                transform.Rotate(0, rotationAmount, 0);
            }

            // 3. Calcular distancia recorrida
            // Usamos distancia horizontal para ignorar caídas
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
        rb.linearVelocity = Vector3.zero; // Frenado en seco al terminar

        OnTackleEnd?.Invoke();
        OnTackleStateChanged?.Invoke(false); // Reactivar movimiento normal
    }

    // --- LOGIC: COLLISIONS ---

    private void OnCollisionEnter(Collision collision)
    {
        // Solo nos importa procesar colisiones si estamos haciendo una tackleada o siendo empujados
        if (!isTackling && !isKnockedBack) return;

        EntityIdentifier otherEntity = collision.gameObject.GetComponent<EntityIdentifier>();

        // Si no tiene identificador, lo tratamos como pared genérica si estamos tackleando
        if (otherEntity == null)
        {
            if (isTackling || isKnockedBack) StopMomentumHitWall();
            return;
        }

        // LÓGICA SI ESTOY TACKLEANDO
        if (isTackling)
        {
            // 1. Choque con OBJETIVO (Kart)
            if (otherEntity.Entity == targetEntity)
            {
                // Aplicar empuje al objetivo
                KartTackle otherTackleScript = collision.gameObject.GetComponent<KartTackle>();
                if (otherTackleScript != null)
                {
                    // Calculamos la dirección del empuje (nuestro forward)
                    otherTackleScript.ApplyKnockback(transform.forward, pushForceSpeed, pushDistance);
                }
                else
                {
                    // Fallback si el otro no tiene script de tackle, usar físicas puras
                    Rigidbody otherRb = collision.rigidbody;
                    if (otherRb) otherRb.AddForce(transform.forward * pushForceSpeed, ForceMode.Impulse);
                }

                OnHitTarget?.Invoke();
                StopTackle(); // Me detengo inmediatamente
            }
            // 2. Choque con OBSTÁCULO (Pared)
            else if (otherEntity.Entity == obstacleEntity)
            {
                OnHitObstacle?.Invoke();
                StopTackle(); // Me detengo inmediatamente
            }
        }

        // LÓGICA SI ESTOY SIENDO EMPUJADO (Knockback)
        if (isKnockedBack)
        {
            // Si choco con una pared mientras me empujan, paro en seco
            if (otherEntity.Entity == obstacleEntity || otherEntity.Entity == Entity.None)
            {
                StopKnockback();
            }
        }
    }

    private void StopMomentumHitWall()
    {
        if (isTackling)
        {
            OnHitObstacle?.Invoke();
            StopTackle();
        }
        if (isKnockedBack)
        {
            StopKnockback();
        }
    }

    // --- LOGIC: RECEIVE KNOCKBACK (Ser empujado) ---

    public void ApplyKnockback(Vector3 direction, float speed, float distance)
    {
        // Detener cualquier acción actual
        if (currentActionCoroutine != null) StopCoroutine(currentActionCoroutine);

        // Iniciar rutina de ser empujado
        currentActionCoroutine = StartCoroutine(KnockbackRoutine(direction.normalized, speed, distance));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float speed, float distance)
    {
        isKnockedBack = true;
        isTackling = false; // Cancelar tackle si me golpean mientras lo hago

        // Desactivar controles normales
        OnTackleStateChanged?.Invoke(true);

        Vector3 startPosition = transform.position;
        float traveledDistance = 0f;

        while (traveledDistance < distance && isKnockedBack)
        {
            // Moverse en la dirección del golpe
            Vector3 velocity = direction * speed;
            velocity.y = rb.linearVelocity.y; // Preservar gravedad
            rb.linearVelocity = velocity;

            // Calcular distancia recorrida
            Vector3 currentPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 startPosFlat = new Vector3(startPosition.x, 0, startPosition.z);
            traveledDistance = Vector3.Distance(startPosFlat, currentPosFlat);

            yield return new WaitForFixedUpdate();
        }

        StopKnockback();
    }

    private void StopKnockback()
    {
        isKnockedBack = false;
        rb.linearVelocity = Vector3.zero;
        OnTackleStateChanged?.Invoke(false); // Devolver control al jugador
    }

    // --- VISUALS ---

    private void OnDrawGizmos()
    {
        if (isTackling)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * tackleDistance);
        }
        else
        {
            // Dibujar rango visualización
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
            Gizmos.DrawRay(transform.position, transform.forward * tackleDistance);
        }
    }
}