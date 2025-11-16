using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

public class WindowsController : MonoBehaviour
{
    [Header("Window References")]
    [SerializeField] private RectTransform windowRectTransform;
    [SerializeField] private WindowsController previousWindow;
    [SerializeField] private Button backButton;
    [SerializeField] private Button showButton;

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimations = true;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    [Header("Events")]
    public UnityEvent OnWindowShow;
    public UnityEvent OnWindowHide;
    public UnityEvent OnWindowShowComplete;
    public UnityEvent OnWindowHideComplete;

    private Vector3 originalScale;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeWindow();
    }

    private void OnEnable()
    {
        // Configurar botones cuando el objeto se activa
        if (backButton)
            backButton.onClick.AddListener(ShowPreviousWindow);

        if (showButton)
            showButton.onClick.AddListener(ShowWindow);
    }

    private void OnDisable()
    {
        // Limpiar listeners cuando el objeto se desactiva
        if (backButton)
            backButton.onClick.RemoveListener(ShowPreviousWindow);

        if (showButton)
            showButton.onClick.RemoveListener(ShowWindow);
    }

    private void InitializeWindow()
    {
        if (isInitialized) return;

        if (!windowRectTransform)
            windowRectTransform = GetComponent<RectTransform>();

        originalScale = windowRectTransform.localScale;
        isInitialized = true;
    }

    public void ShowWindow()
    {
        if (!isInitialized) InitializeWindow();

        gameObject.SetActive(true);
        OnWindowShow?.Invoke();

        if (useAnimations)
        {
            // Animación de Pop In
            windowRectTransform.localScale = Vector3.zero;
            windowRectTransform.DOScale(originalScale, animationDuration)
                .SetEase(showEase)
                .OnComplete(() => OnWindowShowComplete?.Invoke());
        }
        else
        {
            windowRectTransform.localScale = originalScale;
            OnWindowShowComplete?.Invoke();
        }
    }

    public void HideWindow()
    {
        if (!isInitialized) InitializeWindow();

        OnWindowHide?.Invoke();

        if (useAnimations)
        {
            // Animación de Pop Out
            windowRectTransform.DOScale(Vector3.zero, animationDuration)
                .SetEase(hideEase)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    windowRectTransform.localScale = originalScale; // Reset scale
                    OnWindowHideComplete?.Invoke();
                });
        }
        else
        {
            gameObject.SetActive(false);
            OnWindowHideComplete?.Invoke();
        }
    }

    public void ShowPreviousWindow()
    {
        if (previousWindow != null)
        {
            HideWindow();
            previousWindow.ShowWindow();
        }
        else
        {
            // Si no hay ventana anterior, simplemente ocultar esta
            HideWindow();
        }
    }

    public void ToggleWindow()
    {
        if (gameObject.activeInHierarchy)
            HideWindow();
        else
            ShowWindow();
    }

    // Métodos para configurar desde otros scripts
    public void SetPreviousWindow(WindowsController previous)
    {
        previousWindow = previous;
    }

    public void SetUseAnimations(bool useAnim)
    {
        useAnimations = useAnim;
    }

    public void SetAnimationDuration(float duration)
    {
        animationDuration = duration;
    }

    // Métodos para debug y editor
    [ContextMenu("Show Window")]
    private void DebugShowWindow() => ShowWindow();

    [ContextMenu("Hide Window")]
    private void DebugHideWindow() => HideWindow();

    [ContextMenu("Set as Previous Window")]
    private void SetAsPreviousWindow()
    {
        // Buscar todos los WindowsController y establecer este como anterior
        var allWindows = FindObjectsOfType<WindowsController>();
        foreach (var window in allWindows)
        {
            if (window != this)
            {
                window.SetPreviousWindow(this);
                Debug.Log($"{window.name} now has {this.name} as previous window");
            }
        }
    }

    private void OnValidate()
    {
        if (!windowRectTransform)
            windowRectTransform = GetComponent<RectTransform>();
    }
}