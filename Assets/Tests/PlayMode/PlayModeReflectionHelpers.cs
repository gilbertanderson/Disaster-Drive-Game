using System.Reflection;
using NUnit.Framework;

// Reflection helpers shared by the Play Mode suites. The Edit Mode assembly has
// its own copy in TestReflectionHelpers; the two test asmdefs don't reference
// each other, so the helpers can't live in one place.
internal static class PlayModeReflectionHelpers
{
    public static void InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Could not find method '{methodName}' on {target.GetType()}.");
        method.Invoke(target, args);
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
}
