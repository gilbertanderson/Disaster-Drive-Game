using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class VehicleSelectorTests
{
    private GameObject groundGameObject;
    private GameObject selectorGameObject;
    private VehicleSelector selector;
    private GameObject emitterGameObject;
    private ParticleSystem emitter;
    private GameObject vehicleVisual;
    private GameObject vehicleVisual2;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();

        groundGameObject = new GameObject("GroundScroller");
        groundGameObject.AddComponent<GroundScroller>();

        selectorGameObject = new GameObject("VehicleSelector");
        selector = selectorGameObject.AddComponent<VehicleSelector>();
        selectorGameObject.AddComponent<BoxCollider>();

        vehicleVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicleVisual.name = "VehicleVisual1";
        vehicleVisual.transform.parent = selectorGameObject.transform;
        vehicleVisual.transform.localPosition = Vector3.zero;

        vehicleVisual2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicleVisual2.name = "VehicleVisual2";
        vehicleVisual2.transform.parent = selectorGameObject.transform;
        vehicleVisual2.transform.localPosition = new Vector3(1.5f, 0f, 0f);

        emitterGameObject = new GameObject("DirtEmitter");
        emitterGameObject.transform.parent = selectorGameObject.transform;
        emitter = emitterGameObject.AddComponent<ParticleSystem>();

        SetPrivateField(selector, "dirtEmitters", new ParticleSystem[] { emitter });
        SetPrivateField(selector, "vehicleVisuals", new GameObject[] { vehicleVisual, vehicleVisual2 });
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
        Object.DestroyImmediate(groundGameObject);
        Object.DestroyImmediate(selectorGameObject);
        Object.DestroyImmediate(emitterGameObject);
        Object.DestroyImmediate(vehicleVisual);
        Object.DestroyImmediate(vehicleVisual2);
    }

    [Test]
    public void Awake_SetsParticleSimulationSpaceToLocal()
    {
        InvokePrivateMethod("Awake");
        Assert.That(emitter.main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.Local));
    }

    [Test]
    public void Apply_PlacesEmitterBehindTheSelectedVehicleOnly()
    {
        PlayerPrefs.SetInt("VehicleIndex", 1);
        InvokePrivateMethod("Awake");

        Assert.IsTrue(vehicleVisual2.activeSelf, "The second vehicle should be selected after Awake when VehicleIndex is 1.");
        Assert.IsFalse(vehicleVisual.activeSelf, "The first vehicle should be inactive after Awake when VehicleIndex is 1.");
        Assert.That(emitter.transform.localPosition.x, Is.LessThan(0f), "Emitter should sit behind the vehicle on the local X axis.");
    }

    [Test]
    public void Awake_AlignsEmitterToRoadMovementDirection()
    {
        InvokePrivateMethod("Awake");
        Assert.That(emitter.transform.right, Is.EqualTo(Vector3.back).Using(Vector3EqualityComparer.Instance));
    }

    [Test]
    public void Update_DoesNotChangeEmitterRotationFromDefault()
    {
        var initialRotation = emitter.transform.localRotation;
        InvokePrivateMethod("Update");
        Assert.That(emitter.transform.localRotation, Is.EqualTo(initialRotation));
    }

    [Test]
    public void Apply_OrientsLeftAndRightEmittersInOppositeDirections()
    {
        var leftEmitterGameObject = new GameObject("RearDirt_L");
        leftEmitterGameObject.transform.parent = selectorGameObject.transform;
        var leftEmitter = leftEmitterGameObject.AddComponent<ParticleSystem>();

        var rightEmitterGameObject = new GameObject("RearDirt_R");
        rightEmitterGameObject.transform.parent = selectorGameObject.transform;
        var rightEmitter = rightEmitterGameObject.AddComponent<ParticleSystem>();

        SetPrivateField(selector, "dirtEmitters", new ParticleSystem[] { leftEmitter, rightEmitter });
        InvokePrivateMethod("Awake");

        Assert.That(Vector3.Dot(leftEmitter.transform.forward, rightEmitter.transform.forward), Is.LessThan(0f));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }

    private void InvokePrivateMethod(string methodName)
    {
        var method = typeof(VehicleSelector).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(method, $"Could not find method '{methodName}' on VehicleSelector.");
        method.Invoke(selector, null);
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
