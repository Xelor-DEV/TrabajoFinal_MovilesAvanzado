using UnityEngine;
using System.Collections;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody))]
public class WaypointPlatform : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private float distanceThreshold = 0.1f;
    
    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 1f;
    [SerializeField] private float shakeStrength = 0.5f;
    [SerializeField] private int shakeVibrato = 10;

    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;

    private Rigidbody rb;
    private int currentPointIndex = 0;
    private bool isWaiting = false;

    // Diccionario para guardar los padres originales de los objetos que se suben
    // Esto es útil si el objeto ya tenía otro padre antes
    private System.Collections.Generic.Dictionary<Transform, Transform> originalParents = new System.Collections.Generic.Dictionary<Transform, Transform>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // MANTENER KINEMATIC
        rb.isKinematic = true; 
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Crucial para suavidad
    }

    private void Start()
    {
        if (waypoints.Length > 0 && waypoints[0].parent == transform)
        {
            foreach(var point in waypoints) if(point != null) point.SetParent(null);
        }
        
        if(waypoints.Length > 0) transform.position = waypoints[0].position;
    }

    private void FixedUpdate()
    {
        if (waypoints.Length == 0 || isWaiting) return;
        MovePlatform();
    }

    private void MovePlatform()
    {
        Transform targetPoint = waypoints[currentPointIndex];
        Vector3 direction = (targetPoint.position - rb.position).normalized;
        float distance = Vector3.Distance(rb.position, targetPoint.position);

        if (distance < distanceThreshold || distance < speed * Time.fixedDeltaTime)
        {
            StartCoroutine(HandleArrival());
        }
        else
        {
            // MovePosition aplica "fricción física" a lo que esté encima
            Vector3 newPosition = rb.position + (direction * speed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);
        }
    }

    private IEnumerator HandleArrival()
    {
        isWaiting = true;
        rb.MovePosition(waypoints[currentPointIndex].position);
        yield return new WaitForFixedUpdate(); 
        yield return new WaitForSeconds(waitTime);

        yield return transform.DOShakePosition(shakeDuration, new Vector3(shakeStrength, 0, shakeStrength), shakeVibrato, 90, false, true)
                              .WaitForCompletion();

        currentPointIndex++;
        if (currentPointIndex >= waypoints.Length) currentPointIndex = 0;

        isWaiting = false;
    }

    // ---------------------------------------------------------
    //  LÓGICA DE ANCLAJE (PARENTING)
    // ---------------------------------------------------------

    private void OnCollisionEnter(Collision collision)
    {
        // Verificamos si el objeto está ARRIBA de la plataforma (aprox)
        // Esto evita que si te choca de lado se pegue a la pared de la plataforma
        if (collision.transform.position.y > transform.position.y)
        {
            // Guardamos el padre original por si acaso (ej: un objeto dentro de una jerarquía)
            if (!originalParents.ContainsKey(collision.transform))
            {
                originalParents.Add(collision.transform, collision.transform.parent);
            }

            // Hacemos a la plataforma el nuevo padre
            // 'true' mantiene la posición global actual
            collision.transform.SetParent(transform, true);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Cuando el objeto sale (salta o camina fuera), le devolvemos su independencia
        if (originalParents.ContainsKey(collision.transform))
        {
            Transform originalParent = originalParents[collision.transform];
            
            // Restauramos el padre original (o null si no tenía)
            collision.transform.SetParent(originalParent, true);
            
            // Limpiamos la lista
            originalParents.Remove(collision.transform);
        }
        else
        {
            // Fallback por seguridad
            if(collision.transform.parent == transform)
            {
                collision.transform.SetParent(null, true);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawSphere(waypoints[i].position, 0.3f);
                Vector3 nextPoint = waypoints[(i + 1) % waypoints.Length].position;
                Gizmos.DrawLine(waypoints[i].position, nextPoint);
            }
        }
    }
}