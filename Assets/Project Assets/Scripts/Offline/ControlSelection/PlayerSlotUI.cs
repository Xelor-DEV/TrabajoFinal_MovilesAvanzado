using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PlayerSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image slotImage;
    [SerializeField] private TMP_Text playerText;

    private int _playerIndex;
    private bool _isTaken = false;

    public int PlayerIndex => _playerIndex;
    public bool IsTaken => _isTaken;

    public void Initialize(int index, Color color)
    {
        _playerIndex = index;
        playerText.text = $"Player {index + 1}";
        slotImage.color = color;

        // Initial pop animation
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
    }

    public void MarkAsTaken()
    {
        _isTaken = true;

        // Confirmation POP animation (visual feedback only)
        transform.DOScale(1.2f, 0.2f).SetLoops(2, LoopType.Yoyo);

        // Optional: Dim the color slightly to show it is locked
        slotImage.color = Color.Lerp(slotImage.color, Color.gray, 0.5f);
    }

    public RectTransform GetRectTransform()
    {
        return GetComponent<RectTransform>();
    }
}