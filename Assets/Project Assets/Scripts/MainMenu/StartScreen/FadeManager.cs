using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using TMPro;
using System.Threading.Tasks;

public class FadeManager : MonoBehaviour
{
    [Header("Tween References")]
    [SerializeField] private RectTransform targetRectTransform;
    [SerializeField] private RectTransform showPosition;
    [SerializeField] private RectTransform hidePosition;

    [Header("Tween Settings")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.InOutQuad;

    [Header("Loading Settings")]
    [SerializeField] private string baseText = "Loading";
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private float dotInterval = 0.1f;
    [SerializeField] private float textPopDuration = 0.5f;

    [Header("Events")]
    public UnityEvent OnShowBegin;
    public UnityEvent OnShowComplete;
    public UnityEvent OnHideBegin;
    public UnityEvent OnHideComplete;

    private Tween currentTween;
    private bool isLoading = false;

    private void Awake()
    {
        if (!targetRectTransform)
            targetRectTransform = GetComponent<RectTransform>();

        if (loadingText)
            loadingText.gameObject.SetActive(false);
    }

    public void Show()
    {
        if (!showPosition) return;

        KillCurrentTween();
        OnShowBegin?.Invoke();

        currentTween = targetRectTransform.DOAnchorPos(showPosition.anchoredPosition, duration)
            .SetEase(easeType)
            .OnComplete(() => OnShowComplete?.Invoke());
    }

    public void Hide()
    {
        if (!hidePosition) return;

        KillCurrentTween();
        OnHideBegin?.Invoke();

        currentTween = targetRectTransform.DOAnchorPos(hidePosition.anchoredPosition, duration)
            .SetEase(easeType)
            .OnComplete(() => OnHideComplete?.Invoke());
    }

    public async Task ShowAsync()
    {
        if (!showPosition) return;

        KillCurrentTween();
        OnShowBegin?.Invoke();

        var tcs = new TaskCompletionSource<bool>();

        currentTween = targetRectTransform.DOAnchorPos(showPosition.anchoredPosition, duration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                OnShowComplete?.Invoke();
                tcs.SetResult(true);
            });

        await tcs.Task;
    }

    public async Task HideAsync()
    {
        if (!hidePosition) return;

        KillCurrentTween();
        OnHideBegin?.Invoke();

        var tcs = new TaskCompletionSource<bool>();

        currentTween = targetRectTransform.DOAnchorPos(hidePosition.anchoredPosition, duration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                OnHideComplete?.Invoke();
                tcs.SetResult(true);
            });

        await tcs.Task;
    }

    public void Toggle()
    {
        if (!showPosition || !hidePosition) return;

        if (targetRectTransform.anchoredPosition == showPosition.anchoredPosition)
            Hide();
        else
            Show();
    }

    public async Task WaitForTaskAndShowLoading(Task task)
    {
        if (loadingText == null)
        {
            await task;
            return;
        }

        // 1. Bajar la cortina y esperar
        await ShowAsync();

        // 2. Activar texto con efecto POP IN y empezar animación de puntos
        StartLoadingAnimation();

        // 3. Esperar a que la tarea (escena) termine + tiempo de gracia del GlobalSceneLoader
        await task;

        // 4. Detener animación
        StopLoadingAnimation();

        // 5. Subir la cortina
        await HideAsync();
    }

    private void StartLoadingAnimation()
    {
        isLoading = true;
        if (loadingText)
        {
            loadingText.gameObject.SetActive(true);

            // EFECTO POP IN AQUI
            // Reiniciamos escala a 0
            loadingText.transform.localScale = Vector3.zero;
            // Hacemos el tween a escala 1 con un rebote elástico (OutBack)
            loadingText.transform.DOScale(Vector3.one, textPopDuration).SetEase(Ease.OutBack);

            AnimateLoadingText();
        }
    }

    private void StopLoadingAnimation()
    {
        isLoading = false;
        if (loadingText)
            loadingText.gameObject.SetActive(false);
    }

    private async void AnimateLoadingText()
    {
        int dotCount = 0;
        // Pequeña seguridad para evitar bucles si se destruye el objeto
        while (isLoading && this != null && loadingText != null)
        {
            dotCount = (dotCount + 1) % 4;
            loadingText.text = baseText + new string('.', dotCount);
            await Task.Delay((int)(dotInterval * 1000));
        }
    }

    private void KillCurrentTween()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
    }
}