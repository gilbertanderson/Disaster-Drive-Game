using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static PlayModeReflectionHelpers;

/// <summary>
/// Play Mode end-to-end tests for gamepad and mobile (touch) input, driving the
/// same scene as RubricE2ETests with simulated devices — no hardware needed.
/// InputTestFixture restores the input system after each test, so these can run
/// in the same session as the keyboard rubric tests.
/// </summary>
public class MobileAndGamepadE2ETests : InputTestFixture
{
    const string MainScene = "My Game";
    const string PlayerCountKey = "PlayerCount";
    const float WaitTimeout = 2f;
    // StartGame runs a 3-2-1-GO countdown (4 x 0.8s beats) plus a camera intro
    // before IsGameActive flips, so "the run started" needs a generous ceiling.
    const float RunStartTimeout = 15f;

    GameManager gameManager;
    PlayerController player;
    Gamepad gamepad;
    bool hadPlayerCountKey;
    int savedPlayerCount;

    [UnitySetUp]
    public IEnumerator LoadMainScene()
    {
        // Single-player is the mode whose control scheme includes the gamepad
        // bindings; force it for the test, but put the player's own 1P/2P
        // preference back afterwards (see RestoreState).
        hadPlayerCountKey = PlayerPrefs.HasKey(PlayerCountKey);
        savedPlayerCount = PlayerPrefs.GetInt(PlayerCountKey, 1);
        PlayerPrefs.SetInt(PlayerCountKey, 1);

        yield return SceneManager.LoadSceneAsync(MainScene, LoadSceneMode.Single);
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == MainScene);

        gameManager = Object.FindAnyObjectByType<GameManager>();
        player = Object.FindAnyObjectByType<PlayerController>();

        yield return new WaitForSeconds(0.5f);
    }

    // Runs after InputTestFixture's Setup (which resets the input system), in
    // either order relative to LoadMainScene — device adds and the static reset
    // are safe on both sides of the scene load.
    [SetUp]
    public void SetUpDevicesAndState()
    {
        gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Touchscreen>();   // Touchscreen.current, used by BeginTouch/EndTouch
        ResetInputModeState();
    }

    [TearDown]
    public void RestoreState()
    {
        Time.timeScale = 1f;
        ResetInputModeState();

        if (hadPlayerCountKey)
            PlayerPrefs.SetInt(PlayerCountKey, savedPlayerCount);
        else
            PlayerPrefs.DeleteKey(PlayerCountKey);
    }

    // InputModeWatcher and MobileControlsUI are DontDestroyOnLoad singletons
    // that live for the whole Play Mode session; their per-run state has to be
    // reset by hand between tests. The ModeChanged subscriber list is left
    // alone — the live GameManager's controls-hint handler is registered there.
    static void ResetInputModeState()
    {
        SetStaticProperty(typeof(InputModeWatcher), "Mode", InputMode.Keyboard);
        GetPrivateStaticField<HashSet<InputDevice>>(typeof(InputModeWatcher), "ignoredDevices").Clear();

        // Put the touch-controls toggle back to "auto" (follow Touch mode) so a
        // test that flipped it can't leak the explicit on/off choice — the pref
        // is persisted in PlayerPrefs and cached in a static field.
        PlayerPrefs.DeleteKey(MobileControlsUI.TouchControlsPrefKey);
        SetPrivateStaticField(typeof(MobileControlsUI), "touchControlsPref", int.MinValue);

        // Hide the on-screen controls while this test's input system still
        // exists, so the stick removes its virtual gamepad cleanly before
        // InputTestFixture restores the previous input state.
        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        if (mobileControls != null)
            InvokePrivate(mobileControls, "SetShown", false);
    }

    IEnumerator StartRunAndWaitUntilActive()
    {
        gameManager.StartGame();
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => gameManager.IsGameActive, RunStartTimeout);
        Assert.That(gameManager.IsGameActive, Is.True,
            "Run should become active once the start countdown and camera intro finish.");
    }

    // --- Gamepad ---

    [UnityTest]
    public IEnumerator Gamepad_LeftStick_MovesVehicle()
    {
        yield return StartRunAndWaitUntilActive();

        Vector3 startPos = player.transform.position;
        yield return InputSimulationHelpers.HoldStick(this, gamepad.leftStick, Vector2.right, 0.8f);

        Assert.That(player.transform.position.x, Is.GreaterThan(startPos.x + 0.1f),
            "Vehicle should move right when the left stick is held right.");
    }

    [UnityTest]
    public IEnumerator Gamepad_Dpad_MovesVehicle()
    {
        yield return StartRunAndWaitUntilActive();

        Vector3 startPos = player.transform.position;
        yield return InputSimulationHelpers.HoldButton(this, gamepad.dpad.left, 0.8f);

        Assert.That(player.transform.position.x, Is.LessThan(startPos.x - 0.1f),
            "Vehicle should move left when d-pad left is held.");
    }

    [UnityTest]
    public IEnumerator Gamepad_StartButton_TogglesPauseAndResume()
    {
        yield return StartRunAndWaitUntilActive();

        Press(gamepad.startButton, queueEventOnly: true);
        yield return null;
        yield return null;
        Assert.That(gameManager.IsPaused, Is.True, "Start button should pause an active run.");
        Assert.That(Time.timeScale, Is.EqualTo(0f).Within(0.001f));

        Release(gamepad.startButton, queueEventOnly: true);
        yield return null;
        yield return null;

        Press(gamepad.startButton, queueEventOnly: true);
        yield return null;
        yield return null;
        Assert.That(gameManager.IsPaused, Is.False, "Start button should resume a paused run.");
        Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator GamepadUse_SwitchesModeAndControlsHint()
    {
        // A stick wiggle flips the mode without risking a UI submit the way a
        // face-button press could.
        Set(gamepad.leftStick, Vector2.up, queueEventOnly: true);
        yield return null;
        yield return null;

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Gamepad),
            "Gamepad use should switch the input mode to Gamepad.");

        var hint = GetPrivateField<TMP_Text>(gameManager, "controlsHintText");
        Assert.That(hint, Is.Not.Null, "controlsHintText should be wired in the scene.");
        // Assert on the device word, not exact copy, so hint wording can change.
        Assert.That(hint.text, Does.Contain("Start"),
            "Controls hint should show gamepad instructions after gamepad use.");

        Set(gamepad.leftStick, Vector2.zero, queueEventOnly: true);
        yield return null;
    }

    // --- Mobile / touch ---

    [UnityTest]
    public IEnumerator TouchTap_SwitchesToTouchMode_AndShowsMobileControls()
    {
        yield return StartRunAndWaitUntilActive();

        yield return InputSimulationHelpers.Tap(this, ScreenCenter());

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Touch),
            "A touch should switch the input mode to Touch.");

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        Assert.That(mobileControls, Is.Not.Null, "MobileControlsUI should self-bootstrap at runtime.");

        var stickRoot = GetPrivateField<GameObject>(mobileControls, "stickRoot");
        var pauseRoot = GetPrivateField<GameObject>(mobileControls, "pauseRoot");
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => stickRoot.activeInHierarchy, WaitTimeout);

        Assert.That(stickRoot.activeInHierarchy, Is.True,
            "The on-screen stick should appear in Touch mode during an active run.");
        Assert.That(pauseRoot.activeInHierarchy, Is.True,
            "The on-screen pause button should appear in Touch mode during an active run.");
    }

    [UnityTest]
    public IEnumerator MobileControls_Hidden_InKeyboardMode()
    {
        yield return StartRunAndWaitUntilActive();

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        Press(keyboard.spaceKey, queueEventOnly: true);
        yield return null;
        yield return null;

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Keyboard));

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        Assert.That(mobileControls, Is.Not.Null);
        Assert.That(GetPrivateField<GameObject>(mobileControls, "stickRoot").activeInHierarchy, Is.False,
            "The on-screen stick must stay hidden while playing with the keyboard.");
        Assert.That(GetPrivateField<GameObject>(mobileControls, "pauseRoot").activeInHierarchy, Is.True,
            "The top-left pause button persists during a run regardless of input device.");
    }

    [UnityTest]
    public IEnumerator VirtualStick_DrivesVehicle_AndModeStaysTouch()
    {
        yield return StartRunAndWaitUntilActive();

        yield return InputSimulationHelpers.Tap(this, ScreenCenter());

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        var stickRoot = GetPrivateField<GameObject>(mobileControls, "stickRoot");
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => stickRoot.activeInHierarchy, WaitTimeout);
        Assert.That(stickRoot.activeInHierarchy, Is.True);

        // The stick's virtual gamepad device is created when the stick becomes
        // visible; wait until MobileControlsUI has registered it as ignored so
        // the drag below can't be misread as real gamepad input.
        yield return InputSimulationHelpers.WaitUntilOrTimeout(
            () => GetPrivateField<bool>(mobileControls, "stickDeviceIgnored"), WaitTimeout);
        Assert.That(GetPrivateField<bool>(mobileControls, "stickDeviceIgnored"), Is.True,
            "MobileControlsUI should register the stick's virtual device with InputModeWatcher.");

        var handle = stickRoot.transform.Find("StickHandle").gameObject;
        Vector3 startPos = player.transform.position;
        yield return InputSimulationHelpers.DragOnScreenStick(handle, new Vector2(400f, 0f), 0.8f);

        Assert.That(player.transform.position.x, Is.GreaterThan(startPos.x + 0.1f),
            "Vehicle should move right when the on-screen stick is dragged right.");
        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Touch),
            "Driving with the on-screen stick must not flip the input mode away from Touch.");
    }

    [UnityTest]
    public IEnumerator MobilePauseButton_TogglesPause_AndStaysReachableWhilePaused()
    {
        yield return StartRunAndWaitUntilActive();

        yield return InputSimulationHelpers.Tap(this, ScreenCenter());

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        var pauseRoot = GetPrivateField<GameObject>(mobileControls, "pauseRoot");
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => pauseRoot.activeInHierarchy, WaitTimeout);
        Assert.That(pauseRoot.activeInHierarchy, Is.True);

        var pauseButton = pauseRoot.GetComponent<Button>();
        pauseButton.onClick.Invoke();
        yield return null;

        Assert.That(gameManager.IsPaused, Is.True, "The on-screen pause button should pause the run.");
        Assert.That(Time.timeScale, Is.EqualTo(0f).Within(0.001f));
        Assert.That(pauseRoot.activeInHierarchy, Is.True,
            "The pause button must stay visible while paused so the same button can resume.");

        pauseButton.onClick.Invoke();
        yield return null;

        Assert.That(gameManager.IsPaused, Is.False, "The same button should resume the run.");
        Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
    }

    // --- Touch-controls toggle ---

    [UnityTest]
    public IEnumerator ToggleButton_HidesMobileControls_InTouchMode_AndPersistsChoice()
    {
        yield return StartRunAndWaitUntilActive();

        yield return InputSimulationHelpers.Tap(this, ScreenCenter());

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        var stickRoot = GetPrivateField<GameObject>(mobileControls, "stickRoot");
        var pauseRoot = GetPrivateField<GameObject>(mobileControls, "pauseRoot");
        var toggleRoot = GetPrivateField<GameObject>(mobileControls, "toggleRoot");
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => stickRoot.activeInHierarchy, WaitTimeout);
        Assert.That(stickRoot.activeInHierarchy, Is.True);
        Assert.That(toggleRoot.activeInHierarchy, Is.True,
            "The touch-controls toggle should be visible during a run in Touch mode.");

        toggleRoot.GetComponent<Button>().onClick.Invoke();
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => !stickRoot.activeInHierarchy, WaitTimeout);

        Assert.That(stickRoot.activeInHierarchy, Is.False,
            "Toggling touch controls off should hide the on-screen stick even in Touch mode.");
        Assert.That(pauseRoot.activeInHierarchy, Is.True,
            "The top-left pause button persists even with touch controls toggled off.");
        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Touch),
            "The toggle changes controls visibility, not the detected input mode.");
        Assert.That(PlayerPrefs.GetInt(MobileControlsUI.TouchControlsPrefKey, -1), Is.EqualTo(0),
            "The explicit off choice should be persisted in PlayerPrefs.");
        Assert.That(toggleRoot.activeInHierarchy, Is.True,
            "The toggle itself must stay visible so the controls can be turned back on.");

        toggleRoot.GetComponent<Button>().onClick.Invoke();
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => stickRoot.activeInHierarchy, WaitTimeout);

        Assert.That(stickRoot.activeInHierarchy, Is.True,
            "Toggling touch controls back on should re-show the on-screen stick.");
        Assert.That(PlayerPrefs.GetInt(MobileControlsUI.TouchControlsPrefKey, -1), Is.EqualTo(1),
            "The explicit on choice should be persisted in PlayerPrefs.");
    }

    [UnityTest]
    public IEnumerator ToggleButton_ForcesMobileControlsOn_InKeyboardMode()
    {
        yield return StartRunAndWaitUntilActive();

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        Press(keyboard.spaceKey, queueEventOnly: true);
        yield return null;
        yield return null;
        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Keyboard));

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        var stickRoot = GetPrivateField<GameObject>(mobileControls, "stickRoot");
        var toggleRoot = GetPrivateField<GameObject>(mobileControls, "toggleRoot");
        // A touchscreen device is present (added in SetUp), so the toggle is
        // offered even while playing with the keyboard.
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => toggleRoot.activeInHierarchy, WaitTimeout);
        Assert.That(toggleRoot.activeInHierarchy, Is.True);
        Assert.That(stickRoot.activeInHierarchy, Is.False);

        toggleRoot.GetComponent<Button>().onClick.Invoke();
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => stickRoot.activeInHierarchy, WaitTimeout);

        Assert.That(stickRoot.activeInHierarchy, Is.True,
            "Forcing touch controls on should show the stick regardless of input mode.");
        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Keyboard),
            "Forcing touch controls on must not flip the detected input mode.");
    }

    [UnityTest]
    public IEnumerator InGameControlsHint_ShowsAboveToggle_AndTracksInputMode()
    {
        yield return StartRunAndWaitUntilActive();

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        var hintRoot = GetPrivateField<GameObject>(mobileControls, "hintRoot");
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => hintRoot.activeInHierarchy, WaitTimeout);
        Assert.That(hintRoot.activeInHierarchy, Is.True,
            "The in-game controls hint should be visible during a run.");

        var hintLabel = GetPrivateField<TMP_Text>(mobileControls, "hintLabel");

        Set(gamepad.leftStick, Vector2.up, queueEventOnly: true);
        yield return null;
        yield return null;
        Set(gamepad.leftStick, Vector2.zero, queueEventOnly: true);
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => hintLabel.text.Contains("Start"), WaitTimeout);
        Assert.That(hintLabel.text, Does.Contain("Start"),
            "The in-game hint should show gamepad instructions after gamepad use.");

        yield return InputSimulationHelpers.Tap(this, ScreenCenter());
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => hintLabel.text.Contains("Drag stick"), WaitTimeout);
        Assert.That(hintLabel.text, Does.Contain("Drag stick"),
            "The in-game hint should show touch instructions once touch controls are active.");
    }

    [UnityTest]
    public IEnumerator ToggleButton_ShowsOnStartScreen_AndUpdatesStartHint()
    {
        // No StartGame: the scene loads onto the start screen, where the toggle
        // sits under the start panel's own controls hint (top left).
        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        Assert.That(mobileControls, Is.Not.Null, "MobileControlsUI should self-bootstrap at runtime.");
        var toggleRoot = GetPrivateField<GameObject>(mobileControls, "toggleRoot");
        var hintRoot = GetPrivateField<GameObject>(mobileControls, "hintRoot");
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => toggleRoot.activeInHierarchy, WaitTimeout);

        Assert.That(toggleRoot.activeInHierarchy, Is.True,
            "The toggle should be offered on the start screen when a touchscreen is present.");
        Assert.That(hintRoot.activeInHierarchy, Is.False,
            "The overlay hint stays hidden on the start screen — the start panel has its own controls hint.");

        toggleRoot.GetComponent<Button>().onClick.Invoke();
        yield return null;

        Assert.That(PlayerPrefs.GetInt(MobileControlsUI.TouchControlsPrefKey, -1), Is.EqualTo(1),
            "Toggling on from the start screen should persist the on choice.");
        var hint = GetPrivateField<TMP_Text>(gameManager, "controlsHintText");
        Assert.That(hint.text, Does.Contain("Drag stick"),
            "The start screen hint should switch to touch instructions when controls are forced on.");
    }

    // --- Helpers ---

    static Vector2 ScreenCenter()
    {
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }
}
