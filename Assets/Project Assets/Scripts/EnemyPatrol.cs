using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Puntos de patrulla (solo se usa su posición)")]
    public Transform puntoA;
    public Transform puntoB;

    [Header("Movimiento")]
    public float velocidad = 3f;

    [Header("Empuje al Player")]
    public float fuerzaEmpuje = 10f;

    private Rigidbody rb;
    private int direccion; // 1 = hacia puntoB, -1 = hacia puntoA

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Elegir aleatoriamente si inicia yendo hacia A o hacia B
        direccion = Random.value < 0.5f ? -1 : 1;
    }

    void Update()
    {
        // Movimiento en X según la dirección
        rb.linearVelocity = new Vector3(direccion * velocidad, rb.linearVelocity.y, rb.linearVelocity.z);

        // Cambiar a dirección hacia A cuando llega o supera el punto B
        if (direccion == 1 && transform.position.x >= puntoB.position.x)
            direccion = -1;

        // Cambiar a dirección hacia B cuando llega o supera el punto A
        else if (direccion == -1 && transform.position.x <= puntoA.position.x)
            direccion = 1;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Rigidbody rbPlayer = collision.collider.GetComponent<Rigidbody>();

            if (rbPlayer != null)
            {
                // Empuje hacia la dirección del enemigo
                Vector3 fuerza = new Vector3(direccion * fuerzaEmpuje, 0, 0);
                rbPlayer.AddForce(fuerza, ForceMode.Impulse);
            }
        }
    }
}