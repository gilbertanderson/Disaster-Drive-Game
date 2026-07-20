using NUnit.Framework;
using UnityEngine;

// Covers the preferred-orientation preference behind the pause menu's ORIENTATION
// toggle (see OrientationManager): landscape by default, an explicit choice is
// persisted, and the change event lets the pause menu re-render its label.
// The platform-specific side of Apply() compiles away in the Editor, so these
// tests exercise exactly what runs on CI.
public class OrientationPreferenceTests
{
    private bool hadPref;
    private int savedPref;

    [SetUp]
    public void SaveAndResetState()
    {
        // The editor shares PlayerPrefs with real play sessions; snapshot the
        // developer's actual choice and put it back afterwards.
        hadPref = PlayerPrefs.HasKey(OrientationManager.OrientationPrefKey);
        savedPref = PlayerPrefs.GetInt(OrientationManager.OrientationPrefKey, 1);
        ResetOrientationState();
    }

    [TearDown]
    public void RestoreState()
    {
        if (hadPref)
            PlayerPrefs.SetInt(OrientationManager.OrientationPrefKey, savedPref);
        else
            PlayerPrefs.DeleteKey(OrientationManager.OrientationPrefKey);
        ResetOrientationState();
    }

    // The pref cache and the OrientationPreferenceChanged subscriber list are
    // static, so they leak between tests unless reset explicitly.
    private static void ResetOrientationState()
    {
        PlayerPrefs.DeleteKey(OrientationManager.OrientationPrefKey);
        TestReflectionHelpers.SetPrivateStaticField(typeof(OrientationManager), "orientationPref", int.MinValue);
        TestReflectionHelpers.SetPrivateStaticField(typeof(OrientationManager), "OrientationPreferenceChanged", null);
    }

    [Test]
    public void Default_IsLandscape()
    {
        Assert.That(OrientationManager.LandscapePreferred, Is.True,
            "With no saved choice, the game should prefer landscape.");
        Assert.That(OrientationManager.ButtonLabel, Is.EqualTo("ORIENTATION: LANDSCAPE"));
    }

    [Test]
    public void Toggle_SwitchesToPortrait_AndPersists()
    {
        OrientationManager.TogglePreference();

        Assert.That(OrientationManager.LandscapePreferred, Is.False,
            "Toggling from the landscape default should prefer portrait.");
        Assert.That(OrientationManager.ButtonLabel, Is.EqualTo("ORIENTATION: PORTRAIT"));
        Assert.That(PlayerPrefs.GetInt(OrientationManager.OrientationPrefKey, -1), Is.EqualTo(0),
            "The portrait choice should be written to PlayerPrefs.");
    }

    [Test]
    public void ToggleTwice_ReturnsToLandscape_AndPersists()
    {
        OrientationManager.TogglePreference();
        OrientationManager.TogglePreference();

        Assert.That(OrientationManager.LandscapePreferred, Is.True);
        Assert.That(PlayerPrefs.GetInt(OrientationManager.OrientationPrefKey, -1), Is.EqualTo(1),
            "The landscape choice should be written back to PlayerPrefs.");
    }

    [Test]
    public void Toggle_RaisesOrientationPreferenceChanged()
    {
        bool raised = false;
        OrientationManager.OrientationPreferenceChanged += () => raised = true;

        OrientationManager.TogglePreference();

        Assert.That(raised, Is.True,
            "Flipping the toggle should raise OrientationPreferenceChanged so the button label can re-render.");
    }

    [Test]
    public void Choice_SurvivesStaticCacheReset()
    {
        OrientationManager.TogglePreference();   // portrait, persisted

        // Drop the static cache to simulate a fresh session; the persisted
        // choice should be read back from PlayerPrefs.
        TestReflectionHelpers.SetPrivateStaticField(typeof(OrientationManager), "orientationPref", int.MinValue);

        Assert.That(OrientationManager.LandscapePreferred, Is.False,
            "The persisted portrait choice should survive a restart.");
    }
}
