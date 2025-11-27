using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(EntityIdentifier))]
public class Waypoint : MonoBehaviour
{
    [Header("Checkpoint System")]
    [Tooltip("DEJAR VACÍO si este es solo un waypoint de paso. ARRASTRAR un Transform si aquí se guarda partida.")]
    [SerializeField] private Transform respawnPoint;

    [Header("Visual Debug")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Vector3 boxSize = new Vector3(10, 5, 1);
    [Tooltip("El orden se define automáticamente por el RaceManager, pero puedes verlo aquí.")]
    public int indexDebug = 0;

    // Propiedad pública para acceder al respawn
    public Transform RespawnPoint => respawnPoint;

    // Propiedad para que el Manager asigne el índice
    public int Index
    {
        get { return indexDebug; }
        set { indexDebug = value; }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        // Si tiene respawnPoint es AZUL (Checkpoint), si no, es AMARILLO (Paso)
        Color c = (respawnPoint != null) ? Color.cyan : Color.yellow;

        // 1. Dibujar el Trigger (La puerta invisible)
        Gizmos.color = c;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        // Relleno semitransparente
        Gizmos.color = new Color(c.r, c.g, c.b, 0.2f);
        Gizmos.DrawCube(Vector3.zero, boxSize);
        Gizmos.matrix = Matrix4x4.identity;

        // 2. Dibujar la posición de reaparición (solo si existe)
        if (respawnPoint != null)
        {
            Gizmos.color = Color.green;
            // Dibujar una esfera donde reaparecerá el kart
            Gizmos.DrawWireSphere(respawnPoint.position, 1f);
            // Dibujar una línea desde el trigger hasta el punto de reaparición para ver la conexión
            Gizmos.DrawLine(transform.position, respawnPoint.position);
        }
    }
}