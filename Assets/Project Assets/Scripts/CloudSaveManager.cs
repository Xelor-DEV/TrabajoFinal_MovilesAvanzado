using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public class CloudSaveManager : NonPersistentSingleton<CloudSaveManager>
{
    private const string PLAYER_PROFILE_KEY = "player_profile";

    [Header("Version Management")]
    [SerializeField] private GameVersion gameVersion;

    // Evento que se dispara cuando se guardan cambios en la nube
    public event Action<PlayerProfileData> OnProfileDataSaved;
    public event Action<PlayerProfileData> OnProfileDataLoaded;

    public async Task InitializeAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("CloudSaveManager: Player not signed in");
                return;
            }

            Debug.Log("CloudSaveManager initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"CloudSaveManager initialization failed: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> SavePlayerProfileAsync(PlayerProfileData profileData)
    {
        try
        {
            await InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogError("Cannot save profile: Player not signed in");
                return false;
            }

            // Actualizar la versión del juego antes de guardar
            profileData.gameVersion = gameVersion.currentVersion;

            string profileJson = JsonUtility.ToJson(profileData);
            var data = new Dictionary<string, object> { { PLAYER_PROFILE_KEY, profileJson } };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("Player profile saved successfully to cloud");

            // Disparar evento
            OnProfileDataSaved?.Invoke(profileData);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save player profile: {ex.Message}");
            return false;
        }
    }

    public async Task<PlayerProfileData> LoadPlayerProfileAsync()
    {
        try
        {
            await InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogError("Cannot load profile: Player not signed in");
                return await CreateDefaultProfileAsync();
            }

            var keys = new HashSet<string> { PLAYER_PROFILE_KEY };
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (data.TryGetValue(PLAYER_PROFILE_KEY, out var profileValue))
            {
                string profileJson = profileValue.Value.GetAs<string>();
                PlayerProfileData profileData = JsonUtility.FromJson<PlayerProfileData>(profileJson);

                // Verificar versión y migrar si es necesario
                profileData = await MigrateProfileVersionAsync(profileData);

                Debug.Log("Player profile loaded successfully from cloud");
                OnProfileDataLoaded?.Invoke(profileData);
                return profileData;
            }
            else
            {
                Debug.Log("No player profile found in cloud, creating default profile");
                return await CreateDefaultProfileAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load player profile: {ex.Message}");
            return await CreateDefaultProfileAsync();
        }
    }

    private async Task<PlayerProfileData> MigrateProfileVersionAsync(PlayerProfileData profileData)
    {
        if (profileData.gameVersion == gameVersion.currentVersion)
        {
            return profileData; // No necesita migración
        }

        Debug.Log($"Migrating profile from version {profileData.gameVersion} to {gameVersion.currentVersion}");

        // Aquí puedes añadir lógica de migración entre versiones
        // Por ahora, simplemente actualizamos la versión
        profileData.gameVersion = gameVersion.currentVersion;

        // Guardar el perfil migrado
        await SavePlayerProfileAsync(profileData);

        return profileData;
    }

    private async Task<PlayerProfileData> CreateDefaultProfileAsync()
    {
        try
        {
            string playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Player";
            }

            var defaultProfile = new PlayerProfileData
            {
                description = "Welcome to the game!",
                personalStatus = "Online",
                birthDate = new PlayerBirthDate { day = 1, month = 1, year = 2000 },
                iconIndex = 0,
                gameVersion = gameVersion.currentVersion
            };

            // Guardar el perfil por defecto
            await SavePlayerProfileAsync(defaultProfile);
            return defaultProfile;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create default profile: {ex.Message}");
            return new PlayerProfileData
            {
                description = "Welcome to the game!",
                personalStatus = "Online",
                birthDate = new PlayerBirthDate { day = 1, month = 1, year = 2000 },
                iconIndex = 0,
                gameVersion = gameVersion.currentVersion
            };
        }
    }

    public async Task<bool> UpdatePlayerDisplayNameAsync(string newDisplayName)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newDisplayName);
            Debug.Log("Player display name updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update player display name: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GetPlayerDisplayNameAsync()
    {
        try
        {
            return await AuthenticationService.Instance.GetPlayerNameAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get player display name: {ex.Message}");
            return "Player";
        }
    }
}