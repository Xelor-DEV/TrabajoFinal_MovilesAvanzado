using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class SelectedButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Tween Settings")]
    [SerializeField] private float highlightedScale = 1.1f;
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private Vector3 originalScale;
    private Tween currentTween;
    private bool isHighlighted = false;
    private Button button;

    private void Awake()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
    }

    private void OnDisable()
    {
        currentTween?.Kill();
        transform.localScale = originalScale;
        isHighlighted = false;
    }

    private bool IsButtonInteractable()
    {
        return button == null || button.interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        isHighlighted = true;
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale * highlightedScale, scaleDuration).SetEase(easeType);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        isHighlighted = false;
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale, scaleDuration).SetEase(easeType);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale * pressedScale, scaleDuration * 0.5f).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        currentTween?.Kill();

        Vector3 targetScale = isHighlighted ?
            originalScale * highlightedScale :
            originalScale;

        currentTween = transform.DOScale(targetScale, scaleDuration).SetEase(easeType);
    }
}