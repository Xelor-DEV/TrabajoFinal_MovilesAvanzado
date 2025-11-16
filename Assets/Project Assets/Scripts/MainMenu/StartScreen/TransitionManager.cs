using UnityEngine;
using UnityEngine.Events;

public class TransitionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StartScreen_Manager startScreenManager;
    [SerializeField] private FadeManager fadeManager;
    [SerializeField] private WindowsController startScreenWindow;
    [SerializeField] private WindowsController nextWindow;

    [Header("Events")]
    public UnityEvent OnTransitionStart;
    public UnityEvent OnTransitionComplete;

    private bool transitionStarted = false;

    private void OnEnable()
    {
        if (startScreenManager != null && !transitionStarted)
        {
            startScreenManager.onStartGame.AddListener(StartTransition);
        }
    }

    private void OnDisable()
    {
        // Solo removemos si la transición no ha empezado
        if (!transitionStarted && startScreenManager != null)
        {
            startScreenManager.onStartGame.RemoveListener(StartTransition);
        }
    }

    private void StartTransition()
    {
        if (transitionStarted) return;

        transitionStarted = true;
        OnTransitionStart?.Invoke();

        // Remover listener inmediatamente para evitar múltiples llamadas
        if (startScreenManager != null)
        {
            startScreenManager.onStartGame.RemoveListener(StartTransition);
        }

        // Mostrar el fade
        if (fadeManager != null)
        {
            // Agregar listeners para el fade
            fadeManager.OnShowComplete.AddListener(OnFadeShowComplete);
            fadeManager.OnHideComplete.AddListener(OnFadeHideComplete);

            fadeManager.Show();
        }
        else
        {
            // Si no hay fade manager, cambiar ventanas directamente
            ChangeWindows();
        }
    }

    private void OnFadeShowComplete()
    {
        // Remover listener inmediatamente
        if (fadeManager != null)
        {
            fadeManager.OnShowComplete.RemoveListener(OnFadeShowComplete);
        }

        // Cambiar ventanas cuando el fade esté completamente mostrado
        ChangeWindows();
    }

    private void ChangeWindows()
    {
        // Ocultar ventana actual
        if (startScreenWindow != null)
        {
            startScreenWindow.HideWindow();
        }

        // Mostrar siguiente ventana
        if (nextWindow != null)
        {
            // Agregar listener para cuando la nueva ventana termine de mostrarse
            nextWindow.OnWindowShowComplete.AddListener(OnNextWindowShown);
            nextWindow.ShowWindow();
        }
        else
        {
            // Si no hay siguiente ventana, ocultar fade inmediatamente
            if (fadeManager != null)
            {
                fadeManager.Hide();
            }
            else
            {
                CompleteTransition();
            }
        }
    }

    private void OnNextWindowShown()
    {
        // Remover el listener inmediatamente
        if (nextWindow != null)
        {
            nextWindow.OnWindowShowComplete.RemoveListener(OnNextWindowShown);
        }

        // Ocultar el fade
        if (fadeManager != null)
        {
            fadeManager.Hide();
        }
        else
        {
            CompleteTransition();
        }
    }

    private void OnFadeHideComplete()
    {
        // Remover listeners del fade
        if (fadeManager != null)
        {
            fadeManager.OnHideComplete.RemoveListener(OnFadeHideComplete);
        }

        // Transición completa
        CompleteTransition();
    }

    private void CompleteTransition()
    {
        OnTransitionComplete?.Invoke();

        // Opcional: Desactivar este manager si ya no es necesario
        // this.enabled = false;
    }

    // Método público para forzar la transición manualmente (útil para testing)
    [ContextMenu("Start Transition")]
    public void StartManualTransition()
    {
        StartTransition();
    }

    // Método para resetear el manager (por si acaso)
    public void ResetTransition()
    {
        transitionStarted = false;

        if (startScreenManager != null)
        {
            startScreenManager.onStartGame.RemoveListener(StartTransition);
            startScreenManager.onStartGame.AddListener(StartTransition);
        }
    }
}