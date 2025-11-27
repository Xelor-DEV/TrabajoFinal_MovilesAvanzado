using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class SelectedButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Header("Tween Settings")]
    [SerializeField] private float highlightedScale = 1.1f;
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private Vector3 originalScale;
    private Tween currentTween;
    private bool isHighlighted = false;
    private bool isSelected = false;
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
        isSelected = false;
    }

    private bool IsButtonInteractable()
    {
        return button == null || button.interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        isHighlighted = true;
        ApplyHighlightedTween();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        isHighlighted = false;
        if (!isSelected)
        {
            ApplyNormalTween();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        isSelected = true;
        ApplyHighlightedTween();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        isSelected = false;
        if (!isHighlighted)
        {
            ApplyNormalTween();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        ApplyPressedTween();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        if (isHighlighted || isSelected)
        {
            ApplyHighlightedTween();
        }
        else
        {
            ApplyNormalTween();
        }
    }

    private void ApplyHighlightedTween()
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale * highlightedScale, scaleDuration).SetEase(easeType);
    }

    private void ApplyNormalTween()
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale, scaleDuration).SetEase(easeType);
    }

    private void ApplyPressedTween()
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale * pressedScale, scaleDuration * 0.5f).SetEase(Ease.OutQuad);
    }
}