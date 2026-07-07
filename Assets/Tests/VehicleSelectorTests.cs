using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class VehicleSelectorTests
{
    private GameObject groundGameObject;
    private GameObject selectorGameObject;
    private VehicleSelector selector;
    private ParticleSystem emitter;
    private GameObject vehicleVisual;
    private GameObject vehicleVisual2;
    private readonly List<GameObject> disabledSceneGrounds = new();

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();

        foreach (var sceneGround in Object.FindObjectsByType<GroundScroller>(FindObjectsSortMode.None))
        {
            if (!sceneGround.gameObject.activeSelf)
                continue;

            sceneGround.gameObject.SetActive(false);
            disabledSceneGrounds.Add(sceneGround.gameObject);
        }

        groundGameObject = new GameObject("GroundScroller");
        groundGameObject.AddComponent<GroundScroller>();

        selectorGameObject = new GameObject("VehicleSelector");
        selectorGameObject.SetActive(false);
        selectorGameObject.AddComponent<BoxCollider>();

        vehicleVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicleVisual.name = "VehicleVisual1";
        vehicleVisual.transform.parent = selectorGameObject.transform;
        vehicleVisual.transform.localPosition = Vector3.zero;

        vehicleVisual2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicleVisual2.name = "VehicleVisual2";
        vehicleVisual2.transform.parent = selectorGameObject.transform;
        vehicleVisual2.transform.localPosition = new Vector3(1.5f, 0f, 0f);

        var emitterGameObject = new GameObject("DirtEmitter");
        emitterGameObject.transform.parent = selectorGameObject.transform;
        emitter = emitterGameObject.AddComponent<ParticleSystem>();

        selector = selectorGameObject.AddComponent<VehicleSelector>();
        SetPrivateField(selector, "dirtEmitters", new ParticleSystem[] { emitter });
        SetPrivateField(selector, "vehicleVisuals", new GameObject[] { vehicleVisual, vehicleVisual2 });
        SetPrivateField(selector, "groundScroller", groundGameObject.GetComponent<GroundScroller>());

        selectorGameObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
        Object.DestroyImmediate(groundGameObject);
        Object.DestroyImmediate(selectorGameObject);

        foreach (var sceneGround in disabledSceneGrounds)
        {
            if (sceneGround != null)
                sceneGround.SetActive(true);
        }

        disabledSceneGrounds.Clear();
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
        var scroller = groundGameObject.GetComponent<GroundScroller>();
        SetPrivateField(scroller, "scrollDirection", new Vector2(0f, -1f));
        SetPrivateField(selector, "groundScroller", scroller);

        Assert.That(scroller.WorldMoveDirection, Is.EqualTo(Vector3.back));
        Assert.That(GetPrivateField<GroundScroller>(selector, "groundScroller"), Is.SameAs(scroller));

        InvokePrivateMethod("Apply");
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

    [Test]
    public void Awake_CompactsNullVehicleVisuals()
    {
        var vehicleVisual3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicleVisual3.name = "VehicleVisual3";
        vehicleVisual3.transform.parent = selectorGameObject.transform;

        SetPrivateField(selector, "vehicleVisuals", new GameObject[] { vehicleVisual, null, vehicleVisual3 });
        InvokePrivateMethod("Awake");

        var compacted = GetPrivateVehicleVisuals(selector);
        Assert.That(compacted.Length, Is.EqualTo(2));
        Assert.That(compacted[0], Is.SameAs(vehicleVisual));
        Assert.That(compacted[1], Is.SameAs(vehicleVisual3));
    }

    [Test]
    public void FormatVehicleName_StripsPrefixesAndLimitsToTwoWords()
    {
        Assert.That(VehicleSelector.FormatVehicleName("Veh_Armor_Car_01"), Is.EqualTo("Tank"));
        Assert.That(VehicleSelector.FormatVehicleName("SM_Veh_Convertable_01"), Is.EqualTo("Convertible"));
        Assert.That(VehicleSelector.FormatVehicleName("Prefab_K-131"), Is.EqualTo("Jeep"));
        Assert.That(VehicleSelector.FormatVehicleName("Off-road vehicle"), Is.EqualTo("Humvee"));
        Assert.That(VehicleSelector.FormatVehicleName("SURVIVAL ARMORED TRUCK 1"), Is.EqualTo("Armored Truck"));
    }

    [Test]
    public void Apply_UpdatesVehicleNameLabel()
    {
        vehicleVisual.name = "Veh_Armor_Car_01";
        vehicleVisual2.name = "SM_Veh_Convertable_01";

        var labelObject = new GameObject("VehicleNameLabel");
        var label = labelObject.AddComponent<TextMeshProUGUI>();

        SetPrivateField(selector, "vehicleNameText", label);
        InvokePrivateMethod("Awake");

        Assert.That(label.text, Is.EqualTo("Tank"));

        selector.NextVehicle();
        Assert.That(label.text, Is.EqualTo("Convertible"));

        Object.DestroyImmediate(labelObject);
    }

    [Test]
    public void Cycle_NeverLeavesAllVisualsInactive()
    {
        var vehicleVisual3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicleVisual3.name = "VehicleVisual3";
        vehicleVisual3.transform.parent = selectorGameObject.transform;

        SetPrivateField(selector, "vehicleVisuals", new GameObject[] { vehicleVisual, null, vehicleVisual3 });
        InvokePrivateMethod("Awake");

        int count = GetPrivateVehicleVisuals(selector).Length;
        for (int step = 0; step < count; step++)
        {
            selector.NextVehicle();

            int activeCount = 0;
            GameObject activeVisual = null;
            foreach (var visual in GetPrivateVehicleVisuals(selector))
            {
                if (visual != null && visual.activeSelf)
                {
                    activeCount++;
                    activeVisual = visual;
                }
            }

            Assert.That(activeCount, Is.EqualTo(1), $"Step {step}: expected exactly one active vehicle visual.");
            Assert.IsNotNull(activeVisual.GetComponentInChildren<Renderer>(),
                $"Step {step}: active visual should have a renderer.");
        }
    }

    private static GameObject[] GetPrivateVehicleVisuals(VehicleSelector target)
    {
        var field = typeof(VehicleSelector).GetField("vehicleVisuals",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Could not find private field 'vehicleVisuals' on VehicleSelector.");
        return (GameObject[])field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        return (T)field.GetValue(target);
    }

    private void InvokePrivateMethod(string methodName)
    {
        var method = typeof(VehicleSelector).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(method, $"Could not find method '{methodName}' on VehicleSelector.");
        method.Invoke(selector, null);
    }
}
