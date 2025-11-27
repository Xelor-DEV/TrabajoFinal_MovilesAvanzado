using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine; // Necesario para Cinemachine 3.x
using System.Collections;
using System.Collections.Generic;

public class GameInitializer : MonoBehaviour
{
    [Header("Managers References")]
    [SerializeField] private PlayerInputManager playerInputManager;
    [SerializeField] private StartingGridManager gridManager;
    [SerializeField] private MatchDataSO matchData;

    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Race Settings")]
    [Tooltip("Tiempo de espera antes de iniciar la cuenta regresiva")]
    [SerializeField] private float graceTimeDuration = 5f;

    private List<PlayerInput> _spawnedPlayers = new List<PlayerInput>();
    private List<PlayerHUD> _playerHUDs = new List<PlayerHUD>();

    private void Start()
    {
        InitializeInputManager();
        SpawnPlayers();
        StartCoroutine(RaceCountdownRoutine());
    }

    private void InitializeInputManager()
    {
        if (playerInputManager == null) return;

        playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;

        if (!playerInputManager.splitScreen)
            playerInputManager.splitScreen = true;
    }

    private void SpawnPlayers()
    {
        if (matchData == null || gridManager == null) return;

        // Iterar solo sobre los jugadores que se asignaron en el menú anterior
        for (int i = 0; i < matchData.assignedPlayers.Count; i++)
        {
            PlayerAssignment assignment = matchData.assignedPlayers[i];

            // 1. Obtener punto de spawn
            Transform spawnPoint = gridManager.GetSpawnPoint(i);

            // 2. Instanciar el Prefab manualmente
            GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // 3. Configurar PlayerInput
            PlayerInput pInput = playerInstance.GetComponent<PlayerInput>();
            if (pInput != null)
            {
                // Asignar el dispositivo y esquema de control guardado
                pInput.SwitchCurrentControlScheme(assignment.controlScheme, assignment.device);

                // Desactivar movimiento inicialmente
                pInput.DeactivateInput();

                _spawnedPlayers.Add(pInput);
            }

            // 4. Configurar HUD
            PlayerHUD hud = playerInstance.GetComponent<PlayerHUD>();
            if (hud != null)
            {
                _playerHUDs.Add(hud);
            }

            // 5. Configurar CameraSystem y Channels
            CameraSystem camSystem = playerInstance.GetComponent<CameraSystem>();
            if (camSystem != null)
            {
                SetupCameraChannels(camSystem, i);
            }
        }
    }

    private void SetupCameraChannels(CameraSystem camSys, int playerIndex)
    {
        CinemachineBrain brain = camSys.CinemachineBrain;
        CinemachineCamera vCam = camSys.CinemachineCamera;

        if (brain != null && vCam != null)
        {
            // Crear una máscara de canal única por jugador.
            // Channel01 -> 1 << 0
            // Channel02 -> 1 << 1
            // etc.
            OutputChannels channelMask = (OutputChannels)(1 << playerIndex);

            // Asignar al Brain (qué canales VE esta cámara)
            brain.ChannelMask = channelMask;

            // Asignar a la Virtual Camera (en qué canal EMITE esta cámara)
            vCam.OutputChannel = channelMask;
        }
    }

    private IEnumerator RaceCountdownRoutine()
    {
        // 1. Tiempo de Gracia (Nadie se mueve)
        yield return new WaitForSeconds(graceTimeDuration);

        // 2. Cuenta Regresiva (3, 2, 1)
        int countdown = 3;
        while (countdown > 0)
        {
            ShowMessageToAllPlayers(countdown.ToString(), 0.8f);
            // Sonido opcional aquí
            yield return new WaitForSeconds(1f);
            countdown--;
        }

        // 3. GO!
        ShowMessageToAllPlayers("GO!", 1f);

        // 4. Reactivar Inputs
        foreach (var pInput in _spawnedPlayers)
        {
            if (pInput != null)
                pInput.ActivateInput();
        }
    }

    private void ShowMessageToAllPlayers(string text, float duration)
    {
        foreach (var hud in _playerHUDs)
        {
            if (hud != null)
            {
                // Usamos el método existente en PlayerHUD
                hud.ShowCenterMessage(text, duration);
            }
        }
    }
}