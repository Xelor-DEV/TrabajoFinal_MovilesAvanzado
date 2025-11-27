using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;

public class DeviceCursor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image cursorImage;

    [Header("Audio Settings")]
    [Tooltip("El índice del SFX en el AudioManager que sonará al mover el cursor.")]
    [SerializeField] private int moveSfxIndex;
    [Tooltip("El índice del SFX en el AudioManager que sonará al confirmar (Submit).")]
    [SerializeField] private int submitSfxIndex;

    private ControlSelectionManager _manager;
    private RectTransform _rectTransform;
    private int _currentIndex = 0;

    private bool _isLocked = false;

    // Modified Initialize: Now accepts the Color directly
    public void Initialize(ControlSelectionManager manager, Color cursorColor)
    {
        _manager = manager;
        _rectTransform = GetComponent<RectTransform>();

        // Assign the color received from the Manager (from the ScriptableObject)
        cursorImage.color = cursorColor;

        SnapToPosition();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (_isLocked || !context.performed) return;

        // CORRECCIÓN: Leemos un float, no un Vector2, porque el Action es tipo Axis
        float input = context.ReadValue<float>();

        // Usamos el float directamente (input en lugar de input.x)
        if (input > 0.5f)
        {
            MoveCursor(1);
        }
        else if (input < -0.5f)
        {
            MoveCursor(-1);
        }
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (_isLocked || !context.performed) return;

        bool success = _manager.TrySelectPlayer(_currentIndex, GetComponent<PlayerInput>());

        if (success)
        {
            // REPRODUCIR SONIDO DE SUBMIT (Confirmación)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(submitSfxIndex);
            }

            _isLocked = true;
            transform.DOScale(1.5f, 0.15f).SetLoops(2, LoopType.Yoyo);
        }
        else
        {
            // Shake animation on error
            _rectTransform.DOShakeAnchorPos(0.4f, new Vector2(20f, 0f), 20, 90, false, true);

            // Opcional: Si quisieras un sonido de error, iría aquí.
        }
    }

    private void MoveCursor(int direction)
    {
        int totalSlots = _manager.TotalSlots;
        if (totalSlots == 0) return;

        _currentIndex = (_currentIndex + direction + totalSlots) % totalSlots;

        // REPRODUCIR SONIDO DE MOVIMIENTO
        // Lo ponemos aquí para asegurar que solo suene si realmente se ejecuta la lógica de movimiento
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(moveSfxIndex);
        }

        UpdateVisualPosition();
    }

    private void UpdateVisualPosition()
    {
        RectTransform targetSlot = _manager.GetSlotTransform(_currentIndex);

        if (targetSlot != null)
        {
            _rectTransform.DOMove(targetSlot.position, 0.2f).SetEase(Ease.OutQuad);
        }
    }

    private void SnapToPosition()
    {
        RectTransform targetSlot = _manager.GetSlotTransform(_currentIndex);
        if (targetSlot != null)
        {
            _rectTransform.position = targetSlot.position;
        }
    }
}