using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSoundController : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ISelectHandler, ISubmitHandler
{
    [Header("Sound Settings")]
    [SerializeField] private int hoverSelectSoundIndex = 0;
    [SerializeField] private int clickSoundIndex = 1;

    [Header("Configuration")]
    [SerializeField] private bool playHoverSelectSound = true;
    [SerializeField] private bool playClickSound = true;

    private Button button;
    private bool isInteractable = true;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            isInteractable = button.interactable;
        }
    }

    private void OnEnable()
    {
        if (button != null)
        {
            isInteractable = button.interactable;
        }
    }

    private bool CanPlaySound()
    {
        return AudioManager.Instance != null && isInteractable;
    }

    // Sonido cuando el mouse pasa por encima O cuando se selecciona por navegación
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverSelectSound();
    }

    public void OnSelect(BaseEventData eventData)
    {
        PlayHoverSelectSound();
    }

    // Sonido cuando se hace click con el mouse
    public void OnPointerDown(PointerEventData eventData)
    {
        PlayClickSound();
    }

    // Sonido cuando se activa con teclado/mando (Submit)
    public void OnSubmit(BaseEventData eventData)
    {
        PlayClickSound();
    }

    private void PlayHoverSelectSound()
    {
        if (!CanPlaySound() || !playHoverSelectSound) return;
        AudioManager.Instance.PlaySfx(hoverSelectSoundIndex);
    }

    private void PlayClickSound()
    {
        if (!CanPlaySound() || !playClickSound) return;
        AudioManager.Instance.PlaySfx(clickSoundIndex);
    }

    // Método para actualizar el estado de interactividad
    public void UpdateInteractableState()
    {
        if (button != null)
        {
            isInteractable = button.interactable;
        }
    }

    // Métodos para cambiar los índices de sonido en tiempo de ejecución
    public void SetHoverSelectSoundIndex(int index)
    {
        hoverSelectSoundIndex = index;
    }

    public void SetClickSoundIndex(int index)
    {
        clickSoundIndex = index;
    }
}