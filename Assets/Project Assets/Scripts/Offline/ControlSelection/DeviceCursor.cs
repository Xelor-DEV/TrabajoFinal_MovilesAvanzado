using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;

public class DeviceCursor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image cursorImage;

    [Header("Audio Settings")]
    [SerializeField] private int moveSfxIndex;
    [SerializeField] private int submitSfxIndex;

    private ControlSelectionManager _manager;
    private RectTransform _rectTransform;
    private int _currentIndex = 0;

    private bool _isLocked = false;

    public InputDevice LinkedDevice { get; private set; }

    // Se añade el parámetro 'startIndex'
    public void Initialize(ControlSelectionManager manager, Color cursorColor, InputDevice device, int startIndex)
    {
        _manager = manager;
        _rectTransform = GetComponent<RectTransform>();
        LinkedDevice = device;

        // Establecemos el índice inicial (ej: 0 para P1, 1 para P2, 0 para P3...)
        _currentIndex = startIndex;

        cursorImage.color = cursorColor;

        // IMPORTANTE: Forzamos la posición inicial antes de la animación
        SnapToPosition();

        // EFECTO POP-IN
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
    }

    public void PopOutAndDestroy()
    {
        transform.DOKill();
        transform.DOScale(0f, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (_isLocked || !context.performed) return;

        float input = context.ReadValue<float>();

        if (input > 0.5f) MoveCursor(1);
        else if (input < -0.5f) MoveCursor(-1);
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (_isLocked || !context.performed) return;

        bool success = _manager.TrySelectPlayer(_currentIndex, GetComponent<PlayerInput>());

        if (success)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(submitSfxIndex);

            _isLocked = true;
            transform.DOScale(1.5f, 0.15f).SetLoops(2, LoopType.Yoyo);
        }
        else
        {
            _rectTransform.DOShakeAnchorPos(0.4f, new Vector2(20f, 0f), 20, 90, false, true);
        }
    }

    private void MoveCursor(int direction)
    {
        int totalSlots = _manager.TotalSlots;
        if (totalSlots == 0) return;

        _currentIndex = (_currentIndex + direction + totalSlots) % totalSlots;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(moveSfxIndex);

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
            // Al asignar .position (world space) nos aseguramos de caer exactamente sobre el slot
            // independientemente de la jerarquía del cursor.
            _rectTransform.position = targetSlot.position;
        }
    }

    public void Hide() => cursorImage.enabled = false;
}