using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

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

    // Boost variables
    [SerializeField] private float boostTimer;
    [SerializeField] private bool isBoosted;

    // Events para comunicación
    public UnityEvent<float, float, bool> OnMovementUpdate; // speed, inputY, isBoosted
    public UnityEvent<Vector2> OnVFXUpdate; // input (x,y)

    // Properties
    public float CurrentSpeed { get { return rb.linearVelocity.magnitude; } }
    public bool IsBoosted { get { return isBoosted; } }

    private void Awake()
    {
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
        HandleBoostSystem();

        // Disparar evento para animaciones
        OnMovementUpdate?.Invoke(CurrentSpeed, input.y, isBoosted);
        OnVFXUpdate?.Invoke(input);
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

    private void HandleBoostSystem()
    {
        bool shouldActivateBoost = input.y >= kartStats.boostActivationInput;
        bool shouldDeactivateBoost = input.y < kartStats.boostDeactivationInput;

        if (shouldActivateBoost)
        {
            boostTimer += Time.deltaTime;
            if (boostTimer >= kartStats.boostTimeThreshold)
            {
                isBoosted = true;
            }
        }
        else
        {
            if (shouldDeactivateBoost)
            {
                isBoosted = false;
                boostTimer = 0f;
            }
        }
    }

    private void HandleMovement()
    {
        float currentMaxSpeed = isBoosted ? kartStats.boostedMaxSpeed : kartStats.maxSpeed;
        float currentDeceleration = isBoosted ? kartStats.boostedDeceleration : kartStats.deceleration;

        if (Mathf.Abs(input.y) > kartStats.minInputThreshold)
        {
            Vector3 moveDirection = transform.forward * input.y;
            Vector3 targetVelocity = moveDirection * currentMaxSpeed;

            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity,
                kartStats.acceleration * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero,
                currentDeceleration * Time.fixedDeltaTime);
        }
    }

    private void HandleSteering()
    {
        if (CurrentSpeed > kartStats.minSteerSpeed && Mathf.Abs(input.x) > kartStats.minInputThreshold)
        {
            float rotationMultiplier = isMovingForward ? 1f : -1f;
            float currentSteerSpeed = isBoosted ? kartStats.boostedSteerSpeed : kartStats.steerSpeed;
            float rotationAmount = input.x * currentSteerSpeed * rotationMultiplier * Time.fixedDeltaTime;

            transform.Rotate(0, rotationAmount, 0);
        }
    }

    private void HandleVisualRotations()
    {
        if (modelChild == null) return;

        Vector3 targetEuler = Vector3.zero;

        float boostMultiplier = isBoosted ? 1.2f : 1f;

        if (Mathf.Abs(input.x) > kartStats.minInputThreshold)
        {
            targetEuler.z = -input.x * kartStats.maxTiltZ * boostMultiplier;
        }

        if (Mathf.Abs(input.y) > kartStats.minInputThreshold)
        {
            targetEuler.x = Mathf.Abs(input.y) * kartStats.maxTiltX * boostMultiplier;

            if (!isMovingForward)
            {
                targetEuler.x = -targetEuler.x;
            }
        }

        currentVisualRotation = Vector3.Lerp(currentVisualRotation, targetEuler,
            kartStats.visualRotationSpeed * Time.deltaTime);

        modelChild.localRotation = Quaternion.Euler(currentVisualRotation);
    }

    private void ApplySpeedLimit()
    {
        float currentMaxSpeed = isBoosted ? kartStats.boostedMaxSpeed : kartStats.maxSpeed;

        if (rb.linearVelocity.magnitude > currentMaxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
        }
    }
}