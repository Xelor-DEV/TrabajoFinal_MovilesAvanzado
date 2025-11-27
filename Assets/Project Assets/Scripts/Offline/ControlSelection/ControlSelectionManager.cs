using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    private int _playersReadyCount = 0;

    public int TotalSlots => _spawnedSlots.Count;

    private void Start()
    {
        matchData.ClearData();
        SpawnPlayerSlots();
        StartCoroutine(DetectAndSpawnCursors());
    }

    private void SpawnPlayerSlots()
    {
        for (int i = 0; i < matchData.maxPlayers; i++)
        {
            PlayerSlotUI slot = Instantiate(playerSlotPrefab, playerContainer);
            Color c = (i < matchData.playerColors.Length) ? matchData.playerColors[i] : Color.white;
            slot.Initialize(i, c);
            _spawnedSlots.Add(slot);
        }
    }

    private IEnumerator DetectAndSpawnCursors()
    {
        yield return new WaitForEndOfFrame();

        var devices = InputSystem.devices;
        int cursorIndex = 0;

        foreach (var device in devices)
        {
            string schemeToUse = "";

            if (device is Gamepad)
            {
                schemeToUse = "Gamepad";
            }
            else if (device is Keyboard || device is Mouse)
            {
                if (device is Mouse) continue;
                schemeToUse = "KeyboardAndMouse";
            }

            if (!string.IsNullOrEmpty(schemeToUse))
            {
                SpawnCursorForDevice(device, schemeToUse, cursorIndex);
                cursorIndex++;
            }
        }
    }

    private void SpawnCursorForDevice(InputDevice device, string scheme, int index)
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
            Color assignedColor = Color.white;
            if (matchData.playerColors != null && matchData.playerColors.Length > 0)
            {
                assignedColor = matchData.playerColors[index % matchData.playerColors.Length];
            }

            cursorLogic.Initialize(this, assignedColor);
        }
    }

    public bool TrySelectPlayer(int slotIndex, PlayerInput input)
    {
        if (_playersReadyCount >= matchData.maxPlayers) return false;

        PlayerSlotUI slot = _spawnedSlots[slotIndex];

        if (slot.IsTaken) return false;

        slot.MarkAsTaken();
        matchData.SaveAssignment(slot.PlayerIndex, input.devices[0], input.currentControlScheme);

        _playersReadyCount++;
        CheckIfAllReady();

        return true;
    }

    private void CheckIfAllReady()
    {
        if (_playersReadyCount == matchData.maxPlayers)
        {
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