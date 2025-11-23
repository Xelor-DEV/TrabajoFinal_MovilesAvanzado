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
    private bool isMovingForward = true;

    // Properties
    public float CurrentSpeed { get { return rb.linearVelocity.magnitude; } }

    private void Awake()
    {
        // Get references if not set in inspector
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (modelChild == null) modelChild = transform.Find("Model");

        ConfigureRigidbody();
        currentVisualRotation = Vector3.zero;
    }

    private void ConfigureRigidbody()
    {
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.mass = kartStats.mass;
        rb.linearDamping = kartStats.linearDrag;
        rb.angularDamping = kartStats.angularDrag;

        // Freeze rotation on X and Z axes for stability
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        input = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        UpdateMovementState();
        HandleVisualRotations();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleSteering();
        ApplySpeedLimit();
    }

    private void UpdateMovementState()
    {
        isMovingForward = input.y >= 0;
    }

    private void HandleMovement()
    {
        if (Mathf.Abs(input.y) > kartStats.minInputThreshold)
        {
            Vector3 moveDirection = transform.forward * input.y;
            Vector3 targetVelocity = moveDirection * kartStats.maxSpeed;

            // Apply acceleration
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, kartStats.acceleration * Time.fixedDeltaTime);
        }
        else
        {
            // Apply deceleration when no input
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, kartStats.deceleration * Time.fixedDeltaTime);
        }
    }

    private void HandleSteering()
    {
        // Only steer if we're moving and have steering input
        if (CurrentSpeed > kartStats.minSteerSpeed && Mathf.Abs(input.x) > kartStats.minInputThreshold)
        {
            float rotationMultiplier = isMovingForward ? 1f : -1f;
            float rotationAmount = input.x * kartStats.steerSpeed * rotationMultiplier * Time.fixedDeltaTime;

            transform.Rotate(0, rotationAmount, 0);
        }
    }

    private void HandleVisualRotations()
    {
        if (modelChild == null) return;

        Vector3 targetEuler = Vector3.zero;

        // Z-axis: Banking during turns
        if (Mathf.Abs(input.x) > kartStats.minInputThreshold)
        {
            targetEuler.z = -input.x * kartStats.maxTiltZ;
        }

        // X-axis: Pitch during acceleration/braking
        if (Mathf.Abs(input.y) > kartStats.minInputThreshold)
        {
            float pitchDirection = isMovingForward ? -1f : 1f;
            targetEuler.x = input.y * pitchDirection * kartStats.maxTiltX;
        }

        // Smooth interpolation
        currentVisualRotation = Vector3.Lerp(currentVisualRotation, targetEuler,
            kartStats.visualRotationSpeed * Time.deltaTime);

        // Apply smooth rotation
        modelChild.localRotation = Quaternion.Euler(currentVisualRotation);
    }

    private void ApplySpeedLimit()
    {
        if (rb.linearVelocity.magnitude > kartStats.maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * kartStats.maxSpeed;
        }
    }
}