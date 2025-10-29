using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Events;
using System.Collections;

public class StartScreen_Manager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LocalizeStringEvent localizeStringEvent;

    [Header("Device Detection")]
    [SerializeField] private LocalizedString keyboardMouseString;
    [SerializeField] private LocalizedString gamepadString;
    [SerializeField] private LocalizedString mobileString;

    [Header("Events")]
    [SerializeField] private UnityEvent onStartGame;

    private InputDevice lastDevice;
    private bool isMobilePlatform;
    private bool inputReceived = false;

    private void Awake()
    {
        isMobilePlatform = Application.isMobilePlatform;
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        InputSystem.onActionChange += OnInputActionChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        InputSystem.onActionChange -= OnInputActionChange;
    }

    private void Start()
    {
        UpdatePrompt();
    }

    public void OnStartInput(InputAction.CallbackContext context)
    {
        if (inputReceived) return;

        if (context.performed)
        {
            StartGame();
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (inputReceived) return;

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
            case InputDeviceChange.ConfigurationChanged:
                StartCoroutine(UpdatePromptAfterDeviceChange());
                break;
        }
    }

    private IEnumerator UpdatePromptAfterDeviceChange()
    {
        yield return null;
        UpdatePrompt();
    }

    private void OnInputActionChange(object obj, InputActionChange change)
    {
        if (inputReceived) return;

        if (change == InputActionChange.ActionPerformed)
        {
            var inputAction = (InputAction)obj;
            var device = inputAction.activeControl?.device;

            if (device != null && device != lastDevice)
            {
                lastDevice = device;
                UpdatePrompt();
            }
        }
    }

    private void UpdatePrompt()
    {
        if (inputReceived) return;

        if (isMobilePlatform)
        {
            localizeStringEvent.StringReference = mobileString;
        }
        else if (IsGamepadConnected())
        {
            localizeStringEvent.StringReference = gamepadString;
        }
        else
        {
            localizeStringEvent.StringReference = keyboardMouseString;
        }

        StartCoroutine(RefreshText());
    }

    private bool IsGamepadConnected()
    {
        return Gamepad.current != null;
    }

    private IEnumerator RefreshText()
    {
        yield return null;
        localizeStringEvent.RefreshString();
    }

    private void StartGame()
    {
        if (inputReceived) return;

        inputReceived = true;
        onStartGame?.Invoke();
    }
}