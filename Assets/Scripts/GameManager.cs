using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const string BestTimeKey = "BestTime";     // PlayerPrefs key for the longest-survival record
    private const string LeaderboardPrefix = "Leaderboard";
    private const string MusicMutedKey = "MusicMuted"; // PlayerPrefs key for the music toggle
    private const int LeaderboardSize = 5;

    [Header("Timer")]
    [SerializeField] private float startTime = 60f;   // Seconds on the clock at the start of a run
    [SerializeField] private float hitPenalty = 5f;   // Seconds removed when the vehicle hits a rock
    [SerializeField] private float nearMissBonus = 2f;       // Seconds added for a close dodge
    [SerializeField] private float nearMissCooldown = 1.5f;  // Minimum gap between near-miss awards

    [Header("Difficulty")]
    [SerializeField] private float rampInterval = 10f;             // Seconds between difficulty bumps
    [SerializeField] private float playerSpeedIncrease = 0.75f;    // Added to the vehicle's speed each bump
    [SerializeField] private float spawnIntervalMultiplier = 0.9f; // Spawn interval shrinks by this factor each bump

    [Header("Lighting Transition")]
    [SerializeField] private Light sunLight;
    [SerializeField] private float lightingTransitionDuration = 30f; // Seconds from evening to daylight
    [SerializeField] private Color eveningLightColor = new Color(0.76f, 0.8f, 0.92f);
    [SerializeField] private Color sunsetLightColor = new Color(0.92f, 0.78f, 0.65f);
    [SerializeField] private Color daylightLightColor = new Color(1f, 0.98f, 0.92f);
    [SerializeField] private float eveningLightIntensity = 0.55f;
    [SerializeField] private float sunsetLightIntensity = 0.85f;
    [SerializeField] private float daylightLightIntensity = 1.2f;
    [SerializeField] private Color eveningAmbientColor = new Color(0.32f, 0.35f, 0.42f);
    [SerializeField] private Color sunsetAmbientColor = new Color(0.45f, 0.4f, 0.35f);
    [SerializeField] private Color daylightAmbientColor = new Color(0.65f, 0.68f, 0.72f);
    [SerializeField] private Color eveningFogColor = new Color(0.52f, 0.56f, 0.62f);
    [SerializeField] private Color sunsetFogColor = new Color(0.58f, 0.54f, 0.5f);
    [SerializeField] private Color daylightFogColor = new Color(0.72f, 0.74f, 0.77f);
    [SerializeField] private float eveningFogDensity = 0.012f;
    [SerializeField] private float sunsetFogDensity = 0.008f;
    [SerializeField] private float daylightFogDensity = 0.004f;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject startPanel;      // Title, best time, vehicle picker, and Drive button
    [SerializeField] private GameObject gameOverPanel;   // GAME OVER text, run time, and Retry button
    [SerializeField] private GameObject pausePanel;      // PAUSED overlay with resume and music buttons
    [SerializeField] private TMP_Text bestTimeText;      // Longest-survival record shown on the start screen
    [SerializeField] private TMP_Text runTimeText;       // This playthrough's survival time, on the game over screen
    [SerializeField] private TMP_Text gameOverStatsText; // Dodges, hits, streak, and wave on the game over screen
    [SerializeField] private GameObject newBestText;     // "NEW BEST!" banner, shown when the record is beaten
    [SerializeField] private TMP_Text penaltyPopupText;  // "-5s" / "+2s" popup for hit penalties and near-miss bonuses
    [SerializeField] private TMP_Text musicButtonLabel;  // Pause overlay button label ("MUSIC: ON/OFF")
    [SerializeField] private TMP_Text runStatsText;      // Live wave and dodge streak during a run
    [SerializeField] private TMP_Text controlsHintText;  // Short controls reminder on the start screen
    [SerializeField] private float lowTimeWarningThreshold = 10f;
    [SerializeField] private Color lowTimeWarningColor = new Color(1f, 0.55f, 0.2f);

    [Header("Impact Feedback")]
    [SerializeField] private AudioClip impactClip;         // Honk played when the vehicle hits a rock
    [SerializeField] private GameObject dustEffectPrefab;  // Dust burst spawned at the point of impact
    [SerializeField] private CameraShake cameraShake;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;      // Looping background music
    [SerializeField] private AudioClip clickClip;          // UI button click
    [SerializeField] private AudioClip nearMissClip;       // Short reward chirp on a close dodge
    [SerializeField] private float nearMissVolume = 0.5f;
    [SerializeField] private float nearMissPitch = 1.4f;
    [SerializeField] private float menuMusicVolume = 0.3f; // Music volume on the start/game over screens
    [SerializeField] private float playMusicVolume = 0.6f; // Music volume during a run

    [Header("Celebration")]
    [SerializeField] private GameObject fireworksPrefab;   // Spawned when a run sets a new best time

    // False on the start screen and after game over; the player and spawner only run while true.
    public bool IsGameActive { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsVehicleExiting { get; private set; }
    public bool IsWorldAnimating => (IsGameActive || IsVehicleExiting) && !IsPaused;

    // True only while the title screen is showing (not mid-run, not game over).
    public bool IsOnStartScreen => !IsGameActive && startPanel != null && startPanel.activeInHierarchy;

    private float timeRemaining;
    private float gameStartTime;   // Time.timeSinceLevelLoad when the Drive button was pressed
    private float nextRampTime;
    private float lightingElapsed;
    private float displayedTimer;
    private float lastHitTime;
    private float lastNearMissTime;
    private float bestStreak;
    private int rocksDodged;
    private int hitsTaken;
    private AudioSource sfxSource;
    private PlayerController player;
    private SpawnManager spawnManager;
    private Color timerDefaultColor;
    private Color penaltyPopupDefaultColor;
    private Coroutine startPanelHideRoutine;
    private Coroutine gameOverShowRoutine;
    private Coroutine pausePanelRoutine;

    void Start()
    {
        Time.timeScale = 1f;   // A previous session may have ended while paused (timeScale 0)
        timeRemaining = startTime;
        displayedTimer = startTime;
        sfxSource = GetComponent<AudioSource>();
        player = FindAnyObjectByType<PlayerController>();
        spawnManager = FindAnyObjectByType<SpawnManager>();
        if (cameraShake == null)
        {
            cameraShake = Camera.main != null ? Camera.main.GetComponent<CameraShake>() : null;
            if (cameraShake == null)
                cameraShake = FindAnyObjectByType<CameraShake>();
        }

        if (sunLight == null)
            sunLight = RenderSettings.sun;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        lightingElapsed = 0f;
        UpdateLighting();

        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (newBestText != null) newBestText.SetActive(false);
        if (penaltyPopupText != null) penaltyPopupText.gameObject.SetActive(false);
        if (timerText != null) timerDefaultColor = timerText.color;
        if (penaltyPopupText != null) penaltyPopupDefaultColor = penaltyPopupText.color;

        UpdateLeaderboardDisplay();

        if (musicSource != null)
        {
            musicSource.mute = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
            musicSource.volume = menuMusicVolume;
        }
        UpdateMusicButtonLabel();
        UpdateTimerDisplay();
        UpdateControlsHint();
    }

    void Update()
    {
        // Esc pauses/resumes mid-run
        if (IsGameActive && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();

        if (!IsGameActive || IsPaused)
            return;

        UpdateLighting();
        timeRemaining -= Time.deltaTime;
        displayedTimer = Mathf.Lerp(displayedTimer, timeRemaining, Time.deltaTime * 8f);
        UpdateTimerDisplay();
        UpdateRunStatsDisplay();
        UpdateLowTimeWarning();

        float currentStreak = Time.timeSinceLevelLoad - lastHitTime;
        if (currentStreak > bestStreak)
            bestStreak = currentStreak;

        // Difficulty ramp: every rampInterval seconds the vehicle accelerates and rocks
        // spawn sooner and more densely, so dodging gets progressively harder.
        if (Time.timeSinceLevelLoad >= nextRampTime)
        {
            nextRampTime += rampInterval;
            if (player != null) player.speed += playerSpeedIncrease;
            if (spawnManager != null) spawnManager.IncreaseDifficulty(spawnIntervalMultiplier);
        }

        if (timeRemaining <= 0f)
            GameOver();
    }

    private const float SunsetPoint = 1f / 3f; // Fraction of the transition where evening gives way to sunset

    void UpdateLighting()
    {
        if (sunLight == null || lightingElapsed >= lightingTransitionDuration)
            return;

        lightingElapsed = Mathf.Min(lightingElapsed + Time.deltaTime, lightingTransitionDuration);
        float t = lightingTransitionDuration > 0f ? lightingElapsed / lightingTransitionDuration : 1f;

        Color lightColor;
        float intensity;
        Color ambientColor;
        Color fogColor;
        float fogDensity;

        if (t <= SunsetPoint)
        {
            float phaseT = t / SunsetPoint;
            lightColor = Color.Lerp(eveningLightColor, sunsetLightColor, phaseT);
            intensity = Mathf.Lerp(eveningLightIntensity, sunsetLightIntensity, phaseT);
            ambientColor = Color.Lerp(eveningAmbientColor, sunsetAmbientColor, phaseT);
            fogColor = Color.Lerp(eveningFogColor, sunsetFogColor, phaseT);
            fogDensity = Mathf.Lerp(eveningFogDensity, sunsetFogDensity, phaseT);
        }
        else
        {
            float phaseT = (t - SunsetPoint) / (1f - SunsetPoint);
            lightColor = Color.Lerp(sunsetLightColor, daylightLightColor, phaseT);
            intensity = Mathf.Lerp(sunsetLightIntensity, daylightLightIntensity, phaseT);
            ambientColor = Color.Lerp(sunsetAmbientColor, daylightAmbientColor, phaseT);
            fogColor = Color.Lerp(sunsetFogColor, daylightFogColor, phaseT);
            fogDensity = Mathf.Lerp(sunsetFogDensity, daylightFogDensity, phaseT);
        }

        sunLight.color = lightColor;
        sunLight.intensity = intensity;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
    }

    // Wired to the start screen's Drive button.
    public void StartGame()
    {
        if (IsGameActive)
            return;

        IsGameActive = true;
        gameStartTime = Time.timeSinceLevelLoad;
        lastHitTime = gameStartTime;
        lastNearMissTime = -nearMissCooldown;
        bestStreak = 0f;
        rocksDodged = 0;
        hitsTaken = 0;
        nextRampTime = gameStartTime + rampInterval;
        if (startPanel != null)
        {
            if (startPanelHideRoutine != null)
                StopCoroutine(startPanelHideRoutine);
            startPanelHideRoutine = StartCoroutine(UIPanelTransition.Hide(startPanel));
        }
        if (musicSource != null) musicSource.volume = playMusicVolume;
    }

    // Wired to the game over screen's Retry button: back to the start screen.
    public void RestartGame()
    {
        Time.timeScale = 1f;   // In case anything left the game paused
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Wired to the pause overlay's Resume button and the Esc key.
    public void TogglePause()
    {
        if (!IsGameActive)
            return;

        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
        if (pausePanel != null)
        {
            if (pausePanelRoutine != null)
                StopCoroutine(pausePanelRoutine);
            pausePanelRoutine = StartCoroutine(
                IsPaused ? UIPanelTransition.Show(pausePanel) : UIPanelTransition.Hide(pausePanel));
        }
    }

    // Wired to the pause overlay's music button.
    public void ToggleMusic()
    {
        if (musicSource == null)
            return;

        musicSource.mute = !musicSource.mute;
        PlayerPrefs.SetInt(MusicMutedKey, musicSource.mute ? 1 : 0);
        PlayerPrefs.Save();
        UpdateMusicButtonLabel();
    }

    // Added to every UI button so presses give audible feedback.
    public void PlayClick()
    {
        if (sfxSource != null && clickClip != null)
            sfxSource.PlayOneShot(clickClip);
    }

    // Called by PlayerController when the vehicle collides with a rock.
    public void OnPlayerHit(Vector3 hitPoint)
    {
        if (!IsGameActive || IsPaused)
            return;

        timeRemaining -= hitPenalty;
        hitsTaken++;
        lastHitTime = Time.timeSinceLevelLoad;
        if (sfxSource != null && impactClip != null)
            sfxSource.PlayOneShot(impactClip);

        if (dustEffectPrefab != null)
        {
            GameObject dust = Instantiate(dustEffectPrefab, hitPoint, dustEffectPrefab.transform.rotation);
            Destroy(dust, 2f);   // Clean up so repeated hits can't pile up effects
        }

        if (cameraShake != null)
            cameraShake.Shake();

        StartCoroutine(PenaltyFeedback());
        UpdateTimerDisplay();
        if (timeRemaining <= 0f)
            GameOver();
    }

    public void OnNearMiss()
    {
        if (!IsGameActive || IsPaused)
            return;

        if (Time.timeSinceLevelLoad - lastNearMissTime < nearMissCooldown)
            return;

        lastNearMissTime = Time.timeSinceLevelLoad;
        timeRemaining += nearMissBonus;
        PlayNearMissSound();
        StartCoroutine(BonusFeedback());
        UpdateTimerDisplay();
    }

    void PlayNearMissSound()
    {
        if (nearMissClip == null)
            return;

        Vector3 position = player != null ? player.transform.position : transform.position;
        var oneShotObject = new GameObject("NearMissSFX");
        oneShotObject.transform.position = position;
        var source = oneShotObject.AddComponent<AudioSource>();
        source.clip = nearMissClip;
        source.volume = nearMissVolume;
        source.pitch = nearMissPitch;
        source.spatialBlend = 0f;
        source.Play();
        Destroy(oneShotObject, nearMissClip.length / Mathf.Max(nearMissPitch, 0.01f) + 0.1f);
    }

    public void OnRockDodged()
    {
        if (!IsGameActive || IsPaused)
            return;

        rocksDodged++;
    }

    // Flash a "-5s" popup and turn the timer red for a moment so the cost of a hit is obvious.
    IEnumerator PenaltyFeedback()
    {
        if (penaltyPopupText != null)
        {
            penaltyPopupText.text = "-" + Mathf.RoundToInt(hitPenalty) + "s";
            penaltyPopupText.color = new Color(0.95f, 0.25f, 0.2f);
            penaltyPopupText.gameObject.SetActive(true);
        }
        if (timerText != null) timerText.color = new Color(0.95f, 0.25f, 0.2f);

        yield return new WaitForSeconds(0.6f);

        if (penaltyPopupText != null) penaltyPopupText.gameObject.SetActive(false);
        if (penaltyPopupText != null) penaltyPopupText.color = penaltyPopupDefaultColor;
        if (timerText != null) timerText.color = timerDefaultColor;
    }

    IEnumerator BonusFeedback()
    {
        if (penaltyPopupText != null)
        {
            penaltyPopupText.text = "+" + Mathf.RoundToInt(nearMissBonus) + "s";
            penaltyPopupText.color = new Color(0.3f, 0.95f, 0.4f);
            penaltyPopupText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(0.6f);

        if (penaltyPopupText != null)
        {
            penaltyPopupText.gameObject.SetActive(false);
            penaltyPopupText.color = penaltyPopupDefaultColor;
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
            timerText.text = "Time: " + Mathf.CeilToInt(Mathf.Max(displayedTimer, 0f));
    }

    void UpdateRunStatsDisplay()
    {
        if (runStatsText == null)
            return;

        if (!IsGameActive || IsPaused)
        {
            runStatsText.text = string.Empty;
            return;
        }

        int wave = GetCurrentWave();
        float streak = Time.timeSinceLevelLoad - lastHitTime;
        runStatsText.text = "Wave: " + wave + "  Streak: " + Mathf.FloorToInt(streak) + "s";
    }

    int GetCurrentWave()
    {
        if (!IsGameActive)
            return 1;

        return Mathf.Max(1, Mathf.FloorToInt((Time.timeSinceLevelLoad - gameStartTime) / rampInterval) + 1);
    }

    void UpdateLowTimeWarning()
    {
        if (timerText == null || !IsGameActive || IsPaused || timeRemaining > lowTimeWarningThreshold)
            return;

        bool showingPenalty = penaltyPopupText != null && penaltyPopupText.gameObject.activeSelf;
        if (showingPenalty)
            return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f);
        timerText.color = Color.Lerp(timerDefaultColor, lowTimeWarningColor, pulse);
    }

    void UpdateControlsHint()
    {
        if (controlsHintText != null)
            controlsHintText.text = "Dodge Obstacles\nWASD steer\nEsc pause";
    }

    void UpdateMusicButtonLabel()
    {
        if (musicButtonLabel != null && musicSource != null)
            musicButtonLabel.text = musicSource.mute ? "MUSIC: OFF" : "MUSIC: ON";
    }

    void UpdateLeaderboardDisplay()
    {
        if (bestTimeText == null)
            return;

        var scores = LoadLeaderboard();
        if (scores.Count == 0)
        {
            bestTimeText.text = "Best Times:\n—";
            return;
        }

        var lines = new List<string> { "Best Times:" };
        for (int i = 0; i < scores.Count; i++)
            lines.Add((i + 1) + ". " + Mathf.FloorToInt(scores[i]) + "s");
        bestTimeText.text = string.Join("\n", lines);
    }

    List<float> LoadLeaderboard()
    {
        var scores = new List<float>();
        for (int i = 0; i < LeaderboardSize; i++)
        {
            float s = PlayerPrefs.GetFloat(LeaderboardPrefix + i, -1f);
            if (s >= 0f) scores.Add(s);
        }
        if (scores.Count == 0)
        {
            float legacy = PlayerPrefs.GetFloat(BestTimeKey, 0f);
            if (legacy > 0f) scores.Add(legacy);
        }
        scores.Sort((a, b) => b.CompareTo(a));
        return scores;
    }

    int InsertScore(float survival)
    {
        var scores = LoadLeaderboard();
        scores.Add(survival);
        scores.Sort((a, b) => b.CompareTo(a));
        if (scores.Count > LeaderboardSize)
            scores.RemoveRange(LeaderboardSize, scores.Count - LeaderboardSize);

        for (int i = 0; i < scores.Count; i++)
            PlayerPrefs.SetFloat(LeaderboardPrefix + i, scores[i]);
        for (int i = scores.Count; i < LeaderboardSize; i++)
            PlayerPrefs.DeleteKey(LeaderboardPrefix + i);

        if (scores.Count > 0)
            PlayerPrefs.SetFloat(BestTimeKey, scores[0]);
        PlayerPrefs.Save();

        for (int i = 0; i < scores.Count; i++)
            if (Mathf.Approximately(scores[i], survival))
                return i + 1;
        return -1;
    }

    void GameOver()
    {
        IsGameActive = false;
        timeRemaining = 0f;
        UpdateTimerDisplay();

        // The record is the longest survival: real seconds from Drive until the clock ran out.
        float survival = Time.timeSinceLevelLoad - gameStartTime;
        int waveReached = Mathf.Max(1, Mathf.FloorToInt(survival / rampInterval) + 1);
        string statsText = "Dodged: " + rocksDodged
            + "\nHits: " + hitsTaken
            + "\nBest Streak: " + Mathf.FloorToInt(bestStreak) + "s"
            + "\nWave: " + waveReached;

        if (runTimeText != null)
            runTimeText.text = "Time: " + Mathf.FloorToInt(survival) + "s";

        if (gameOverStatsText != null)
            gameOverStatsText.text = statsText;
        else if (runTimeText != null)
            runTimeText.text += "\n" + statsText;

        int rank = InsertScore(survival);
        bool madeTopFive = rank > 0;
        if (newBestText != null)
        {
            newBestText.SetActive(madeTopFive);
            var rankText = newBestText.GetComponent<TMP_Text>();
            if (rankText != null)
                rankText.text = rank == 1 ? "NEW BEST!" : "TOP 5! #" + rank;
        }

        if (madeTopFive && rank == 1 && fireworksPrefab != null)
        {
            GameObject fx = Instantiate(fireworksPrefab, new Vector3(0f, 1f, 2.5f), fireworksPrefab.transform.rotation);
            Destroy(fx, 6f);
        }

        UpdateLeaderboardDisplay();
        if (gameOverPanel != null)
        {
            if (gameOverShowRoutine != null)
                StopCoroutine(gameOverShowRoutine);
            gameOverShowRoutine = StartCoroutine(UIPanelTransition.Show(gameOverPanel));
        }
        if (musicSource != null) musicSource.volume = menuMusicVolume;
        if (spawnManager != null) spawnManager.StopSpawning();
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            IsVehicleExiting = true;
            player.BeginExitDrive();
        }
    }

    public void OnVehicleExitComplete()
    {
        IsVehicleExiting = false;
        if (player != null)
        {
            player.enabled = false;
            player.gameObject.SetActive(false);
        }
    }
}
