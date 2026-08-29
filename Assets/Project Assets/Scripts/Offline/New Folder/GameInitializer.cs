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

    [Header("Background Configuration")]
    [Tooltip("Referencia a la cámara que limpia el fondo de la pantalla")]
    [SerializeField] private Camera backgroundCamera; 

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
        SetupBackgroundCamera();
        InitializeInputManager();
        SpawnPlayers();
        StartCoroutine(RaceCountdownRoutine());
    }

    private void InitializeInputManager()
    {
        if (playerInputManager == null) return;

        playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;

        // Cuando lo hacemos manual, a veces es mejor desactivar la l�gica autom�tica
        // de split screen del manager para evitar conflictos, ya que nosotros controlaremos los rects.
        playerInputManager.splitScreen = false;
    }

    private void SetupBackgroundCamera()
    {
        if (backgroundCamera == null)
        {
            Debug.LogWarning("Background Camera no asignada en GameInitializer.");
            return;
        }

        // Forzamos la configuración para que limpie cualquier residuo visual
        backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        backgroundCamera.backgroundColor = Color.black;

        // Depth en -1 asegura que se renderice ANTES que las cámaras de los jugadores
        backgroundCamera.depth = -1;

        // Culling Mask en 0 (Nothing) para que no procese objetos 3D, solo el color de fondo
        backgroundCamera.cullingMask = 0;

        // Nos aseguramos de que cubra toda la pantalla
        backgroundCamera.rect = new Rect(0, 0, 1, 1);
    }

    private void SpawnPlayers()
    {
        if (matchData == null || gridManager == null) return;

        int totalPlayers = matchData.assignedPlayers.Count;
        int totalSpawnPoints = gridManager.SpawnPoints.Length;

        // --- LÓGICA DE POSICIONES ALEATORIAS ÚNICAS ---
        // 1. Creamos una lista con todos los índices disponibles (0, 1, 2, 3...)
        List<int> randomSpawnIndices = new List<int>();
        for (int j = 0; j < totalSpawnPoints; j++)
        {
            randomSpawnIndices.Add(j);
        }

        // 2. Barajamos la lista (Fisher-Yates Shuffle simple) para desordenarla
        // Esto asegura que al tomar índices secuencialmente de esta lista, sean aleatorios y no se repitan.
        for (int j = 0; j < randomSpawnIndices.Count; j++)
        {
            int temp = randomSpawnIndices[j];
            int randomIndex = Random.Range(j, randomSpawnIndices.Count);
            randomSpawnIndices[j] = randomSpawnIndices[randomIndex];
            randomSpawnIndices[randomIndex] = temp;
        }

        for (int i = 0; i < totalPlayers; i++)
        {
            // Ahora 'i' coincide perfectamente con el orden de los jugadores
            PlayerAssignment assignment = matchData.assignedPlayers[i];
            
            // SEGURIDAD: Si por alguna razón el slot está vacío (jugador no se unió), lo saltamos
            if (assignment == null) continue;

            // 3. ELEGIR POSICIÓN
            // Usamos la lista barajada. El módulo (%) evita errores si hay más jugadores que puntos de spawn
            // (aunque idealmente deberías tener suficientes puntos para todos).
            int uniqueRandomIndex = randomSpawnIndices[i % randomSpawnIndices.Count];
            Transform spawnPoint = gridManager.GetSpawnPoint(uniqueRandomIndex);

            // 1. Instanciar
            GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            KartProgressTracker tracker = playerInstance.GetComponent<KartProgressTracker>();
            if (tracker != null)
            {
                tracker.PlayerID = assignment.playerIndex;
            }
            
            // 2. Configurar Input (Recordando la corrección de IDs de la respuesta anterior)
            PlayerInput pInput = playerInstance.GetComponent<PlayerInput>();
            if (pInput != null)
            {
                List<InputDevice> devicesToUse = new List<InputDevice>();
                
                // Asegúrate de haber implementado el cambio de 'deviceIds' en MatchDataSO
                // que te mencioné en la respuesta anterior.
                foreach (int id in assignment.deviceIds) 
                {
                    InputDevice dev = InputSystem.GetDeviceById(id);
                    if (dev != null) devicesToUse.Add(dev);
                }

                pInput.SwitchCurrentControlScheme(assignment.controlScheme, devicesToUse.ToArray());
                pInput.DeactivateInput();
                _spawnedPlayers.Add(pInput);
            }

            // 3. Configurar HUD
            PlayerHUD hud = playerInstance.GetComponent<PlayerHUD>();
            if (hud != null)
            {
                _playerHUDs.Add(hud);
                
                Color assignedColor = Color.white;
                // Usamos assignment.playerIndex para buscar el color correcto
                if (matchData.playerColors != null && matchData.playerColors.Length > assignment.playerIndex)
                {
                    assignedColor = matchData.playerColors[assignment.playerIndex];
                }

                // Aquí es donde veías el texto mal. Ahora assignment.playerIndex será correcto (0 para P1, 1 para P2)
                hud.SetPlayerLabel(assignment.playerIndex + 1, assignedColor);
            }

            // 4. Configurar Cámara (Split Screen)
            CameraSystem camSystem = playerInstance.GetComponent<CameraSystem>();
            if (camSystem != null)
            {
                // Usamos assignment.playerIndex para asegurar la posición correcta en pantalla
                SetupCameraChannels(camSystem, assignment.playerIndex);
                ConfigureCameraViewport(camSystem.CM, assignment.playerIndex, totalPlayers);
                camSystem.Canvas.worldCamera = camSystem.CM;
                camSystem.Canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

                // Buscamos el AudioListener en la c�mara referenciada por CameraSystem
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

        // Declaramos las variables UNA SOLA VEZ aqu� arriba para evitar el error CS0136.
        // X, Y = Posici�n inicial (0 a 1)
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
        // --- CASO 2: 3 O M�S JUGADORES (GRID AUTOM�TICO) ---
        else
        {
            // Calculamos columnas y filas
            int cols = Mathf.CeilToInt(Mathf.Sqrt(totalPlayers));
            int rows = Mathf.CeilToInt((float)totalPlayers / cols);

            // Ajuste est�tico para 5 y 6 jugadores (3 columnas x 2 filas se ve mejor en monitores anchos)
            if (totalPlayers >= 5 && totalPlayers <= 6)
            {
                cols = 3;
                rows = 2;
            }

            rectW = 1f / cols;
            rectH = 1f / rows;

            // Calculamos posici�n en la grilla
            int colIndex = playerIndex % cols;
            int rowIndex = playerIndex / cols;

            // Invertimos la fila porque Unity UI (0,0) es abajo-izquierda, 
            // pero queremos que el Jugador 1 empiece arriba-izquierda.
            int invertedRowIndex = (rows - 1) - rowIndex;

            rectX = colIndex * rectW;
            rectY = invertedRowIndex * rectH;
        }

        // Aplicamos el rect�ngulo final a la c�mara
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