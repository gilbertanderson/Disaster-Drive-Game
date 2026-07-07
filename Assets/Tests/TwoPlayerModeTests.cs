using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

// Covers the competitive two-player mode: per-player clocks, vehicle-vehicle
// collision penalties, elimination order, and duplicate-vehicle prevention.
public class TwoPlayerModeTests
{
    private GameObject gameManagerObject;
    private GameManager gameManager;
    private GameObject player1Object;
    private GameObject player2Object;
    private PlayerController player1;
    private PlayerController player2;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();

        gameManagerObject = new GameObject("GameManager");
        gameManager = gameManagerObject.AddComponent<GameManager>();

        player1Object = new GameObject("Player1Vehicle");
        player1 = player1Object.AddComponent<PlayerController>();
        player1.playerIndex = 0;

        player2Object = new GameObject("Player2Vehicle");
        player2 = player2Object.AddComponent<PlayerController>();
        player2.playerIndex = 1;

        SetPrivateField(gameManager, "players", new[] { player1, player2 });
        SetPrivateProperty(gameManager, "IsGameActive", true);
        SetPrivateProperty(gameManager, "IsTwoPlayerMode", true);
        SetPrivateField(gameManager, "timeRemaining", 30f);
        SetPrivateField(gameManager, "timeRemaining2", 30f);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
        Object.DestroyImmediate(gameManagerObject);
        Object.DestroyImmediate(player1Object);
        Object.DestroyImmediate(player2Object);
    }

    [Test]
    public void OnPlayerHit_ChargesOnlyTheHitPlayersClock()
    {
        gameManager.OnPlayerHit(Vector3.zero, player2);

        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining"), Is.EqualTo(30f).Within(0.001f),
            "Player 1's clock should be untouched when Player 2 hits a rock.");
        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining2"), Is.EqualTo(25f).Within(0.001f));
    }

    [Test]
    public void OnPlayerHit_WithoutPlayerChargesPlayerOne()
    {
        gameManager.OnPlayerHit(Vector3.zero);

        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining"), Is.EqualTo(25f).Within(0.001f));
        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining2"), Is.EqualTo(30f).Within(0.001f));
    }

    [Test]
    public void OnVehicleCollision_TieBreakChargesTwoSecondsEach()
    {
        gameManager.OnVehicleCollision(Vector3.zero, player1, player2);

        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining"), Is.EqualTo(28f).Within(0.001f));
        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining2"), Is.EqualTo(28f).Within(0.001f));
    }

    [Test]
    public void OnVehicleCollision_ChargesRammerMoreThanVictim()
    {
        player1Object.transform.position = new Vector3(2f, 0f, 0f);
        player2Object.transform.position = Vector3.zero;
        SetPrivateProperty(player1, "CurrentMovementDirection", Vector3.right);
        SetPrivateProperty(player2, "CurrentMovementDirection", Vector3.zero);

        gameManager.OnVehicleCollision(Vector3.zero, player1, player2);

        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining"), Is.EqualTo(26f).Within(0.001f));
        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining2"), Is.EqualTo(28f).Within(0.001f));
    }

    [Test]
    public void OnVehicleCollision_IgnoresRepeatWithinCooldown()
    {
        gameManager.OnVehicleCollision(Vector3.zero, player1, player2);
        gameManager.OnVehicleCollision(Vector3.zero, player2, player1);

        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining"), Is.EqualTo(28f).Within(0.001f),
            "The second report of the same bump should be deduped by the cooldown.");
        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining2"), Is.EqualTo(28f).Within(0.001f));
    }

    [Test]
    public void OnVehicleCollision_DoesNothingInSinglePlayerMode()
    {
        SetPrivateProperty(gameManager, "IsTwoPlayerMode", false);

        gameManager.OnVehicleCollision(Vector3.zero, player1, player2);

        Assert.That(GetPrivateField<float>(gameManager, "timeRemaining"), Is.EqualTo(30f).Within(0.001f));
    }

    [Test]
    public void FirstClockExpiry_EliminatesWithoutEndingTheRun()
    {
        SetPrivateField(gameManager, "timeRemaining", 4f);

        gameManager.OnPlayerHit(Vector3.zero, player1);

        var eliminated = GetPrivateField<bool[]>(gameManager, "eliminated");
        Assert.IsTrue(eliminated[0], "Player 1 should be eliminated once their clock runs out.");
        Assert.IsFalse(eliminated[1]);
        Assert.IsTrue(gameManager.IsGameActive, "The run continues while Player 2 still has time.");
        Assert.IsTrue(gameManager.IsVehicleExiting, "The eliminated vehicle should start its exit drive.");
    }

    [Test]
    public void SecondClockExpiry_EndsTheRun()
    {
        SetPrivateField(gameManager, "timeRemaining", 4f);
        gameManager.OnPlayerHit(Vector3.zero, player1);
        SetPrivateField(gameManager, "timeRemaining2", 4f);
        gameManager.OnPlayerHit(Vector3.zero, player2);

        var eliminated = GetPrivateField<bool[]>(gameManager, "eliminated");
        Assert.IsTrue(eliminated[0]);
        Assert.IsTrue(eliminated[1]);
        Assert.IsFalse(gameManager.IsGameActive);
    }

    [Test]
    public void TwoPlayerGameOver_SkipsLeaderboard()
    {
        SetPrivateField(gameManager, "timeRemaining", 4f);
        gameManager.OnPlayerHit(Vector3.zero, player1);
        SetPrivateField(gameManager, "timeRemaining2", 4f);
        gameManager.OnPlayerHit(Vector3.zero, player2);

        Assert.IsFalse(gameManager.IsGameActive);
        Assert.IsFalse(PlayerPrefs.HasKey("Leaderboard0"),
            "Competitive results should not enter the single-player leaderboard.");
        Assert.IsFalse(PlayerPrefs.HasKey("BestTime"));
    }

    [Test]
    public void ShowTwoPlayerGameOver_NamesTheLongerSurvivorAsWinner()
    {
        var labelObject = new GameObject("RunTimeText");
        var label = labelObject.AddComponent<TextMeshProUGUI>();
        SetPrivateField(gameManager, "runTimeText", label);
        var survival = GetPrivateField<float[]>(gameManager, "survivalTime");
        survival[0] = 12f;
        survival[1] = 33f;

        InvokePrivateMethod(gameManager, "ShowTwoPlayerGameOver");

        Assert.That(label.text, Does.StartWith("Player 2 Wins!"));
        Object.DestroyImmediate(labelObject);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        return (T)field.GetValue(target);
    }

    private static void SetPrivateProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(property, $"Could not find property '{propertyName}' on {target.GetType()}.");
        property.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Could not find method '{methodName}' on {target.GetType()}.");
        method.Invoke(target, null);
    }
}

// Duplicate-vehicle prevention between the two start-screen selectors.
public class TwoPlayerVehicleSelectorTests
{
    private GameObject selector1Object;
    private GameObject selector2Object;
    private VehicleSelector selector1;
    private VehicleSelector selector2;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();
        selector1Object = CreateSelector("Selector1", 0, out selector1);
        selector2Object = CreateSelector("Selector2", 1, out selector2);
        SetPrivateField(selector1, "otherSelector", selector2);
        SetPrivateField(selector2, "otherSelector", selector1);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
        Object.DestroyImmediate(selector1Object);
        Object.DestroyImmediate(selector2Object);
    }

    static GameObject CreateSelector(string name, int playerIndex, out VehicleSelector selector)
    {
        var go = new GameObject(name);
        selector = go.AddComponent<VehicleSelector>();
        go.AddComponent<BoxCollider>();

        var visuals = new GameObject[3];
        for (int i = 0; i < visuals.Length; i++)
        {
            visuals[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visuals[i].name = name + "Visual" + i;
            visuals[i].transform.parent = go.transform;
        }

        SetPrivateField(selector, "playerIndex", playerIndex);
        SetPrivateField(selector, "vehicleVisuals", visuals);
        return go;
    }

    static void Awaken(VehicleSelector selector)
    {
        typeof(VehicleSelector).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(selector, null);
    }

    [Test]
    public void Selectors_UseSeparatePlayerPrefsKeys()
    {
        Awaken(selector1);
        Awaken(selector2);

        selector1.NextVehicle();   // Also skips selector2's saved index (both start at 0)
        selector2.NextVehicle();

        Assert.IsTrue(PlayerPrefs.HasKey("VehicleIndex"), "Player 1 keeps the legacy key.");
        Assert.IsTrue(PlayerPrefs.HasKey("VehicleIndex2"), "Player 2 stores its own choice.");
        Assert.That(PlayerPrefs.GetInt("VehicleIndex"), Is.Not.EqualTo(PlayerPrefs.GetInt("VehicleIndex2")));
    }

    [Test]
    public void Cycle_SkipsTheOtherPlayersVehicle()
    {
        Awaken(selector1);
        Awaken(selector2);
        SetPrivateField(selector2, "index", 1);

        selector1.NextVehicle();   // From 0: index 1 is taken, so land on 2

        Assert.That(selector1.CurrentIndex, Is.EqualTo(2));
    }

    [Test]
    public void EnsureDistinctFrom_BumpsDuplicateSelection()
    {
        Awaken(selector1);
        Awaken(selector2);
        Assert.That(selector1.CurrentIndex, Is.EqualTo(selector2.CurrentIndex), "Both load index 0 by default.");

        selector2.EnsureDistinctFrom(selector1);

        Assert.That(selector2.CurrentIndex, Is.Not.EqualTo(selector1.CurrentIndex));
    }

    [Test]
    public void Cycle_IgnoresInactiveOtherSelector()
    {
        Awaken(selector1);
        Awaken(selector2);
        SetPrivateField(selector2, "index", 1);
        selector2Object.SetActive(false);   // Single-player mode: Player 2 disabled

        selector1.NextVehicle();

        Assert.That(selector1.CurrentIndex, Is.EqualTo(1), "All vehicles are available when Player 2 is inactive.");
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Could not find private field '{fieldName}' on {target.GetType()}.");
        field.SetValue(target, value);
    }
}

public class SessionScoreTests
{
    [SetUp]
    public void SetUp()
    {
        SessionScore.WinsP1 = 0;
        SessionScore.WinsP2 = 0;
    }

    [Test]
    public void RecordWin_IncrementsCorrectPlayer()
    {
        SessionScore.RecordWin(0);
        SessionScore.RecordWin(1);
        SessionScore.RecordWin(1);

        Assert.That(SessionScore.WinsP1, Is.EqualTo(1));
        Assert.That(SessionScore.WinsP2, Is.EqualTo(2));
    }

    [Test]
    public void FormatLine_ShowsSessionTotals()
    {
        SessionScore.RecordWin(0);
        SessionScore.RecordWin(0);
        SessionScore.RecordWin(1);

        Assert.That(SessionScore.FormatLine(), Is.EqualTo("Wins this session: P1 2 – 1 P2"));
    }
}
