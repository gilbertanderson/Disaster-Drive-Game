using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static PlayModeReflectionHelpers;

/// <summary>
/// Play Mode end-to-end tests mapped to PROJECT_RUBRIC.md pre-submit checklist.
/// Run with video: Disaster → Run Rubric E2E with Video (or PlayMode tab in Test Runner).
/// </summary>
public class RubricE2ETests
{
    const string MainScene = "My Game";
    const float RecordSeconds = 2.5f;

    GameManager gameManager;
    PlayerController player;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync(MainScene, LoadSceneMode.Single);
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == MainScene);

        gameManager = Object.FindAnyObjectByType<GameManager>();
        player = Object.FindAnyObjectByType<PlayerController>();

        if (Keyboard.current == null)
            InputSystem.AddDevice<Keyboard>();

        yield return new WaitForSeconds(0.5f);
    }

    // --- Criterion 1: Gameplay (PROJECT_RUBRIC pre-submit) ---

    [UnityTest]
    public IEnumerator Rubric_01_StartScreen_ShowsDisasterTitle()
    {
        yield return RubricE2ERecording.Begin("01_start_screen_disaster_title");

        Assert.That(gameManager, Is.Not.Null);
        Assert.That(gameManager.IsOnStartScreen, Is.True, "Start panel should be visible on load.");

        var title = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include)
            .FirstOrDefault(t => t.text != null && t.text.Contains("DISASTER"));
        Assert.That(title, Is.Not.Null, "DISASTER title text should be on the start screen.");

        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        yield return RubricE2ERecording.End();
    }

    [UnityTest]
    public IEnumerator Rubric_02_Gameplay_DriveStartsActiveRun()
    {
        yield return RubricE2ERecording.Begin("02_drive_starts_run");

        Assert.That(gameManager.IsGameActive, Is.False);
        yield return StartRunAndWaitUntilActive();

        Assert.That(gameManager.IsGameActive, Is.True);
        Assert.That(gameManager.IsPaused, Is.False);
        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        yield return RubricE2ERecording.End();
    }

    [UnityTest]
    public IEnumerator Rubric_03_Gameplay_WasdMovesVehicle()
    {
        yield return RubricE2ERecording.Begin("03_wasd_vehicle_movement");

        yield return StartRunAndWaitUntilActive();

        Vector3 startPos = player.transform.position;
        yield return HoldKey(Key.D, 0.8f);

        Assert.That(player.transform.position.x, Is.GreaterThan(startPos.x + 0.1f),
            "Vehicle should move right when D is held after Drive is pressed.");

        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        yield return RubricE2ERecording.End();
    }

    [UnityTest]
    public IEnumerator Rubric_04_Gameplay_RockHit_AppliesTimerPenalty()
    {
        yield return RubricE2ERecording.Begin("04_rock_hit_timer_penalty");

        yield return StartRunAndWaitUntilActive();

        float before = GetPrivateField<float>(gameManager, "timeRemaining");
        gameManager.OnPlayerHit(player != null ? player.transform.position : Vector3.zero);
        float after = GetPrivateField<float>(gameManager, "timeRemaining");

        Assert.That(after, Is.EqualTo(before - 5f).Within(0.01f),
            "Rock hit should subtract 5 seconds from the timer (rubric: timer penalty on hit).");

        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        yield return RubricE2ERecording.End();
    }

    [UnityTest]
    public IEnumerator Rubric_05_Gameplay_NearMiss_AddsBonusTime()
    {
        yield return RubricE2ERecording.Begin("05_near_miss_bonus");

        yield return StartRunAndWaitUntilActive();
        SetPrivateField(gameManager, "lastNearMissTime", -10f);
        float before = GetPrivateField<float>(gameManager, "timeRemaining");

        gameManager.OnNearMiss();
        float after = GetPrivateField<float>(gameManager, "timeRemaining");

        Assert.That(after, Is.EqualTo(before + 2f).Within(0.01f),
            "Near miss should add +2s (rubric polish beyond design doc).");

        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        yield return RubricE2ERecording.End();
    }

    [UnityTest]
    public IEnumerator Rubric_06_Gameplay_Pause_FreezesRun()
    {
        yield return RubricE2ERecording.Begin("06_pause_overlay");

        yield return StartRunAndWaitUntilActive();

        gameManager.TogglePause();
        Assert.That(gameManager.IsPaused, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(0f));

        gameManager.TogglePause();
        Assert.That(gameManager.IsPaused, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));

        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        Time.timeScale = 1f;
        yield return RubricE2ERecording.End();
    }

    [UnityTest]
    public IEnumerator Rubric_07_Gameplay_GameOver_ShowsPanel()
    {
        yield return RubricE2ERecording.Begin("07_game_over_retry");

        yield return StartRunAndWaitUntilActive();

        InvokePrivate(gameManager, "GameOver");
        yield return new WaitForSeconds(1.5f);

        Assert.That(gameManager.IsGameActive, Is.False);

        var gameOverPanel = GetPrivateField<GameObject>(gameManager, "gameOverPanel");
        Assert.That(gameOverPanel, Is.Not.Null);
        Assert.That(gameOverPanel.activeInHierarchy, Is.True,
            "Game over panel with Retry should appear when the timer hits zero.");

        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        yield return RubricE2ERecording.End();
    }

    [UnityTest]
    public IEnumerator Rubric_08_Gameplay_RocksSpawnDuringRun()
    {
        yield return RubricE2ERecording.Begin("08_rocks_spawn_from_top");

        var spawner = Object.FindAnyObjectByType<SpawnManager>();
        Assert.That(spawner, Is.Not.Null);

        yield return StartRunAndWaitUntilActive();
        yield return new WaitForSeconds(3f);

        Assert.That(MoveDown.ActiveRockCount, Is.GreaterThan(0),
            "Rocks should spawn from the top and scroll toward the player during an active run.");

        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        yield return RubricE2ERecording.End();
    }

    [UnityTest]
    public IEnumerator Rubric_08b_GameOver_VehicleStaysVisibleDuringExitDrive()
    {
        yield return RubricE2ERecording.Begin("08b_game_over_exit_drive");

        yield return StartRunAndWaitUntilActive();

        var playerObject = player.gameObject;
        Assert.That(playerObject.activeSelf, Is.True);

        InvokePrivate(gameManager, "GameOver");
        yield return new WaitForSeconds(0.2f);

        Assert.That(gameManager.IsGameActive, Is.False);
        Assert.That(gameManager.IsVehicleExiting, Is.True);
        Assert.That(playerObject.activeSelf, Is.True,
            "Vehicle should remain visible while driving off-screen after game over.");

        yield return new WaitForSeconds(0.5f);
        Assert.That(playerObject.activeSelf, Is.True,
            "Vehicle should still be visible before the minimum exit drive duration elapses.");

        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        yield return RubricE2ERecording.End();
    }

    // --- Criterion 2: Music & sound ---

    [UnityTest]
    public IEnumerator Rubric_09_Audio_ImpactAndMusicConfigured()
    {
        yield return RubricE2ERecording.Begin("09_audio_clips_configured");

        Assert.That(GetPrivateField<AudioClip>(gameManager, "impactClip"), Is.Not.Null,
            "Impact honk on rock hit (rubric criterion 2).");
        Assert.That(GetPrivateField<AudioSource>(gameManager, "musicSource"), Is.Not.Null,
            "Background music source should be wired in the scene.");
        Assert.That(GetPrivateField<AudioClip>(gameManager, "clickClip"), Is.Not.Null,
            "UI click sound for buttons.");
        Assert.That(GetPrivateField<AudioClip>(gameManager, "nearMissClip"), Is.Not.Null,
            "Near-miss reward chirp.");

        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        yield return RubricE2ERecording.End();
    }

    // --- Criterion 3: Particle effects ---

    [UnityTest]
    public IEnumerator Rubric_10_Particles_EffectPrefabsAssigned()
    {
        yield return RubricE2ERecording.Begin("10_particle_prefabs");

        Assert.That(GetPrivateField<GameObject>(gameManager, "dustEffectPrefab"), Is.Not.Null,
            "Hit dust burst on vehicle collision.");
        Assert.That(GetPrivateField<GameObject>(gameManager, "fireworksPrefab"), Is.Not.Null,
            "Fireworks for new #1 best time.");

        yield return StartRunAndWaitUntilActive();
        yield return new WaitForSeconds(2f);
        var rock = Object.FindObjectsByType<MoveDown>(FindObjectsInactive.Include).FirstOrDefault();
        if (rock != null)
        {
            Assert.That(GetPrivateField<GameObject>(rock, "destroyEffectPrefab"), Is.Not.Null,
                "Rock rubble prefab on spawned obstacles.");
        }

        var vehicleSelector = Object.FindAnyObjectByType<VehicleSelector>();
        Assert.That(vehicleSelector, Is.Not.Null, "Vehicle picker with dirt emitters should exist.");

        yield return RubricE2ERecording.CaptureForSeconds(RecordSeconds);
        yield return RubricE2ERecording.End();
    }

    // StartGame runs a 3-2-1-GO countdown (4 x 0.8s beats) plus a camera intro
    // before IsGameActive flips, so a fixed post-StartGame wait races the run start.
    IEnumerator StartRunAndWaitUntilActive()
    {
        gameManager.StartGame();
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => gameManager.IsGameActive, 15f);
        Assert.That(gameManager.IsGameActive, Is.True,
            "Run should become active once the start countdown and camera intro finish.");
    }

    static IEnumerator HoldKey(Key key, float seconds)
    {
        using (StateEvent.From(Keyboard.current, out var eventPtr))
        {
            Keyboard.current[key].WriteValueIntoEvent(1f, eventPtr);
            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();
        }

        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
            yield return null;

        using (StateEvent.From(Keyboard.current, out var eventPtr))
        {
            Keyboard.current[key].WriteValueIntoEvent(0f, eventPtr);
            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();
        }
    }
}
