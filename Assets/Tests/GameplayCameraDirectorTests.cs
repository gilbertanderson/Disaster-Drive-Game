using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GameplayCameraDirectorTests
{
    private GameObject cameraObject;
    private GameObject rigObject;
    private GameObject frontAnchorObject;
    private GameObject rearAnchorObject;
    private CameraShake cameraShake;
    private GameplayCameraDirector director;
    private bool hadRearViewPref;
    private int savedRearViewPref;

    [SetUp]
    public void SetUp()
    {
        hadRearViewPref = PlayerPrefs.HasKey(GameplayCameraDirector.RearViewPrefKey);
        savedRearViewPref = PlayerPrefs.GetInt(GameplayCameraDirector.RearViewPrefKey, 0);
        PlayerPrefs.DeleteKey(GameplayCameraDirector.RearViewPrefKey);
        GameplayCameraDirector.ResetRearViewPrefCacheForTests();

        cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetPositionAndRotation(
            new Vector3(0f, 16f, 2.5f),
            Quaternion.Euler(90f, 0f, -90f));

        rigObject = new GameObject("IntroCameraRig");
        frontAnchorObject = CreateAnchor("IntroCameraFront", rigObject.transform,
            new Vector3(11.5f, 2.6f, 0f), Quaternion.Euler(10f, 270f, 0f));
        rearAnchorObject = CreateAnchor("IntroCameraRear", rigObject.transform,
            new Vector3(-12f, 4.5f, 0f), Quaternion.Euler(25f, 90f, 0f));

        cameraShake = cameraObject.AddComponent<CameraShake>();
        director = cameraObject.AddComponent<GameplayCameraDirector>();

        SetPrivateField(director, "introRig", rigObject.transform);
        SetPrivateField(director, "frontAnchor", frontAnchorObject.transform);
        SetPrivateField(director, "rearAnchor", rearAnchorObject.transform);
        SetPrivateField(director, "cameraShake", cameraShake);
        SetPrivateField(director, "beatDuration", 0.8f);

        director.CacheGameplayPose();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(rigObject);
        Object.DestroyImmediate(cameraObject);

        if (hadRearViewPref)
            PlayerPrefs.SetInt(GameplayCameraDirector.RearViewPrefKey, savedRearViewPref);
        else
            PlayerPrefs.DeleteKey(GameplayCameraDirector.RearViewPrefKey);
        GameplayCameraDirector.ResetRearViewPrefCacheForTests();
        TestReflectionHelpers.SetPrivateStaticField(typeof(GameplayCameraDirector), "RearViewChanged", null);
    }

    [Test]
    public void PlayIntroSequence_EndsAtGameplayPose()
    {
        director.SimulateFullIntro();

        Assert.That(director.IsIntroComplete, Is.True);
        Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(0f, 16f, 2.5f)).Using(Vector3EqualityComparer.Instance));
        Assert.That(Quaternion.Angle(cameraObject.transform.rotation, Quaternion.Euler(90f, 0f, -90f)), Is.LessThan(0.1f));
    }

    [Test]
    public void StartIntroSequence_CutsStraightToFrontShot()
    {
        director.StartIntroSequence(null);

        Assert.That(Vector3.Distance(cameraObject.transform.position, frontAnchorObject.transform.position), Is.LessThan(0.001f));
        Assert.That(Quaternion.Angle(cameraObject.transform.rotation, frontAnchorObject.transform.rotation), Is.LessThan(0.1f));
    }

    [Test]
    public void PlayIntroSequence_HoldsFrontShotThroughCountdown()
    {
        director.StartIntroSequence(null);

        for (int beat = 0; beat <= 2; beat++)
        {
            AdvanceBeat(beat);
            Assert.That(Vector3.Distance(cameraObject.transform.position, frontAnchorObject.transform.position), Is.LessThan(0.1f),
                $"Camera should hold the front shot during beat {beat}.");
        }
    }

    [Test]
    public void StartIntroSequence_PositionsRigAtVehicleMidpoint()
    {
        var vehicleA = new GameObject("VehicleA");
        var vehicleB = new GameObject("VehicleB");
        vehicleA.transform.position = new Vector3(-20f, 0f, 0f);
        vehicleB.transform.position = new Vector3(20f, 0f, 0f);

        try
        {
            director.StartIntroSequence(new List<Transform> { vehicleA.transform, vehicleB.transform });
            Assert.That(rigObject.transform.position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(rigObject.transform.position.z, Is.EqualTo(0f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(vehicleA);
            Object.DestroyImmediate(vehicleB);
        }
    }

    [Test]
    public void PlayIntroSequence_SyncsCameraShakeRestPosition()
    {
        director.SimulateFullIntro();

        cameraShake.Shake();
        cameraShake.SyncRestPosition();

        Assert.That(cameraObject.transform.localPosition, Is.EqualTo(new Vector3(0f, 16f, 2.5f)).Using(Vector3EqualityComparer.Instance));
    }

    [Test]
    public void PlayIntroSequence_EndsAtRearPose_WhenRearViewEnabled()
    {
        GameplayCameraDirector.SetRearViewPref(1);

        director.SimulateFullIntro();

        Assert.That(director.IsIntroComplete, Is.True);
        Assert.That(Vector3.Distance(cameraObject.transform.position, rearAnchorObject.transform.position), Is.LessThan(0.001f));
        Assert.That(Quaternion.Angle(cameraObject.transform.rotation, rearAnchorObject.transform.rotation), Is.LessThan(0.1f));
    }

    [Test]
    public void ApplyPreferredGameplayPose_TogglesBetweenTopDownAndRear()
    {
        director.SimulateFullIntro();

        GameplayCameraDirector.SetRearViewPref(1);
        director.ApplyPreferredGameplayPose();
        Assert.That(Vector3.Distance(cameraObject.transform.position, rearAnchorObject.transform.position), Is.LessThan(0.001f),
            "Enabling rear view should snap to the rear anchor.");

        GameplayCameraDirector.SetRearViewPref(0);
        director.ApplyPreferredGameplayPose();
        Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(0f, 16f, 2.5f)).Using(Vector3EqualityComparer.Instance),
            "Disabling rear view should restore the top-down gameplay pose.");
    }

    [Test]
    public void ToggleRearViewPref_PersistsAndRaisesChanged()
    {
        bool raised = false;
        GameplayCameraDirector.RearViewChanged += () => raised = true;

        Assert.That(GameplayCameraDirector.RearViewEnabled, Is.False);
        GameplayCameraDirector.ToggleRearViewPref();

        Assert.That(GameplayCameraDirector.RearViewEnabled, Is.True);
        Assert.That(PlayerPrefs.GetInt(GameplayCameraDirector.RearViewPrefKey, 0), Is.EqualTo(1));
        Assert.That(raised, Is.True);
        Assert.That(GameplayCameraDirector.RearViewButtonLabel, Does.Contain("ON"));
    }

    void AdvanceBeat(int beatIndex)
    {
        const float step = 0.05f;
        director.PlayCountdownBeat(beatIndex);
        for (int i = 0; i < 17; i++)
            director.SimulateBeatStep(step);
    }

    static GameObject CreateAnchor(string name, Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        var anchor = new GameObject(name);
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = localPosition;
        anchor.transform.localRotation = localRotation;
        return anchor;
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }
}
