using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class SliderTweenController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Tween Targets")]
    [SerializeField] private Transform sliderBackground;
    [SerializeField] private Transform sliderFill;
    [SerializeField] private Transform sliderHandle;

    [Header("Selection Tween Settings")]
    [SerializeField] private float highlightedScale = 1.05f;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    [Header("Value Change Tween Settings")]
    [SerializeField] private bool enableValueChangeEffect = true;
    [SerializeField] private float handlePunchScale = 0.2f;
    [SerializeField] private float punchDuration = 0.3f;

    private Vector3 originalBackgroundScale;
    private Vector3 originalFillScale;
    private Vector3 originalHandleScale;
    private Tween currentBackgroundTween;
    private Tween currentFillTween;
    private Tween currentHandleTween;
    private Tween currentValueChangeTween;
    private bool isSelected = false;
    private bool isHovered = false;
    private bool isInteractable = true;
    private Slider slider;
    private bool isChangingValue = false;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        // Guardar escalas originales
        originalBackgroundScale = sliderBackground != null ? sliderBackground.localScale : Vector3.one;
        originalFillScale = sliderFill != null ? sliderFill.localScale : Vector3.one;
        originalHandleScale = sliderHandle != null ? sliderHandle.localScale : Vector3.one;

        // Suscribirse al cambio de valor del slider
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    private void OnEnable()
    {
        if (slider != null)
        {
            isInteractable = slider.interactable;
        }
    }

    private void OnDisable()
    {
        KillAllTweens();
        ResetAllScales();
        isSelected = false;
        isHovered = false;
        isChangingValue = false;
    }

    private void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    private void KillAllTweens()
    {
        currentBackgroundTween?.Kill();
        currentFillTween?.Kill();
        currentHandleTween?.Kill();
        currentValueChangeTween?.Kill();
    }

    private bool IsInteractable()
    {
        return slider != null && slider.interactable && isInteractable;
    }

    private bool ShouldBeHighlighted()
    {
        return isHovered || isSelected;
    }

    // Hover con mouse
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        isHovered = true;
        ApplySelectionTween();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        isHovered = false;
        if (!isSelected)
        {
            RemoveSelectionTween();
        }
    }

    // Selección con navegación
    public void OnSelect(BaseEventData eventData)
    {
        if (!IsInteractable()) return;

        isSelected = true;
        ApplySelectionTween();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!IsInteractable()) return;

        isSelected = false;
        if (!isHovered)
        {
            RemoveSelectionTween();
        }
    }

    // Tween cuando se selecciona/hover
    private void ApplySelectionTween()
    {
        if (!ShouldBeHighlighted()) return;

        // Animar background
        if (sliderBackground != null)
        {
            currentBackgroundTween?.Kill();
            currentBackgroundTween = sliderBackground.DOScale(originalBackgroundScale * highlightedScale, scaleDuration)
                .SetEase(easeType);
        }

        // Animar fill
        if (sliderFill != null)
        {
            currentFillTween?.Kill();
            currentFillTween = sliderFill.DOScale(originalFillScale * highlightedScale, scaleDuration)
                .SetEase(easeType);
        }

        // Animar handle (solo si no está cambiando valor)
        if (sliderHandle != null && !isChangingValue)
        {
            currentHandleTween?.Kill();
            currentHandleTween = sliderHandle.DOScale(originalHandleScale * highlightedScale, scaleDuration)
                .SetEase(easeType);
        }
    }

    // Remover tween de selección
    private void RemoveSelectionTween()
    {
        // Volver a escala normal del background
        if (sliderBackground != null)
        {
            currentBackgroundTween?.Kill();
            currentBackgroundTween = sliderBackground.DOScale(originalBackgroundScale, scaleDuration)
                .SetEase(easeType);
        }

        // Volver a escala normal del fill
        if (sliderFill != null)
        {
            currentFillTween?.Kill();
            currentFillTween = sliderFill.DOScale(originalFillScale, scaleDuration)
                .SetEase(easeType);
        }

        // Volver a escala normal del handle (solo si no está cambiando valor)
        if (sliderHandle != null && !isChangingValue)
        {
            currentHandleTween?.Kill();
            currentHandleTween = sliderHandle.DOScale(originalHandleScale, scaleDuration)
                .SetEase(easeType);
        }
    }

    // Tween cuando cambia el valor del slider
    private void OnSliderValueChanged(float value)
    {
        if (!IsInteractable() || !enableValueChangeEffect) return;

        // Solo animar el handle cuando cambia el valor
        if (sliderHandle != null)
        {
            // Cancelar cualquier tween de valor anterior
            currentValueChangeTween?.Kill();

            // Marcar que estamos cambiando valor
            isChangingValue = true;

            // Determinar la escala objetivo basada en el estado de selección
            Vector3 targetScale = ShouldBeHighlighted() ?
                originalHandleScale * highlightedScale :
                originalHandleScale;

            // Crear una secuencia para el efecto de cambio de valor
            Sequence valueChangeSequence = DOTween.Sequence();

            // Paso 1: Escala aumentada momentáneamente
            valueChangeSequence.Append(
                sliderHandle.DOScale(targetScale * (1f + handlePunchScale), punchDuration * 0.3f)
            );

            // Paso 2: Volver a la escala objetivo
            valueChangeSequence.Append(
                sliderHandle.DOScale(targetScale, punchDuration * 0.7f)
            );

            // Paso 3: Resetear el flag cuando termine
            valueChangeSequence.OnComplete(() => {
                isChangingValue = false;
            });

            currentValueChangeTween = valueChangeSequence;
        }
    }

    // Resetear todas las escalas a sus valores originales
    private void ResetAllScales()
    {
        if (sliderBackground != null)
            sliderBackground.localScale = originalBackgroundScale;

        if (sliderFill != null)
            sliderFill.localScale = originalFillScale;

        if (sliderHandle != null)
            sliderHandle.localScale = originalHandleScale;
    }

    // Método para actualizar el estado de interactividad
    public void UpdateInteractableState()
    {
        if (slider != null)
        {
            isInteractable = slider.interactable;

            // Si no es interactuable, quitar todos los tweens
            if (!isInteractable)
            {
                KillAllTweens();
                ResetAllScales();
                isSelected = false;
                isHovered = false;
                isChangingValue = false;
            }
        }
    }

    // Métodos para configurar los targets en tiempo de ejecución
    public void SetSliderBackground(Transform background)
    {
        sliderBackground = background;
        originalBackgroundScale = background.localScale;
    }

    public void SetSliderFill(Transform fill)
    {
        sliderFill = fill;
        originalFillScale = fill.localScale;
    }

    public void SetSliderHandle(Transform handle)
    {
        sliderHandle = handle;
        originalHandleScale = handle.localScale;
    }
}