using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class GamepadWindowsController : WindowsController
{
    [Header("Gamepad Navigation")]
    [SerializeField] private bool setFirstSelected = true;
    [SerializeField] private RectTransform firstSelected;
    [SerializeField] private EventSystem eventSystem;

    protected override void Awake()
    {
        base.Awake();

        if (eventSystem == null)
            eventSystem = FindObjectOfType<EventSystem>();
    }

    public override void ShowWindow()
    {
        base.ShowWindow();

        if (setFirstSelected && firstSelected != null && eventSystem != null)
        {
            // Establecer el primer elemento seleccionado para gamepad
            eventSystem.firstSelectedGameObject = firstSelected.gameObject;
            eventSystem.SetSelectedGameObject(firstSelected.gameObject);
        }
    }

    public override void HideWindow()
    {
        base.HideWindow();

        if (eventSystem != null)
        {
            // Limpiar la selección al ocultar la ventana
            eventSystem.firstSelectedGameObject = null;
            eventSystem.SetSelectedGameObject(null);
        }
    }

    // Métodos para configurar desde otros scripts
    public void SetFirstSelected(RectTransform newFirstSelected)
    {
        firstSelected = newFirstSelected;
    }

    public void SetEventSystem(EventSystem newEventSystem)
    {
        eventSystem = newEventSystem;
    }

    public void SetSetFirstSelected(bool value)
    {
        setFirstSelected = value;
    }

    private void OnValidate()
    {
        if (eventSystem == null)
            eventSystem = FindObjectOfType<EventSystem>();
    }
}
