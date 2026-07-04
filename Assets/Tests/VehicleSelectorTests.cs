using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class VehicleSelectorTests
{
    private GameObject selectorGameObject;
    private VehicleSelector selector;
    private GameObject playerGameObject;
    private PlayerController playerController;
    private GameObject emitterGameObject;
    private ParticleSystem emitter;

    [SetUp]
    public void SetUp()
    {
        playerGameObject = new GameObject("PlayerController");
        playerController = playerGameObject.AddComponent<PlayerController>();

        var groundGameObject = new GameObject("GroundScroller");
        groundGameObject.AddComponent<GroundScroller>();

        selectorGameObject = new GameObject("VehicleSelector");
        selector = selectorGameObject.AddComponent<VehicleSelector>();

        emitterGameObject = new GameObject("DirtEmitter");
        emitterGameObject.transform.parent = selectorGameObject.transform;
        emitter = emitterGameObject.AddComponent<ParticleSystem>();

        SetPrivateField(selector, "dirtEmitters", new ParticleSystem[] { emitter });
        SetPrivateField(selector, "vehicleVisuals", new GameObject[0]);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(selectorGameObject);
        Object.DestroyImmediate(playerGameObject);
        Object.DestroyImmediate(emitterGameObject);
    }

    [Test]
    public void LeftInputPointsEmitterRightAxisLeft()
    {
        SetCurrentMovementDirection(Vector3.left);
        InvokeUpdate();

        Assert.That(emitter.transform.right, Is.EqualTo(Vector3.left).Using(Vector3EqualityComparer.Instance));
    }

    [Test]
    public void RightInputPointsEmitterRightAxisRight()
    {
        SetCurrentMovementDirection(Vector3.right);
        InvokeUpdate();

        Assert.That(emitter.transform.right, Is.EqualTo(Vector3.right).Using(Vector3EqualityComparer.Instance));
    }

    [Test]
    public void NoPlayerInputFallsBackToGroundMoveDirection()
    {
        SetCurrentMovementDirection(Vector3.zero);
        InvokeUpdate();

        var groundScroller = selectorGameObject.scene.GetRootGameObjects()[0].GetComponentInChildren<GroundScroller>();
        Assert.IsNotNull(groundScroller, "GroundScroller should be present in the scene.");
        Assert.That(emitter.transform.right, Is.EqualTo(groundScroller.WorldMoveDirection).Using(Vector3EqualityComparer.Instance));
    }

    private void SetCurrentMovementDirection(Vector3 direction)
    {
        var field = typeof(PlayerController).GetField("<CurrentMovementDirection>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Could not find CurrentMovementDirection backing field.");
        field.SetValue(playerController, direction);

        SetPrivateField(selector, "playerController", playerController);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }

    private void InvokeUpdate()
    {
        var updateMethod = typeof(VehicleSelector).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(updateMethod, "Could not find Update() method on VehicleSelector.");
        updateMethod.Invoke(selector, null);
    }
}

internal sealed class Vector3EqualityComparer : IEqualityComparer<Vector3>
{
    public static readonly Vector3EqualityComparer Instance = new Vector3EqualityComparer();
    private const float Epsilon = 1e-4f;

    public bool Equals(Vector3 x, Vector3 y)
    {
        return Vector3.SqrMagnitude(x - y) < Epsilon * Epsilon;
    }

    public int GetHashCode(Vector3 obj)
    {
        return obj.GetHashCode();
    }
}
