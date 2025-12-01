using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class OnlineKartMovement : NetworkBehaviour
{
    [Header("Kart Configuration")]
    [SerializeField] private OnlineKartStats kartStats;

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

    // Events
    public UnityEvent<float, float, bool> OnMovementUpdate; // speed, inputY, isBoosted
    public UnityEvent<Vector2> OnVFXUpdate; // input (x,y)

    // Properties
    public float CurrentSpeed => rb.linearVelocity.magnitude;
    public bool IsBoosted => isBoosted;

    // Control state
    private bool isInputEnabled = true;
    private bool isExternalForceActive = false;
    private float externalSpeedMultiplier = 1.0f;

    // Tackle state (para sincronizar con OnlineKartTackleAttacker)
    private bool isTackling = false;

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
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezeRotationY;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner || !isInputEnabled) return;
        input = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (isTackling)
        {
            // Mantener animaciones cuando se tacklea
            OnMovementUpdate?.Invoke(CurrentSpeed, 1f, isBoosted);
            OnVFXUpdate?.Invoke(new Vector2(0f, 1f));
        }
        else if (isInputEnabled)
        {
            UpdateMovementState();
            HandleVisualRotations();
            HandleBoostSystem();

            OnMovementUpdate?.Invoke(CurrentSpeed, input.y, isBoosted);
            OnVFXUpdate?.Invoke(input);
        }
        else
        {
            OnMovementUpdate?.Invoke(CurrentSpeed, 0, false);
            OnVFXUpdate?.Invoke(Vector2.zero);
        }
    }

    private void FixedUpdate()
    {
        // Normalmente dejarías la física al server y usarías NetworkRigidbody/NetworkTransform.
        // Si quieres server-only, cambia esta condición a IsServer.
        if (!IsOwner && !IsServer) return;

        if (isInputEnabled || isExternalForceActive)
        {
            HandleMovement();

            if (!isTackling)
            {
                HandleSteering();
            }

            ApplySpeedLimit();
        }
    }

    private void UpdateMovementState()
    {
        isMovingForward = input.y >= 0;
    }

    private void HandleBoostSystem()
    {
        if (isTackling) return;

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

    private void HandleSteering()
    {
        if (CurrentSpeed > kartStats.minSteerSpeed &&
            Mathf.Abs(input.x) > kartStats.minInputThreshold)
        {
            float rotationMultiplier = isMovingForward ? 1f : -1f;
            float currentSteerSpeed = isBoosted ? kartStats.boostedSteerSpeed : kartStats.steerSpeed;
            float rotationAmount = input.x * currentSteerSpeed * rotationMultiplier * Time.fixedDeltaTime;

            transform.Rotate(0, rotationAmount, 0);
        }
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        externalSpeedMultiplier = multiplier;
    }

    private void HandleMovement()
    {
        if (isExternalForceActive) return;

        float currentMaxSpeed = (isBoosted ? kartStats.boostedMaxSpeed : kartStats.maxSpeed) * externalSpeedMultiplier;
        float currentDeceleration = isBoosted ? kartStats.boostedDeceleration : kartStats.deceleration;

        float currentVerticalSpeed = rb.linearVelocity.y;
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (Mathf.Abs(input.y) > kartStats.minInputThreshold)
        {
            Vector3 moveDirection = transform.forward * input.y;
            moveDirection.y = 0;
            moveDirection.Normalize();

            Vector3 targetHorizontalVelocity = moveDirection * currentMaxSpeed;

            Vector3 newHorizontalVelocity = Vector3.Lerp(
                currentHorizontalVelocity,
                targetHorizontalVelocity,
                kartStats.acceleration * Time.fixedDeltaTime
            );

            rb.linearVelocity = new Vector3(newHorizontalVelocity.x, currentVerticalSpeed, newHorizontalVelocity.z);
        }
        else
        {
            Vector3 newHorizontalVelocity = Vector3.Lerp(
                currentHorizontalVelocity,
                Vector3.zero,
                currentDeceleration * Time.fixedDeltaTime
            );

            rb.linearVelocity = new Vector3(newHorizontalVelocity.x, currentVerticalSpeed, newHorizontalVelocity.z);
        }
    }

    private void HandleVisualRotations()
    {
        if (modelChild == null) return;

        Vector3 targetEuler = Vector3.zero;

        float boostMultiplier = isBoosted ? 1.2f : 1f;

        if (!isTackling && Mathf.Abs(input.x) > kartStats.minInputThreshold)
        {
            targetEuler.z = -input.x * kartStats.maxTiltZ * boostMultiplier;
        }

        if (!isTackling && Mathf.Abs(input.y) > kartStats.minInputThreshold)
        {
            targetEuler.x = Mathf.Abs(input.y) * kartStats.maxTiltX * boostMultiplier;

            if (!isMovingForward)
            {
                targetEuler.x = -targetEuler.x;
            }
        }

        currentVisualRotation = Vector3.Lerp(
            currentVisualRotation,
            targetEuler,
            kartStats.visualRotationSpeed * Time.deltaTime
        );

        modelChild.localRotation = Quaternion.Euler(currentVisualRotation);
    }

    private void ApplySpeedLimit()
    {
        float currentMaxSpeed = (isBoosted ? kartStats.boostedMaxSpeed : kartStats.maxSpeed) * externalSpeedMultiplier;

        if (rb.linearVelocity.magnitude > currentMaxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
        }
    }

    public void EnableInput() => isInputEnabled = true;

    public void DisableInput()
    {
        isInputEnabled = false;
        input = Vector2.zero;
    }

    public void SetExternalForceActive(bool active)
    {
        isExternalForceActive = active;
    }

    public void SetTacklingState(bool tackling)
    {
        isTackling = tackling;

        if (tackling)
        {
            isBoosted = false;
            boostTimer = 0f;
        }
    }
}
