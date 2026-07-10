using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WallScrollerTests
{
    [UnityTest]
    public IEnumerator Update_MovesWhileWorldAnimating()
    {
        var segment = new GameObject("WallTile");
        segment.transform.position = new Vector3(5f, 0f, -11.4f);

        var grassObject = CreateGrassObject();
        var grassScroller = grassObject.AddComponent<GroundScroller>();

        var scroller = segment.AddComponent<WallScroller>();
        scroller.Configure(grassScroller);

        var managerObject = new GameObject("GameManager");
        var gameManager = managerObject.AddComponent<GameManager>();
        SetPrivateField(scroller, "gameManager", gameManager);
        SetPrivateProperty(gameManager, "IsGameActive", true);
        SetPrivateProperty(gameManager, "IsVehicleExiting", false);
        SetPrivateProperty(gameManager, "IsPaused", false);

        // Let a real editor frame elapse so Time.deltaTime is nonzero; invoking Update()
        // in the same frame everything was set up can see a zero delta and never move.
        yield return null;

        Vector3 start = segment.transform.position;
        typeof(WallScroller).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(scroller, null);

        Assert.That(segment.transform.position, Is.Not.EqualTo(start));

        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(grassObject);
        Object.DestroyImmediate(segment);
    }

    [Test]
    public void Update_DoesNotMoveWhenWorldIsInactive()
    {
        var segment = new GameObject("WallTile");
        segment.transform.position = new Vector3(5f, 0f, 16.3f);

        var grassObject = CreateGrassObject();
        var grassScroller = grassObject.AddComponent<GroundScroller>();

        var scroller = segment.AddComponent<WallScroller>();
        scroller.Configure(grassScroller);

        var managerObject = new GameObject("GameManager");
        var gameManager = managerObject.AddComponent<GameManager>();
        SetPrivateField(scroller, "gameManager", gameManager);
        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", false);
        SetPrivateProperty(gameManager, "IsPaused", false);

        Vector3 start = segment.transform.position;
        typeof(WallScroller).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(scroller, null);

        Assert.That(segment.transform.position, Is.EqualTo(start));

        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(grassObject);
        Object.DestroyImmediate(segment);
    }

    [Test]
    public void TileLength_ReflectsAuthoredValue()
    {
        var segment = new GameObject("WallTile");
        var scroller = segment.AddComponent<WallScroller>();
        SetPrivateField(scroller, "tileLength", 32.78541f);

        Assert.That(scroller.TileLength, Is.EqualTo(32.78541f).Within(0.001f));

        Object.DestroyImmediate(segment);
    }

    [Test]
    public void Update_RecycleThresholdIgnoresTileLength_UsesPivotOnly()
    {
        // The tile's pivot represents its LEADING (spawn-side) edge, which is also the
        // last part of the tile to clear the screen. Recycling should therefore trigger
        // off the pivot alone (same threshold as a short segment), regardless of how
        // long the authored tile is -- confirms no per-tile-length fudge factor crept
        // into the bottom-threshold math.
        var segment = new GameObject("WallTile");
        segment.transform.position = new Vector3(-20f, 0f, 11.5f); // already well past bottom-of-screen

        var grassObject = CreateGrassObject();
        var grassScroller = grassObject.AddComponent<GroundScroller>();
        SetPrivateField(grassScroller, "scrollDirection", new Vector2(-1f, 0f)); // world -X, matching the scene's real travel axis

        var cameraObject = new GameObject("Cam");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 16f, 2.5f);
        camera.transform.eulerAngles = new Vector3(90f, 90f, 0f);

        var scroller = segment.AddComponent<WallScroller>();
        SetPrivateField(scroller, "tileLength", 32.78541f);
        scroller.gameCamera = camera;
        scroller.Configure(grassScroller);

        var spawnManagerObject = new GameObject("WallSpawnManager");
        var spawnManager = spawnManagerObject.AddComponent<WallSpawnManager>();
        SetPrivateField(spawnManager, "grassScroller", grassScroller);

        var partnerObject = new GameObject("PartnerTile");
        partnerObject.transform.position = new Vector3(20f, 0f, 11.5f);
        var partnerScroller = partnerObject.AddComponent<WallScroller>();
        SetPrivateField(partnerScroller, "tileLength", 32.78541f);

        var pairs = new System.Collections.Generic.List<(WallScroller, WallScroller)> { (scroller, partnerScroller) };
        SetPrivateField(spawnManager, "lanePairs", pairs);

        SetPrivateField(scroller, "spawnManager", spawnManager);
        typeof(WallScroller).GetMethod("UpdateBottomThreshold", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(scroller, null);

        typeof(WallScroller).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(scroller, null);
        typeof(WallScroller).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(scroller, null);

        // Recycled: should now sit directly behind (ahead of, along -moveDir) the partner tile.
        float expectedX = partnerObject.transform.position.x + 32.78541f;
        Assert.That(segment.transform.position.x, Is.EqualTo(expectedX).Within(0.01f));

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(partnerObject);
        Object.DestroyImmediate(spawnManagerObject);
        Object.DestroyImmediate(grassObject);
        Object.DestroyImmediate(segment);
    }

    [Test]
    public void Recycle_WaitsForLateUpdate_SoPartnerPositionIsFinalForTheFrame()
    {
        // Leapfrog placement reads the partner tile's live position, but Update order
        // between the two tiles in a lane is unspecified. Recycling during Update could
        // therefore run before the partner's own move for the frame, placing the tile
        // against a stale position and leaving a one-frame-of-travel gap at the seam.
        // Recycling belongs in LateUpdate, after every tile has moved.
        var segment = new GameObject("WallTile");
        segment.transform.position = new Vector3(-20f, 0f, 11.5f); // already past bottom-of-screen

        var grassObject = CreateGrassObject();
        var grassScroller = grassObject.AddComponent<GroundScroller>();
        SetPrivateField(grassScroller, "scrollDirection", new Vector2(-1f, 0f));

        var cameraObject = new GameObject("Cam");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 16f, 2.5f);
        camera.transform.eulerAngles = new Vector3(90f, 90f, 0f);

        var scroller = segment.AddComponent<WallScroller>();
        SetPrivateField(scroller, "tileLength", 32.78541f);
        scroller.gameCamera = camera;
        scroller.Configure(grassScroller);

        var spawnManagerObject = new GameObject("WallSpawnManager");
        var spawnManager = spawnManagerObject.AddComponent<WallSpawnManager>();
        SetPrivateField(spawnManager, "grassScroller", grassScroller);

        var partnerObject = new GameObject("PartnerTile");
        partnerObject.transform.position = new Vector3(20f, 0f, 11.5f);
        var partnerScroller = partnerObject.AddComponent<WallScroller>();
        SetPrivateField(partnerScroller, "tileLength", 32.78541f);

        var pairs = new System.Collections.Generic.List<(WallScroller, WallScroller)> { (scroller, partnerScroller) };
        SetPrivateField(spawnManager, "lanePairs", pairs);

        SetPrivateField(scroller, "spawnManager", spawnManager);
        typeof(WallScroller).GetMethod("UpdateBottomThreshold", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(scroller, null);

        // Update alone must not recycle, even though the tile is past the threshold.
        typeof(WallScroller).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(scroller, null);
        Assert.That(segment.transform.position.x, Is.LessThan(0f),
            "Tile recycled during Update; recycling must wait for LateUpdate.");

        // The partner's own Update move lands after the exiting tile's Update.
        partnerObject.transform.position += new Vector3(-0.5f, 0f, 0f);

        typeof(WallScroller).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(scroller, null);

        // Seam must be exact against the partner's final position for the frame.
        float expectedX = partnerObject.transform.position.x + 32.78541f;
        Assert.That(segment.transform.position.x, Is.EqualTo(expectedX).Within(0.001f));

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(partnerObject);
        Object.DestroyImmediate(spawnManagerObject);
        Object.DestroyImmediate(grassObject);
        Object.DestroyImmediate(segment);
    }

    // GroundScroller carries [RequireComponent(typeof(Renderer))]; a bare GameObject can't
    // satisfy that (Renderer is abstract), so AddComponent<GroundScroller>() fails unless a
    // concrete renderer already exists.
    private static GameObject CreateGrassObject()
    {
        var grassObject = new GameObject("Grass");
        grassObject.AddComponent<MeshRenderer>();
        var meshFilter = grassObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        return grassObject;
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
}
