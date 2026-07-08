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
    public void ActiveRockCount_ResetsOnSubsystemRegistration()
    {
        // ActiveRockCount is a static counter shared by every test in the run; reset it
        // first so this test's expected increments don't depend on execution order.
        typeof(MoveDown).GetMethod("ResetActiveRockCount", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, null);

        var rock = new GameObject("Rock");
        rock.AddComponent<Rigidbody>().useGravity = false;
        rock.AddComponent<MoveDown>();
        Assert.That(MoveDown.ActiveRockCount, Is.EqualTo(1));

        Object.DestroyImmediate(rock);
        Assert.That(MoveDown.ActiveRockCount, Is.EqualTo(0));

        rock = new GameObject("Rock2");
        rock.AddComponent<Rigidbody>().useGravity = false;
        rock.AddComponent<MoveDown>();
        Assert.That(MoveDown.ActiveRockCount, Is.EqualTo(1));

        typeof(MoveDown).GetMethod("ResetActiveRockCount", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, null);
        Assert.That(MoveDown.ActiveRockCount, Is.EqualTo(0));

        Object.DestroyImmediate(rock);
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
    public void BeginExitDrive_DoesNotThrowWhenCameraUnresolved()
    {
        var playerObject = new GameObject("Player");
        var rb = playerObject.AddComponent<Rigidbody>();
        var collider = playerObject.AddComponent<BoxCollider>();
        var controller = playerObject.AddComponent<PlayerController>();

        SetPrivateField(controller, "playerRb", rb);
        SetPrivateField(controller, "playerCollider", collider);
        controller.gameCamera = null;

        Assert.DoesNotThrow(() => controller.BeginExitDrive());
        Assert.IsTrue((bool)GetPrivateField(controller, "isExiting"));

        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void GetExitBounds_IgnoresParticleRenderers()
    {
        var playerObject = new GameObject("Player");
        var rb = playerObject.AddComponent<Rigidbody>();
        var collider = playerObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(2f, 2f, 4f);
        var controller = playerObject.AddComponent<PlayerController>();

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(playerObject.transform, false);
        visual.transform.localScale = new Vector3(2f, 1.5f, 4f);

        var emitterObject = new GameObject("DirtEmitter");
        emitterObject.transform.SetParent(playerObject.transform, false);
        emitterObject.transform.localPosition = new Vector3(0f, 0f, -20f);
        emitterObject.AddComponent<ParticleSystem>();

        SetPrivateField(controller, "playerRb", rb);
        SetPrivateField(controller, "playerCollider", collider);
        rb.position = new Vector3(0f, 0.5f, 0f);

        var bounds = InvokeGetExitBounds(controller);

        Assert.That(bounds.size.z, Is.LessThan(10f),
            "Exit bounds should come from the vehicle mesh, not distant particle emitters.");

        Object.DestroyImmediate(emitterObject);
        Object.DestroyImmediate(visual);
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void GetExitBounds_FallsBackToColliderWhenNoMeshRenderers()
    {
        var playerObject = new GameObject("Player");
        var rb = playerObject.AddComponent<Rigidbody>();
        var collider = playerObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(2f, 2f, 4f);
        var controller = playerObject.AddComponent<PlayerController>();

        SetPrivateField(controller, "playerRb", rb);
        SetPrivateField(controller, "playerCollider", collider);
        rb.position = new Vector3(1f, 0.5f, 2f);

        var bounds = InvokeGetExitBounds(controller);

        Assert.That(bounds.center, Is.EqualTo(collider.bounds.center).Using(Vector3EqualityComparer.Instance));
        Assert.That(bounds.size, Is.EqualTo(collider.bounds.size).Using(Vector3EqualityComparer.Instance));

        Object.DestroyImmediate(playerObject);
    }

    private static Bounds InvokeGetExitBounds(PlayerController controller)
    {
        return (Bounds)typeof(PlayerController).GetMethod("GetExitBounds", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
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
    public void BeginExitDrive_ViaBottom_OpposesUpScreenDirection()
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

        controller.BeginExitDrive(true);

        Assert.IsTrue((bool)GetPrivateField(controller, "isExiting"));
        Vector3 exitDirection = (Vector3)GetPrivateField(controller, "exitDirection");
        Assert.That(exitDirection, Is.EqualTo(Vector3.left).Using(Vector3EqualityComparer.Instance),
            "A bottom exit should head the opposite way from the default top exit (Vector3.right in this camera rig).");

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void ExitDriveTick_ViaBottom_CompletesAgainstBottomThresholdNotTop()
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

        SetPrivateField(controller, "playerRb", rb);
        SetPrivateField(controller, "playerCollider", collider);
        SetPrivateField(controller, "gameManager", gameManager);
        controller.gameCamera = camera;
        controller.speed = 10f;
        // Bottom exit heads toward Vector3.left in this rig; place the vehicle well past that edge.
        rb.position = new Vector3(-50f, 0.45f, 2.4f);

        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", true);
        SetPrivateProperty(gameManager, "IsPaused", false);

        controller.BeginExitDrive(true);
        SetPrivateField(controller, "exitStartTime", Time.time - 1000f);
        InvokeFixedUpdate(controller);

        Assert.IsFalse((bool)GetPrivateField(controller, "isExiting"),
            "Once past the bottom edge, a bottom exit should complete just like a top exit does at the top edge.");
        Assert.IsFalse(playerObject.activeSelf);

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

    [Test]
    public void MoveDown_SyncsSpeedToGroundScrollerLikeTrees()
    {
        SetPrivateProperty(gameManager, "IsGameActive", true);
        SetPrivateProperty(gameManager, "IsPaused", false);

        var groundObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var scroller = groundObject.AddComponent<GroundScroller>();

        var cameraObject = new GameObject("GameCamera");
        var camera = cameraObject.AddComponent<Camera>();

        var rockObject = new GameObject("Rock");
        var rb = rockObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        var mover = rockObject.AddComponent<MoveDown>();
        mover.gameCamera = camera;
        mover.speed = 999f;  // Stale value; the per-tick refresh must overwrite it.

        SetPrivateField(mover, "gameManager", gameManager);
        SetPrivateField(mover, "groundScroller", scroller);
        SetPrivateField(mover, "objectRb", rb);
        SetPrivateField(mover, "moveDirection", Vector3.right);
        SetPrivateField(mover, "bottomThreshold", 1000f);
        SetPrivateField(mover, "minZ", -50f);
        SetPrivateField(mover, "maxZ", 50f);

        InvokeFixedUpdate(mover);

        // Rocks must travel at the same world speed as the ground plane and the
        // decorative trees: GroundScroller.PropScrollSpeed with the shared multiplier.
        float multiplier = (float)GetPrivateField(mover, "groundSpeedMultiplier");
        Assert.That(mover.speed, Is.EqualTo(scroller.PropScrollSpeed(multiplier)).Within(0.001f));

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(rockObject);
        Object.DestroyImmediate(groundObject);
    }

    [Test]
    public void MoveDown_KeepsManualSpeedWithoutGroundScroller()
    {
        SetPrivateProperty(gameManager, "IsGameActive", true);
        SetPrivateProperty(gameManager, "IsPaused", false);

        var cameraObject = new GameObject("GameCamera");
        var camera = cameraObject.AddComponent<Camera>();

        var rockObject = new GameObject("Rock");
        var rb = rockObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        var mover = rockObject.AddComponent<MoveDown>();
        mover.gameCamera = camera;
        mover.speed = 7.5f;

        SetPrivateField(mover, "gameManager", gameManager);
        SetPrivateField(mover, "objectRb", rb);
        SetPrivateField(mover, "moveDirection", Vector3.right);
        SetPrivateField(mover, "bottomThreshold", 1000f);
        SetPrivateField(mover, "minZ", -50f);
        SetPrivateField(mover, "maxZ", 50f);

        InvokeFixedUpdate(mover);

        Assert.That(mover.speed, Is.EqualTo(7.5f).Within(0.001f),
            "Without a GroundScroller (tests, minimal scenes) the assigned speed must stick.");

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(rockObject);
    }

    [Test]
    public void MoveDown_AwardsNearMissInsideLateralGap()
    {
        var nearMissManagerObject = new GameObject("NearMissGameManager");
        var nearMissManager = nearMissManagerObject.AddComponent<GameManager>();
        SetPrivateProperty(nearMissManager, "IsGameActive", true);
        SetPrivateProperty(nearMissManager, "IsPaused", false);
        SetPrivateField(nearMissManager, "timeRemaining", 10f);
        SetPrivateField(nearMissManager, "lastNearMissTime", -10f);

        var playerObject = new GameObject("Player");
        playerObject.transform.position = new Vector3(0f, 0f, 0f);

        var rockObject = new GameObject("Rock");
        var rb = rockObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        var mover = rockObject.AddComponent<MoveDown>();
        var cameraObject = new GameObject("Camera");
        mover.gameCamera = cameraObject.AddComponent<Camera>();
        SetPrivateField(mover, "gameManager", nearMissManager);
        SetPrivateField(mover, "objectRb", rb);
        SetPrivateField(mover, "moveDirection", Vector3.right);
        SetPrivateField(mover, "playerTransform", playerObject.transform);
        SetPrivateField(mover, "nearMissDistance", 2f);
        SetPrivateField(mover, "nearMissChecked", false);
        SetPrivateField(mover, "hitPlayer", false);
        rb.position = new Vector3(2f, 0f, 1f);

        typeof(MoveDown).GetMethod("TryAwardNearMiss", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(mover, null);

        Assert.That((float)GetPrivateField(nearMissManager, "timeRemaining"), Is.EqualTo(12f).Within(0.001f));

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(rockObject);
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(nearMissManagerObject);
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
