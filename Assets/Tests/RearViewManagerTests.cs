using NUnit.Framework;
using UnityEngine;

public class RearViewManagerTests
{
    private bool hadPref;
    private int savedPref;

    [SetUp]
    public void SaveAndResetState()
    {
        hadPref = PlayerPrefs.HasKey(RearViewManager.RearViewPrefKey);
        savedPref = PlayerPrefs.GetInt(RearViewManager.RearViewPrefKey, 0);
        ResetState();
    }

    [TearDown]
    public void RestoreState()
    {
        if (hadPref)
            PlayerPrefs.SetInt(RearViewManager.RearViewPrefKey, savedPref);
        else
            PlayerPrefs.DeleteKey(RearViewManager.RearViewPrefKey);
        ResetState();
    }

    static void ResetState()
    {
        PlayerPrefs.DeleteKey(RearViewManager.RearViewPrefKey);
        TestReflectionHelpers.SetPrivateStaticField(typeof(RearViewManager), "rearViewPref", int.MinValue);
        TestReflectionHelpers.SetPrivateStaticField(typeof(RearViewManager), "RearViewPreferenceChanged", null);
    }

    [Test]
    public void DefaultsToOff()
    {
        Assert.That(RearViewManager.RearViewEnabled, Is.False);
        Assert.That(RearViewManager.ButtonLabel, Is.EqualTo("REAR VIEW: OFF"));
    }

    [Test]
    public void TogglePreference_PersistsAndRaisesChanged()
    {
        bool raised = false;
        RearViewManager.RearViewPreferenceChanged += () => raised = true;

        RearViewManager.TogglePreference();

        Assert.That(RearViewManager.RearViewEnabled, Is.True);
        Assert.That(PlayerPrefs.GetInt(RearViewManager.RearViewPrefKey, 0), Is.EqualTo(1));
        Assert.That(raised, Is.True);
        Assert.That(RearViewManager.ButtonLabel, Is.EqualTo("REAR VIEW: ON"));
    }
}
