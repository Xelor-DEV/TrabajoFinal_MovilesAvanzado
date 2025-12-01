using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class OnlineKartJump : NetworkBehaviour
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

    public UnityEvent OnJumpPerformed;

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
        // De nuevo, si quieres que el server sea el único que simula física,
        // cambia esto a `if (!IsServer) return;`
        if (!IsOwner && !IsServer) return;

        CheckGrounded();
        UpdateCoyoteTime();
        UpdateJumpStates();
        ApplyExtraGravity();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (isTackling) return;

        if (context.performed && (isGrounded || coyoteTimer > 0))
        {
            RequestJumpRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestJumpRpc()
    {
        if (isTackling) return;
        if (isGrounded || coyoteTimer > 0)
        {
            PerformJumpRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void PerformJumpRpc()
    {
        Vector3 velocity = rb.linearVelocity;
        float gravityStrength = Physics.gravity.y * gravityMultiplier;

        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityStrength);
        rb.linearVelocity = velocity;

        SetJumpingState(true);
        coyoteTimer = 0f;
        OnJumpPerformed?.Invoke();
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
        if (wasGroundedLastFrame && !isGrounded)
        {
            coyoteTimer = coyoteTimeDuration;
        }
        else if (coyoteTimer > 0)
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }
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
            Gizmos.color = isGrounded ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(groundCheckPoint.position, groundCheckRadius);

            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);

            if (showCoyoteTimeGizmo && coyoteTimer > 0)
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
                Gizmos.DrawSphere(groundCheckPoint.position, groundCheckRadius * 1.2f);

                Gizmos.color = new Color(1, 0.5f, 0, 1f);
                Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius * 1.2f);
            }
        }
    }
}
