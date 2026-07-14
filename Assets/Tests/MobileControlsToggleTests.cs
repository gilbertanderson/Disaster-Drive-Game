using NUnit.Framework;
using UnityEngine;

// Covers the touch-controls toggle preference that decides whether the
// on-screen mobile controls (MobileControlsUI) are shown: until the player
// makes an explicit choice the controls follow Touch mode automatically;
// once toggled, the persisted on/off choice wins on any device.
public class MobileControlsToggleTests
{
    private bool hadPref;
    private int savedPref;

    [SetUp]
    public void SaveAndResetState()
    {
        // The editor shares PlayerPrefs with real play sessions; snapshot the
        // developer's actual toggle choice and put it back afterwards.
        hadPref = PlayerPrefs.HasKey(MobileControlsUI.TouchControlsPrefKey);
        savedPref = PlayerPrefs.GetInt(MobileControlsUI.TouchControlsPrefKey, -1);
        ResetToggleState();
    }

    [TearDown]
    public void RestoreState()
    {
        if (hadPref)
            PlayerPrefs.SetInt(MobileControlsUI.TouchControlsPrefKey, savedPref);
        else
            PlayerPrefs.DeleteKey(MobileControlsUI.TouchControlsPrefKey);
        ResetToggleState();
    }

    // The pref cache, the TouchControlsChanged subscriber list, and the input
    // mode are all static, so they leak between tests unless reset explicitly.
    private static void ResetToggleState()
    {
        PlayerPrefs.DeleteKey(MobileControlsUI.TouchControlsPrefKey);
        TestReflectionHelpers.SetPrivateStaticField(typeof(MobileControlsUI), "touchControlsPref", int.MinValue);
        TestReflectionHelpers.SetPrivateStaticField(typeof(MobileControlsUI), "TouchControlsChanged", null);
        TestReflectionHelpers.SetStaticProperty(typeof(InputModeWatcher), "Mode", InputMode.Keyboard);
    }

    private static void SetPref(int value)
    {
        TestReflectionHelpers.InvokePrivateStatic(typeof(MobileControlsUI), "SetTouchControlsPref", value);
    }

    private static void SetMode(InputMode mode)
    {
        TestReflectionHelpers.SetStaticProperty(typeof(InputModeWatcher), "Mode", mode);
    }

    [Test]
    public void Auto_FollowsTouchMode_WhenPlayerHasNotToggled()
    {
        Assert.That(MobileControlsUI.TouchControlsActive, Is.False,
            "With no explicit choice, touch controls should be inactive in Keyboard mode.");

        SetMode(InputMode.Touch);

        Assert.That(MobileControlsUI.TouchControlsActive, Is.True,
            "With no explicit choice, touch controls should follow Touch mode.");
    }

    [Test]
    public void ExplicitOff_HidesControls_EvenInTouchMode()
    {
        SetMode(InputMode.Touch);
        SetPref(0);

        Assert.That(MobileControlsUI.TouchControlsActive, Is.False,
            "An explicit off choice must win over Touch mode.");
    }

    [Test]
    public void ExplicitOn_ShowsControls_EvenInKeyboardMode()
    {
        SetPref(1);

        Assert.That(MobileControlsUI.TouchControlsActive, Is.True,
            "An explicit on choice must win over Keyboard mode.");
    }

    [Test]
    public void Choice_IsPersisted_AndReloadedFromPlayerPrefs()
    {
        SetPref(1);

        Assert.That(PlayerPrefs.GetInt(MobileControlsUI.TouchControlsPrefKey, -1), Is.EqualTo(1),
            "The toggle choice should be written to PlayerPrefs.");

        // Drop the static cache to simulate a fresh session; the persisted
        // choice should be read back.
        TestReflectionHelpers.SetPrivateStaticField(typeof(MobileControlsUI), "touchControlsPref", int.MinValue);

        Assert.That(MobileControlsUI.TouchControlsActive, Is.True,
            "The persisted choice should survive a restart.");
    }

    [Test]
    public void Toggling_RaisesTouchControlsChanged()
    {
        bool raised = false;
        MobileControlsUI.TouchControlsChanged += () => raised = true;

        SetPref(1);

        Assert.That(raised, Is.True,
            "Flipping the toggle should raise TouchControlsChanged so the controls hint can re-render.");
    }

    [Test]
    public void InGameHint_MatchesModeAndToggle()
    {
        Assert.That(BuildHint(InputMode.Gamepad, false, false), Does.Contain("Start"),
            "Gamepad hint should mention the Start button.");
        Assert.That(BuildHint(InputMode.Keyboard, false, false), Does.Contain("WASD"),
            "Keyboard hint should mention WASD.");
        Assert.That(BuildHint(InputMode.Keyboard, true, false), Does.Contain("stick"),
            "With touch controls forced on, the hint should describe the on-screen stick.");
        Assert.That(BuildHint(InputMode.Touch, false, false), Does.Contain("off"),
            "In Touch mode with controls toggled off, the hint should say they are off.");
        Assert.That(BuildHint(InputMode.Keyboard, false, true), Does.Contain("P2"),
            "Two-player hint should cover both players.");
    }

    private static string BuildHint(InputMode mode, bool controlsOn, bool twoPlayer)
    {
        return (string)TestReflectionHelpers.InvokePrivateStatic(
            typeof(MobileControlsUI), "BuildHint", mode, controlsOn, twoPlayer);
    }
}
