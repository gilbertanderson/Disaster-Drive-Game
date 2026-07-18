using NUnit.Framework;
using UnityEngine;

public class RearViewCameraControllerTests
{
    private bool hadPref;
    private int savedPref;

    [SetUp]
    public void SaveAndResetState()
    {
        hadPref = PlayerPrefs.HasKey(RearViewCameraController.RearViewPrefKey);
        savedPref = PlayerPrefs.GetInt(RearViewCameraController.RearViewPrefKey, 0);
        ResetState();
    }

    [TearDown]
    public void RestoreState()
    {
        if (hadPref)
            PlayerPrefs.SetInt(RearViewCameraController.RearViewPrefKey, savedPref);
        else
            PlayerPrefs.DeleteKey(RearViewCameraController.RearViewPrefKey);
        ResetState();
    }

    [Test]
    public void RearView_DefaultsOff()
    {
        Assert.That(RearViewCameraController.RearViewEnabled, Is.False);
        Assert.That(RearViewCameraController.ButtonLabel, Is.EqualTo("REAR VIEW: OFF"));
    }

    [Test]
    public void TogglePreference_EnablesAndPersistsRearView()
    {
        RearViewCameraController.TogglePreference();

        Assert.That(RearViewCameraController.RearViewEnabled, Is.True);
        Assert.That(PlayerPrefs.GetInt(RearViewCameraController.RearViewPrefKey), Is.EqualTo(1));
        Assert.That(RearViewCameraController.ButtonLabel, Is.EqualTo("REAR VIEW: ON"));
    }

    [Test]
    public void TogglePreference_RaisesChangedEvent()
    {
        bool raised = false;
        RearViewCameraController.RearViewPreferenceChanged += () => raised = true;

        RearViewCameraController.TogglePreference();

        Assert.That(raised, Is.True);
    }

    [Test]
    public void PersistedChoice_ReloadsFromPlayerPrefs()
    {
        PlayerPrefs.SetInt(RearViewCameraController.RearViewPrefKey, 1);
        TestReflectionHelpers.SetPrivateStaticField(
            typeof(RearViewCameraController), "rearViewPref", int.MinValue);

        Assert.That(RearViewCameraController.RearViewEnabled, Is.True);
    }

    private static void ResetState()
    {
        PlayerPrefs.DeleteKey(RearViewCameraController.RearViewPrefKey);
        TestReflectionHelpers.SetPrivateStaticField(
            typeof(RearViewCameraController), "rearViewPref", int.MinValue);
        TestReflectionHelpers.SetPrivateStaticField(
            typeof(RearViewCameraController), "RearViewPreferenceChanged", null);
    }
}
