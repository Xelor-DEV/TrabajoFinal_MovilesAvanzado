using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderSoundController : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [Header("Sound Settings")]
    [SerializeField] private int hoverSelectSoundIndex = 2;
    [SerializeField] private int valueChangeSoundIndex = 3;

    [Header("Configuration")]
    [SerializeField] private bool playHoverSelectSound = true;
    [SerializeField] private bool playValueChangeSound = true;
    [SerializeField] private float valueChangeThreshold = 0.01f;

    private Slider slider;
    private bool isInteractable = true;
    private float lastValue;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (slider != null)
        {
            isInteractable = slider.interactable;
            lastValue = slider.value;

            // Suscribirse al evento de cambio de valor
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    private void OnEnable()
    {
        if (slider != null)
        {
            isInteractable = slider.interactable;
            lastValue = slider.value;
        }
    }

    private void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    private bool CanPlaySound()
    {
        return AudioManager.Instance != null && isInteractable;
    }

    // Mismo sonido para hover y selección
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverSelectSound();
    }

    public void OnSelect(BaseEventData eventData)
    {
        PlayHoverSelectSound();
    }

    private void OnSliderValueChanged(float newValue)
    {
        if (!CanPlaySound() || !playValueChangeSound) return;

        // Verificar si el cambio es significativo
        if (Mathf.Abs(newValue - lastValue) < valueChangeThreshold)
            return;

        AudioManager.Instance.PlaySfx(valueChangeSoundIndex);
        lastValue = newValue;
    }

    private void PlayHoverSelectSound()
    {
        if (!CanPlaySound() || !playHoverSelectSound) return;
        AudioManager.Instance.PlaySfx(hoverSelectSoundIndex);
    }

    // Método para actualizar el estado de interactividad
    public void UpdateInteractableState()
    {
        if (slider != null)
        {
            isInteractable = slider.interactable;
        }
    }

    // Métodos para cambiar los índices de sonido en tiempo de ejecución
    public void SetHoverSelectSoundIndex(int index)
    {
        hoverSelectSoundIndex = index;
    }

    public void SetValueChangeSoundIndex(int index)
    {
        valueChangeSoundIndex = index;
    }

    public void SetValueChangeThreshold(float threshold)
    {
        valueChangeThreshold = Mathf.Clamp01(threshold);
    }
}