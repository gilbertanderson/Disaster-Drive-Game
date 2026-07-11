using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Play Mode end-to-end tests for gamepad and mobile (touch) input, driving the
/// same scene as RubricE2ETests with simulated devices — no hardware needed.
/// InputTestFixture restores the input system after each test, so these can run
/// in the same session as the keyboard rubric tests.
/// </summary>
public class MobileAndGamepadE2ETests : InputTestFixture
{
    const string MainScene = "My Game";
    const float WaitTimeout = 2f;

    GameManager gameManager;
    PlayerController player;
    Gamepad gamepad;

    [UnitySetUp]
    public IEnumerator LoadMainScene()
    {
        // Single-player is the mode whose control scheme includes the gamepad
        // bindings; make sure a stale 2P preference can't leak into these tests.
        PlayerPrefs.SetInt("PlayerCount", 1);

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
    }

    // InputModeWatcher and MobileControlsUI are DontDestroyOnLoad singletons
    // that live for the whole Play Mode session; their per-run state has to be
    // reset by hand between tests. The ModeChanged subscriber list is left
    // alone — the live GameManager's controls-hint handler is registered there.
    static void ResetInputModeState()
    {
        typeof(InputModeWatcher)
            .GetProperty("Mode", BindingFlags.Static | BindingFlags.Public)
            .SetValue(null, InputMode.Keyboard);
        ((HashSet<InputDevice>)typeof(InputModeWatcher)
            .GetField("ignoredDevices", BindingFlags.Static | BindingFlags.NonPublic)
            .GetValue(null)).Clear();

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        if (mobileControls != null)
            SetPrivateField(mobileControls, "stickDeviceIgnored", false);
    }

    // --- Gamepad ---

    [UnityTest]
    public IEnumerator Gamepad_LeftStick_MovesVehicle()
    {
        gameManager.StartGame();
        yield return new WaitForSeconds(0.3f);

        Vector3 startPos = player.transform.position;
        yield return InputSimulationHelpers.HoldStick(this, gamepad.leftStick, Vector2.right, 0.8f);

        Assert.That(player.transform.position.x, Is.GreaterThan(startPos.x + 0.1f),
            "Vehicle should move right when the left stick is held right.");
    }

    [UnityTest]
    public IEnumerator Gamepad_Dpad_MovesVehicle()
    {
        gameManager.StartGame();
        yield return new WaitForSeconds(0.3f);

        Vector3 startPos = player.transform.position;
        yield return InputSimulationHelpers.HoldButton(this, gamepad.dpad.left, 0.8f);

        Assert.That(player.transform.position.x, Is.LessThan(startPos.x - 0.1f),
            "Vehicle should move left when d-pad left is held.");
    }

    [UnityTest]
    public IEnumerator Gamepad_StartButton_TogglesPauseAndResume()
    {
        gameManager.StartGame();
        yield return new WaitForSeconds(0.3f);

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
        Assert.That(hint.text, Does.Contain("Start pause"),
            "Controls hint should show gamepad instructions after gamepad use.");

        Set(gamepad.leftStick, Vector2.zero, queueEventOnly: true);
        yield return null;
    }

    // --- Mobile / touch ---

    [UnityTest]
    public IEnumerator TouchTap_SwitchesToTouchMode_AndShowsMobileControls()
    {
        gameManager.StartGame();
        yield return new WaitForSeconds(0.3f);

        yield return InputSimulationHelpers.Tap(this, ScreenCenter());

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Touch),
            "A touch should switch the input mode to Touch.");

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        Assert.That(mobileControls, Is.Not.Null, "MobileControlsUI should self-bootstrap at runtime.");

        var stickRoot = GetPrivateField<GameObject>(mobileControls, "stickRoot");
        var pauseRoot = GetPrivateField<GameObject>(mobileControls, "pauseRoot");
        yield return WaitUntilOrTimeout(() => stickRoot.activeInHierarchy);

        Assert.That(stickRoot.activeInHierarchy, Is.True,
            "The on-screen stick should appear in Touch mode during an active run.");
        Assert.That(pauseRoot.activeInHierarchy, Is.True,
            "The on-screen pause button should appear in Touch mode during an active run.");
    }

    [UnityTest]
    public IEnumerator MobileControls_Hidden_InKeyboardMode()
    {
        gameManager.StartGame();
        yield return new WaitForSeconds(0.3f);

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        Press(keyboard.spaceKey, queueEventOnly: true);
        yield return null;
        yield return null;

        Assert.That(InputModeWatcher.Mode, Is.EqualTo(InputMode.Keyboard));

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        Assert.That(mobileControls, Is.Not.Null);
        Assert.That(GetPrivateField<GameObject>(mobileControls, "stickRoot").activeInHierarchy, Is.False,
            "The on-screen stick must stay hidden while playing with the keyboard.");
        Assert.That(GetPrivateField<GameObject>(mobileControls, "pauseRoot").activeInHierarchy, Is.False,
            "The on-screen pause button must stay hidden while playing with the keyboard.");
    }

    [UnityTest]
    public IEnumerator VirtualStick_DrivesVehicle_AndModeStaysTouch()
    {
        gameManager.StartGame();
        yield return new WaitForSeconds(0.3f);

        yield return InputSimulationHelpers.Tap(this, ScreenCenter());

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        var stickRoot = GetPrivateField<GameObject>(mobileControls, "stickRoot");
        yield return WaitUntilOrTimeout(() => stickRoot.activeInHierarchy);
        Assert.That(stickRoot.activeInHierarchy, Is.True);

        // The stick's virtual gamepad device is created when the stick becomes
        // visible; wait until MobileControlsUI has registered it as ignored so
        // the drag below can't be misread as real gamepad input.
        yield return WaitUntilOrTimeout(() => GetPrivateField<bool>(mobileControls, "stickDeviceIgnored"));
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
        gameManager.StartGame();
        yield return new WaitForSeconds(0.3f);

        yield return InputSimulationHelpers.Tap(this, ScreenCenter());

        var mobileControls = Object.FindAnyObjectByType<MobileControlsUI>();
        var pauseRoot = GetPrivateField<GameObject>(mobileControls, "pauseRoot");
        yield return WaitUntilOrTimeout(() => pauseRoot.activeInHierarchy);
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

    // --- Helpers ---

    static Vector2 ScreenCenter()
    {
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    static IEnumerator WaitUntilOrTimeout(System.Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + WaitTimeout;
        while (!condition() && Time.realtimeSinceStartup < deadline)
            yield return null;
    }

    static T GetPrivateField<T>(object target, string name)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        return field != null ? (T)field.GetValue(target) : default;
    }

    static void SetPrivateField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }
}
