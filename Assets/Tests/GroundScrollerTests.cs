using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GroundScrollerTests
{
    // Accessing renderer.material (not sharedMaterial) instantiates a copy the first time,
    // and Unity logs an Error-level message about it in edit mode; both tests trigger this
    // once (via Start() below), so it must be whitelisted or the run fails on the log.
    private static void ExpectMaterialInstantiationWarning()
    {
        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex("Instantiating material.*"));
    }

    private static void InvokeStart(GroundScroller scroller)
    {
        typeof(GroundScroller).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(scroller, null);
    }

    [UnityTest]
    public IEnumerator Update_ScrollsWhileWorldAnimating()
    {
        var groundObject = new GameObject("Ground");
        var renderer = groundObject.AddComponent<MeshRenderer>();
        var meshFilter = groundObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        var scroller = groundObject.AddComponent<GroundScroller>();
        var managerObject = new GameObject("GameManager");
        var gameManager = managerObject.AddComponent<GameManager>();

        SetPrivateField(scroller, "gameManager", gameManager);
        SetPrivateProperty(gameManager, "IsGameActive", true);
        SetPrivateProperty(gameManager, "IsVehicleExiting", false);
        SetPrivateProperty(gameManager, "IsPaused", false);

        // Start() caches the renderer's material and must run before Update(); calling
        // Update() alone (as before) leaves groundMaterial null and throws.
        ExpectMaterialInstantiationWarning();
        InvokeStart(scroller);

        // Let a real editor frame elapse so Time.deltaTime is nonzero; invoking Update()
        // in the same frame Start() ran can see a zero delta and produce no scroll.
        yield return null;

        typeof(GroundScroller).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(scroller, null);

        var material = renderer.material;
        bool hasOffset = material.HasProperty("_BaseMap")
            ? material.GetTextureOffset("_BaseMap") != Vector2.zero
            : material.mainTextureOffset != Vector2.zero;
        Assert.IsTrue(hasOffset);

        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(groundObject);
    }

    [Test]
    public void Update_DoesNotScrollWhenWorldIsInactive()
    {
        var groundObject = new GameObject("Ground");
        var renderer = groundObject.AddComponent<MeshRenderer>();
        var meshFilter = groundObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        var scroller = groundObject.AddComponent<GroundScroller>();
        var managerObject = new GameObject("GameManager");
        var gameManager = managerObject.AddComponent<GameManager>();

        SetPrivateField(scroller, "gameManager", gameManager);
        SetPrivateProperty(gameManager, "IsGameActive", false);
        SetPrivateProperty(gameManager, "IsVehicleExiting", false);
        SetPrivateProperty(gameManager, "IsPaused", false);

        ExpectMaterialInstantiationWarning();
        InvokeStart(scroller);

        typeof(GroundScroller).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(scroller, null);

        var material = renderer.material;
        Vector2 offset = material.HasProperty("_BaseMap")
            ? material.GetTextureOffset("_BaseMap")
            : material.mainTextureOffset;
        Assert.That(offset, Is.EqualTo(Vector2.zero));

        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(groundObject);
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
