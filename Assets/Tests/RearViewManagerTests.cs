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
        TestReflectionHelpers.SetPrivateStaticField(typeof(RearViewManager), "viewModePref", int.MinValue);
        TestReflectionHelpers.SetPrivateStaticField(typeof(RearViewManager), "RearViewPreferenceChanged", null);
    }

    [Test]
    public void DefaultsToNormal()
    {
        Assert.That(RearViewManager.CurrentMode, Is.EqualTo(RearViewManager.ViewMode.Normal));
        Assert.That(RearViewManager.RearViewEnabled, Is.False);
        Assert.That(RearViewManager.ButtonLabel, Is.EqualTo("VIEW: TOP"));
    }

    [Test]
    public void TogglePreference_PersistsAndRaisesChanged()
    {
        bool raised = false;
        RearViewManager.RearViewPreferenceChanged += () => raised = true;

        RearViewManager.TogglePreference();

        Assert.That(RearViewManager.CurrentMode, Is.EqualTo(RearViewManager.ViewMode.RearChase));
        Assert.That(RearViewManager.RearViewEnabled, Is.True);
        Assert.That(PlayerPrefs.GetInt(RearViewManager.RearViewPrefKey, 0), Is.EqualTo(1));
        Assert.That(raised, Is.True);
        Assert.That(RearViewManager.ButtonLabel, Is.EqualTo("VIEW: REAR"));
    }

    [Test]
    public void CycleView_TogglesNormalAndRearOnly()
    {
        Assert.That(RearViewManager.CurrentMode, Is.EqualTo(RearViewManager.ViewMode.Normal));

        RearViewManager.CycleView();
        Assert.That(RearViewManager.CurrentMode, Is.EqualTo(RearViewManager.ViewMode.RearChase));
        Assert.That(RearViewManager.ButtonLabel, Is.EqualTo("VIEW: REAR"));

        RearViewManager.CycleView();
        Assert.That(RearViewManager.CurrentMode, Is.EqualTo(RearViewManager.ViewMode.Normal));
        Assert.That(RearViewManager.ButtonLabel, Is.EqualTo("VIEW: TOP"));
    }

    [Test]
    public void CurrentMode_MigratesLegacyIsometricPrefToNormal()
    {
        PlayerPrefs.SetInt(RearViewManager.RearViewPrefKey, (int)RearViewManager.ViewMode.Isometric);
        TestReflectionHelpers.SetPrivateStaticField(typeof(RearViewManager), "viewModePref", int.MinValue);

        Assert.That(RearViewManager.CurrentMode, Is.EqualTo(RearViewManager.ViewMode.Normal));
        Assert.That(RearViewManager.ButtonLabel, Is.EqualTo("VIEW: TOP"));
    }
}
