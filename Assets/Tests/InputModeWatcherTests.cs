using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

// Covers the device-mode detection that drives the adaptive UI (touch controls,
// controls hint). InputTestFixture snapshots and restores the whole input system
// per test, so the simulated keyboards/gamepads/touchscreens never leak out.
public class InputModeWatcherTests : InputTestFixture
{
    private GameObject watcherObject;
    private InputModeWatcher watcher;

    [SetUp]
    public void SetUpWatcher()
    {
        ResetWatcherStatics();
    }

    [TearDown]
    public void TearDownWatcher()
    {
        if (watcherObject != null)
            Object.DestroyImmediate(watcherObject);
        ResetWatcherStatics();
    }

    // Mode, the ModeChanged subscriber list, and the ignored-device set are all
    // static, so they leak between tests unless reset explicitly.
    private static void ResetWatcherStatics()
    {
        TestReflectionHelpers.SetStaticProperty(typeof(InputModeWatcher), "Mode", InputMode.Keyboard);
        TestReflectionHelpers.SetPrivateStaticField(typeof(InputModeWatcher), "ModeChanged", null);
        TestReflectionHelpers.GetPrivateStaticField<HashSet<InputDevice>>(
            typeof(InputModeWatcher), "ignoredDevices").Clear();
        TestReflectionHelpers.SetPrivateStaticField(typeof(MobileControlsUI), "touchControlsPref", int.MinValue);
    }

    // Awake and Update don't run automatically for ordinary scripts in Edit Mode,
    // so the lifecycle is driven by hand: Awake once on creation, Update after
    // each simulated input (before another input update clears the
    // wasPressedThisFrame flags the watcher polls).
    private void CreateWatcher()
    {
        watcherObject = new GameObject("InputModeWatcher");
        watcher = watcherObject.AddComponent<InputModeWatcher>();
        TestReflectionHelpers.InvokePrivate(watcher, "Awake");
    }

    private void PumpWatcher()
    {
        TestReflectionHelpers.InvokePrivate(watcher, "Update");
    }

    [Test]
    public void Mode_DefaultsToKeyboard_WithNoDevices()
    {
        CreateWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Keyboard));
    }

    [Test]
    public void Awake_StartsInGamepadMode_WhenPadAlreadyConnected()
    {
        InputSystem.AddDevice<Gamepad>();

        CreateWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Gamepad),
            "A machine with a pad attached (e.g. Steam Deck) should start in Gamepad mode.");
    }

    [Test]
    public void GamepadButton_SwitchesToGamepadMode_AndRaisesModeChanged()
    {
        CreateWatcher();
        var pad = InputSystem.AddDevice<Gamepad>();
        bool raised = false;
        InputModeWatcher.ModeChanged += () => raised = true;

        Press(pad.buttonSouth);
        PumpWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Gamepad));
        Assert.That(raised, Is.True, "ModeChanged should fire when the mode flips to Gamepad.");
    }

    [Test]
    public void GamepadStick_SwitchesToGamepadMode()
    {
        CreateWatcher();
        var pad = InputSystem.AddDevice<Gamepad>();

        Set(pad.leftStick, Vector2.up);
        PumpWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Gamepad));
    }

    [Test]
    public void TouchPress_SwitchesToTouchMode()
    {
        CreateWatcher();
        InputSystem.AddDevice<Touchscreen>();

        BeginTouch(1, new Vector2(100f, 100f));
        PumpWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Touch));
    }

    [Test]
    public void KeyboardKey_SwitchesBackToKeyboardMode()
    {
        CreateWatcher();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var pad = InputSystem.AddDevice<Gamepad>();

        Press(pad.buttonSouth);
        PumpWatcher();
        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Gamepad));

        Release(pad.buttonSouth);
        Press(keyboard.spaceKey);
        PumpWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Keyboard));
    }

    [Test]
    public void TouchBeatsGamepad_WhenBothActuatedSameFrame()
    {
        CreateWatcher();
        var pad = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Touchscreen>();

        // Queue the stick deflection without updating; BeginTouch then processes
        // both events in the same input update.
        Set(pad.leftStick, Vector2.right, queueEventOnly: true);
        BeginTouch(1, new Vector2(50f, 50f));
        PumpWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Touch),
            "Touch should win when touch and gamepad are actuated in the same frame.");
    }

    [Test]
    public void IgnoredGamepad_DoesNotSwitchMode()
    {
        CreateWatcher();
        var virtualPad = InputSystem.AddDevice<Gamepad>();
        InputModeWatcher.IgnoreDevice(virtualPad);

        Set(virtualPad.leftStick, Vector2.right);
        Press(virtualPad.buttonSouth);
        PumpWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Keyboard),
            "The on-screen stick's virtual gamepad must not flip the mode to Gamepad.");
    }

    [Test]
    public void SecondNonIgnoredGamepad_StillSwitchesMode()
    {
        CreateWatcher();
        var virtualPad = InputSystem.AddDevice<Gamepad>();
        InputModeWatcher.IgnoreDevice(virtualPad);
        var realPad = InputSystem.AddDevice<Gamepad>();

        Press(realPad.buttonSouth);
        PumpWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Gamepad),
            "A real pad should still switch the mode while a virtual pad is ignored.");
    }

    [Test]
    public void TouchControlsActive_BlocksVirtualGamepadFromSwitchingMode()
    {
        CreateWatcher();
        TestReflectionHelpers.SetStaticProperty(typeof(InputModeWatcher), "Mode", InputMode.Touch);
        TestReflectionHelpers.SetPrivateStaticField(typeof(MobileControlsUI), "touchControlsPref", 1);

        var virtualPad = InputSystem.AddDevice<Gamepad>();
        Set(virtualPad.leftStick, Vector2.right);
        PumpWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Touch),
            "Touch mode with active on-screen controls must not flip to Gamepad from a virtual stick.");
    }

    [Test]
    public void ModeChanged_NotRaised_WhenModeUnchanged()
    {
        CreateWatcher();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        bool raised = false;
        InputModeWatcher.ModeChanged += () => raised = true;

        Press(keyboard.aKey);
        PumpWatcher();

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Keyboard));
        Assert.That(raised, Is.False, "Keyboard input while already in Keyboard mode should not re-raise ModeChanged.");
    }
}
