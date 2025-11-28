using UnityEngine;
using DG.Tweening; // [NUEVO] Necesario para el efecto Pop In

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(EntityIdentifier))]
public class Waypoint : MonoBehaviour
{
    [Header("Checkpoint System")]
    [Tooltip("DEJAR VACÍO si este es solo un waypoint de paso. ARRASTRAR un Transform si aquí se guarda partida.")]
    [SerializeField] private Transform respawnPoint;

    [Header("Visual Effects")]
    [Tooltip("El objeto visual del portal (partículas, mesh, etc.). Se activará al desbloquear el checkpoint.")]
    [SerializeField] private GameObject portalVisual; // [NUEVO] Referencia al objeto visual
    [SerializeField] private float popInDuration = 0.5f;

    [Header("Visual Debug")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color debugColor = new Color(1f, 0.92f, 0.016f, 0.5f);
    [SerializeField] private Vector3 boxSize = new Vector3(10, 5, 1);
    [SerializeField] private Vector3 boxCenter = Vector3.zero;

    public int indexDebug = 0;
    private BoxCollider _boxCollider;
    private bool isActivated = false; // Para evitar activar la animación múltiples veces
    private Vector3 portalVisualScale;

    // [NUEVO] Propiedad rápida para saber si tiene checkpoint
    public bool HasCheckpoint => respawnPoint != null;

    public Transform RespawnPoint => respawnPoint;

    public int Index
    {
        get { return indexDebug; }
        set { indexDebug = value; }
    }

    private void Start()
    {
        // [NUEVO] Al inicio, si hay un visual asignado, lo apagamos para que no se vea.
        if (portalVisual != null)
        {
            portalVisual.SetActive(false);
            // Aseguramos que la escala sea 0 para el efecto pop-in
            portalVisualScale = portalVisual.transform.localScale;
            portalVisual.transform.localScale = Vector3.zero;
        }
    }

    // [NUEVO] Método para activar el efecto visual del portal
    public void ActivatePortal()
    {
        if (isActivated || portalVisual == null) return;

        isActivated = true;
        portalVisual.SetActive(true);

        // Efecto Pop In con DOTween
        // Scale 0 -> 1 con efecto elástico (OutBack)
        portalVisual.transform.DOScale(portalVisualScale, popInDuration).SetEase(Ease.OutBack);
    }

    private void OnValidate()
    {
        if (_boxCollider == null) _boxCollider = GetComponent<BoxCollider>();

        if (_boxCollider != null)
        {
            _boxCollider.isTrigger = true;
            _boxCollider.size = boxSize;
            _boxCollider.center = boxCenter;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Color baseColor = (respawnPoint != null) ? Color.cyan : debugColor;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

        Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f);
        Gizmos.DrawCube(boxCenter, boxSize);
        Gizmos.color = baseColor;
        Gizmos.DrawWireCube(boxCenter, boxSize);

        // Dirección
        Gizmos.color = Color.red;
        Gizmos.DrawLine(boxCenter, boxCenter + Vector3.forward * (boxSize.z * 0.5f + 2f));

        Gizmos.matrix = oldMatrix;

        if (respawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(respawnPoint.position, 1f);
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(respawnPoint.position, 1f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, respawnPoint.position);

            Gizmos.color = Color.blue;
            Vector3 respawnForward = respawnPoint.position + respawnPoint.forward * 2f;
            Gizmos.DrawLine(respawnPoint.position, respawnForward);
        }
    }
}