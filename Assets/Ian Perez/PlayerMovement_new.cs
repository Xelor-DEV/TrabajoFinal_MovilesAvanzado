using UnityEngine;
using System.Collections;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControllers : NetworkBehaviour
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

    [Header("Empujón")]
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
    private Vector2 input2D;
    private Vector3 inputDir;
    private bool isGrounded;
    private bool canPush = true;
    private bool canTackle = true;
    private bool isTackling = false;
    private float currentSpeed = 0f;
    private float smoothTurnVelocity;
    private Vector3 currentVelocity = Vector3.zero;
    private PlayerNetwork playerNetwork;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        playerNetwork = GetComponent<PlayerNetwork>();
    }

    public override void OnNetworkSpawn()
    {
        // Solo el owner procesa entrada local y usa cámara local
        if (IsOwner)
        {
            // Si el prefab tiene una cámara hija, úsala (actívala). Si no, crea una cámara local.
            Camera childCam = GetComponentInChildren<Camera>(true);
            if (childCam != null)
            {
                childCam.gameObject.SetActive(true);
                cam = childCam.transform;
            }
            else
            {
                GameObject camGO = new GameObject("PlayerCamera");
                camGO.transform.SetParent(transform);
                camGO.transform.localPosition = new Vector3(0f, 1.6f, -3f);
                camGO.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
                Camera c = camGO.AddComponent<Camera>();
                c.nearClipPlane = 0.1f;
                cam = camGO.transform;
            }
        }
        else
        {
            // No-owner: desactivar cámaras hijas (NetworkTransform sincroniza)
            Camera childCam = GetComponentInChildren<Camera>(true);
            if (childCam != null)
                childCam.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);

        ReadInputActions();
        HandleButtonInput(isGrounded);
    }

    void HandleButtonInput(bool isGrounded)
    {
        // Leer entrada de botones directamente desde el Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
                JumpServerRpc();
            if (Keyboard.current.eKey.wasPressedThisFrame && canPush)
                StartCoroutine(Push());
            if (Keyboard.current.leftShiftKey.wasPressedThisFrame && canTackle)
                StartCoroutine(Tackle());
        }
    }

    [Rpc(SendTo.Server)]
    void JumpServerRpc()
    {
        // Solo el servidor aplica el salto para evitar saltos infinitos
        if (!IsServer) return;
        
        Jump();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        if (!isTackling)
        {
            HandleMovement();
        }
        else
        {
            HandleTackleMovement();
            // Detectar colisiones durante el tackle usando raycast
            DetectTackleHit();
        }
    }

    void DetectTackleHit()
    {
        // Raycast hacia adelante para detectar jugadores en el rango de tackle
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit hit, 2.5f))
        {
            Rigidbody otherRb = hit.collider.attachedRigidbody;
            var otherNetObj = otherRb != null ? otherRb.GetComponent<NetworkObject>() : null;
            
            if (otherRb != null && otherRb != rb && otherNetObj != null && playerNetwork != null)
            {
                // Usar ServerRpc para aplicar fuerza (servidor lo sincroniza)
                float force = Mathf.Lerp(pushForce, pushForce * 2f, currentSpeed / tackleMaxSpeed);
                Vector3 dir = (hit.point - transform.position).normalized;
                dir = new Vector3(dir.x, 0, dir.z).normalized;
                Vector3 finalForce = dir * force + Vector3.up * pushUpForce;
                playerNetwork.ApplyForceServerRpc(otherNetObj.NetworkObjectId, finalForce);
                
                // Detener el tackle
                isTackling = false;
                currentVelocity = Vector3.zero;
            }
        }
    }

    void ReadInputActions()
    {
        // Leer movimiento directamente desde teclado (WASD)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        input2D = new Vector2(h, v);
        inputDir = new Vector3(input2D.x, 0f, input2D.y).normalized;
    }

    void HandleMovement()
    {
        if (inputDir.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + (cam != null ? cam.eulerAngles.y : transform.eulerAngles.y);
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref smoothTurnVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);
            Vector3 targetVelocity = moveDir.normalized * currentSpeed;

            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, driftFactor * Time.fixedDeltaTime);
            
            // Aplicar movimiento preservando velocidad Y (gravedad)
            rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, acceleration * Time.fixedDeltaTime);
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, 0.1f);
            
            // Frenar XZ pero mantener Y (gravedad)
            rb.linearVelocity = new Vector3(currentVelocity.x * 0.95f, rb.linearVelocity.y, currentVelocity.z * 0.95f);
        }
    }

    void Jump()
    {
        // Usar física normal de Rigidbody
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    IEnumerator Push()
    {
        canPush = false;
        Vector3 dir = transform.forward + Vector3.up * 0.3f;

        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out RaycastHit hit, 2f))
        {
            Rigidbody otherRb = hit.collider.attachedRigidbody;
            var otherNetObj = otherRb != null ? otherRb.GetComponent<NetworkObject>() : null;
            if (otherRb != null && otherRb != rb && otherNetObj != null && playerNetwork != null)
            {
                // Usar ServerRpc para aplicar fuerza
                playerNetwork.ApplyForceServerRpc(otherNetObj.NetworkObjectId, dir.normalized * pushForce + Vector3.up * pushUpForce);
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
            float targetAngle = cam != null ? cam.eulerAngles.y : transform.eulerAngles.y;
            float limitedAngle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, tackleTurnLimit * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, limitedAngle, 0);

            tackleSpeed = Mathf.MoveTowards(tackleSpeed, tackleMaxSpeed, tackleAcceleration * Time.fixedDeltaTime);
            currentVelocity = transform.forward * tackleSpeed;
            
            // Aplicar movimiento según tipo de rigidbody
            if (rb.isKinematic)
            {
                rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
            }
            else
            {
                rb.linearVelocity = currentVelocity;
            }

            yield return null;
        }

        yield return new WaitForSeconds(tackleCooldown);
        canTackle = true;
    }

    void HandleTackleMovement()
    {
        // Mantener velocidad de tackle hacia adelante, preservar Y
        Vector3 tackleVel = transform.forward * tackleMaxSpeed;
        rb.linearVelocity = new Vector3(tackleVel.x, rb.linearVelocity.y, tackleVel.z);
    }

    void OnCollisionEnter(Collision col)
    {
        if (!IsOwner) return;

        if (isTackling)
        {
            Rigidbody otherRb = col.rigidbody;
            var otherNetObj = otherRb != null ? otherRb.GetComponent<NetworkObject>() : null;
            if (otherRb != null && otherRb != rb && otherNetObj != null && playerNetwork != null)
            {
                // Usar ServerRpc para aplicar fuerza
                float force = Mathf.Lerp(pushForce, pushForce * 2f, currentSpeed / tackleMaxSpeed);
                Vector3 dir = (col.transform.position - transform.position).normalized;
                dir = new Vector3(dir.x, 0, dir.z).normalized;
                Vector3 finalForce = dir * force + Vector3.up * pushUpForce;
                playerNetwork.ApplyForceServerRpc(otherNetObj.NetworkObjectId, finalForce);
            }

            isTackling = false;
            currentVelocity = Vector3.zero;
        }
    }
}
