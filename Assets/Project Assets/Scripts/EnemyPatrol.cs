using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Configuración de Objetivo")]
    [Tooltip("Define qué tipo de entidad activa el empuje (por defecto Kart)")]
    public Entity targetEntity = Entity.Kart;

    [Header("Puntos de patrulla")]
    public Transform puntoA;
    public Transform puntoB;

    [Header("Movimiento")]
    public float velocidad = 3f;

    [Header("Empuje al Player")]
    [Tooltip("Fuerza con la que el Moai empuja hacia atrás")]
    public float fuerzaEmpuje = 20f;

    private Rigidbody rb;
    private Vector3 targetPoint;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Comprobación de seguridad por si olvidaste asignar los puntos
        if (puntoA != null && puntoB != null)
        {
            targetPoint = puntoB.position;
        }
        else
        {
            Debug.LogError("¡Faltan asignar los Puntos A o B en el script EnemyPatrol!");
        }
    }

    void FixedUpdate()
    {
        // Solo nos movemos si los puntos existen
        if (puntoA != null && puntoB != null)
        {
            MoverPatrullaSinGirar();
        }
    }

    private void MoverPatrullaSinGirar()
    {
        Vector3 direccion = (targetPoint - transform.position);

        if (direccion.magnitude < 0.5f)
        {
            CambiarObjetivo();
        }

        direccion.Normalize();

        Vector3 nuevaVelocidad = direccion * velocidad;
        // Nota: linearVelocity se usa en Unity 6+. Si usas una versión anterior, cambia a rb.velocity
        rb.linearVelocity = new Vector3(nuevaVelocidad.x, rb.linearVelocity.y, nuevaVelocidad.z);
    }

    private void CambiarObjetivo()
    {
        if (Vector3.Distance(targetPoint, puntoA.position) < 1f)
            targetPoint = puntoB.position;
        else
            targetPoint = puntoA.position;
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
    // NUEVA SECCIÓN: GIZMOS PARA VISUALIZACIÓN
    // ---------------------------------------------------------
    private void OnDrawGizmos()
    {
        // Si no hemos asignado los puntos, no dibujamos nada para evitar errores
        if (puntoA == null || puntoB == null) return;

        // 1. Dibujar la línea de trayectoria
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(puntoA.position, puntoB.position);

        // 2. Dibujar esferas en los puntos para verlos claramente
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(puntoA.position, 0.5f); // El 0.5f es el radio de la esfera
        Gizmos.DrawSphere(puntoB.position, 0.5f);

        // 3. (Opcional) Dibujar una línea desde el enemigo hasta su destino actual
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPoint);
        }
    }
}