using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Necesario para LayoutRebuilder
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class ControlSelectionManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private MatchDataSO matchData;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Scene References")]
    [SerializeField] private Transform playerContainer;
    [SerializeField] private Transform canvasRoot;

    [Header("Prefabs")]
    [SerializeField] private PlayerSlotUI playerSlotPrefab;
    [SerializeField] private GameObject cursorPrefab;

    [Header("Events")]
    public UnityEvent OnSelectionComplete;

    private List<PlayerSlotUI> _spawnedSlots = new List<PlayerSlotUI>();

    // Diccionario para rastrear qu� cursor pertenece a qu� dispositivo
    private Dictionary<InputDevice, DeviceCursor> _activeCursors = new Dictionary<InputDevice, DeviceCursor>();

    private int _playersReadyCount = 0;

    public int TotalSlots => _spawnedSlots.Count;

private void Start()
    {
        // CAMBIO: Usamos el nuevo método que crea los huecos vacíos
        matchData.InitializeList(); 
        
        SpawnPlayerSlots();
        CheckInitialDevices();
    }
    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void SpawnPlayerSlots()
    {
        // Limpiar slots previos si los hubiera
        foreach (Transform child in playerContainer) Destroy(child.gameObject);
        _spawnedSlots.Clear();

        for (int i = 0; i < matchData.maxPlayers; i++)
        {
            PlayerSlotUI slot = Instantiate(playerSlotPrefab, playerContainer);
            Color c = (i < matchData.playerColors.Length) ? matchData.playerColors[i] : Color.white;
            slot.Initialize(i, c);
            _spawnedSlots.Add(slot);
        }

        // SOLUCI�N AL PROBLEMA DE POSICI�N X=0:
        // Forzamos al sistema de UI a calcular las posiciones de los slots inmediatamente
        // para que cuando aparezcan los cursores, los slots ya tengan sus coordenadas X correctas.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(playerContainer.GetComponent<RectTransform>());
    }

    private void CheckInitialDevices()
    {
        foreach (InputDevice device in InputSystem.devices)
        {
            HandleDeviceConnection(device);
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
                HandleDeviceConnection(device);
                break;

            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
                HandleDeviceDisconnection(device);
                break;
        }
    }

    private void HandleDeviceConnection(InputDevice device)
    {
        if (_activeCursors.ContainsKey(device)) return;

        string schemeToUse = "";

        if (device is Gamepad)
        {
            schemeToUse = "Gamepad";
        }
        else if (device is Keyboard || device is Mouse)
        {
            if (device is Mouse) return;
            schemeToUse = "KeyboardAndMouse";
        }

        if (!string.IsNullOrEmpty(schemeToUse))
        {
            SpawnCursorForDevice(device, schemeToUse);
        }
    }

    private void HandleDeviceDisconnection(InputDevice device)
    {
        if (_activeCursors.TryGetValue(device, out DeviceCursor cursor))
        {
            cursor.PopOutAndDestroy();
            _activeCursors.Remove(device);
        }
    }

    private void SpawnCursorForDevice(InputDevice device, string scheme)
    {
        var playerInput = PlayerInput.Instantiate(
            cursorPrefab,
            controlScheme: scheme,
            pairWithDevice: device
        );

        playerInput.transform.SetParent(canvasRoot, false);

        var cursorLogic = playerInput.GetComponent<DeviceCursor>();
        if (cursorLogic != null)
        {
            // L�GICA DE ASIGNACI�N DE SLOT:
            // Obtenemos el �ndice basado en cu�ntos cursores hay ya conectados.
            int cursorIndex = _activeCursors.Count;

            // Si hay m�s dispositivos que slots (ej: 3er mando, 2 slots),
            // usamos el operador % para volver al principio.
            // Mando 1 (index 0) -> Slot 0
            // Mando 2 (index 1) -> Slot 1
            // Mando 3 (index 2) -> Slot 0 (Se queda ah� mirando)
            int targetSlotIndex = cursorIndex % matchData.maxPlayers;

            // Asignar color
            Color assignedColor = Color.white;
            if (matchData.playerColors != null && matchData.playerColors.Length > 0)
            {
                // Usamos el cursorIndex para ciclar colores tambi�n
                assignedColor = matchData.playerColors[cursorIndex % matchData.playerColors.Length];
            }

            // Inicializar pasando el slot objetivo
            cursorLogic.Initialize(this, assignedColor, device, targetSlotIndex);

            _activeCursors.Add(device, cursorLogic);
        }
    }

    public bool TrySelectPlayer(int slotIndex, PlayerInput input)
    {
        if (_playersReadyCount >= matchData.maxPlayers) return false;

        PlayerSlotUI slot = _spawnedSlots[slotIndex];

        if (slot.IsTaken) return false;

        slot.MarkAsTaken();

        // CAMBIO: Pasamos 'input.devices' completo para capturar Teclado Y Mouse si aplica
        matchData.SaveAssignment(slot.PlayerIndex, input.devices, input.currentControlScheme);

        _playersReadyCount++;
        CheckIfAllReady();

        return true;
    }
    
    private void CheckIfAllReady()
    {
        if (_playersReadyCount == matchData.maxPlayers)
        {
            foreach (DeviceCursor cursor in _activeCursors.Values)
            {
                cursor.Hide();
            }

            Debug.Log("Selection Complete! Loading next scene...");
            OnSelectionComplete?.Invoke();
        }
    }

    public RectTransform GetSlotTransform(int index)
    {
        if (index >= 0 && index < _spawnedSlots.Count)
        {
            return _spawnedSlots[index].GetRectTransform();
        }
        return null;
    }
}