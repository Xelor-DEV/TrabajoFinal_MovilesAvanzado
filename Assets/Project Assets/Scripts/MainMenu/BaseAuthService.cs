using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System;
using System.Threading.Tasks;

public abstract class BaseAuthService : MonoBehaviour
{
    [Header("Authentication Events")]
    public UnityEvent<PlayerInfo> OnSignedIn;
    public UnityEvent<Exception> OnSignInFailed;
    public UnityEvent OnSignedOut;
    public UnityEvent OnSessionExpired;

    protected bool IsInitialized { get; private set; } = false;

    protected virtual async void Start()
    {
        await InitializeAuthentication();
    }

    protected virtual async Task InitializeAuthentication()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log($"Unity Services initialized: {UnityServices.State}");
            }

            SetupAuthenticationEvents();
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize Unity Services: {ex.Message}");
            OnSignInFailed?.Invoke(ex);
        }
    }

    protected virtual void SetupAuthenticationEvents()
    {
        AuthenticationService.Instance.SignedIn += HandleSignedIn;
        AuthenticationService.Instance.SignInFailed += HandleSignInFailed;
        AuthenticationService.Instance.SignedOut += HandleSignedOut;
        AuthenticationService.Instance.Expired += HandleSessionExpired;
    }

    protected virtual void HandleSignedIn()
    {
        Debug.Log($"Player signed in - ID: {AuthenticationService.Instance.PlayerId}");
        OnSignedIn?.Invoke(AuthenticationService.Instance.PlayerInfo);
    }

    protected virtual void HandleSignInFailed(RequestFailedException exception)
    {
        Debug.LogError($"Sign in failed: {exception.Message}");
        OnSignInFailed?.Invoke(exception);
    }

    protected virtual void HandleSignedOut()
    {
        Debug.Log("Player signed out");
        OnSignedOut?.Invoke();
    }

    protected virtual void HandleSessionExpired()
    {
        Debug.Log("Player session expired");
        OnSessionExpired?.Invoke();
    }

    public abstract Task SignInAsync();

    protected virtual void OnDestroy()
    {
        if (AuthenticationService.Instance != null)
        {
            AuthenticationService.Instance.SignedIn -= HandleSignedIn;
            AuthenticationService.Instance.SignInFailed -= HandleSignInFailed;
            AuthenticationService.Instance.SignedOut -= HandleSignedOut;
            AuthenticationService.Instance.Expired -= HandleSessionExpired;
        }
    }
}