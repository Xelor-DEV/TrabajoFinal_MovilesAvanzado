using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class AuthTransitionManager : MonoBehaviour
{
    [Header("Window References")]
    [SerializeField] private WindowsController startScreenWindow;
    [SerializeField] private WindowsController authWindow;
    [SerializeField] private WindowsController mainMenuWindow;

    [Header("UI References")]
    [SerializeField] private Button authLoginButton;

    [Header("Service References")]
    [SerializeField] private StartScreen_Manager startScreenManager;
    [SerializeField] private UnityAccountAuthService authService;
    [SerializeField] private FadeManager fadeManager;

    [Header("Transition Events")]
    public UnityEvent OnStartToAuthTransition;
    public UnityEvent OnAuthToMainMenuTransition;
    public UnityEvent OnTransitionComplete;

    private bool isTransitioning = false;
    private Action pendingTransitionAction;

    private void OnEnable()
    {
        // Suscribirse al evento del StartScreen_Manager
        if (startScreenManager != null)
        {
            startScreenManager.onStartGame.AddListener(HandleStartScreenEvent);
        }

        if (authLoginButton != null)
        {
            authLoginButton.onClick.AddListener(HandleAuthLoginButton);
        }

        if (authService != null)
        {
            authService.OnSignedIn.AddListener(HandleAuthSuccess);
            authService.OnSignInFailed.AddListener(HandleAuthFailure);
        }
    }

    private void OnDisable()
    {
        // Limpiar todos los listeners
        if (startScreenManager != null)
        {
            startScreenManager.onStartGame.RemoveListener(HandleStartScreenEvent);
        }

        if (authLoginButton != null)
        {
            authLoginButton.onClick.RemoveListener(HandleAuthLoginButton);
        }

        if (authService != null)
        {
            authService.OnSignedIn.RemoveListener(HandleAuthSuccess);
            authService.OnSignInFailed.RemoveListener(HandleAuthFailure);
        }

        CleanupFadeListeners();
    }

    private void HandleStartScreenEvent()
    {
        if (isTransitioning) return;
        StartTransition(TransitionToAuthWindow);
    }

    private async void HandleAuthLoginButton()
    {
        if (isTransitioning) return;
        await StartAuthProcess();
    }

    private void StartTransition(Action transitionAction)
    {
        isTransitioning = true;
        pendingTransitionAction = transitionAction;

        CleanupFadeListeners();
        fadeManager.OnShowComplete.AddListener(ExecutePendingTransition);
        fadeManager.Show();
    }

    private void ExecutePendingTransition()
    {
        fadeManager.OnShowComplete.RemoveListener(ExecutePendingTransition);
        pendingTransitionAction?.Invoke();
    }

    private void TransitionToAuthWindow()
    {
        OnStartToAuthTransition?.Invoke();

        if (startScreenWindow != null)
            startScreenWindow.HideWindow();

        if (authWindow != null)
            authWindow.ShowWindow();

        fadeManager.Hide();
        CompleteTransition();
    }

    private async Task StartAuthProcess()
    {
        // El fade permanece visible durante el proceso de autenticación
        if (authService != null)
        {
            try
            {
                await authService.SignInAsync();
                // El resultado se maneja en los eventos HandleAuthSuccess/HandleAuthFailure
            }
            catch (Exception ex)
            {
                Debug.LogError($"Auth process exception: {ex.Message}");
                fadeManager.Hide();
                CompleteTransition();
            }
        }
        else
        {
            Debug.LogError("AuthService reference is missing!");
            fadeManager.Hide();
            CompleteTransition();
        }
    }

    private void HandleAuthSuccess(PlayerInfo playerInfo)
    {
        // Autenticación exitosa, proceder al main menu
        StartTransition(TransitionToMainMenu);
    }

    private void HandleAuthFailure(Exception exception)
    {
        // En caso de error, ocultar el fade y permanecer en auth window
        Debug.LogError($"Authentication failed: {exception.Message}");
        fadeManager.Hide();
        CompleteTransition();
    }

    private void TransitionToMainMenu()
    {
        OnAuthToMainMenuTransition?.Invoke();

        if (authWindow != null)
            authWindow.HideWindow();

        if (mainMenuWindow != null)
            mainMenuWindow.ShowWindow();

        fadeManager.Hide();
        CompleteTransition();
    }

    private void CompleteTransition()
    {
        isTransitioning = false;
        pendingTransitionAction = null;
        OnTransitionComplete?.Invoke();
        CleanupFadeListeners();
    }

    private void CleanupFadeListeners()
    {
        if (fadeManager != null)
        {
            fadeManager.OnShowComplete.RemoveAllListeners();
            fadeManager.OnHideComplete.RemoveAllListeners();
        }
    }

    // Método para debug
    [ContextMenu("Force Start to Auth Transition")]
    private void DebugStartToAuthTransition()
    {
        if (!isTransitioning)
            StartTransition(TransitionToAuthWindow);
    }
}