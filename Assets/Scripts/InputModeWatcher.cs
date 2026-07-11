using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMode
{
    Keyboard,
    Gamepad,
    Touch
}

// Tracks which kind of device the player last used (keyboard, gamepad, or touch)
// so the UI can adapt without any manual toggle. Self-bootstraps at runtime;
// nothing in the scene references it directly.
public class InputModeWatcher : MonoBehaviour
{
    public static InputMode Mode { get; private set; } = InputMode.Keyboard;
    public static event Action ModeChanged;

    // Virtual devices created by on-screen controls report as gamepads; they must
    // not flip the mode to Gamepad while the player is really using touch.
    private static readonly HashSet<InputDevice> ignoredDevices = new HashSet<InputDevice>();

    public static void IgnoreDevice(InputDevice device)
    {
        if (device == null)
            return;
        // Virtual devices are recreated each time the on-screen controls are
        // shown; drop entries whose device no longer exists so the set doesn't
        // grow for the lifetime of the process.
        ignoredDevices.RemoveWhere(d => !d.added);
        ignoredDevices.Add(device);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("InputModeWatcher");
        DontDestroyOnLoad(go);
        go.AddComponent<InputModeWatcher>();
    }

    private void Awake()
    {
        // Best initial guess before the player touches anything: phones/tablets
        // start in Touch, a machine with a pad attached (e.g. Steam Deck) starts
        // in Gamepad, everything else starts in Keyboard.
        if (Application.isMobilePlatform)
            SetMode(InputMode.Touch);
        else if (Gamepad.current != null)
            SetMode(InputMode.Gamepad);
        else
            SetMode(InputMode.Keyboard);
    }

    private void Update()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            SetMode(InputMode.Touch);
            return;
        }

        foreach (var pad in Gamepad.all)
        {
            if (ignoredDevices.Contains(pad))
                continue;
            if (GamepadActuated(pad))
            {
                SetMode(InputMode.Gamepad);
                return;
            }
        }

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            SetMode(InputMode.Keyboard);
    }

    private static bool GamepadActuated(Gamepad pad)
    {
        return pad.leftStick.ReadValue().sqrMagnitude > 0.04f
            || pad.dpad.ReadValue().sqrMagnitude > 0.04f
            || pad.buttonSouth.wasPressedThisFrame
            || pad.buttonEast.wasPressedThisFrame
            || pad.startButton.wasPressedThisFrame;
    }

    private static void SetMode(InputMode mode)
    {
        if (Mode == mode)
            return;
        Mode = mode;
        ModeChanged?.Invoke();
    }
}
