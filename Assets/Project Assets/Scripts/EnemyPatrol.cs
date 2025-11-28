using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Configuración de Objetivo")]
    [Tooltip("Define qué tipo de entidad activa el empuje (por defecto Kart)")]
    public Entity targetEntity = Entity.Kart; // Variable configurable, por defecto Kart

    [Header("Puntos de patrulla")]
    public Transform puntoA;
    public Transform puntoB;

    [Header("Movimiento")]
    public float velocidad = 3f;

    [Header("Empuje al Player")]
    [Tooltip("Fuerza con la que el Moai empuja hacia atrás")]
    public float fuerzaEmpuje = 20f; // Aumenté un poco el valor por defecto ya que es un obstáculo pesado

    private Rigidbody rb;
    private Vector3 targetPoint; // Hacia dónde vamos actualmente

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // IMPORTANTE: Congelamos TODA la rotación. 
        // El Moai no debe girar ni por código ni por colisiones físicas.
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Definir destino inicial
        targetPoint = puntoB.position;
    }

    void FixedUpdate()
    {
        MoverPatrullaSinGirar();
    }

    private void MoverPatrullaSinGirar()
    {
        // 1. Calcular dirección hacia el objetivo
        Vector3 direccion = (targetPoint - transform.position);

        // Ignoramos la altura para movimiento puramente horizontal si es necesario
        // direccion.y = 0; 

        // Si estamos cerca del objetivo, cambiamos al otro
        if (direccion.magnitude < 0.5f)
        {
            CambiarObjetivo();
            // No retornamos para asegurar que la velocidad se actualice correctamente en este frame
        }

        direccion.Normalize();

        // 2. Movimiento suave usando Velocity
        // Solo afectamos X y Z, mantenemos la velocidad Y original para gravedad
        Vector3 nuevaVelocidad = direccion * velocidad;
        rb.linearVelocity = new Vector3(nuevaVelocidad.x, rb.linearVelocity.y, nuevaVelocidad.z);

        // NOTA: No hay código de rotación aquí. El objeto mantendrá la rotación que le pusiste en la escena.
    }

    private void CambiarObjetivo()
    {
        // Alternar destino entre A y B
        if (Vector3.Distance(targetPoint, puntoA.position) < 1f)
            targetPoint = puntoB.position;
        else
            targetPoint = puntoA.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Buscar el componente EntityIdentifier en el objeto que nos chocó
        EntityIdentifier entityId = collision.collider.GetComponent<EntityIdentifier>();

        // 2. Verificar si tiene el identificador y si es del tipo que buscamos (Kart)
        if (entityId != null && entityId.Entity == targetEntity)
        {
            Rigidbody rbTarget = collision.collider.GetComponent<Rigidbody>();

            if (rbTarget != null)
            {
                // 3. Lógica de Empuje "Moai":
                // Siempre empuja en la dirección del 'transform.forward' del Moai.
                // Si el Moai mira hacia atrás de la pista, empujará al jugador hacia atrás,
                // sin importar si el jugador chocó de frente o de lado.
                Vector3 direccionEmpuje = transform.forward;

                // Aplicamos el empuje
                rbTarget.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode.Impulse);
            }
        }
    }
}