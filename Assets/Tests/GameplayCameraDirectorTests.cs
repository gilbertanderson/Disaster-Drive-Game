using NUnit.Framework;
using UnityEngine;

public class GameplayCameraDirectorTests
{
    private GameObject cameraObject;
    private GameObject anchorObject;
    private CameraShake cameraShake;
    private GameplayCameraDirector director;

    [SetUp]
    public void SetUp()
    {
        cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetPositionAndRotation(
            new Vector3(0f, 16f, 2.5f),
            Quaternion.Euler(90f, 0f, -90f));

        anchorObject = new GameObject("IntroCameraAnchor");
        anchorObject.transform.SetPositionAndRotation(
            new Vector3(-6f, 6f, -1f),
            Quaternion.Euler(35f, 0f, 0f));

        cameraShake = cameraObject.AddComponent<CameraShake>();
        director = cameraObject.AddComponent<GameplayCameraDirector>();

        SetPrivateField(director, "introAnchor", anchorObject.transform);
        SetPrivateField(director, "cameraShake", cameraShake);
        SetPrivateField(director, "introInDuration", 0.5f);
        SetPrivateField(director, "introHoldDuration", 1.7f);
        SetPrivateField(director, "returnDuration", 1f);

        director.CacheGameplayPose();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(anchorObject);
        Object.DestroyImmediate(cameraObject);
    }

    [Test]
    public void PlayIntroSequence_EndsAtGameplayPose()
    {
        const float step = 0.05f;
        const float totalDuration = 3.5f;
        for (float elapsed = 0f; elapsed <= totalDuration; elapsed += step)
            director.SimulateIntroStep(step);

        Assert.That(director.IsIntroComplete, Is.True);
        Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(0f, 16f, 2.5f)).Using(Vector3EqualityComparer.Instance));
        Assert.That(Quaternion.Angle(cameraObject.transform.rotation, Quaternion.Euler(90f, 0f, -90f)), Is.LessThan(0.1f));
    }

    [Test]
    public void PlayIntroSequence_SyncsCameraShakeRestPosition()
    {
        const float step = 0.05f;
        for (int i = 0; i < 60; i++)
            director.SimulateIntroStep(step);

        cameraShake.Shake();
        cameraShake.SyncRestPosition();

        Assert.That(cameraObject.transform.localPosition, Is.EqualTo(new Vector3(0f, 16f, 2.5f)).Using(Vector3EqualityComparer.Instance));
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }
}
