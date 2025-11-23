using UnityEngine;

public class KartAnimationController : MonoBehaviour
{
    [Header("Animator Reference")]
    [SerializeField] private Animator animator;

    [Header("Animation Settings")]
    [SerializeField] private float boostAnimationSpeed = 1.5f;
    [SerializeField] private float animationSmoothTime = 0.1f;

    private float currentAnimationSpeed = 0f;

    private void HandleMovementData(float speed, float inputY, bool isBoosted)
    {
        if (animator == null) return;

        // Basamos la animación principalmente en el input Y, no en la velocidad física
        float targetAnimationSpeed = CalculateAnimationSpeedFromInput(inputY);

        // Interpolación suave para transiciones naturales
        currentAnimationSpeed = Mathf.Lerp(currentAnimationSpeed, targetAnimationSpeed,
            animationSmoothTime * Time.deltaTime * 10f);

        // Aplicar boost a la velocidad de animación si está activo
        if (isBoosted && inputY > 0)
        {
            animator.speed = boostAnimationSpeed;
        }
        else
        {
            animator.speed = 1f;
        }

        // Establecer el parámetro en el Animator
        animator.SetFloat("Speed", currentAnimationSpeed);
    }

    private float CalculateAnimationSpeedFromInput(float inputY)
    {
        // Convertir input Y directo a rango -10 a 10
        float animationValue = inputY * 10f;

        // Aplicar umbrales para evitar valores muy pequeños
        if (Mathf.Abs(animationValue) < 0.1f)
        {
            animationValue = 0f;
        }

        return animationValue;
    }
}