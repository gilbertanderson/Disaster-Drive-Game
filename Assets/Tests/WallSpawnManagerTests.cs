using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WallSpawnManagerTests
{
    [Test]
    public void OnSegmentLeftScreen_SnapsDirectlyBehindPartnerTile()
    {
        const float laneZ = 11.5f;
        const float tileLength = 6f;

        var grassObject = CreateGrassObject();
        var grassScroller = grassObject.AddComponent<GroundScroller>();
        SetPrivateField(grassScroller, "scrollDirection", new Vector2(-1f, 0f)); // world -X

        var spawnObject = new GameObject("WallSpawnManager");
        var spawnManager = spawnObject.AddComponent<WallSpawnManager>();
        SetPrivateField(spawnManager, "grassScroller", grassScroller);

        var partnerObject = new GameObject("PartnerTile");
        partnerObject.transform.position = new Vector3(20f, 0.05f, laneZ);
        var partnerScroller = partnerObject.AddComponent<WallScroller>();
        partnerScroller.LaneZ = laneZ;

        var exitingObject = new GameObject("ExitingTile");
        exitingObject.transform.position = new Vector3(5f, 0.05f, laneZ);
        var exitingScroller = exitingObject.AddComponent<WallScroller>();
        exitingScroller.LaneZ = laneZ;
        SetPrivateField(exitingScroller, "tileLength", tileLength);

        SetLanePairs(spawnManager, (exitingScroller, partnerScroller));

        spawnManager.OnSegmentLeftScreen(exitingScroller);

        float expectedX = 20f + tileLength;
        Assert.That(exitingObject.transform.position.x, Is.EqualTo(expectedX).Within(0.01f));
        Assert.That(exitingObject.transform.position.z, Is.EqualTo(laneZ).Within(0.01f));

        Object.DestroyImmediate(partnerObject);
        Object.DestroyImmediate(exitingObject);
        Object.DestroyImmediate(spawnObject);
        Object.DestroyImmediate(grassObject);
    }

    [Test]
    public void OnSegmentLeftScreen_DoesNothingWhenTileHasNoRegisteredPartner()
    {
        var grassObject = CreateGrassObject();
        var grassScroller = grassObject.AddComponent<GroundScroller>();
        SetPrivateField(grassScroller, "scrollDirection", new Vector2(-1f, 0f));

        var spawnObject = new GameObject("WallSpawnManager");
        var spawnManager = spawnObject.AddComponent<WallSpawnManager>();
        SetPrivateField(spawnManager, "grassScroller", grassScroller);
        // No lane pairs registered -- simulates a stray tile that isn't part of any lane.

        var loneObject = new GameObject("LoneTile");
        loneObject.transform.position = new Vector3(5f, 0.05f, 11.5f);
        var loneScroller = loneObject.AddComponent<WallScroller>();
        loneScroller.LaneZ = 11.5f;

        spawnManager.OnSegmentLeftScreen(loneScroller);

        Assert.That(loneObject.transform.position.x, Is.EqualTo(5f).Within(0.01f));

        Object.DestroyImmediate(loneObject);
        Object.DestroyImmediate(spawnObject);
        Object.DestroyImmediate(grassObject);
    }

    [Test]
    public void OnSegmentLeftScreen_DoesNothingWhenWorldIsNotAnimating()
    {
        const float laneZ = 11.5f;
        const float tileLength = 6f;

        var grassObject = CreateGrassObject();
        var grassScroller = grassObject.AddComponent<GroundScroller>();
        SetPrivateField(grassScroller, "scrollDirection", new Vector2(-1f, 0f));

        var spawnObject = new GameObject("WallSpawnManager");
        var spawnManager = spawnObject.AddComponent<WallSpawnManager>();
        SetPrivateField(spawnManager, "grassScroller", grassScroller);

        var managerObject = new GameObject("GameManager");
        var gameManager = managerObject.AddComponent<GameManager>();
        SetPrivateField(spawnManager, "gameManager", gameManager);
        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", false);
        SetPrivateProperty(gameManager, "IsPaused", false);

        var partnerObject = new GameObject("PartnerTile");
        partnerObject.transform.position = new Vector3(20f, 0.05f, laneZ);
        var partnerScroller = partnerObject.AddComponent<WallScroller>();

        var exitingObject = new GameObject("ExitingTile");
        exitingObject.transform.position = new Vector3(5f, 0.05f, laneZ);
        var exitingScroller = exitingObject.AddComponent<WallScroller>();
        SetPrivateField(exitingScroller, "tileLength", tileLength);

        SetLanePairs(spawnManager, (exitingScroller, partnerScroller));

        spawnManager.OnSegmentLeftScreen(exitingScroller);

        Assert.That(exitingObject.transform.position.x, Is.EqualTo(5f).Within(0.01f));

        Object.DestroyImmediate(partnerObject);
        Object.DestroyImmediate(exitingObject);
        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(spawnObject);
        Object.DestroyImmediate(grassObject);
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

    private static void SetLanePairs(WallSpawnManager manager, params (WallScroller, WallScroller)[] pairs)
    {
        var field = typeof(WallSpawnManager).GetField("lanePairs", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Could not find private field 'lanePairs' on WallSpawnManager.");
        var list = new List<(WallScroller a, WallScroller b)>(pairs);
        field.SetValue(manager, list);
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
