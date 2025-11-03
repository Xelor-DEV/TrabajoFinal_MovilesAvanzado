using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

public class AnonymousAuthService : BaseAuthService
{
    public override async Task SignInAsync()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning($"[{ServiceType}] Authentication service not initialized");
            throw new InvalidOperationException("Authentication service not initialized");
        }

        try
        {
            // Verificar si ya está autenticado
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"[{ServiceType}] Player is already signed in");
                HandleSignedIn();
                return;
            }

            // Marcar este servicio como la fuente activa de autenticación
            isActiveAuthSource = true;

            // Realizar autenticación anónima
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{ServiceType}] Anonymous sign in failed: {ex.Message}");
            isActiveAuthSource = false; // Resetear en caso de error
            throw;
        }
    }
}