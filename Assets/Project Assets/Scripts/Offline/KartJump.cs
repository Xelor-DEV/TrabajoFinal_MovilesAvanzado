using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class KartJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float groundCheckRadius = 0.3f; // Usaremos esto como radio de la esfera
    [SerializeField] private LayerMask groundLayerMask = 1;
    [SerializeField] private float gravityMultiplier = 3.0f;

    [Header("References")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Rigidbody rb;

    [Header("Events")]
    public UnityEvent<bool> OnGroundedStateChanged;
    public UnityEvent<bool> OnJumpingStateChanged;
    public UnityEvent<bool> OnFallingStateChanged;

    // State variables
    private bool isGrounded = false; // Empezar en false es más seguro
    private bool isJumping = false;
    private bool isFalling = false;

    private void FixedUpdate()
    {
        CheckGrounded();
        UpdateJumpStates();
        ApplyExtraGravity(); // NUEVO
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            Jump();
        }
    }

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;

        if (groundCheckPoint != null)
        {
            // CAMBIO CLAVE: Usamos CheckSphere en lugar de Raycast.
            // CheckSphere devuelve true si hay CUALQUIER colisionador de la capa suelo
            // dentro de este radio, incluso si el punto está hundido bajo tierra.
            isGrounded = Physics.CheckSphere(
                groundCheckPoint.position,
                groundCheckRadius,
                groundLayerMask,
                QueryTriggerInteraction.Ignore
            );
        }

        if (wasGrounded != isGrounded)
        {
            OnGroundedStateChanged?.Invoke(isGrounded);
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
            // rb.linearVelocity para Unity 6
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

        // Usamos la gravedad modificada en la formula para asegurar que llegamos a la altura deseada
        // a pesar de que ahora la gravedad es mas fuerte.
        float gravityStrength = Physics.gravity.y * gravityMultiplier;

        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityStrength);
        rb.linearVelocity = velocity;

        SetJumpingState(true);
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
        // Si estamos cayendo (velocidad Y negativa) o simplemente no estamos en el suelo
        if (!isGrounded)
        {
            // Aplicamos fuerza extra hacia abajo. 
            // Multiplicamos por (gravityMultiplier - 1) porque la física ya aplica 1x gravedad por defecto.
            Vector3 extraGravityForce = Physics.gravity * (gravityMultiplier - 1);
            rb.AddForce(extraGravityForce, ForceMode.Acceleration);
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheckPoint != null)
        {
            // Visualizamos la esfera de detección real
            Gizmos.color = isGrounded ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(groundCheckPoint.position, groundCheckRadius);

            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}