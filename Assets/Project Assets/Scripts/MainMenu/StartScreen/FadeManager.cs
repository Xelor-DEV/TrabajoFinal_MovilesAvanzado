using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class FadeManager : MonoBehaviour
{
    [Header("Tween References")]
    [SerializeField] private RectTransform targetRectTransform;
    [SerializeField] private RectTransform showPosition;
    [SerializeField] private RectTransform hidePosition;

    [Header("Tween Settings")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.InOutQuad;

    [Header("Events")]
    public UnityEvent OnShowBegin;
    public UnityEvent OnShowComplete;
    public UnityEvent OnHideBegin;
    public UnityEvent OnHideComplete;

    private Tween currentTween;

    private void Awake()
    {
        if (!targetRectTransform)
            targetRectTransform = GetComponent<RectTransform>();
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

    public void Toggle()
    {
        if (!showPosition || !hidePosition) return;

        if (targetRectTransform.anchoredPosition == showPosition.anchoredPosition)
            Hide();
        else
            Show();
    }

    private void KillCurrentTween()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
    }
}