using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GameplayCameraDirectorTests
{
    private GameObject cameraObject;
    private GameObject rigObject;
    private GameObject frontAnchorObject;
    private CameraShake cameraShake;
    private GameplayCameraDirector director;

    [SetUp]
    public void SetUp()
    {
        cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetPositionAndRotation(
            new Vector3(0f, 16f, 2.5f),
            Quaternion.Euler(90f, 0f, -90f));

        rigObject = new GameObject("IntroCameraRig");
        frontAnchorObject = CreateAnchor("IntroCameraFront", rigObject.transform,
            new Vector3(11.5f, 2.6f, 0f), Quaternion.Euler(10f, 270f, 0f));

        cameraShake = cameraObject.AddComponent<CameraShake>();
        director = cameraObject.AddComponent<GameplayCameraDirector>();

        SetPrivateField(director, "introRig", rigObject.transform);
        SetPrivateField(director, "frontAnchor", frontAnchorObject.transform);
        SetPrivateField(director, "cameraShake", cameraShake);
        SetPrivateField(director, "beatDuration", 0.8f);

        director.CacheGameplayPose();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(rigObject);
        Object.DestroyImmediate(cameraObject);
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
