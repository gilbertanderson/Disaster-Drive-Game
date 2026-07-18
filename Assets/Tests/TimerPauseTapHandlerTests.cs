using NUnit.Framework;
using TMPro;
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

    [Test]
    public void EnsureRuntimeUiRefs_AttachesTapHandlerOnlyToTimerHudLabels()
    {
        var p1TimerObject = new GameObject("P1Timer");
        var p2TimerObject = new GameObject("P2Timer");
        TMP_Text countdownText = null;
        TMP_Text eliminationBannerText = null;

        try
        {
            var p1Timer = p1TimerObject.AddComponent<TextMeshProUGUI>();
            var p2Timer = p2TimerObject.AddComponent<TextMeshProUGUI>();
            p1Timer.raycastTarget = false;
            p2Timer.raycastTarget = false;
            TestReflectionHelpers.SetPrivateField(gameManager, "timerText", p1Timer);
            TestReflectionHelpers.SetPrivateField(gameManager, "timer2Text", p2Timer);

            TestReflectionHelpers.InvokePrivate(gameManager, "EnsureRuntimeUiRefs");

            AssertTimerHasPauseHandler(p1Timer);
            AssertTimerHasPauseHandler(p2Timer);

            countdownText = TestReflectionHelpers.GetPrivateField<TMP_Text>(gameManager, "countdownText");
            eliminationBannerText = TestReflectionHelpers.GetPrivateField<TMP_Text>(gameManager, "eliminationBannerText");

            Assert.That(countdownText.GetComponent<TimerPauseTapHandler>(), Is.Null,
                "Countdown text is cloned from the timer and must not inherit tap-to-pause.");
            Assert.That(eliminationBannerText.GetComponent<TimerPauseTapHandler>(), Is.Null,
                "Elimination banner text is cloned from the timer and must not inherit tap-to-pause.");
            Assert.That(countdownText.raycastTarget, Is.False,
                "The countdown clone must not block touches aimed at gameplay.");
            Assert.That(eliminationBannerText.raycastTarget, Is.False,
                "The elimination banner clone must not block touches aimed at gameplay.");
        }
        finally
        {
            if (countdownText != null)
                Object.DestroyImmediate(countdownText.gameObject);
            if (eliminationBannerText != null)
                Object.DestroyImmediate(eliminationBannerText.gameObject);
            Object.DestroyImmediate(p1TimerObject);
            Object.DestroyImmediate(p2TimerObject);
        }
    }

    private void AssertTimerHasPauseHandler(TMP_Text timer)
    {
        var attachedHandler = timer.GetComponent<TimerPauseTapHandler>();

        Assert.That(timer.raycastTarget, Is.True,
            "Timer labels must be raycastable so EventSystem taps reach the pause handler.");
        Assert.That(attachedHandler, Is.Not.Null,
            "Timer labels should receive TimerPauseTapHandler at runtime.");
        Assert.That(TestReflectionHelpers.GetPrivateField<GameManager>(attachedHandler, "gameManager"),
            Is.SameAs(gameManager), "The attached handler should be initialized with this GameManager.");
    }
}
