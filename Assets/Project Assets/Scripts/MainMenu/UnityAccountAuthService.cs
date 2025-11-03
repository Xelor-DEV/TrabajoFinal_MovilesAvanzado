using UnityEngine.Events;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;

public class UnityAccountAuthService : BaseAuthService
{
    [Header("Unity Account Events")]
    public UnityEvent OnUnityAccountSignInStarted;

    protected override async void Start()
    {
        await InitializeAuthentication();

        if (PlayerAccountService.Instance != null)
        {
            PlayerAccountService.Instance.SignedIn += HandleUnityAccountSignedIn;
        }
    }

    public override async Task SignInAsync()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("Authentication service not initialized");
            return;
        }

        try
        {
            OnUnityAccountSignInStarted?.Invoke();
            await PlayerAccountService.Instance.StartSignInAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Unity Account sign in failed: {ex.Message}");
            OnSignInFailed?.Invoke(ex);
        }
    }

    private async void HandleUnityAccountSignedIn()
    {
        try
        {
            string accessToken = PlayerAccountService.Instance.AccessToken;
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Sign in with Unity failed: {ex.Message}");
            OnSignInFailed?.Invoke(ex);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (PlayerAccountService.Instance != null)
        {
            PlayerAccountService.Instance.SignedIn -= HandleUnityAccountSignedIn;
        }
    }
}