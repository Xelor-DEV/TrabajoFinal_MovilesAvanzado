using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

using UnityEngine.InputSystem.Utilities;


#if UNITY_EDITOR
using UnityEditor;
#endif

[InputControlLayout(displayName = "Mobile Virtual Gamepad", stateType = typeof(MobileVirtualGamepadState))]
public class MobileVirtualGamepad : InputDevice
{
    [InputControl(name = "stick", layout = "Stick", usage = "Primary2DMotion")]
    public StickControl stick { get; private set; }

    [InputControl(name = "buttonSouth", layout = "Button", usage = "PrimaryAction")]
    public ButtonControl buttonSouth { get; private set; }

    [InputControl(name = "buttonWest", layout = "Button")]
    public ButtonControl buttonWest { get; private set; }

    [InputControl(name = "buttonEast", layout = "Button")]
    public ButtonControl buttonEast { get; private set; }

    // Estado optimizado como los gamepads reales
    private struct MobileVirtualGamepadState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('M', 'V', 'G', 'P');

        // Stick - offset 0 (8 bytes)
        [InputControl(layout = "Stick", format = "VEC2",
            usage = "Primary2DMotion", offset = 0)]
        public Vector2 stick;

        // Campo de botones - offset 8 (4 bytes)
        [InputControl(name = "buttonSouth", layout = "Button", format = "BIT",
            offset = 8, bit = 0)]
        [InputControl(name = "buttonWest", layout = "Button", format = "BIT",
            offset = 8, bit = 1)]
        [InputControl(name = "buttonEast", layout = "Button", format = "BIT",
            offset = 8, bit = 2)]
        public uint buttons;
    }

    protected override void FinishSetup()
    {
        base.FinishSetup();
        stick = GetChildControl<StickControl>("stick");
        buttonSouth = GetChildControl<ButtonControl>("buttonSouth");
        buttonWest = GetChildControl<ButtonControl>("buttonWest");
        buttonEast = GetChildControl<ButtonControl>("buttonEast");
    }

    // Registro para tiempo de ejecución
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeRuntime()
    {
        InputSystem.RegisterLayout<MobileVirtualGamepad>();
    }

    // Registro para editor
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void InitializeEditor()
    {
        InputSystem.RegisterLayout<MobileVirtualGamepad>();

        // Fuerza la actualización de los layouts en el editor
        InputSystem.onSettingsChange += () => {
            // Check if the layout is already registered
            if (InputSystem.LoadLayout("MobileVirtualGamepad") == null)
            {
                // Register the layout if it doesn't exist
                InputSystem.RegisterLayout<MobileVirtualGamepad>();
                Debug.Log("Mobile Virtual Gamepad layout registered.");
            }
            else
            {
                Debug.Log("Mobile Virtual Gamepad layout is already registered.");
            }
        };
    }
#endif
}