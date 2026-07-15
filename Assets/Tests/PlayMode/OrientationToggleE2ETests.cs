using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static PlayModeReflectionHelpers;

/// <summary>
/// Play Mode end-to-end test for the pause menu's runtime-built ROTATION
/// toggle (cloned from the music button in GameManager.EnsureRuntimeUiRefs).
/// Drives the same scene as the other E2E suites.
/// </summary>
public class OrientationToggleE2ETests
{
    const string MainScene = "My Game";
    // StartGame runs a 3-2-1-GO countdown plus a camera intro before
    // IsGameActive flips, so "the run started" needs a generous ceiling.
    const float RunStartTimeout = 15f;

    GameManager gameManager;
    bool hadPref;
    int savedPref;

    [UnitySetUp]
    public IEnumerator LoadMainScene()
    {
        hadPref = PlayerPrefs.HasKey(OrientationManager.OrientationPrefKey);
        savedPref = PlayerPrefs.GetInt(OrientationManager.OrientationPrefKey, 1);
        ResetOrientationState();

        yield return SceneManager.LoadSceneAsync(MainScene, LoadSceneMode.Single);
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == MainScene);

        gameManager = Object.FindAnyObjectByType<GameManager>();
        yield return new WaitForSeconds(0.5f);
    }

    [TearDown]
    public void RestoreState()
    {
        Time.timeScale = 1f;
        if (hadPref)
            PlayerPrefs.SetInt(OrientationManager.OrientationPrefKey, savedPref);
        else
            PlayerPrefs.DeleteKey(OrientationManager.OrientationPrefKey);
        ResetOrientationState();
    }

    // The pref cache and subscriber list are static and leak between tests
    // unless reset; the live GameManager's label handler re-registers itself
    // via OnEnable on the next scene load.
    static void ResetOrientationState()
    {
        PlayerPrefs.DeleteKey(OrientationManager.OrientationPrefKey);
        SetPrivateStaticField(typeof(OrientationManager), "orientationPref", int.MinValue);
    }

    [UnityTest]
    public IEnumerator PauseMenu_RotationToggle_FlipsPreference_WithoutTouchingMusic()
    {
        gameManager.StartGame();
        yield return InputSimulationHelpers.WaitUntilOrTimeout(() => gameManager.IsGameActive, RunStartTimeout);
        Assert.That(gameManager.IsGameActive, Is.True,
            "Run should become active once the start countdown and camera intro finish.");

        gameManager.TogglePause();
        yield return null;
        Assert.That(gameManager.IsPaused, Is.True);

        var pausePanel = GetPrivateField<GameObject>(gameManager, "pausePanel");
        Assert.That(pausePanel, Is.Not.Null, "pausePanel should be wired in the scene.");
        var rotationTransform = pausePanel.transform.Find("RotationButtonRuntime");
        Assert.That(rotationTransform, Is.Not.Null,
            "GameManager should clone a rotation toggle into the pause menu at startup.");

        var button = rotationTransform.GetComponent<Button>();
        var label = rotationTransform.GetComponentInChildren<TMP_Text>(true);
        Assert.That(button, Is.Not.Null);
        Assert.That(label, Is.Not.Null);
        Assert.That(label.text, Is.EqualTo("ROTATION: LANDSCAPE"),
            "The toggle should show the landscape default before any choice is made.");

        var musicSource = GetPrivateField<AudioSource>(gameManager, "musicSource");
        bool muteBefore = musicSource != null && musicSource.mute;

        button.onClick.Invoke();
        yield return null;

        Assert.That(OrientationManager.LandscapePreferred, Is.False,
            "Clicking the toggle should switch the preference to portrait.");
        Assert.That(PlayerPrefs.GetInt(OrientationManager.OrientationPrefKey, -1), Is.EqualTo(0),
            "The portrait choice should be persisted.");
        Assert.That(label.text, Is.EqualTo("ROTATION: PORTRAIT"),
            "The button label should follow the preference.");

        // The clone starts from the music button, whose scene-wired persistent
        // ToggleMusic call must NOT survive on the copy.
        if (musicSource != null)
            Assert.That(musicSource.mute, Is.EqualTo(muteBefore),
                "The rotation toggle must not inherit the music button's persistent ToggleMusic call.");
    }
}
