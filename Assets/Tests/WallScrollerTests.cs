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
        var segment = new GameObject("FenceSegment");
        segment.transform.position = new Vector3(5f, 0f, -11.4f);

        var grassObject = new GameObject("Grass");
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
        var segment = new GameObject("FenceSegment");
        segment.transform.position = new Vector3(5f, 0f, 16.3f);

        var grassObject = new GameObject("Grass");
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
