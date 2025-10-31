using UnityEngine;
using UnityEngine.UI;

public class PanController_UI : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private RawImage targetRawImage;
    
    [Header("Pan Configuration")]
    [SerializeField] private Vector2 panDirection = new Vector2(1f, 1f);
    [SerializeField] private float panSpeed = 0.1f;
    [SerializeField] private bool autoStart = true;
    
    [Header("UV Settings")]
    [SerializeField] private Vector2 tiling = new Vector2(4f, 4f);
    [SerializeField] private bool seamlessLoop = true;

    private Rect initialUVRect;
    private bool isPanning;
    private Vector2 currentUVOffset;

    private void Awake()
    {
        if (targetRawImage == null)
            targetRawImage = GetComponent<RawImage>();
    }

    private void Start()
    {
        InitializeUVSettings();
        if (autoStart)
            StartPan();
    }

    private void InitializeUVSettings()
    {
        if (targetRawImage != null)
        {
            targetRawImage.uvRect = new Rect(0, 0, tiling.x, tiling.y);
            initialUVRect = targetRawImage.uvRect;
        }
    }

    private void Update()
    {
        if (!isPanning) return;

        // Mover las UVs basado en tiempo real
        currentUVOffset += panDirection.normalized * panSpeed * Time.deltaTime;
        
        // Aplicar seamless looping
        if (seamlessLoop)
        {
            currentUVOffset.x = Mathf.Repeat(currentUVOffset.x, 1f);
            currentUVOffset.y = Mathf.Repeat(currentUVOffset.y, 1f);
        }

        // Aplicar a la textura
        Rect uvRect = targetRawImage.uvRect;
        uvRect.position = currentUVOffset;
        targetRawImage.uvRect = uvRect;
    }

    public void StartPan() => isPanning = true;
    public void PausePan() => isPanning = false;
    
    public void StopPan()
    {
        isPanning = false;
        if (targetRawImage != null)
            targetRawImage.uvRect = initialUVRect;
        currentUVOffset = Vector2.zero;
    }

    public void SetPanSpeed(float newSpeed) => panSpeed = Mathf.Max(newSpeed, 0.01f);
    public void SetPanDirection(Vector2 newDirection) => panDirection = newDirection;
}