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
        groundGameObject.AddComponent<MeshRenderer>();
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
    public void Awake_DisablesCameraRigsEmbeddedInVehicleVisuals()
    {
        // Mirrors the armored truck prefab, which ships with its own chase camera child.
        var rig = new GameObject("Main Camera");
        rig.transform.parent = vehicleVisual.transform;
        var camera = rig.AddComponent<Camera>();
        var listener = rig.AddComponent<AudioListener>();

        InvokePrivateMethod("Awake");

        Assert.IsFalse(camera.enabled, "Embedded vehicle cameras must not override the top-down gameplay camera.");
        Assert.IsFalse(listener.enabled, "Embedded audio listeners would conflict with the gameplay camera's listener.");
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
        Assert.IsNotNull(groundGameObject);
        var scroller = groundGameObject.GetComponent<GroundScroller>();
        Assert.IsNotNull(scroller);

        SetPrivateField(scroller, "scrollDirection", new Vector2(-1f, 0f));
        SetPrivateField(selector, "groundScroller", scroller);

        var box = selector.GetComponent<BoxCollider>();
        Assert.IsNotNull(box);
        box.center = Vector3.zero;
        box.size = new Vector3(4f, 2f, 8f);

        var positionDirtEmitters = typeof(VehicleSelector).GetMethod(
            "PositionDirtEmitters",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(positionDirtEmitters);
        positionDirtEmitters.Invoke(selector, new object[] { box });

        Assert.That(scroller.WorldMoveDirection, Is.EqualTo(Vector3.left));
        Assert.That(emitter.transform.right, Is.EqualTo(Vector3.left).Using(Vector3EqualityComparer.Instance));
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
        Assert.That(VehicleSelector.FormatVehicleName("SURVIVAL ARMORED TRUCK 1"), Is.EqualTo("Armored Car"));
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

    [Test]
    public void CacheWheels_IncludesOnlyMeshWheelsExcludingSteeringWheelAndWheelColliders()
    {
        var frontLeft = CreateWheelChild(vehicleVisual, "Wheel_FL", new Vector3(-1f, 0f, 2f));
        var steeringWheel = CreateWheelChild(vehicleVisual, "SM_Veh_Convertable_SteeringW", new Vector3(0f, 0.5f, 1f));
        var physicsWheel = CreateWheelChild(vehicleVisual, "Wheel_Physics", new Vector3(1f, 0f, 2f));
        physicsWheel.AddComponent<WheelCollider>();

        var group = new GameObject("wheels");
        group.transform.parent = vehicleVisual.transform;
        var rearLeft = CreateWheelChild(group, "Wheel_RL", new Vector3(-1f, 0f, -2f));

        InvokePrivateMethod("CacheWheels");

        var wheels = GetPrivateField<Transform[]>(selector, "wheelTransforms");
        Assert.That(wheels.Length, Is.EqualTo(2));
        // Transform implements IEnumerable (over its children), so NUnit's CollectionAssert
        // compares wheel entries structurally (as sequences of children) rather than by
        // identity - every leaf cube here has no children, so they'd all read as "equal".
        // Array.IndexOf uses UnityEngine.Object's identity-based Equals instead.
        Assert.That(System.Array.IndexOf(wheels, frontLeft.transform), Is.GreaterThanOrEqualTo(0));
        Assert.That(System.Array.IndexOf(wheels, rearLeft.transform), Is.GreaterThanOrEqualTo(0));
        Assert.That(System.Array.IndexOf(wheels, steeringWheel.transform), Is.EqualTo(-1));
        Assert.That(System.Array.IndexOf(wheels, physicsWheel.transform), Is.EqualTo(-1));
    }

    [Test]
    public void CacheWheels_ClassifiesFrontWheelsByHighestLocalZ()
    {
        var frontLeft = CreateWheelChild(vehicleVisual, "Wheel_FL", new Vector3(-1f, 0f, 2f));
        var frontRight = CreateWheelChild(vehicleVisual, "Wheel_FR", new Vector3(1f, 0f, 2f));
        var rearLeft = CreateWheelChild(vehicleVisual, "Wheel_RL", new Vector3(-1f, 0f, -2f));
        var rearRight = CreateWheelChild(vehicleVisual, "Wheel_RR", new Vector3(1f, 0f, -2f));

        InvokePrivateMethod("CacheWheels");

        var wheels = GetPrivateField<Transform[]>(selector, "wheelTransforms");
        var isFront = GetPrivateField<bool[]>(selector, "wheelIsFront");

        for (int i = 0; i < wheels.Length; i++)
        {
            bool expectedFront = wheels[i] == frontLeft.transform || wheels[i] == frontRight.transform;
            Assert.That(isFront[i], Is.EqualTo(expectedFront), $"Wheel '{wheels[i].name}' front classification mismatch.");
        }
        Assert.That(rearLeft, Is.Not.Null);
        Assert.That(rearRight, Is.Not.Null);
    }

    [Test]
    public void CacheWheels_ClassifiesFrontByBoundsCenterWhenLocalPositionsMatch()
    {
        // Mimics FBX packs that leave every sub-mesh at the same localPosition and bake the
        // real front/rear offset into vertex data. Classification must use bounds.center, and
        // must pick the horizontal axis with the larger spread (X here, Z is identical).
        var front = CreateWheelChildWithVertexOffset(vehicleVisual, "Wheel_Front", Vector3.zero, new Vector3(2f, 0f, 0f));
        var rear = CreateWheelChildWithVertexOffset(vehicleVisual, "Wheel_Back", Vector3.zero, new Vector3(-2f, 0f, 0f));

        InvokePrivateMethod("CacheWheels");

        var wheels = GetPrivateField<Transform[]>(selector, "wheelTransforms");
        var isFront = GetPrivateField<bool[]>(selector, "wheelIsFront");
        var canSteer = GetPrivateField<bool[]>(selector, "wheelCanSteer");

        Assert.That(wheels.Length, Is.EqualTo(2));
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == front.transform)
            {
                Assert.That(isFront[i], Is.True, "Bounds-centered +X wheel should classify as front.");
                // Combined-style meshes that sit on the midline (lateral ≈ 0) are not steerable.
                Assert.That(canSteer[i], Is.False);
            }
            else if (wheels[i] == rear.transform)
            {
                Assert.That(isFront[i], Is.False, "Bounds-centered -X wheel should classify as rear.");
                Assert.That(canSteer[i], Is.False);
            }
            else
            {
                Assert.Fail($"Unexpected wheel '{wheels[i].name}'.");
            }
        }
    }

    [Test]
    public void WheelSpinDegreesPerSecond_ScalesWithSpeedAndInverselyWithRadius()
    {
        Assert.That(VehicleSelector.WheelSpinDegreesPerSecond(0f, 0.5f), Is.EqualTo(0f));
        Assert.That(VehicleSelector.WheelSpinDegreesPerSecond(5f, 0f), Is.EqualTo(0f));

        float fullRadius = VehicleSelector.WheelSpinDegreesPerSecond(5f, 0.5f);
        float halfRadius = VehicleSelector.WheelSpinDegreesPerSecond(5f, 0.25f);
        Assert.That(halfRadius, Is.EqualTo(fullRadius * 2f).Within(0.001f));
    }

    [Test]
    public void TickWheelSpin_DoesNothingWhenNotDriving()
    {
        var wheel = CreateWheelChild(vehicleVisual, "Wheel_RL", new Vector3(0f, 0f, -1f));
        InvokePrivateMethod("CacheWheels");

        var before = wheel.transform.localRotation;
        selector.TickWheelSpin(1f, false);

        Assert.That(wheel.transform.localRotation, Is.EqualTo(before));
    }

    [Test]
    public void TickWheelSpin_RotatesWheelProportionalToWorldSpeed()
    {
        var wheel = CreateWheelChild(vehicleVisual, "Wheel_RL", new Vector3(0f, 0f, -1f));
        InvokePrivateMethod("CacheWheels");

        var scroller = groundGameObject.GetComponent<GroundScroller>();
        SetPrivateField(scroller, "worldScrollSpeed", 6f);

        const float deltaTime = 0.1f;
        selector.TickWheelSpin(deltaTime, true);

        // Default WorldMoveDirection is (0,0,-1) with an identity-rotated visual, which
        // ComputeSpinSign maps to a negative spin sign (see VehicleSelector.cs).
        float expectedDegrees = Mathf.Repeat(VehicleSelector.WheelSpinDegreesPerSecond(6f, 0.5f) * deltaTime * -1f, 360f);
        Vector3 euler = wheel.transform.localRotation.eulerAngles;
        Assert.That(euler.x, Is.EqualTo(expectedDegrees).Within(0.01f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(euler.y, 0f)), Is.LessThan(0.01f), "A rear wheel should not steer.");
    }

    [Test]
    public void TickWheelSpin_PreservesBakedImportCorrectionRotation()
    {
        // Some FBX packs (e.g. a Z-up-authored model) bake a fixed correction rotation into
        // every sub-mesh's transform so it renders right-side up under Unity's Y-up convention.
        // CacheWheels must capture that as the wheel's base rotation, and TickWheelSpin must
        // roll on top of it rather than overwriting it outright.
        var wheel = CreateWheelChild(vehicleVisual, "Wheel_RL", new Vector3(0f, 0f, -1f));
        var bakedCorrection = Quaternion.Euler(270f, 0f, 0f);
        wheel.transform.localRotation = bakedCorrection;
        InvokePrivateMethod("CacheWheels");

        var scroller = groundGameObject.GetComponent<GroundScroller>();
        SetPrivateField(scroller, "worldScrollSpeed", 6f);

        const float deltaTime = 0.1f;
        selector.TickWheelSpin(deltaTime, true);

        float expectedDegrees = Mathf.Repeat(VehicleSelector.WheelSpinDegreesPerSecond(6f, 0.5f) * deltaTime * -1f, 360f);
        Quaternion expected = bakedCorrection * Quaternion.AngleAxis(expectedDegrees, Vector3.right);
        Assert.That(Quaternion.Angle(wheel.transform.localRotation, expected), Is.LessThan(0.5f),
            "Roll should compose on top of the baked correction rotation, not replace it.");
    }

    [Test]
    public void TickWheelSpin_FlipsDirectionWithWorldMoveDirection()
    {
        var wheel = CreateWheelChild(vehicleVisual, "Wheel_RL", new Vector3(0f, 0f, -1f));
        InvokePrivateMethod("CacheWheels");

        var scroller = groundGameObject.GetComponent<GroundScroller>();
        SetPrivateField(scroller, "worldScrollSpeed", 6f);

        selector.TickWheelSpin(0.1f, true);
        float forwardDelta = Mathf.DeltaAngle(0f, wheel.transform.localEulerAngles.x);

        wheel.transform.localRotation = Quaternion.identity;
        SetPrivateField(selector, "wheelRollAngles", new float[] { 0f });
        SetPrivateField(scroller, "scrollDirection", new Vector2(0f, 1f));

        selector.TickWheelSpin(0.1f, true);
        float backwardDelta = Mathf.DeltaAngle(0f, wheel.transform.localEulerAngles.x);

        Assert.That(Mathf.Sign(forwardDelta), Is.Not.EqualTo(Mathf.Sign(backwardDelta)));
    }

    [Test]
    public void TickWheelSpin_SteersFrontWheelTowardPlayerSteerInput()
    {
        var frontWheel = CreateWheelChild(vehicleVisual, "Wheel_FL", new Vector3(-1f, 0f, 2f));
        var rearWheel = CreateWheelChild(vehicleVisual, "Wheel_RL", new Vector3(-1f, 0f, -2f));
        InvokePrivateMethod("CacheWheels");

        // Zero the road speed so no roll is baked into the same quaternion as the steer
        // angle - combining rotation about two different axes doesn't decompose back into
        // clean, independent Euler components, so isolating steer requires roll = 0 here.
        var scroller = groundGameObject.GetComponent<GroundScroller>();
        SetPrivateField(scroller, "worldScrollSpeed", 0f);

        var controllerObject = new GameObject("PlayerController");
        var controller = controllerObject.AddComponent<PlayerController>();
        SetPrivateSteerInput(controller, 1f);
        SetPrivateField(selector, "playerController", controller);

        selector.TickWheelSpin(1f, true);   // Large deltaTime so MoveTowards fully reaches the target angle

        float frontSteerY = Mathf.DeltaAngle(0f, frontWheel.transform.localEulerAngles.y);
        float rearSteerY = Mathf.DeltaAngle(0f, rearWheel.transform.localEulerAngles.y);

        Assert.That(frontSteerY, Is.EqualTo(28f).Within(0.5f));
        Assert.That(rearSteerY, Is.EqualTo(0f).Within(0.5f), "A rear wheel should not steer.");

        Object.DestroyImmediate(controllerObject);
    }

    [Test]
    public void Apply_RebuildsWheelCacheWhenVehicleChanges()
    {
        var wheelOnSecondVehicle = CreateWheelChild(vehicleVisual2, "Wheel_RL", new Vector3(0f, 0f, -1f));

        PlayerPrefs.SetInt("VehicleIndex", 0);
        InvokePrivateMethod("Awake");

        var wheelsAtStart = GetPrivateField<Transform[]>(selector, "wheelTransforms");
        Assert.That(wheelsAtStart.Length, Is.EqualTo(0));

        selector.NextVehicle();

        var wheelsAfterSwitch = GetPrivateField<Transform[]>(selector, "wheelTransforms");
        Assert.That(wheelsAfterSwitch.Length, Is.EqualTo(1));
        Assert.That(wheelsAfterSwitch[0], Is.SameAs(wheelOnSecondVehicle.transform));
    }

    private static GameObject CreateWheelChild(GameObject parent, string name, Vector3 localPosition)
    {
        var wheel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wheel.name = name;
        wheel.transform.parent = parent.transform;
        wheel.transform.localPosition = localPosition;
        return wheel;
    }

    // Same localPosition as a sibling, but mesh vertices (and thus Renderer.bounds.center)
    // are shifted so front/rear classification must read geometry rather than the transform.
    private static GameObject CreateWheelChildWithVertexOffset(
        GameObject parent, string name, Vector3 localPosition, Vector3 vertexOffset)
    {
        var wheel = CreateWheelChild(parent, name, localPosition);
        var filter = wheel.GetComponent<MeshFilter>();
        var source = filter.sharedMesh;
        var mesh = Object.Instantiate(source);
        var vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] += vertexOffset;
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
        return wheel;
    }

    private static void SetPrivateSteerInput(PlayerController controller, float value)
    {
        var property = typeof(PlayerController).GetProperty("SteerInput",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(property, "Could not find property 'SteerInput' on PlayerController.");
        property.SetValue(controller, value);
    }

    private static GameObject[] GetPrivateVehicleVisuals(VehicleSelector target)
    {
        var field = typeof(VehicleSelector).GetField("vehicleVisuals",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Could not find private field 'vehicleVisuals' on VehicleSelector.");
        return (GameObject[])field.GetValue(target);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        return (T)field.GetValue(target);
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
