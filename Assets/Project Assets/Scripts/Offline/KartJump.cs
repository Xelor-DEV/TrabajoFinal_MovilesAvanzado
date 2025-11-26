using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class KartJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayerMask = 1;
    [SerializeField] private float gravityMultiplier = 3.0f;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTimeDuration = 0.15f;
    [SerializeField] private bool showCoyoteTimeGizmo = true;

    [Header("References")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Rigidbody rb;

    [Header("Events")]
    public UnityEvent<bool> OnGroundedStateChanged;
    public UnityEvent<bool> OnJumpingStateChanged;
    public UnityEvent<bool> OnFallingStateChanged;

    // State variables
    private bool isGrounded = false;
    private bool isJumping = false;
    private bool isFalling = false;
    private bool isTackling = false;

    // Coyote Time variables
    private float coyoteTimer = 0f;
    private bool wasGroundedLastFrame = false;

    private void FixedUpdate()
    {
        CheckGrounded();
        UpdateCoyoteTime();
        UpdateJumpStates();
        ApplyExtraGravity();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isTackling) return;

        if (context.performed && (isGrounded || coyoteTimer > 0))
        {
            Jump();
        }
    }

    private void CheckGrounded()
    {
        wasGroundedLastFrame = isGrounded;

        if (groundCheckPoint != null)
        {
            isGrounded = Physics.CheckSphere(
                groundCheckPoint.position,
                groundCheckRadius,
                groundLayerMask,
                QueryTriggerInteraction.Ignore
            );
        }

        if (wasGroundedLastFrame != isGrounded)
        {
            OnGroundedStateChanged?.Invoke(isGrounded);
        }
    }

    private void UpdateCoyoteTime()
    {
        // Activar Coyote Time cuando se sale del suelo
        if (wasGroundedLastFrame && !isGrounded)
        {
            coyoteTimer = coyoteTimeDuration;
        }
        // Reducir el timer si está activo
        else if (coyoteTimer > 0)
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }
        // Resetear si se vuelve a tocar el suelo
        else if (isGrounded)
        {
            coyoteTimer = 0f;
        }
    }

    private void UpdateJumpStates()
    {
        if (isGrounded)
        {
            if (isJumping || isFalling)
            {
                SetJumpingState(false);
                SetFallingState(false);
            }
        }
        else
        {
            bool shouldBeFalling = rb.linearVelocity.y <= 0;

            if (shouldBeFalling && !isFalling)
            {
                SetFallingState(true);
                SetJumpingState(false);
            }
            else if (!shouldBeFalling && !isJumping)
            {
                SetJumpingState(true);
                SetFallingState(false);
            }
        }
    }

    private void Jump()
    {
        Vector3 velocity = rb.linearVelocity;
        float gravityStrength = Physics.gravity.y * gravityMultiplier;

        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityStrength);
        rb.linearVelocity = velocity;

        SetJumpingState(true);
        coyoteTimer = 0f; // Consumir Coyote Time al saltar
    }

    private void SetJumpingState(bool jumping)
    {
        if (jumping != isJumping)
        {
            isJumping = jumping;
            OnJumpingStateChanged?.Invoke(isJumping);
        }
    }

    private void SetFallingState(bool falling)
    {
        if (falling != isFalling)
        {
            isFalling = falling;
            OnFallingStateChanged?.Invoke(isFalling);
        }
    }

    private void ApplyExtraGravity()
    {
        if (!isGrounded)
        {
            Vector3 extraGravityForce = Physics.gravity * (gravityMultiplier - 1);
            rb.AddForce(extraGravityForce, ForceMode.Acceleration);
        }
    }

    public void SetTackling(bool tackling)
    {
        isTackling = tackling;
    }

    private void OnDrawGizmos()
    {
        if (groundCheckPoint != null)
        {
            // Gizmo para detección normal de suelo
            Gizmos.color = isGrounded ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(groundCheckPoint.position, groundCheckRadius);

            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);

            // Gizmo para Coyote Time
            if (showCoyoteTimeGizmo && coyoteTimer > 0)
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // Naranja transparente
                Gizmos.DrawSphere(groundCheckPoint.position, groundCheckRadius * 1.2f);

                Gizmos.color = new Color(1, 0.5f, 0, 1f); // Naranja sólido
                Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius * 1.2f);
            }
        }
    }
}