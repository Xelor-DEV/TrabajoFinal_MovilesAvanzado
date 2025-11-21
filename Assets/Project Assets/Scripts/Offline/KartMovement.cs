using UnityEngine;
using UnityEngine.InputSystem;

public class KartMovement : MonoBehaviour
{
    [Header("Kart Configuration")]
    [SerializeField] private KartStats kartStats;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform modelChild;

    // Movement variables
    private Vector2 input;
    private Vector3 currentVisualRotation;
    private Vector3 moveDirection;
    private bool isMovingForward = true;

    private void Awake()
    {
        // Get references if not set in inspector
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (modelChild == null) modelChild = transform.Find("Model");

        // Configure Rigidbody
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        currentVisualRotation = Vector3.zero;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        input = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        HandleVisualRotations();
        UpdateMovementDirection();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        ApplyDragAndLimits();
    }

    private void UpdateMovementDirection()
    {
        // Determinar si estamos avanzando o retrocediendo
        isMovingForward = input.y >= 0;

        // Calcular direcci�n de movimiento basada en la rotaci�n actual del kart
        if (Mathf.Abs(input.y) > 0.1f)
        {
            moveDirection = transform.forward * input.y;

            // Aplicar direcci�n lateral solo si hay movimiento forward/backward
            if (Mathf.Abs(input.x) > 0.1f)
            {
                // Para el movimiento arcade, aplicamos rotaci�n directamente al transform
                float rotationAmount = input.x * kartStats.steerSpeed * Time.fixedDeltaTime;

                if (isMovingForward)
                {
                    // Rotaci�n normal cuando avanzamos
                    transform.Rotate(0, rotationAmount, 0);
                }
                else
                {
                    // Rotaci�n invertida cuando retrocedemos (como en Mario Kart)
                    transform.Rotate(0, -rotationAmount, 0);
                }
            }
        }
        else
        {
            moveDirection = Vector3.zero;
        }
    }

    private void HandleMovement()
    {
        if (moveDirection != Vector3.zero)
        {
            // Calcular velocidad objetivo
            float targetSpeed = kartStats.maxSpeed * Mathf.Abs(input.y);
            Vector3 targetVelocity = moveDirection * targetSpeed;

            // Aplicar aceleraci�n suave
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, kartStats.acceleration * Time.fixedDeltaTime);
        }
    }

    private void HandleVisualRotations()
    {
        if (modelChild == null) return;

        Vector3 targetEuler = Vector3.zero;

        // Z-axis: Banking durante los giros
        targetEuler.z = Mathf.Clamp(-input.x * kartStats.maxTiltZ, -kartStats.maxTiltZ, kartStats.maxTiltZ);

        // X-axis: Pitch durante aceleraci�n/frenado
        if (isMovingForward)
        {
            targetEuler.x = Mathf.Clamp(-input.y * kartStats.maxTiltX, -kartStats.maxTiltX, kartStats.maxTiltX);
        }
        else
        {
            // Invertir el pitch cuando retrocedemos
            targetEuler.x = Mathf.Clamp(input.y * kartStats.maxTiltX, -kartStats.maxTiltX, kartStats.maxTiltX);
        }

        // Smooth interpolation
        currentVisualRotation = Vector3.Lerp(currentVisualRotation, targetEuler, Time.deltaTime * kartStats.visualRotationSpeed);

        // Apply smooth rotation
        modelChild.localRotation = Quaternion.Euler(currentVisualRotation);
    }

    private void ApplyDragAndLimits()
    {
        // Aplicar drag cuando no hay input
        if (Mathf.Abs(input.y) < 0.1f)
        {
            rb.linearVelocity *= kartStats.drag;
        }

        // Limitar velocidad m�xima
        if (rb.linearVelocity.magnitude > kartStats.maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * kartStats.maxSpeed;
        }
    }

    // M�todo p�blico para obtener informaci�n del movimiento
    public float GetCurrentSpeed()
    {
        return rb.linearVelocity.magnitude;
    }

    public bool IsMovingForward()
    {
        return isMovingForward;
    }
}