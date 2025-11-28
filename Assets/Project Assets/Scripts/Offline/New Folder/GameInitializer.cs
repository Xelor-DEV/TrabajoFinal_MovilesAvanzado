using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
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

    [Header("Split Screen Configuration")]
    [Tooltip("Si es TRUE: Jugador 1 Arriba, Jugador 2 Abajo. Si es FALSE: Izquierda/Derecha. (Solo afecta a 2 jugadores)")]
    [SerializeField] private bool splitScreenTopBottom = true;

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

        // Cuando lo hacemos manual, a veces es mejor desactivar la lógica automática
        // de split screen del manager para evitar conflictos, ya que nosotros controlaremos los rects.
        playerInputManager.splitScreen = false;
    }

    private void SpawnPlayers()
    {
        if (matchData == null || gridManager == null) return;

        int totalPlayers = matchData.assignedPlayers.Count;

        for (int i = 0; i < totalPlayers; i++)
        {
            PlayerAssignment assignment = matchData.assignedPlayers[i];
            Transform spawnPoint = gridManager.GetSpawnPoint(i);

            // 1. Instanciar
            GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // 2. Configurar Input
            PlayerInput pInput = playerInstance.GetComponent<PlayerInput>();
            if (pInput != null)
            {
                pInput.SwitchCurrentControlScheme(assignment.controlScheme, assignment.device);
                pInput.DeactivateInput();
                _spawnedPlayers.Add(pInput);
            }

            // 3. Configurar HUD
            PlayerHUD hud = playerInstance.GetComponent<PlayerHUD>();
            if (hud != null)
            {
                _playerHUDs.Add(hud);

                // 1. Obtener el color. Usamos un color por defecto (blanco) por seguridad
                Color assignedColor = Color.white;

                // Verificamos que el array de colores tenga suficientes elementos para evitar errores
                if (matchData.playerColors != null && matchData.playerColors.Length > i)
                {
                    assignedColor = matchData.playerColors[i];
                }

                // 2. Pasamos el número (i+1) y el color
                hud.SetPlayerLabel(i + 1, assignedColor);
            }

            // 4. Configurar CameraSystem, Channels y VIEWPORT (Pantalla partida)
            CameraSystem camSystem = playerInstance.GetComponent<CameraSystem>();
            if (camSystem != null)
            {
                SetupCameraChannels(camSystem, i);
                ConfigureCameraViewport(camSystem.CM, i, totalPlayers);
                camSystem.Canvas.worldCamera = camSystem.CM;
                camSystem.Canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

                // Buscamos el AudioListener en la cámara referenciada por CameraSystem
                AudioListener listener = camSystem.CM.GetComponent<AudioListener>();

                // Si existe y NO es el primer jugador (i > 0), lo eliminamos.
                if (listener != null && i > 0)
                {
                    Destroy(listener);
                }
            }
        }
    }

    private void ConfigureCameraViewport(Camera cam, int playerIndex, int totalPlayers)
    {
        if (cam == null) return;

        // Declaramos las variables UNA SOLA VEZ aquí arriba para evitar el error CS0136.
        // X, Y = Posición inicial (0 a 1)
        // W, H = Ancho y Alto (0 a 1)
        float rectX, rectY, rectW, rectH;

        // --- CASO 1: SOLO 2 JUGADORES ---
        if (totalPlayers == 2)
        {
            if (splitScreenTopBottom)
            {
                // MODO: ARRIBA / ABAJO
                rectX = 0f;
                rectW = 1f;
                rectH = 0.5f;
                // Si es P1 (index 0) Y=0.5 (Arriba), Si es P2 Y=0 (Abajo)
                rectY = (playerIndex == 0) ? 0.5f : 0f;
            }
            else
            {
                // MODO: IZQUIERDA / DERECHA
                rectY = 0f;
                rectH = 1f;
                rectW = 0.5f;
                // Si es P1 (index 0) X=0 (Izq), Si es P2 X=0.5 (Der)
                rectX = (playerIndex == 0) ? 0f : 0.5f;
            }
        }
        // --- CASO 2: 3 O MÁS JUGADORES (GRID AUTOMÁTICO) ---
        else
        {
            // Calculamos columnas y filas
            int cols = Mathf.CeilToInt(Mathf.Sqrt(totalPlayers));
            int rows = Mathf.CeilToInt((float)totalPlayers / cols);

            // Ajuste estético para 5 y 6 jugadores (3 columnas x 2 filas se ve mejor en monitores anchos)
            if (totalPlayers >= 5 && totalPlayers <= 6)
            {
                cols = 3;
                rows = 2;
            }

            rectW = 1f / cols;
            rectH = 1f / rows;

            // Calculamos posición en la grilla
            int colIndex = playerIndex % cols;
            int rowIndex = playerIndex / cols;

            // Invertimos la fila porque Unity UI (0,0) es abajo-izquierda, 
            // pero queremos que el Jugador 1 empiece arriba-izquierda.
            int invertedRowIndex = (rows - 1) - rowIndex;

            rectX = colIndex * rectW;
            rectY = invertedRowIndex * rectH;
        }

        // Aplicamos el rectángulo final a la cámara
        cam.rect = new Rect(rectX, rectY, rectW, rectH);
    }

    private void SetupCameraChannels(CameraSystem camSys, int playerIndex)
    {
        CinemachineBrain brain = camSys.CinemachineBrain;
        CinemachineCamera vCam = camSys.CinemachineCamera;

        if (brain != null && vCam != null)
        {
            OutputChannels channelMask = (OutputChannels)(1 << playerIndex);
            brain.ChannelMask = channelMask;
            vCam.OutputChannel = channelMask;
        }
    }

    private IEnumerator RaceCountdownRoutine()
    {
        yield return new WaitForSeconds(graceTimeDuration);
        int countdown = 3;
        while (countdown > 0)
        {
            ShowMessageToAllPlayers(countdown.ToString(), 0.8f);
            yield return new WaitForSeconds(1f);
            countdown--;
        }
        ShowMessageToAllPlayers("GO!", 1f);
        foreach (var pInput in _spawnedPlayers)
        {
            if (pInput != null) pInput.ActivateInput();
        }
    }

    private void ShowMessageToAllPlayers(string text, float duration)
    {
        foreach (var hud in _playerHUDs)
        {
            if (hud != null) hud.ShowCenterMessage(text, duration);
        }
    }
}