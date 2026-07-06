using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class VehicleExitTests
{
    private GameObject gameManagerObject;
    private GameManager gameManager;

    [SetUp]
    public void SetUp()
    {
        gameManagerObject = new GameObject("GameManager");
        gameManager = gameManagerObject.AddComponent<GameManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameManagerObject);
    }

    [Test]
    public void IsWorldAnimating_TrueWhileVehicleExitingAfterGameOver()
    {
        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", true);
        SetPrivateProperty(gameManager, "IsPaused", false);
        Assert.IsTrue(gameManager.IsWorldAnimating);
    }

    [Test]
    public void IsWorldAnimating_FalseWhenInactiveAndNotExiting()
    {
        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", false);
        SetPrivateProperty(gameManager, "IsPaused", false);
        Assert.IsFalse(gameManager.IsWorldAnimating);
    }

    [Test]
    public void IsWorldAnimating_FalseWhenExitingButPaused()
    {
        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", true);
        SetPrivateProperty(gameManager, "IsPaused", true);
        Assert.IsFalse(gameManager.IsWorldAnimating);
    }

    [Test]
    public void TopAlongTravel_IsBeyondBottomAlongTravelAlongUpScreen()
    {
        var cameraObject = new GameObject("TestCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 16f, 2.5f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, -90f);
        Vector3 moveDirection = Vector3.right;

        float bottom = ScreenEdgeUtility.BottomAlongTravel(camera, 0f, moveDirection);
        float top = ScreenEdgeUtility.TopAlongTravel(camera, 0f, moveDirection);

        Assert.Greater(top, bottom);
        Object.DestroyImmediate(cameraObject);
    }

    [Test]
    public void ExitDriveTick_DoesNotCompleteOnFirstFrameAtDefaultPosition()
    {
        var playerObject = new GameObject("Player");
        var rb = playerObject.AddComponent<Rigidbody>();
        var collider = playerObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(2f, 2f, 4f);
        var controller = playerObject.AddComponent<PlayerController>();

        var cameraObject = new GameObject("GameCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 16f, 2.5f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, -90f);

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(playerObject.transform, false);
        visual.transform.localScale = new Vector3(2f, 1.5f, 4f);

        SetPrivateField(controller, "playerRb", rb);
        SetPrivateField(controller, "playerCollider", collider);
        SetPrivateField(controller, "gameManager", gameManager);
        controller.gameCamera = camera;
        controller.speed = 10f;
        rb.position = new Vector3(-6f, 0.45f, 2.4f);

        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", true);
        SetPrivateProperty(gameManager, "IsPaused", false);

        controller.BeginExitDrive();
        InvokeFixedUpdate(controller);

        Assert.IsTrue((bool)GetPrivateField(controller, "isExiting"));
        Assert.IsTrue(playerObject.activeSelf);

        Object.DestroyImmediate(visual);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(playerObject);
    }

    private static void InvokeFixedUpdate(PlayerController controller)
    {
        typeof(PlayerController).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(controller, null);
    }

    [Test]
    public void BeginExitDrive_SetsExitingAndOpposesDownScreenDirection()
    {
        var playerObject = new GameObject("Player");
        var rb = playerObject.AddComponent<Rigidbody>();
        var collider = playerObject.AddComponent<BoxCollider>();
        var controller = playerObject.AddComponent<PlayerController>();

        var cameraObject = new GameObject("GameCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 16f, 2.5f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, -90f);

        SetPrivateField(controller, "playerRb", rb);
        SetPrivateField(controller, "playerCollider", collider);
        controller.gameCamera = camera;

        controller.BeginExitDrive();

        Assert.IsTrue((bool)GetPrivateField(controller, "isExiting"));
        Vector3 exitDirection = (Vector3)GetPrivateField(controller, "exitDirection");
        Assert.That(exitDirection, Is.EqualTo(Vector3.right).Using(Vector3EqualityComparer.Instance));
        Assert.That(Vector3.Dot(exitDirection, Vector3.left), Is.LessThan(0f));

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void Update_KeepsDirtEmittersPlayingWhileVehicleExiting()
    {
        var groundObject = new GameObject("GroundScroller");
        groundObject.AddComponent<GroundScroller>();

        var selectorObject = new GameObject("VehicleSelector");
        var selector = selectorObject.AddComponent<VehicleSelector>();
        selectorObject.AddComponent<BoxCollider>();

        var emitterObject = new GameObject("DirtEmitter");
        emitterObject.transform.parent = selectorObject.transform;
        var emitter = emitterObject.AddComponent<ParticleSystem>();
        emitter.Stop();

        SetPrivateField(selector, "gameManager", gameManager);
        SetPrivateField(selector, "dirtEmitters", new ParticleSystem[] { emitter });
        SetPrivateField(selector, "vehicleVisuals", new GameObject[0]);

        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", true);
        SetPrivateProperty(gameManager, "IsPaused", false);

        typeof(VehicleSelector).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(selector, null);

        Assert.IsTrue(emitter.isPlaying);

        Object.DestroyImmediate(groundObject);
        Object.DestroyImmediate(selectorObject);
    }

    [Test]
    public void MoveDown_MovesWhileVehicleExitingWhenGameInactive()
    {
        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", true);
        SetPrivateProperty(gameManager, "IsPaused", false);

        var cameraObject = new GameObject("GameCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 16f, 2.5f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, -90f);

        var rockObject = new GameObject("Rock");
        var rb = rockObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        var mover = rockObject.AddComponent<MoveDown>();
        mover.gameCamera = camera;
        mover.speed = 5f;

        SetPrivateField(mover, "gameManager", gameManager);
        SetPrivateField(mover, "objectRb", rb);
        SetPrivateField(mover, "moveDirection", Vector3.right);
        SetPrivateField(mover, "bottomThreshold", 1000f);
        SetPrivateField(mover, "minZ", -50f);
        SetPrivateField(mover, "maxZ", 50f);

        Vector3 startPos = new Vector3(0f, 0.6f, 0f);
        rb.position = startPos;

        InvokeFixedUpdate(mover);

        Assert.Greater(Vector3.Distance(rb.position, startPos), 0.01f);

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(rockObject);
    }

    [Test]
    public void MoveDown_DoesNotMoveWhenWorldNotAnimating()
    {
        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", false);
        SetPrivateProperty(gameManager, "IsPaused", false);

        var cameraObject = new GameObject("GameCamera");
        var camera = cameraObject.AddComponent<Camera>();

        var rockObject = new GameObject("Rock");
        var rb = rockObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        var mover = rockObject.AddComponent<MoveDown>();
        mover.gameCamera = camera;
        mover.speed = 5f;

        SetPrivateField(mover, "gameManager", gameManager);
        SetPrivateField(mover, "objectRb", rb);
        SetPrivateField(mover, "moveDirection", Vector3.right);
        SetPrivateField(mover, "bottomThreshold", 1000f);
        SetPrivateField(mover, "minZ", -50f);
        SetPrivateField(mover, "maxZ", 50f);

        Vector3 startPos = new Vector3(0f, 0.6f, 0f);
        rb.position = startPos;

        InvokeFixedUpdate(mover);

        Assert.That(rb.position, Is.EqualTo(startPos).Using(Vector3EqualityComparer.Instance));

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(rockObject);
    }

    private static void InvokeFixedUpdate(MoveDown mover)
    {
        typeof(MoveDown).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(mover, null);
    }

    private static void SetPrivateProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(property, $"Could not find property '{propertyName}' on {target.GetType()}.");
        property.SetValue(target, value);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        return field.GetValue(target);
    }
}
