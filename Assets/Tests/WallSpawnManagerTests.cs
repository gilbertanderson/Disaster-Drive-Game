using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WallSpawnManagerTests
{
    [Test]
    public void OnSegmentLeftScreen_SnapsAheadOfLeadSegmentInSameLane()
    {
        const float spacing = 3f;
        const float laneZ = 11.5f;

        var grassObject = new GameObject("Grass");
        var grassScroller = grassObject.AddComponent<GroundScroller>();

        var spawnObject = new GameObject("WallSpawnManager");
        var spawnManager = spawnObject.AddComponent<WallSpawnManager>();
        SetPrivateField(spawnManager, "grassScroller", grassScroller);
        SetPrivateField(spawnManager, "segmentSpacing", spacing);
        SetPrivateField(spawnManager, "spacingMeasured", true);
        SetPrivateField(spawnManager, "segmentY", 0.05f);

        var leadObject = new GameObject("LeadSegment");
        leadObject.transform.position = new Vector3(20f, 0.05f, laneZ);
        var leadScroller = leadObject.AddComponent<WallScroller>();
        leadScroller.LaneZ = laneZ;

        var trailingObject = new GameObject("TrailingSegment");
        trailingObject.transform.position = new Vector3(5f, 0.05f, laneZ);
        var trailingScroller = trailingObject.AddComponent<WallScroller>();
        trailingScroller.LaneZ = laneZ;

        spawnManager.OnSegmentLeftScreen(trailingScroller);

        float expectedX = 20f + spacing;
        Assert.That(trailingObject.transform.position.x, Is.EqualTo(expectedX).Within(0.01f));
        Assert.That(trailingObject.transform.position.z, Is.EqualTo(laneZ).Within(0.01f));

        Object.DestroyImmediate(leadObject);
        Object.DestroyImmediate(trailingObject);
        Object.DestroyImmediate(spawnObject);
        Object.DestroyImmediate(grassObject);
    }

    [Test]
    public void OnSegmentLeftScreen_IgnoresSegmentsInOtherLanes()
    {
        const float spacing = 3f;
        const float laneZ = 11.5f;
        const float otherLaneZ = -7.9f;

        var grassObject = new GameObject("Grass");
        var grassScroller = grassObject.AddComponent<GroundScroller>();

        var spawnObject = new GameObject("WallSpawnManager");
        var spawnManager = spawnObject.AddComponent<WallSpawnManager>();
        SetPrivateField(spawnManager, "grassScroller", grassScroller);
        SetPrivateField(spawnManager, "segmentSpacing", spacing);
        SetPrivateField(spawnManager, "spacingMeasured", true);
        SetPrivateField(spawnManager, "segmentY", 0.05f);
        SetPrivateField(spawnManager, "spawnX", 12f);

        var otherLaneObject = new GameObject("OtherLaneSegment");
        otherLaneObject.transform.position = new Vector3(30f, 0.05f, otherLaneZ);
        var otherLaneScroller = otherLaneObject.AddComponent<WallScroller>();
        otherLaneScroller.LaneZ = otherLaneZ;

        var trailingObject = new GameObject("TrailingSegment");
        trailingObject.transform.position = new Vector3(5f, 0.05f, laneZ);
        var trailingScroller = trailingObject.AddComponent<WallScroller>();
        trailingScroller.LaneZ = laneZ;

        spawnManager.OnSegmentLeftScreen(trailingScroller);

        Assert.That(trailingObject.transform.position.x, Is.EqualTo(12f).Within(0.01f));

        Object.DestroyImmediate(otherLaneObject);
        Object.DestroyImmediate(trailingObject);
        Object.DestroyImmediate(spawnObject);
        Object.DestroyImmediate(grassObject);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }
}
