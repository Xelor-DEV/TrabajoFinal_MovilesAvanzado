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

    [Header("Player Identity")]
    [SerializeField] private TMP_Text playerLabelText;

    [Header("Position Animation Settings")]
    [SerializeField] private float posScaleDuration = 0.3f;
    [SerializeField] private float posOvershoot = 1.2f;

    [Header("Message References")]
    [SerializeField] private TMP_Text centerMessageText;

    [Header("Spell References")]
    [SerializeField] private Image spellIcon;

    // State variables
    private bool isCooldownActive = false;
    private Coroutine cooldownCoroutine;
    private Sequence popSequence;
    private Coroutine messageCoroutine;

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

        if (spellIcon != null)
        {
            // Establecer el color a blanco con transparencia 0 (R=1, G=1, B=1, A=0)
            spellIcon.color = new Color(1f, 1f, 1f, 0f);
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
    // MODIFICADO: Ahora hace Pop In
    public void UpdatePosition(int position)
    {
        if (positionText != null)
        {
            // Si el texto cambia, hacemos la animación
            if (positionText.text != position.ToString())
            {
                positionText.text = position.ToString();

                // Resetear escala y animar
                positionText.transform.DOKill();
                positionText.transform.localScale = Vector3.one; // Empezar de tamaño normal

                // Secuencia de Pop: Escalar grande -> Volver a normal
                positionText.transform.DOScale(posOvershoot, posScaleDuration * 0.5f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        positionText.transform.DOScale(1f, posScaleDuration * 0.5f).SetEase(Ease.OutQuad);
                    });
            }
        }

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

    public void ShowCenterMessage(string message, float duration = 0f)
    {
        if (centerMessageText != null)
        {
            // 1. Matar animaciones anteriores y detener temporizadores previos
            // Esto es crucial para que no haya delay entre llamadas rápidas
            centerMessageText.DOKill();
            if (messageCoroutine != null) StopCoroutine(messageCoroutine);

            // 2. Configurar el texto y activar el objeto
            centerMessageText.text = message;
            centerMessageText.gameObject.SetActive(true);

            // 3. Resetear la escala a 0 (inicio del Pop)
            centerMessageText.transform.localScale = Vector3.zero;

            // 4. Animación Pop In muy rápida (0.15s) con rebote (OutBack)
            centerMessageText.transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);

            // 5. Si hay duración, iniciar la cuenta para ocultarlo
            if (duration > 0f)
            {
                messageCoroutine = StartCoroutine(HideMessageAfterDelay(duration));
            }
        }
    }

    // Añadir en PlayerHUD.cs
    public void UpdateSpellIcon(Sprite icon, bool active)
    {
        if (spellIcon != null)
        {
            spellIcon.sprite = icon;
            // Si hay icono es opaco, si no es transparente
            spellIcon.color = active ? Color.white : new Color(1, 1, 1, 0);

            // Un pequeño efecto Pop visual
            if (active)
            {
                spellIcon.transform.DOKill();
                spellIcon.transform.localScale = Vector3.zero;
                spellIcon.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
        }
    }

    public void HideCenterMessage()
    {
        if (centerMessageText != null)
        {
            // Matamos cualquier animación de entrada que esté ocurriendo
            centerMessageText.DOKill();

            // Animación Pop Out rápida (0.1s) hacia adentro (InBack)
            centerMessageText.transform.DOScale(0f, 0.1f)
                .SetEase(Ease.InBack)
                .OnComplete(() => centerMessageText.gameObject.SetActive(false));
        }
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideCenterMessage();
    }

    public void SetPlayerLabel(int playerNumber, Color playerColor)
    {
        if (playerLabelText != null)
        {
            playerLabelText.text = $"Player {playerNumber}";

            // Asignamos el color proveniente del ScriptableObject
            playerLabelText.color = playerColor;
        }
    }

    // Properties
    public bool IsCooldownActive => isCooldownActive;
}