using System.Reflection;
using NUnit.Framework;
using UnityEngine;

internal static class TestReflectionHelpers
{
    public static T InvokePrivate<T>(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Could not find method '{methodName}' on {target.GetType()}.");
        return (T)method.Invoke(target, args);
    }

    public static object InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Could not find method '{methodName}' on {target.GetType()}.");
        return method.Invoke(target, args);
    }

    public static object InvokePrivateStatic(System.Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Could not find static method '{methodName}' on {type}.");
        return method.Invoke(null, args);
    }

    public static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        return (T)field.GetValue(target);
    }

    public static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }

    public static void SetPrivateProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(property, $"Could not find property '{propertyName}' on {target.GetType()}.");
        property.SetValue(target, value);
    }

    public static T GetPrivateProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(property, $"Could not find property '{propertyName}' on {target.GetType()}.");
        return (T)property.GetValue(target);
    }

    public static T GetPrivateStaticField<T>(System.Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private static field '{fieldName}' on {type}.");
        return (T)field.GetValue(null);
    }

    public static void SetPrivateStaticField(System.Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private static field '{fieldName}' on {type}.");
        field.SetValue(null, value);
    }

    public static void SetStaticProperty(System.Type type, string propertyName, object value)
    {
        var property = type.GetProperty(propertyName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(property, $"Could not find static property '{propertyName}' on {type}.");
        property.SetValue(null, value);
    }

    public static GameObject CreateGrassObject(Vector3 position = default)
    {
        var grassObject = new GameObject("GrassObject");
        grassObject.transform.position = position;
        grassObject.AddComponent<MeshRenderer>();
        grassObject.AddComponent<MeshFilter>();
        var meshFilter = grassObject.GetComponent<MeshFilter>();
        meshFilter.mesh = Resources.Load<Mesh>("grass") ?? CreateDefaultCubeMesh();
        return grassObject;
    }

    private static Mesh CreateDefaultCubeMesh()
    {
        var mesh = new Mesh { name = "Default Cube" };
        mesh.vertices = new[]
        {
            new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0),
            new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1)
        };
        mesh.triangles = new[]
        {
            0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
            0, 5, 4, 0, 1, 5, 2, 3, 7, 2, 7, 6,
            0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5
        };
        mesh.RecalculateNormals();
        return mesh;
    }
}
