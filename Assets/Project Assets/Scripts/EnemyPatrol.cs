using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Configuración de Objetivo")]
    [Tooltip("Define qué tipo de entidad activa el empuje (por defecto Kart)")]
    public Entity targetEntity = Entity.Kart;

    [Header("Puntos de patrulla")]
    [Tooltip("Arrastra aquí los objetos hijos que marcan la ruta")]
    public Transform puntoA;
    public Transform puntoB;

    [Header("Movimiento")]
    public float velocidad = 3f;

    [Header("Empuje al Player")]
    [Tooltip("Fuerza con la que el Moai empuja hacia atrás")]
    public float fuerzaEmpuje = 20f;

    private Rigidbody rb;
    private Vector3 currentTargetPoint; // El destino actual
    
    // Variables para guardar las posiciones fijas del mundo
    private Vector3 worldPosA;
    private Vector3 worldPosB;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Comprobación de seguridad
        if (puntoA != null && puntoB != null)
        {
            // --- CORRECCIÓN CLAVE ---
            // Guardamos las posiciones exactas del mundo al iniciar el juego.
            // Así, aunque el enemigo se mueva y arrastre los transforms hijos,
            // nosotros recordamos dónde estaban al principio.
            worldPosA = puntoA.position;
            worldPosB = puntoB.position;

            // Empezamos yendo hacia el punto B (usando la coordenada guardada, no el transform)
            currentTargetPoint = worldPosB;
        }
        else
        {
            Debug.LogError("¡Faltan asignar los Puntos A o B en el script EnemyPatrol!");
        }
    }

    void FixedUpdate()
    {
        if (puntoA != null && puntoB != null)
        {
            MoverPatrullaSinGirar();
        }
    }

    private void MoverPatrullaSinGirar()
    {
        // Calculamos dirección hacia la coordenada guardada (currentTargetPoint)
        Vector3 direccion = (currentTargetPoint - transform.position);

        // Comprobamos distancia
        if (direccion.magnitude < 0.5f)
        {
            CambiarObjetivo();
        }

        direccion.Normalize();

        Vector3 nuevaVelocidad = direccion * velocidad;
        // Mantenemos la velocidad Y original para la gravedad
        rb.linearVelocity = new Vector3(nuevaVelocidad.x, rb.linearVelocity.y, nuevaVelocidad.z);
    }

    private void CambiarObjetivo()
    {
        // Comparamos distancias usando las coordenadas fijas (worldPosA y worldPosB)
        // Si estamos cerca de A, vamos a B. Si no, vamos a A.
        if (Vector3.Distance(currentTargetPoint, worldPosA) < 0.1f) // Margen pequeño de error
            currentTargetPoint = worldPosB;
        else
            currentTargetPoint = worldPosA;
    }

    private void OnCollisionEnter(Collision collision)
    {
        EntityIdentifier entityId = collision.collider.GetComponent<EntityIdentifier>();

        if (entityId != null && entityId.Entity == targetEntity)
        {
            Rigidbody rbTarget = collision.collider.GetComponent<Rigidbody>();

            if (rbTarget != null)
            {
                Vector3 direccionEmpuje = transform.forward;
                rbTarget.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode.Impulse);
            }
        }
    }

    // ---------------------------------------------------------
    // GIZMOS ACTUALIZADOS
    // ---------------------------------------------------------
    private void OnDrawGizmos()
    {
        if (puntoA == null || puntoB == null) return;

        Vector3 drawPosA;
        Vector3 drawPosB;

        // Si estamos jugando, dibujamos las posiciones "recordadas" (fijas en el mundo)
        // Si estamos editando, dibujamos las posiciones de los transforms (para poder moverlos)
        if (Application.isPlaying)
        {
            drawPosA = worldPosA;
            drawPosB = worldPosB;
        }
        else
        {
            drawPosA = puntoA.position;
            drawPosB = puntoB.position;
        }

        // 1. Dibujar la línea de trayectoria
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(drawPosA, drawPosB);

        // 2. Dibujar esferas
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(drawPosA, 0.5f); 
        Gizmos.DrawSphere(drawPosB, 0.5f);

        // 3. Línea hacia el objetivo actual
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTargetPoint);
        }
    }
}