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
    [SerializeField] private Button authUnityLoginButton;
    [SerializeField] private Button authAnonymousLoginButton;

    [Header("Service References")]
    [SerializeField] private StartScreen_Manager startScreenManager;
    [SerializeField] private UnityAccountAuthService unityAuthService;
    [SerializeField] private AnonymousAuthService anonymousAuthService;
    [SerializeField] private FadeManager fadeManager;
    [SerializeField] private MainMenuManager mainMenuManager;

    [Header("Transition Events")]
    public UnityEvent OnStartToAuthTransition;
    public UnityEvent OnAuthToMainMenuTransition;
    public UnityEvent OnTransitionComplete;

    private bool isTransitioning = false;
    private Action pendingTransitionAction;
    private bool authInProgress = false;
    private string currentAuthService = "";

    private void OnEnable()
    {
        if (startScreenManager != null)
        {
            startScreenManager.onStartGame.AddListener(HandleStartScreenEvent);
        }

        if (authUnityLoginButton != null)
        {
            authUnityLoginButton.onClick.AddListener(HandleUnityAuthLogin);
        }

        if (authAnonymousLoginButton != null)
        {
            authAnonymousLoginButton.onClick.AddListener(HandleAnonymousAuthLogin);
        }

        if (unityAuthService != null)
        {
            unityAuthService.OnSignedIn.AddListener(HandleAuthSuccess);
            unityAuthService.OnSignInFailed.AddListener(HandleAuthFailure);
        }

        if (anonymousAuthService != null)
        {
            anonymousAuthService.OnSignedIn.AddListener(HandleAuthSuccess);
            anonymousAuthService.OnSignInFailed.AddListener(HandleAuthFailure);
        }
    }

    private void OnDisable()
    {
        if (startScreenManager != null)
        {
            startScreenManager.onStartGame.RemoveListener(HandleStartScreenEvent);
        }

        if (authUnityLoginButton != null)
        {
            authUnityLoginButton.onClick.RemoveListener(HandleUnityAuthLogin);
        }

        if (authAnonymousLoginButton != null)
        {
            authAnonymousLoginButton.onClick.RemoveListener(HandleAnonymousAuthLogin);
        }

        if (unityAuthService != null)
        {
            unityAuthService.OnSignedIn.RemoveListener(HandleAuthSuccess);
            unityAuthService.OnSignInFailed.RemoveListener(HandleAuthFailure);
        }

        if (anonymousAuthService != null)
        {
            anonymousAuthService.OnSignedIn.RemoveListener(HandleAuthSuccess);
            anonymousAuthService.OnSignInFailed.RemoveListener(HandleAuthFailure);
        }

        CleanupFadeListeners();
    }

    private void HandleStartScreenEvent()
    {
        if (isTransitioning) return;
        StartTransition(TransitionToAuthWindow);
    }

    private async void HandleUnityAuthLogin()
    {
        if (isTransitioning || authInProgress) return;
        currentAuthService = "Unity";
        await StartAuthProcess(unityAuthService.SignInAsync, "Unity");
    }

    private async void HandleAnonymousAuthLogin()
    {
        if (isTransitioning || authInProgress) return;
        currentAuthService = "Anonymous";
        await StartAuthProcess(anonymousAuthService.SignInAsync, "Anonymous");
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

    private async Task StartAuthProcess(Func<Task> authMethod, string authType)
    {
        authInProgress = true;
        SetAuthButtonsInteractable(false);

        try
        {
            fadeManager.Show();
            await authMethod();
        }
        catch (Exception ex)
        {
            Debug.LogError($"{authType} Auth process exception: {ex.Message}");
            HandleAuthFailure(ex);
        }
    }

    // En el método HandleAuthSuccess, añadir:
    private async void HandleAuthSuccess(PlayerInfo playerInfo)
    {
        string playerId = AuthenticationService.Instance.PlayerId;
        Debug.Log($"Authentication successful for player: {playerId} via {currentAuthService}");

        // Loguear en Vivox después de autenticarse
        try
        {
            await VivoxLobbyManager.Instance.LoginVivox();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Vivox login failed: {ex.Message}");
            // Continuar aunque falle Vivox
        }

        await TransitionToMainMenuWithUIUpdate();
    }

    private async Task TransitionToMainMenuWithUIUpdate()
    {
        try
        {
            Debug.Log("Waiting for UI to update before showing main menu...");

            if (mainMenuManager != null)
            {
                // Suscribirse temporalmente al evento de actualización de UI
                var uiUpdateTask = mainMenuManager.WaitForUIUpdate();

                // Cargar el perfil (esto disparará la actualización de UI)
                await mainMenuManager.LoadPlayerProfile();

                // Esperar a que la UI se actualice completamente
                await uiUpdateTask;
            }

            Debug.Log("UI updated successfully, proceeding to main menu");

            // Ahora proceder con la transición al menú principal
            TransitionToMainMenu();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update UI during transition: {ex.Message}");
            // Continuar con la transición aunque falle la actualización de UI
            TransitionToMainMenu();
        }
    }


    private void HandleAuthFailure(Exception exception)
    {
        Debug.LogError($"Authentication failed via {currentAuthService}: {exception.Message}");

        fadeManager.Hide();
        ResetAuthState();
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
        ResetAuthState();
        CompleteTransition();
    }

    private void CompleteTransition()
    {
        isTransitioning = false;
        pendingTransitionAction = null;
        OnTransitionComplete?.Invoke();
        CleanupFadeListeners();
    }

    private void ResetAuthState()
    {
        authInProgress = false;
        currentAuthService = "";
        SetAuthButtonsInteractable(true);
    }

    private void SetAuthButtonsInteractable(bool interactable)
    {
        if (authUnityLoginButton != null)
            authUnityLoginButton.interactable = interactable;

        if (authAnonymousLoginButton != null)
            authAnonymousLoginButton.interactable = interactable;
    }

    private void CleanupFadeListeners()
    {
        if (fadeManager != null)
        {
            fadeManager.OnShowComplete.RemoveAllListeners();
            fadeManager.OnHideComplete.RemoveAllListeners();
        }
    }

    [ContextMenu("Force Start to Auth Transition")]
    private void DebugStartToAuthTransition()
    {
        if (!isTransitioning)
            StartTransition(TransitionToAuthWindow);
    }
}