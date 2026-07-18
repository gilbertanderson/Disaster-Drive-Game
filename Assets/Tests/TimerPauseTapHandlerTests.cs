using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

// Covers the tap-to-pause handler GameManager attaches to the timer HUD
// labels at runtime: tapping toggles pause exactly like the Esc key, and
// inherits TogglePause's IsGameActive guard so taps before gameplay starts
// (start screen, countdown) do nothing.
public class TimerPauseTapHandlerTests
{
    private GameObject gameManagerObject;
    private GameManager gameManager;
    private GameObject timerObject;
    private TimerPauseTapHandler handler;

    [SetUp]
    public void SetUp()
    {
        gameManagerObject = new GameObject("GameManager");
        gameManager = gameManagerObject.AddComponent<GameManager>();
        timerObject = new GameObject("TimerText");
        handler = timerObject.AddComponent<TimerPauseTapHandler>();
        handler.Init(gameManager);
        Time.timeScale = 1f;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        Object.DestroyImmediate(timerObject);
        Object.DestroyImmediate(gameManagerObject);
    }

    private void Tap()
    {
        handler.OnPointerClick(new PointerEventData(EventSystem.current));
    }

    [Test]
    public void Tap_DuringActiveRun_PausesGame()
    {
        TestReflectionHelpers.SetPrivateProperty(gameManager, "IsGameActive", true);

        Tap();

        Assert.That(gameManager.IsPaused, Is.True, "Tapping the timer should pause an active run.");
        Assert.That(Time.timeScale, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void Tap_BeforeGameplayStarts_DoesNothing()
    {
        Tap();

        Assert.That(gameManager.IsPaused, Is.False,
            "Tapping the timer should do nothing before gameplay starts.");
        Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Tap_WhilePaused_Resumes()
    {
        TestReflectionHelpers.SetPrivateProperty(gameManager, "IsGameActive", true);

        Tap();
        Tap();

        Assert.That(gameManager.IsPaused, Is.False,
            "A second tap should resume, mirroring Esc's toggle behavior.");
        Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
    }
}
