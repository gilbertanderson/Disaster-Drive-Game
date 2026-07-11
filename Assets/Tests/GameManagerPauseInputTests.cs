using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

// Covers the pause hotkeys GameManager.Update polls directly (Esc on keyboard,
// Start on gamepad). Only the pause-engage half runs here: after pausing,
// Update early-returns, but the resume path falls through into scene-dependent
// UI code, so resume is exercised by the Play Mode suite instead.
public class GameManagerPauseInputTests : InputTestFixture
{
    private GameObject gameManagerObject;
    private GameManager gameManager;
    private Gamepad gamepad;

    [SetUp]
    public void SetUpGameManager()
    {
        gameManagerObject = new GameObject("GameManager");
        gameManager = gameManagerObject.AddComponent<GameManager>();
        gamepad = InputSystem.AddDevice<Gamepad>();
        Time.timeScale = 1f;
    }

    [TearDown]
    public void TearDownGameManager()
    {
        Time.timeScale = 1f;
        Object.DestroyImmediate(gameManagerObject);
    }

    // Update doesn't run automatically in Edit Mode; invoke it right after the
    // simulated press, while wasPressedThisFrame is still set.
    private void PumpGameManager()
    {
        TestReflectionHelpers.InvokePrivate(gameManager, "Update");
    }

    [Test]
    public void GamepadStartButton_PausesActiveRun()
    {
        TestReflectionHelpers.SetPrivateProperty(gameManager, "IsGameActive", true);

        Press(gamepad.startButton);
        PumpGameManager();

        Assert.That(gameManager.IsPaused, Is.True, "Start button should pause an active run.");
        Assert.That(Time.timeScale, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void GamepadStartButton_Ignored_WhenGameInactive()
    {
        Press(gamepad.startButton);
        PumpGameManager();

        Assert.That(gameManager.IsPaused, Is.False,
            "Start button should do nothing on the start screen.");
        Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void KeyboardEscape_PausesActiveRun()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        TestReflectionHelpers.SetPrivateProperty(gameManager, "IsGameActive", true);

        Press(keyboard.escapeKey);
        PumpGameManager();

        Assert.That(gameManager.IsPaused, Is.True, "Esc should pause an active run.");
        Assert.That(Time.timeScale, Is.EqualTo(0f).Within(0.001f));
    }
}
