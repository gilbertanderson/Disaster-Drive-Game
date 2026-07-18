using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static PlayModeReflectionHelpers;

public class RearViewToggleE2ETests
{
    private const string MainScene = "My Game";
    private const float RunStartTimeout = 15f;

    private GameManager gameManager;
    private bool hadPref;
    private int savedPref;

    [UnitySetUp]
    public IEnumerator LoadMainScene()
    {
        hadPref = PlayerPrefs.HasKey(RearViewCameraController.RearViewPrefKey);
        savedPref = PlayerPrefs.GetInt(RearViewCameraController.RearViewPrefKey, 0);
        ResetRearViewState();

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
            PlayerPrefs.SetInt(RearViewCameraController.RearViewPrefKey, savedPref);
        else
            PlayerPrefs.DeleteKey(RearViewCameraController.RearViewPrefKey);
        ResetRearViewState();
    }

    [UnityTest]
    public IEnumerator PauseMenu_RearViewToggle_EnablesInsetCameraWithoutChangingMusic()
    {
        gameManager.StartGame();
        yield return InputSimulationHelpers.WaitUntilOrTimeout(
            () => gameManager.IsGameActive, RunStartTimeout);
        Assert.That(gameManager.IsGameActive, Is.True);

        gameManager.TogglePause();
        yield return null;

        var pausePanel = GetPrivateField<GameObject>(gameManager, "pausePanel");
        Transform toggle = pausePanel.transform.Find("RearViewButtonRuntime");
        Assert.That(toggle, Is.Not.Null);
        var button = toggle.GetComponent<Button>();
        var label = toggle.GetComponentInChildren<TMP_Text>(true);
        Assert.That(label.text, Is.EqualTo("REAR VIEW: OFF"));

        var musicSource = GetPrivateField<AudioSource>(gameManager, "musicSource");
        bool muteBefore = musicSource != null && musicSource.mute;

        button.onClick.Invoke();
        yield return null;

        Assert.That(RearViewCameraController.RearViewEnabled, Is.True);
        Assert.That(label.text, Is.EqualTo("REAR VIEW: ON"));
        Transform rearCamera = Camera.main.transform.Find("RearViewCamera");
        Assert.That(rearCamera, Is.Not.Null);
        Assert.That(rearCamera.GetComponent<Camera>().enabled, Is.True);
        if (musicSource != null)
            Assert.That(musicSource.mute, Is.EqualTo(muteBefore));
    }

    private static void ResetRearViewState()
    {
        PlayerPrefs.DeleteKey(RearViewCameraController.RearViewPrefKey);
        SetPrivateStaticField(
            typeof(RearViewCameraController), "rearViewPref", int.MinValue);
    }
}
