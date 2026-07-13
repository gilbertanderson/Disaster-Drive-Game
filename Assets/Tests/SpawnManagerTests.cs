using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SpawnManagerTests
{
    private GameObject spawnManagerObject;
    private SpawnManager spawnManager;

    [SetUp]
    public void SetUp()
    {
        spawnManagerObject = new GameObject("SpawnManager");
        spawnManager = spawnManagerObject.AddComponent<SpawnManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(spawnManagerObject);
    }

    [Test]
    public void CacheSpawnX_PlacesSpawnLineOffScreenPastTopEdge()
    {
        var cameraObject = new GameObject("MainCamera") { tag = "MainCamera" };
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 16f, 2.5f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, -90f);

        // Camera.main caches the last-resolved main camera and only reliably
        // refreshes on a camera's enable/disable event; tagging the GameObject
        // before the Camera component existed doesn't trigger that, so force a
        // refresh here or CacheSpawnX() below can see a stale/null camera.
        cameraObject.SetActive(false);
        cameraObject.SetActive(true);

        float groundY = 0.06f;
        SetPrivateField(spawnManager, "cachedGroundLevelY", groundY);
        float spawnMargin = (float)GetPrivateField(spawnManager, "spawnMargin");

        InvokePrivate(spawnManager, "CacheSpawnX");

        Vector3 moveDirection = ScreenEdgeUtility.ComputeTravelDirection(camera);
        float topAlongTravel = ScreenEdgeUtility.TopAlongTravel(camera, groundY, moveDirection);
        Vector3 spawnPos = new Vector3(spawnManager.spawnX, groundY, 0f);
        float spawnAlongTravel = Vector3.Dot(spawnPos, moveDirection);

        // The spawn line sits spawnMargin behind the camera's top edge along the
        // travel axis, i.e. fully off-screen before the rock starts moving down.
        Assert.That(spawnAlongTravel, Is.EqualTo(topAlongTravel - spawnMargin).Within(0.01f),
            "Rocks should spawn spawnMargin past the camera's top edge.");

        Vector3 viewport = camera.WorldToViewportPoint(spawnPos);
        bool offScreen = viewport.z < 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f;
        Assert.IsTrue(offScreen,
            $"Spawn position {spawnPos} should project outside the viewport, got {viewport}.");

        Object.DestroyImmediate(cameraObject);
    }

    [Test]
    public void RockSpeedMultiplier_MatchesTreeSpeedMultiplier()
    {
        var treeObject = new GameObject("Tree");
        var tree = treeObject.AddComponent<TreeScroller>();

        float rockMultiplier = (float)GetPrivateField(spawnManager, "rockSpeedMultiplier");
        float treeMultiplier = (float)GetPrivateField(tree, "treeSpeedMultiplier");
        var moverObject = new GameObject("Mover");
        moverObject.AddComponent<Rigidbody>().useGravity = false;
        float moverMultiplier = (float)GetPrivateField(moverObject.AddComponent<MoveDown>(), "groundSpeedMultiplier");

        // Rocks and trees must scroll at the same world speed as the ground plane;
        // all three multipliers feed GroundScroller.PropScrollSpeed and must agree.
        Assert.That(rockMultiplier, Is.EqualTo(treeMultiplier).Within(0.0001f),
            "SpawnManager.rockSpeedMultiplier must match TreeScroller.treeSpeedMultiplier.");
        Assert.That(moverMultiplier, Is.EqualTo(treeMultiplier).Within(0.0001f),
            "MoveDown.groundSpeedMultiplier must match TreeScroller.treeSpeedMultiplier.");

        Object.DestroyImmediate(moverObject);
        Object.DestroyImmediate(treeObject);
    }

    [Test]
    public void FitBoxColliderToVisual_TightWhenShellCarriesTheYaw()
    {
        // Spawn-time layout after the fix: the SHELL is yawed, the visual sits
        // unrotated inside it — the box must hug the mesh, not a world AABB.
        var (rock, visual, collider) = BuildRockWithStretchedCubeVisual();
        rock.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

        InvokePrivate(spawnManager, "FitBoxColliderToVisual", rock, visual);

        AssertTightFit(collider);

        Object.DestroyImmediate(rock);
    }

    [Test]
    public void FitBoxColliderToVisual_TightForUnrotatedShell()
    {
        var (rock, visual, collider) = BuildRockWithStretchedCubeVisual();

        InvokePrivate(spawnManager, "FitBoxColliderToVisual", rock, visual);

        AssertTightFit(collider);

        Object.DestroyImmediate(rock);
    }

    static (GameObject rock, GameObject visual, BoxCollider collider) BuildRockWithStretchedCubeVisual()
    {
        var rock = new GameObject("RockShell");
        var collider = rock.AddComponent<BoxCollider>();

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(rock.transform, false);
        visual.transform.localScale = new Vector3(3f, 1f, 1f);

        return (rock, visual, collider);
    }

    static void AssertTightFit(BoxCollider collider)
    {
        // Mesh is a 3x1x1 cube in shell-local space; the old world-AABB fit
        // inflated X/Z to (3+1)/sqrt(2) = 2.83 when yawed 45 degrees.
        const float padding = 1.03f;  // fit padding upper bound
        Assert.That(collider.size.x, Is.EqualTo(3f).Within(3f * (padding - 1f) + 0.01f),
            "Collider X should hug the mesh, not the rotation-inflated AABB.");
        Assert.That(collider.size.y, Is.EqualTo(1f).Within(padding - 1f + 0.01f));
        Assert.That(collider.size.z, Is.EqualTo(1f).Within(padding - 1f + 0.01f),
            "Collider Z should hug the mesh, not the rotation-inflated AABB.");
        Assert.That(collider.center, Is.EqualTo(Vector3.zero).Using(Vector3EqualityComparer.Instance));
    }

    [Test]
    public void TryDetectPlayerOverlap_NoHitWhenPlayerDrivesCloseBeside()
    {
        var (rock, visual, collider) = BuildRockWithStretchedCubeVisual();
        rock.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        var rb = rock.AddComponent<Rigidbody>();
        rb.useGravity = false;
        var mover = rock.AddComponent<MoveDown>();

        InvokePrivate(spawnManager, "FitBoxColliderToVisual", rock, visual);

        var player = new GameObject("Player") { tag = "Player" };
        try
        {
            var playerCollider = player.AddComponent<BoxCollider>();
            playerCollider.size = Vector3.one;

            SetPrivateField(mover, "objectRb", rb);
            SetPrivateField(mover, "rockCollider", collider);
            SetPrivateField(mover, "playerTransform", player.transform);
            SetPrivateField(mover, "moveDirection", Vector3.right);

            // Park the player beside the rock, just outside the tight box. The player's
            // box is unrotated while the rock is yawed 45 degrees, so its effective
            // reach along the rock's local Z axis is its half-diagonal (~0.71), not its
            // face half-extent (0.5); required separation is ~0.51 + 0.71 = 1.22, so 1.3
            // clears it with margin. The old rotation-inflated box (half-extent ~1.44)
            // registered a hit even here.
            player.transform.position = rock.transform.rotation * new Vector3(0f, 0f, 1.3f);
            Physics.SyncTransforms();

            InvokePrivate(mover, "TryDetectPlayerOverlap");
            Assert.IsFalse((bool)GetPrivateField(mover, "hitPlayer"),
                "Driving close beside a rock must not register a hit.");

            // Move into genuine contact: the hit must still register.
            player.transform.position = rock.transform.rotation * new Vector3(0f, 0f, 0.8f);
            Physics.SyncTransforms();

            InvokePrivate(mover, "TryDetectPlayerOverlap");
            Assert.IsTrue((bool)GetPrivateField(mover, "hitPlayer"),
                "Actual contact with the rock body must still count as a hit.");
        }
        finally
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(rock);
        }
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(method, $"Could not find method '{methodName}' on {target.GetType()}.");
        method.Invoke(target, args);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find field '{fieldName}' on {target.GetType()}.");
        return field.GetValue(target);
    }
}
