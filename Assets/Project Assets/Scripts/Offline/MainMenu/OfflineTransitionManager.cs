using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class OfflineTransitionManager : MonoBehaviour
{
    [Header("Window References")]
    [SerializeField] private WindowsController startScreenWindow;
    [SerializeField] private WindowsController playOptionsWindow;

    [Header("Service References")]
    [SerializeField] private StartScreen_Manager startScreenManager;
    [SerializeField] private FadeManager fadeManager;

    [Header("Transition Events")]
    public UnityEvent OnStartToPlayOptionsTransition;
    public UnityEvent OnTransitionComplete;

    private bool isTransitioning = false;
    private System.Action pendingTransitionAction;

    private void OnEnable()
    {
        if (startScreenManager != null)
        {
            startScreenManager.onStartGame.AddListener(HandleStartScreenEvent);
        }
    }

    private void OnDisable()
    {
        if (startScreenManager != null)
        {
            startScreenManager.onStartGame.RemoveListener(HandleStartScreenEvent);
        }

        CleanupFadeListeners();
    }

    private void HandleStartScreenEvent()
    {
        if (isTransitioning) return;
        StartTransition(TransitionToPlayOptions);
    }

    private void StartTransition(System.Action transitionAction)
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

    private void TransitionToPlayOptions()
    {
        OnStartToPlayOptionsTransition?.Invoke();

        if (startScreenWindow != null)
            startScreenWindow.HideWindow();

        if (playOptionsWindow != null)
            playOptionsWindow.ShowWindow();

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

    [ContextMenu("Force Start to PlayOptions Transition")]
    private void DebugStartToPlayOptionsTransition()
    {
        if (!isTransitioning)
            StartTransition(TransitionToPlayOptions);
    }
}
