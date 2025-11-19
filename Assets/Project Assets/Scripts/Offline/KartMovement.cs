using UnityEngine;
using UnityEngine.InputSystem;

public class KartMovement : MonoBehaviour
{
    [Header("Kart Configuration")]
    [SerializeField] private KartStats kartStats;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform modelChild;
    [SerializeField] private Transform cameraReference;

    // Movement variables
    private Vector2 input;
    private Vector3 currentVisualRotation;
    private Vector3 cameraForward;
    private Vector3 cameraRight;

    private void Awake()
    {
        // Get references if not set in inspector
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (modelChild == null) modelChild = transform.Find("Model");
        if (cameraReference == null) cameraReference = Camera.main.transform;

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
    }

    private void FixedUpdate()
    {
        HandleMovement();
        ApplyDragAndLimits();
    }

    private void LateUpdate()
    {
        UpdateCameraVectors();
    }

    private void UpdateCameraVectors()
    {
        cameraForward = Vector3.ProjectOnPlane(cameraReference.forward, Vector3.up).normalized;
        cameraRight = Vector3.ProjectOnPlane(cameraReference.right, Vector3.up).normalized;
    }

    private void HandleMovement()
    {
        // Only move if there's forward/backward input
        if (Mathf.Abs(input.y) > 0.1f)
        {
            // Calculate movement direction based on camera
            Vector3 moveDirection = (cameraForward * input.y + cameraRight * input.x).normalized;

            // Apply velocity in the movement direction
            Vector3 targetVelocity = moveDirection * kartStats.maxSpeed * Mathf.Abs(input.y);
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, kartStats.acceleration * Time.fixedDeltaTime);

            // Only rotate if we're moving forward/backward
            if (rb.linearVelocity.magnitude > 0.5f)
            {
                float steerInput = input.x;
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, kartStats.steerSpeed * Mathf.Abs(steerInput) * Time.fixedDeltaTime);
            }
        }
        else
        {
            // Slow down when no input
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, kartStats.deceleration * Time.fixedDeltaTime);
        }
    }

    private void HandleVisualRotations()
    {
        if (modelChild == null) return;

        Vector3 targetEuler = Vector3.zero;

        // Z-axis: Banking during turns
        targetEuler.z = Mathf.Clamp(-input.x * kartStats.maxTiltZ, -kartStats.maxTiltZ, kartStats.maxTiltZ);

        // X-axis: Pitch during acceleration/braking
        targetEuler.x = Mathf.Clamp(-input.y * kartStats.maxTiltX, -kartStats.maxTiltX, kartStats.maxTiltX);

        // Smooth interpolation
        currentVisualRotation = Vector3.Lerp(currentVisualRotation, targetEuler, Time.deltaTime * kartStats.visualRotationSpeed);

        // Apply smooth rotation
        modelChild.localRotation = Quaternion.Euler(currentVisualRotation);
    }

    private void ApplyDragAndLimits()
    {
        // Apply drag when not accelerating
        if (Mathf.Abs(input.y) < 0.1f)
        {
            rb.linearVelocity *= kartStats.drag;
        }

        // Limit maximum speed
        if (rb.linearVelocity.magnitude > kartStats.maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * kartStats.maxSpeed;
        }
    }
}