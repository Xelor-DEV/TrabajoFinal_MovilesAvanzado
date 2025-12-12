using UnityEngine;
using TMPro;
using DG.Tweening; // Importante: Necesitas DOTween instalado

public class PlayerConfigManager : MonoBehaviour
{
    [Header("References")]
    public MatchDataSO matchData;
    public TMP_Text playerCountText;

    [Header("Animation Settings")]
    [SerializeField] private float punchScaleAmount = 0.5f; // Cuánto crece al cambiar
    [SerializeField] private float animationDuration = 0.1f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 10f;     // Fuerza del temblor
    [SerializeField] private Color errorColor = Color.red;

    private Color originalColor;
    private Vector3 originalScale;
    private RectTransform textRect;

    private void Start()
    {
        if (matchData == null || playerCountText == null)
        {
            Debug.LogError("PlayerConfigManager: Faltan referencias (MatchData o Text).");
            return;
        }

        // Guardamos valores originales para resetear animaciones
        textRect = playerCountText.GetComponent<RectTransform>();
        originalColor = playerCountText.color;
        originalScale = textRect.localScale;

        // Validar que el valor actual esté dentro de los rangos al iniciar
        matchData.currentPlayerSelection = Mathf.Clamp(
            matchData.currentPlayerSelection,
            matchData.minPlayers,
            matchData.maxPossiblePlayers
        );

        UpdateDisplay();
    }

    public void IncreasePlayers()
    {
        // Verificamos si podemos subir más
        if (matchData.currentPlayerSelection < matchData.maxPossiblePlayers)
        {
            matchData.currentPlayerSelection++;
            UpdateDisplay();
            AnimateSuccess();
        }
        else
        {
            // Ya estamos en el máximo, mostramos error
            AnimateFailure();
            Debug.Log("Límite máximo de jugadores alcanzado.");
        }
    }

    public void DecreasePlayers()
    {
        // Verificamos si podemos bajar más
        if (matchData.currentPlayerSelection > matchData.minPlayers)
        {
            matchData.currentPlayerSelection--;
            UpdateDisplay();
            AnimateSuccess();
        }
        else
        {
            // Ya estamos en el mínimo, mostramos error
            AnimateFailure();
            Debug.Log("Límite mínimo de jugadores alcanzado.");
        }
    }

    private void UpdateDisplay()
    {
        playerCountText.text = matchData.currentPlayerSelection.ToString();
    }

    // Animación de éxito (Escala rápida)
    private void AnimateSuccess()
    {
        // Matamos cualquier tween previo en este objeto para evitar conflictos
        textRect.DOKill();

        // Reseteamos valores por si acaso venía de una animación interrumpida
        textRect.localScale = originalScale;
        playerCountText.color = originalColor;

        // Efecto "Punch" (se agranda y vuelve a su sitio)
        textRect.DOPunchScale(Vector3.one * punchScaleAmount, animationDuration, 1, 0);
    }

    // Animación de fallo (Temblor + Color Rojo)
    private void AnimateFailure()
    {
        textRect.DOKill();
        playerCountText.DOKill(); // Matamos tweens de color también

        playerCountText.color = originalColor;

        // 1. Temblor (Shake)
        textRect.DOShakeAnchorPos(shakeDuration, shakeStrength, 20, 90, false, true);

        // 2. Cambio de color a Rojo y vuelta al original
        playerCountText.DOColor(errorColor, shakeDuration / 2)
            .OnComplete(() => playerCountText.DOColor(originalColor, shakeDuration / 2));
    }

    private void OnDestroy()
    {
        // Buena práctica: Limpiar tweens al destruir el objeto
        if (textRect != null) textRect.DOKill();
        if (playerCountText != null) playerCountText.DOKill();
    }
}