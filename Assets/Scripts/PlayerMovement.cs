using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float maxSpeed = 8f;
    public float acceleration = 12f;
    public float turnSmoothTime = 0.2f;
    public float driftFactor = 0.92f;

    [Header("Salto")]
    public float jumpForce = 6f;
    public LayerMask groundMask;
    public Transform groundCheck;
    public float groundRadius = 0.3f;

    [Header("Empuj�n")]
    public float pushForce = 6f;
    public float pushUpForce = 4f;
    public float pushCooldown = 1f;

    [Header("Tacleada")]
    public float tackleMaxSpeed = 15f;
    public float tackleAcceleration = 25f;
    public float tackleCooldown = 2f;
    public float tackleTurnLimit = 0.3f;

    private Rigidbody rb;
    private Transform cam;
    private Vector3 inputDir;
    private bool isGrounded;
    private bool canPush = true;
    private bool canTackle = true;
    private bool isTackling = false;
    private float currentSpeed = 0f;
    private float smoothTurnVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main.transform;
        rb.freezeRotation = true;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
        HandleInput();

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            Jump();

        if (Input.GetKeyDown(KeyCode.E) && canPush)
            StartCoroutine(Push());

        if (Input.GetKeyDown(KeyCode.LeftShift) && canTackle)
            StartCoroutine(Tackle());
    }

    void FixedUpdate()
    {
        if (!isTackling)
            HandleMovement();
        else
            HandleTackleMovement();
    }

    void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDir = new Vector3(h, 0f, v).normalized;
    }

    void HandleMovement()
    {
        if (inputDir.magnitude > 0.1f)
        {
            // Direcci�n relativa a la c�mara
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref smoothTurnVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            // Aceleraci�n progresiva
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);
            Vector3 targetVelocity = moveDir.normalized * currentSpeed;

            // Derrape (reduce ligeramente el control)
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, driftFactor * Time.fixedDeltaTime);
        }
        else
        {
            // Frenado gradual
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, 0.1f);
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    IEnumerator Push()
    {
        canPush = false;
        Vector3 dir = transform.forward + Vector3.up * 0.3f;

        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out RaycastHit hit, 2f))
        {
            Rigidbody otherRb = hit.collider.attachedRigidbody;
            if (otherRb != null && otherRb != rb)
            {
                otherRb.AddForce(dir.normalized * pushForce + Vector3.up * pushUpForce, ForceMode.Impulse);
            }
        }

        yield return new WaitForSeconds(pushCooldown);
        canPush = true;
    }

    IEnumerator Tackle()
    {
        canTackle = false;
        isTackling = true;
        float tackleSpeed = 0f;

        while (isTackling)
        {
            // Rotaci�n parcial con c�mara
            float targetAngle = cam.eulerAngles.y;
            float limitedAngle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, tackleTurnLimit * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, limitedAngle, 0);

            tackleSpeed = Mathf.MoveTowards(tackleSpeed, tackleMaxSpeed, tackleAcceleration * Time.fixedDeltaTime);
            rb.linearVelocity = transform.forward * tackleSpeed;

            yield return null;
        }

        yield return new WaitForSeconds(tackleCooldown);
        canTackle = true;
    }

    void HandleTackleMovement()
    {
        rb.MovePosition(rb.position + transform.forward * tackleMaxSpeed * Time.fixedDeltaTime);
    }

    void OnCollisionEnter(Collision col)
    {
        if (isTackling)
        {
            Rigidbody otherRb = col.rigidbody;
            if (otherRb != null && otherRb != rb)
            {
                float force = Mathf.Lerp(pushForce, pushForce * 2f, rb.linearVelocity.magnitude / tackleMaxSpeed);
                Vector3 dir = (col.transform.position - transform.position).normalized + Vector3.up * 0.5f;
                otherRb.AddForce(dir * force, ForceMode.Impulse);
            }

            // Detener tackle al chocar
            isTackling = false;
            rb.linearVelocity = Vector3.zero;
        }
    }
}
