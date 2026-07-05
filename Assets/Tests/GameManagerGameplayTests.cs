using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class GameManagerGameplayTests
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
    public void OnNearMiss_AddsBonusTimeAfterCooldown()
    {
        SetPrivateProperty(gameManager, "IsGameActive", true);
        SetPrivateProperty(gameManager, "IsPaused", false);
        SetPrivateField(gameManager, "timeRemaining", 10f);
        SetPrivateField(gameManager, "lastNearMissTime", -10f);

        gameManager.OnNearMiss();

        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining"), Is.EqualTo(12f).Within(0.001f));
    }

    [Test]
    public void OnNearMiss_IgnoresCallsInsideCooldown()
    {
        SetPrivateProperty(gameManager, "IsGameActive", true);
        SetPrivateProperty(gameManager, "IsPaused", false);
        SetPrivateField(gameManager, "timeRemaining", 10f);
        SetPrivateField(gameManager, "lastNearMissTime", Time.timeSinceLevelLoad);

        gameManager.OnNearMiss();

        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining"), Is.EqualTo(10f).Within(0.001f));
    }

    [Test]
    public void OnRockDodged_IncrementsWhileGameActive()
    {
        SetPrivateProperty(gameManager, "IsGameActive", true);
        SetPrivateProperty(gameManager, "IsPaused", false);
        SetPrivateField(gameManager, "rocksDodged", 0);

        gameManager.OnRockDodged();

        Assert.That(GetPrivateField<int>(gameManager, "rocksDodged"), Is.EqualTo(1));
    }

    [Test]
    public void OnPlayerHit_TracksHitsTaken()
    {
        SetPrivateProperty(gameManager, "IsGameActive", true);
        SetPrivateProperty(gameManager, "IsPaused", false);
        SetPrivateField(gameManager, "hitsTaken", 0);
        SetPrivateField(gameManager, "timeRemaining", 30f);

        gameManager.OnPlayerHit(Vector3.zero);

        Assert.That(GetPrivateField<int>(gameManager, "hitsTaken"), Is.EqualTo(1));
        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining"), Is.EqualTo(25f).Within(0.001f));
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        return (T)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }

    private static void SetPrivateProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(property, $"Could not find property '{propertyName}' on {target.GetType()}.");
        property.SetValue(target, value);
    }
}
