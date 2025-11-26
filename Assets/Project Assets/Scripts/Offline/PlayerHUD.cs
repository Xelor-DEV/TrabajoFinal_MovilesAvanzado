using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class PlayerHUD : MonoBehaviour
{
    [Header("Push Ability Settings")]
    [SerializeField] private Image pushAbilityFill;
    [SerializeField] private RectTransform pushContainer;
    [SerializeField] private float popInDuration = 0.3f;
    [SerializeField] private float popOutDuration = 0.2f;
    [SerializeField] private float overshootScale = 1.1f;

    [Header("Tackle Attacker Reference")]
    [SerializeField] private KartTackleAttacker tackleAttacker;

    [Header("Race Info References")]
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text positionSuffixText;
    [SerializeField] private TMP_Text lapsText;

    [Header("Message References")]
    [SerializeField] private TMP_Text centerMessageText;

    // State variables
    private bool isCooldownActive = false;
    private Coroutine cooldownCoroutine;
    private Sequence popSequence;

    private void Start()
    {
        // Initialize push container to be hidden
        if (pushContainer != null)
        {
            pushContainer.localScale = Vector3.zero;
            pushContainer.gameObject.SetActive(false);
        }

        // Initialize fill amount
        if (pushAbilityFill != null)
        {
            pushAbilityFill.fillAmount = 0f;
        }
    }

    private void OnDestroy()
    {
        // Clean up tweens
        if (popSequence != null && popSequence.IsActive())
        {
            popSequence.Kill();
        }
    }

    // Este método se llama por el evento OnCooldownStarted del KartTackleAttacker
    public void StartPushCooldown()
    {
        if (isCooldownActive) return;

        if (tackleAttacker == null)
        {
            Debug.LogWarning("TackleAttacker reference is missing in HUD.");
            return;
        }

        isCooldownActive = true;

        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);

        // Obtener la duración del cooldown directamente del tackleAttacker
        float cooldownDuration = tackleAttacker.TackleCooldown;
        cooldownCoroutine = StartCoroutine(CooldownRoutine(cooldownDuration));
    }

    private IEnumerator CooldownRoutine(float duration)
    {
        // Show container with pop in animation
        ShowPushContainer();

        // Animate fill amount from 0 to 1 over the cooldown duration
        if (pushAbilityFill != null)
        {
            pushAbilityFill.fillAmount = 0f;
            pushAbilityFill.DOFillAmount(1f, duration).SetEase(Ease.Linear);
        }

        // Wait for cooldown to complete
        yield return new WaitForSeconds(duration);

        // Hide container with pop out animation
        HidePushContainer();
        isCooldownActive = false;
    }

    private void ShowPushContainer()
    {
        if (pushContainer == null) return;

        pushContainer.gameObject.SetActive(true);

        // Kill any existing animation
        if (popSequence != null && popSequence.IsActive())
            popSequence.Kill();

        // Create pop in animation: 0 -> 1.1 -> 1
        popSequence = DOTween.Sequence();
        popSequence.Append(pushContainer.DOScale(overshootScale, popInDuration * 0.7f).SetEase(Ease.OutBack));
        popSequence.Append(pushContainer.DOScale(1f, popInDuration * 0.3f).SetEase(Ease.InOutBack));
    }

    private void HidePushContainer()
    {
        if (pushContainer == null) return;

        // Kill any existing animation
        if (popSequence != null && popSequence.IsActive())
            popSequence.Kill();

        // Create pop out animation: 1 -> 1.1 -> 0
        popSequence = DOTween.Sequence();
        popSequence.Append(pushContainer.DOScale(overshootScale, popOutDuration * 0.3f).SetEase(Ease.OutBack));
        popSequence.Append(pushContainer.DOScale(0f, popOutDuration * 0.7f).SetEase(Ease.InOutBack));
        popSequence.OnComplete(() => pushContainer.gameObject.SetActive(false));
    }

    // Este método se llama por el evento OnCooldownFinished del KartTackleAttacker
    public void ResetPushAbility()
    {
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }

        if (pushAbilityFill != null)
        {
            pushAbilityFill.DOKill();
            pushAbilityFill.fillAmount = 0f;
        }

        HidePushContainer();
        isCooldownActive = false;
    }

    // Public methods for other HUD elements (to be implemented later)
    public void UpdatePosition(int position)
    {
        if (positionText != null)
            positionText.text = position.ToString();

        UpdatePositionSuffix(position);
    }

    public void UpdatePositionSuffix(int position)
    {
        if (positionSuffixText != null)
        {
            string suffix = "th";

            if (position == 1) suffix = "st";
            else if (position == 2) suffix = "nd";
            else if (position == 3) suffix = "rd";

            positionSuffixText.text = suffix;
        }
    }

    public void UpdateLaps(int currentLap, int totalLaps)
    {
        if (lapsText != null)
            lapsText.text = $"{currentLap}/{totalLaps}";
    }

    public void ShowCenterMessage(string message, float duration = 0f)
    {
        if (centerMessageText != null)
        {
            centerMessageText.text = message;
            centerMessageText.gameObject.SetActive(true);

            if (duration > 0f)
            {
                StartCoroutine(HideMessageAfterDelay(duration));
            }
        }
    }

    public void HideCenterMessage()
    {
        if (centerMessageText != null)
            centerMessageText.gameObject.SetActive(false);
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideCenterMessage();
    }

    // Properties
    public bool IsCooldownActive => isCooldownActive;
}