using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private const string BestTimeKey = "BestTime";     // PlayerPrefs key for the longest-survival record
    private const string LeaderboardPrefix = "Leaderboard";
    private const string MusicMutedKey = "MusicMuted"; // PlayerPrefs key for the music toggle
    private const string PlayerCountKey = "PlayerCount"; // PlayerPrefs key for the 1P/2P mode toggle
    private const int LeaderboardSize = 5;
    private const float TwoPlayerTimerOffsetX = 260f; // Horizontal split between P1/P2 timers in 2P mode
    private const float PenaltyPopupOffsetX = 230f;   // Popup offset outward from its own timer (matches original scene-authored spacing)
    private const float PenaltyPopupOffsetY = -5f;    // Popup sits just below its timer's own baseline

    // P1 vehicle-picker arrows slide left in 2P mode to make room for the P2 picker.
    // Two-player X's clear Drive's edges (Drive is 300 wide, arrows 76 wide, both
    // centered on the same row) with a 22px gap so P1's pair reads as fully left of
    // Drive and P2's pair fully right, instead of the old -170/170 values, which
    // overlapped Drive's box by 18px on each side.
    private const float PrevVehicleButtonSinglePlayerX = -170f;
    private const float NextVehicleButtonSinglePlayerX = 170f;
    private const float PrevVehicleButtonTwoPlayerX = -290f;
    private const float NextVehicleButtonTwoPlayerX = -210f;

    // P1's vehicle name sits alone, centered under Drive, in 1P; in 2P it narrows and
    // shifts under P1's own arrow pair so it lines up on one row with P2's name (at the
    // mirrored +250/500 spot on P2VehicleNameText), the pair reading as centered under Drive.
    private const float VehicleNameTextSinglePlayerX = 0f;
    private const float VehicleNameTextSinglePlayerWidth = 640f;
    private const float VehicleNameTextTwoPlayerX = -250f;
    private const float VehicleNameTextTwoPlayerWidth = 500f;

    // P1's vehicle slides outward in 2P so the pair frames the start-screen shot;
    // P2's spot is scene-authored (z -3.5) since it only exists in 2P. In 1P, P1
    // returns to its scene-authored preview position.
    // Lateral (world Z) start slots. Screen-right is world −Z with the top-down
    // camera, so these are centered on the camera/road line at Z = 2.5.
    private const float Player1VehicleSinglePlayerZ = 2.5f;
    private const float Player1VehicleTwoPlayerZ = 8.25f;

    [Header("Timer")]
    [SerializeField] private float startTime = 60f;   // Seconds on the clock at the start of a run
    [SerializeField] private float hitPenalty = 5f;   // Seconds removed when the vehicle hits a rock
    [SerializeField] private float nearMissBonus = 2f;       // Seconds added for a close dodge
    [SerializeField] private float nearMissCooldown = 1.5f;  // Minimum gap between near-miss awards

    [Header("Two Player")]
    [SerializeField] private PlayerController[] players;         // P1 (and P2) controllers; found by playerIndex if unassigned
    [SerializeField] private GameObject player2Object;           // Player 2 vehicle hierarchy; active only in 2P mode
    [SerializeField] private GameObject player2SelectorUI;       // P2 vehicle picker cluster on the start panel
    [SerializeField] private RectTransform prevVehicleButtonRect; // P1 picker left arrow; repositioned per player count
    [SerializeField] private RectTransform nextVehicleButtonRect; // P1 picker right arrow; repositioned per player count
    [SerializeField] private RectTransform vehicleNameTextRect;   // P1 vehicle name label; repositioned per player count
    [SerializeField] private TMP_Text timer2Text;                // P2 clock in the HUD (2P only)
    [SerializeField] private TMP_Text playerCountButtonLabel;    // "1 Player" / "2 Players" toggle button label
    [SerializeField] private float vehicleCollisionCooldown = 1f; // One penalty per bump, not per contact pair
    [SerializeField] private float vehicleCrashRammerPenalty = 4f;
    [SerializeField] private float vehicleCrashVictimPenalty = 2f;
    [SerializeField] private float vehicleCrashTiePenalty = 2f;

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
    [SerializeField] private TMP_Text penaltyPopupText;  // Legacy center popup; used as template for runtime split popups
    [SerializeField] private TMP_Text penaltyPopupP1;
    [SerializeField] private TMP_Text penaltyPopupP2;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text eliminationBannerText;
    [SerializeField] private TMP_Text musicButtonLabel;  // Pause overlay button label ("MUSIC: ON/OFF")
    [SerializeField] private TMP_Text runStatsText;      // Live wave and dodge streak during a run
    [SerializeField] private TMP_Text controlsHintText;  // Short controls reminder on the start screen
    [SerializeField] private float lowTimeWarningThreshold = 10f;
    [SerializeField] private Color lowTimeWarningColor = new Color(1f, 0.55f, 0.2f);
    [SerializeField] private float countdownBeatDuration = 0.8f;

    [Header("Impact Feedback")]
    [SerializeField] private AudioClip impactClip;         // Honk played when the vehicle hits a rock
    [SerializeField] private AudioClip vehicleCrashClip;   // Distinct sound for vehicle-vehicle hits
    [SerializeField] private GameObject dustEffectPrefab;  // Dust burst spawned at the point of impact
    [SerializeField] private CameraShake cameraShake;

    [Header("Camera")]
    [SerializeField] private GameplayCameraDirector cameraDirector;

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
    public bool IsTwoPlayerMode { get; private set; }

    // True only while the title screen is showing (not mid-run, not game over).
    public bool IsOnStartScreen => !IsGameActive && startPanel != null && startPanel.activeInHierarchy;

    private float timeRemaining;    // Player 1's clock (the only clock in 1P mode)
    private float timeRemaining2;   // Player 2's clock (2P mode only)
    private float gameStartTime;   // Time.timeSinceLevelLoad when the Drive button was pressed
    private float nextRampTime;
    private float lightingElapsed;
    private float displayedTimer;
    private float displayedTimer2;
    private float lastHitTime;
    private readonly float[] lastNearMissTime = { float.NegativeInfinity, float.NegativeInfinity };
    private float lastVehicleCollisionTime = float.NegativeInfinity;
    private readonly bool[] eliminated = new bool[2];   // Set once a player's clock expires (2P)
    private readonly bool[] exitStarted = new bool[2];  // Guards against starting a player's exit drive twice
    private readonly float[] survivalTime = new float[2];
    private int exitingVehicles;   // Vehicles currently driving off screen; keeps IsVehicleExiting accurate
    private float bestStreak;
    private int rocksDodged;
    private int hitsTaken;
    private AudioSource sfxSource;
    private PlayerController player;
    private SpawnManager spawnManager;
    private Color timerDefaultColorP1;
    private Color timerDefaultColorP2;
    private Color penaltyPopupDefaultColorP1;
    private Color penaltyPopupDefaultColorP2;
    private Coroutine countdownRoutine;
    private Coroutine eliminationBannerRoutine;
    private Coroutine startPanelHideRoutine;
    private Coroutine gameOverShowRoutine;
    private Coroutine pausePanelRoutine;
    private readonly Coroutine[] penaltyFeedbackRoutines = new Coroutine[2];
    private readonly Coroutine[] bonusFeedbackRoutines = new Coroutine[2];
    private TwoPlayerUIStyler uiStyler;
    private TMP_Text rotationButtonLabel;  // Label of the runtime-cloned pause-menu rotation toggle
    private TMP_Text touchControlsButtonLabel;  // Label of the runtime-cloned pause-menu touch-controls toggle
    private TMP_Text rearViewButtonLabel;  // Label of the runtime-cloned rear-camera toggle
    private RearViewCameraController rearViewCamera;

    void Start()
    {
        Time.timeScale = 1f;   // A previous session may have ended while paused (timeScale 0)
        timeRemaining = startTime;
        timeRemaining2 = startTime;
        displayedTimer = startTime;
        displayedTimer2 = startTime;
        sfxSource = GetComponent<AudioSource>();
        ResolvePlayers();
        spawnManager = FindAnyObjectByType<SpawnManager>();
        if (cameraShake == null)
        {
            cameraShake = Camera.main != null ? Camera.main.GetComponent<CameraShake>() : null;
            if (cameraShake == null)
                cameraShake = FindAnyObjectByType<CameraShake>();
        }
        if (cameraDirector == null)
        {
            cameraDirector = Camera.main != null ? Camera.main.GetComponent<GameplayCameraDirector>() : null;
            if (cameraDirector == null)
                cameraDirector = FindAnyObjectByType<GameplayCameraDirector>();
        }
        if (Camera.main != null)
        {
            rearViewCamera = Camera.main.GetComponent<RearViewCameraController>();
            if (rearViewCamera == null)
                rearViewCamera = Camera.main.gameObject.AddComponent<RearViewCameraController>();
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
        EnsureRuntimeUiRefs();
        if (penaltyPopupP1 != null) penaltyPopupP1.gameObject.SetActive(false);
        if (penaltyPopupP2 != null) penaltyPopupP2.gameObject.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (eliminationBannerText != null) eliminationBannerText.gameObject.SetActive(false);
        ApplyTimerThemeColors();
        EnsureIdentityMarkers();
        EnsureUiStyler();

        UpdateLeaderboardDisplay();

        if (musicSource != null)
        {
            musicSource.mute = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
            musicSource.volume = menuMusicVolume;
        }
        UpdateMusicButtonLabel();
        SetTwoPlayerMode(PlayerPrefs.GetInt(PlayerCountKey, 1) == 2);
        ApplyTimerThemeColors();
        UpdateTimerDisplay();
        UpdateControlsHint();
        RefreshVehiclePickerLabels();
    }

    // Use the serialized references when present; otherwise find every controller in
    // the scene (Player 2 starts inactive) and order them by playerIndex.
    void ResolvePlayers()
    {
        if (players == null || players.Length == 0)
            players = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        System.Array.Sort(players, (a, b) =>
        {
            if (a == null || b == null) return a == null ? 1 : -1;
            return a.playerIndex.CompareTo(b.playerIndex);
        });

        player = players.Length > 0 ? players[0] : null;
    }

    PlayerController GetPlayer(int index)
    {
        return players != null && index >= 0 && index < players.Length ? players[index] : null;
    }

    int ResolvePlayerIndex(PlayerController who)
    {
        if (who == null)
            return 0;
        return Mathf.Clamp(who.playerIndex, 0, 1);
    }

    float GetClock(int index) => index == 1 ? timeRemaining2 : timeRemaining;

    void AddClock(int index, float delta)
    {
        if (index == 1) timeRemaining2 += delta;
        else timeRemaining += delta;
    }

    void Update()
    {
        // Esc or any real gamepad's Start pauses/resumes mid-run (one toggle max
        // per frame). Virtual pads from the on-screen sticks are skipped so they
        // can't hide a physical Start press via Gamepad.current.
        if (IsGameActive && WasPausePressedThisFrame())
            TogglePause();

        if (!IsGameActive || IsPaused)
            return;

        UpdateLighting();
        if (!eliminated[0])
        {
            timeRemaining -= Time.deltaTime;
            displayedTimer = Mathf.Lerp(displayedTimer, timeRemaining, Time.deltaTime * 8f);
        }
        if (IsTwoPlayerMode && !eliminated[1])
        {
            timeRemaining2 -= Time.deltaTime;
            displayedTimer2 = Mathf.Lerp(displayedTimer2, timeRemaining2, Time.deltaTime * 8f);
        }
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
            if (players != null)
            {
                foreach (var p in players)
                {
                    if (p == null || !p.gameObject.activeInHierarchy || p.IsExiting)
                        continue;
                    int index = ResolvePlayerIndex(p);
                    if (eliminated[index])
                        continue;
                    p.speed += playerSpeedIncrease;
                }
            }
            if (spawnManager != null) spawnManager.IncreaseDifficulty(spawnIntervalMultiplier);
        }

        CheckClockExpirations();
    }

    // A player whose clock runs dry is eliminated; the run ends when nobody is left.
    void CheckClockExpirations()
    {
        if (!IsGameActive)
            return;

        if (!eliminated[0] && timeRemaining <= 0f)
            EliminatePlayer(0);
        if (IsGameActive && IsTwoPlayerMode && !eliminated[1] && timeRemaining2 <= 0f)
            EliminatePlayer(1);
    }

    void EliminatePlayer(int index)
    {
        if (eliminated[index])
            return;

        eliminated[index] = true;
        survivalTime[index] = Time.timeSinceLevelLoad - gameStartTime;
        if (index == 0)
        {
            timeRemaining = 0f;
            displayedTimer = 0f;
        }
        else
        {
            timeRemaining2 = 0f;
            displayedTimer2 = 0f;
        }
        UpdateTimerDisplay();

        bool anyoneLeft = !eliminated[0] || (IsTwoPlayerMode && !eliminated[1]);
        if (!anyoneLeft)
        {
            GameOver();   // Starts the remaining exit drives itself
            return;
        }

        if (IsTwoPlayerMode)
            StartCoroutine(ShowEliminationBanner(index));

        // Someone is still driving: this vehicle is out while the run continues.
        BeginPlayerExit(index, exitViaBottom: true);
    }

    void BeginPlayerExit(int index, bool exitViaBottom = false)
    {
        if (exitStarted[index])
            return;

        var p = GetPlayer(index);
        if (p == null || !p.gameObject.activeInHierarchy)
            return;

        exitStarted[index] = true;
        exitingVehicles++;
        IsVehicleExiting = true;
        p.BeginExitDrive(exitViaBottom);
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
        if (IsGameActive || countdownRoutine != null)
            return;

        lastHitTime = Time.timeSinceLevelLoad;
        lastNearMissTime[0] = float.NegativeInfinity;
        lastNearMissTime[1] = float.NegativeInfinity;
        lastVehicleCollisionTime = float.NegativeInfinity;
        bestStreak = 0f;
        rocksDodged = 0;
        hitsTaken = 0;
        timeRemaining = startTime;
        timeRemaining2 = startTime;
        displayedTimer = startTime;
        displayedTimer2 = startTime;
        for (int i = 0; i < eliminated.Length; i++)
        {
            eliminated[i] = false;
            exitStarted[i] = false;
            survivalTime[i] = 0f;
        }
        ReapplyVehicleStats();
        ApplyTimerThemeColors();
        UpdateTimerDisplay();
        if (startPanel != null)
        {
            if (startPanelHideRoutine != null)
                StopCoroutine(startPanelHideRoutine);
            startPanelHideRoutine = StartCoroutine(UIPanelTransition.Hide(startPanel));
        }
        if (musicSource != null) musicSource.volume = playMusicVolume;
        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);
        countdownRoutine = StartCoroutine(RunCountdown());
    }

    IEnumerator RunCountdown()
    {
        string[] steps = { "3", "2", "1", "GO!" };
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        if (cameraDirector != null)
            cameraDirector.StartIntroSequence(GetActivePlayerRoots());

        for (int i = 0; i < steps.Length; i++)
        {
            if (countdownText != null)
            {
                countdownText.text = steps[i];
                countdownText.color = steps[i] == "GO!" ? PlayerColors.BonusGreen : Color.white;
            }
            cameraDirector?.PlayCountdownBeat(i);
            yield return new WaitForSeconds(countdownBeatDuration);
        }

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (cameraDirector != null)
        {
            while (!cameraDirector.IsIntroComplete)
                yield return null;
        }

        IsGameActive = true;
        gameStartTime = Time.timeSinceLevelLoad;
        // Streak timing starts when driving begins, not when Drive was pressed
        // (the countdown would otherwise inflate the first streak by ~3s).
        lastHitTime = gameStartTime;
        nextRampTime = gameStartTime + rampInterval;
        countdownRoutine = null;
    }

    // True if Esc or a non-ignored gamepad Start was pressed this frame.
    static bool WasPausePressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;

        foreach (var pad in Gamepad.all)
        {
            if (InputModeWatcher.IsDeviceIgnored(pad))
                continue;
            if (pad.startButton.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    // True while a player is still racing (not eliminated and not mid exit-drive).
    public bool IsPlayerInteractable(PlayerController who)
    {
        if (who == null || !who.gameObject.activeInHierarchy || who.IsExiting)
            return false;
        return !eliminated[ResolvePlayerIndex(who)];
    }

    List<Transform> GetActivePlayerRoots()
    {
        var roots = new List<Transform>();
        if (players == null)
            return roots;

        foreach (var p in players)
        {
            if (p == null || !p.gameObject.activeInHierarchy)
                continue;
            if (!IsTwoPlayerMode && p.playerIndex != 0)
                continue;
            roots.Add(p.transform);
        }

        return roots;
    }

    void ReapplyVehicleStats()
    {
        if (players == null)
            return;

        foreach (var p in players)
        {
            if (p == null)
                continue;
            p.ResetSpeedToBase();
            var selector = p.GetComponent<VehicleSelector>();
            if (selector != null)
                selector.ReapplyStats();
        }
    }

    void ApplyTimerThemeColors()
    {
        timerDefaultColorP1 = IsTwoPlayerMode ? PlayerColors.P1 : Color.white;
        timerDefaultColorP2 = PlayerColors.P2;
        if (timerText != null)
            timerText.color = timerDefaultColorP1;
        if (timer2Text != null)
            timer2Text.color = timerDefaultColorP2;
        if (penaltyPopupP1 != null)
            penaltyPopupDefaultColorP1 = penaltyPopupP1.color;
        if (penaltyPopupP2 != null)
            penaltyPopupDefaultColorP2 = penaltyPopupP2.color;
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

    // Wired to the pause overlay's runtime-built rotation button (see
    // EnsureRuntimeUiRefs). The label refresh arrives via the
    // OrientationPreferenceChanged event.
    public void ToggleRotation()
    {
        OrientationManager.TogglePreference();
        PlayClick();
    }

    // Wired to the pause overlay's runtime-built touch-controls button (see
    // EnsureRuntimeUiRefs). The label refresh arrives via the
    // TouchControlsChanged event.
    public void ToggleTouchControls()
    {
        MobileControlsUI.ToggleTouchControlsPref();
        PlayClick();
    }

    // Wired to the pause overlay's runtime-built rear-view button. The rear
    // camera is an inset view, so toggling it never changes steering axes.
    public void ToggleRearView()
    {
        RearViewCameraController.TogglePreference();
        PlayClick();
    }

    // Wired to the start screen's player-count toggle button.
    public void TogglePlayerCount()
    {
        if (IsGameActive)
            return;

        SetTwoPlayerMode(!IsTwoPlayerMode);
        PlayerPrefs.SetInt(PlayerCountKey, IsTwoPlayerMode ? 2 : 1);
        PlayerPrefs.Save();
    }

    void SetTwoPlayerMode(bool twoPlayer)
    {
        IsTwoPlayerMode = twoPlayer;
        PositionTimerHud(twoPlayer);

        // Player 2's vehicle stands next to Player 1 on the start screen so the
        // second picker has something to preview. SetActive runs its Awake the
        // first time, which loads P2's own saved vehicle choice.
        if (player2Object != null)
            player2Object.SetActive(twoPlayer);
        if (player2SelectorUI != null)
            player2SelectorUI.SetActive(twoPlayer);
        if (prevVehicleButtonRect != null)
            prevVehicleButtonRect.anchoredPosition = new Vector2(
                twoPlayer ? PrevVehicleButtonTwoPlayerX : PrevVehicleButtonSinglePlayerX,
                prevVehicleButtonRect.anchoredPosition.y);
        if (nextVehicleButtonRect != null)
            nextVehicleButtonRect.anchoredPosition = new Vector2(
                twoPlayer ? NextVehicleButtonTwoPlayerX : NextVehicleButtonSinglePlayerX,
                nextVehicleButtonRect.anchoredPosition.y);
        if (vehicleNameTextRect != null)
        {
            vehicleNameTextRect.anchoredPosition = new Vector2(
                twoPlayer ? VehicleNameTextTwoPlayerX : VehicleNameTextSinglePlayerX,
                vehicleNameTextRect.anchoredPosition.y);
            vehicleNameTextRect.sizeDelta = new Vector2(
                twoPlayer ? VehicleNameTextTwoPlayerWidth : VehicleNameTextSinglePlayerWidth,
                vehicleNameTextRect.sizeDelta.y);
        }
        if (timer2Text != null)
            timer2Text.gameObject.SetActive(twoPlayer);
        if (playerCountButtonLabel != null)
            playerCountButtonLabel.text = twoPlayer ? "2 Players" : "1 Player";

        var p1 = GetPlayer(0);
        var p2 = GetPlayer(1);
        if (p1 != null)
        {
            Vector3 vehiclePos = p1.transform.position;
            vehiclePos.z = twoPlayer ? Player1VehicleTwoPlayerZ : Player1VehicleSinglePlayerZ;
            p1.transform.position = vehiclePos;
        }
        if (p1 != null)
            p1.ApplyControlScheme(twoPlayer
                ? PlayerController.ControlScheme.WasdAndLeftStick
                : PlayerController.ControlScheme.WasdAndArrows);
        if (p2 != null)
        {
            if (twoPlayer)
                p2.ApplyControlScheme(PlayerController.ControlScheme.ArrowsAndRightStick);
            else
                p2.DisableMovementInput();
        }

        // The two players may never drive the same vehicle.
        if (twoPlayer && p1 != null && p2 != null)
        {
            var selector1 = p1.GetComponent<VehicleSelector>();
            var selector2 = p2.GetComponent<VehicleSelector>();
            if (selector1 != null && selector2 != null)
                selector2.EnsureDistinctFrom(selector1);
        }

        ApplyTimerThemeColors();
        UpdateTimerDisplay();
        UpdateControlsHint();
        RefreshVehiclePickerLabels();
        EnsureIdentityMarkers();
        uiStyler?.Refresh();
    }

    // Splits the P1/P2 timers apart (and moves their penalty popups with them) so they never
    // overlap in 2P mode, and recenters everything when back to 1P.
    void PositionTimerHud(bool twoPlayer)
    {
        float baseY = timerText != null ? timerText.rectTransform.anchoredPosition.y : -20f;
        float p1X = twoPlayer ? -TwoPlayerTimerOffsetX : 0f;
        float p2X = TwoPlayerTimerOffsetX;

        if (timerText != null)
            timerText.rectTransform.anchoredPosition = new Vector2(p1X, baseY);
        if (timer2Text != null)
            timer2Text.rectTransform.anchoredPosition = new Vector2(p2X, baseY);

        float popupY = baseY + PenaltyPopupOffsetY;
        float p1PopupX = twoPlayer ? p1X - PenaltyPopupOffsetX : p1X + PenaltyPopupOffsetX;
        float p2PopupX = p2X + PenaltyPopupOffsetX;
        if (penaltyPopupP1 != null)
            penaltyPopupP1.rectTransform.anchoredPosition = new Vector2(p1PopupX, popupY);
        if (penaltyPopupP2 != null)
            penaltyPopupP2.rectTransform.anchoredPosition = new Vector2(p2PopupX, popupY);
    }

    void EnsureRuntimeUiRefs()
    {
        // Backward-compatible migration: if scene still has only one popup, repurpose it for P1 and clone P2.
        if (penaltyPopupP1 == null)
            penaltyPopupP1 = penaltyPopupText;

        if (penaltyPopupP1 != null && penaltyPopupP2 == null)
        {
            GameObject p2Popup = Instantiate(penaltyPopupP1.gameObject, penaltyPopupP1.transform.parent);
            p2Popup.name = "PenaltyPopupP2Runtime";
            penaltyPopupP2 = p2Popup.GetComponent<TMP_Text>();
            penaltyPopupP2.raycastTarget = false;
        }

        if (countdownText == null && timerText != null)
        {
            GameObject go = Instantiate(timerText.gameObject, timerText.transform.parent);
            go.name = "CountdownTextRuntime";
            countdownText = go.GetComponent<TMP_Text>();
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, -220f);
                rt.sizeDelta = new Vector2(500f, 140f);
            }
            countdownText.text = "3";
            // The clone inherits the timer's raycastTarget; it sits mid-screen
            // and must not swallow taps meant for the game underneath.
            countdownText.raycastTarget = false;
        }

        if (eliminationBannerText == null && timerText != null)
        {
            GameObject go = Instantiate(timerText.gameObject, timerText.transform.parent);
            go.name = "EliminationBannerRuntime";
            eliminationBannerText = go.GetComponent<TMP_Text>();
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, -150f);
                rt.sizeDelta = new Vector2(900f, 100f);
            }
            eliminationBannerText.text = "PLAYER 1 IS OUT!";
            eliminationBannerText.raycastTarget = false;
        }

        // Rotation toggle for the pause menu: cloned from the music button so
        // it inherits the sprite, font, colors, and UIButtonFeedback. The six
        // actions use a compact two-column grid that remains visible on wide,
        // short phone screens.
        if (rotationButtonLabel == null && musicButtonLabel != null)
        {
            var musicButton = musicButtonLabel.GetComponentInParent<Button>(true);
            if (musicButton != null)
            {
                GameObject go = Instantiate(musicButton.gameObject, musicButton.transform.parent);
                go.name = "RotationButtonRuntime";
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = new Vector2(190f, -60f);
                var button = go.GetComponent<Button>();
                // Instantiate copies the scene-wired persistent ToggleMusic
                // call, which RemoveAllListeners cannot clear — swap the whole
                // event for a fresh one.
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(ToggleRotation);
                rotationButtonLabel = go.GetComponentInChildren<TMP_Text>(true);
                UpdateRotationButtonLabel();
            }
        }

        // Touch-controls toggle: bottom-left cell in the settings grid.
        if (touchControlsButtonLabel == null && musicButtonLabel != null)
        {
            var musicButton = musicButtonLabel.GetComponentInParent<Button>(true);
            if (musicButton != null)
            {
                GameObject go = Instantiate(musicButton.gameObject, musicButton.transform.parent);
                go.name = "TouchControlsButtonRuntime";
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = new Vector2(-190f, -180f);
                var button = go.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(ToggleTouchControls);
                touchControlsButtonLabel = go.GetComponentInChildren<TMP_Text>(true);
                UpdateTouchControlsButtonLabel();
            }
        }

        // Rear-view toggle: bottom-right cell opposite touch controls.
        if (rearViewButtonLabel == null && musicButtonLabel != null)
        {
            var musicButton = musicButtonLabel.GetComponentInParent<Button>(true);
            if (musicButton != null)
            {
                GameObject go = Instantiate(musicButton.gameObject, musicButton.transform.parent);
                go.name = "RearViewButtonRuntime";
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = new Vector2(190f, -180f);
                var button = go.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(ToggleRearView);
                rearViewButtonLabel = go.GetComponentInChildren<TMP_Text>(true);
                UpdateRearViewButtonLabel();
            }
        }

        // Tap-to-pause on the timer HUD. Attached after the clone blocks above
        // so the countdown/banner copies of the timer don't inherit the handler.
        AttachTimerPauseTapHandler(timerText);
        AttachTimerPauseTapHandler(timer2Text);
    }

    void AttachTimerPauseTapHandler(TMP_Text label)
    {
        if (label == null)
            return;
        label.raycastTarget = true;
        var handler = label.GetComponent<TimerPauseTapHandler>();
        if (handler == null)
            handler = label.gameObject.AddComponent<TimerPauseTapHandler>();
        handler.Init(this);
    }

    void EnsureIdentityMarkers()
    {
        if (players == null)
            return;

        foreach (var p in players)
        {
            if (p == null)
                continue;
            if (p.GetComponent<PlayerIdentityMarker>() == null)
                p.gameObject.AddComponent<PlayerIdentityMarker>();
        }
    }

    void EnsureUiStyler()
    {
        uiStyler = GetComponent<TwoPlayerUIStyler>();
        if (uiStyler == null)
            uiStyler = gameObject.AddComponent<TwoPlayerUIStyler>();
        uiStyler.Init(this, timerText, timer2Text, penaltyPopupP1, penaltyPopupP2, countdownText, eliminationBannerText);
    }

    void RefreshVehiclePickerLabels()
    {
        if (players == null)
            return;
        foreach (var p in players)
        {
            if (p == null)
                continue;
            var selector = p.GetComponent<VehicleSelector>();
            if (selector != null)
                selector.RefreshLabel();
        }
    }

    // Added to every UI button so presses give audible feedback.
    public void PlayClick()
    {
        if (sfxSource != null && clickClip != null)
            sfxSource.PlayOneShot(clickClip);
    }

    // Called by MoveDown when a vehicle collides with a rock. The parameterless
    // overload keeps older callers (and tests) working; it charges Player 1.
    public void OnPlayerHit(Vector3 hitPoint)
    {
        OnPlayerHit(hitPoint, null);
    }

    public void OnPlayerHit(Vector3 hitPoint, PlayerController who)
    {
        if (!IsGameActive || IsPaused)
            return;

        int index = ResolvePlayerIndex(who);
        if (eliminated[index])
            return;

        AddClock(index, -hitPenalty);
        hitsTaken++;
        lastHitTime = Time.timeSinceLevelLoad;
        PlayImpactFeedback(hitPoint);
        StartPenaltyFeedback(index, hitPenalty);
        UpdateTimerDisplay();
        CheckClockExpirations();
    }

    // Called by PlayerController when the two vehicles crash into each other.
    // Both sides of the contact report it, so a cooldown dedupes the pair (and
    // stops a lingering scrape from double-charging). Each player loses the same
    // penalty as a rock hit, and both get shoved apart so the Bouncy material
    // reads as an actual bounce.
    public void OnVehicleCollision(Vector3 hitPoint, PlayerController a, PlayerController b)
    {
        if (!IsGameActive || IsPaused || !IsTwoPlayerMode || a == null || b == null)
            return;

        if (eliminated[ResolvePlayerIndex(a)] || eliminated[ResolvePlayerIndex(b)])
            return;

        if (Time.timeSinceLevelLoad - lastVehicleCollisionTime < vehicleCollisionCooldown)
            return;

        lastVehicleCollisionTime = Time.timeSinceLevelLoad;

        Vector3 apart = a.transform.position - b.transform.position;
        apart.y = 0f;
        if (apart.sqrMagnitude < 0.001f)
            apart = Vector3.forward;

        // Knock each body away from the other (a ← away from b, b ← away from a).
        a.ApplyCrashKnockback(apart);
        b.ApplyCrashKnockback(-apart);

        int indexA = ResolvePlayerIndex(a);
        int indexB = ResolvePlayerIndex(b);
        // Approach speed must use the toward-other direction: -apart for A (a→b),
        // apart for B (b→a). Passing the away-from-other vector inverted rammer blame.
        float approachA = a.GetApproachSpeed(-apart);
        float approachB = b.GetApproachSpeed(apart);

        float penaltyA;
        float penaltyB;
        if (Mathf.Abs(approachA - approachB) <= 0.05f)
        {
            penaltyA = vehicleCrashTiePenalty;
            penaltyB = vehicleCrashTiePenalty;
        }
        else if (approachA > approachB)
        {
            penaltyA = vehicleCrashRammerPenalty;
            penaltyB = vehicleCrashVictimPenalty;
        }
        else
        {
            penaltyA = vehicleCrashVictimPenalty;
            penaltyB = vehicleCrashRammerPenalty;
        }

        AddClock(indexA, -penaltyA);
        AddClock(indexB, -penaltyB);
        hitsTaken += 2;   // Both players took a hit
        lastHitTime = Time.timeSinceLevelLoad;
        PlayVehicleCrashFeedback(hitPoint);
        StartPenaltyFeedback(indexA, penaltyA);
        StartPenaltyFeedback(indexB, penaltyB);
        UpdateTimerDisplay();
        CheckClockExpirations();
    }

    void PlayImpactFeedback(Vector3 hitPoint)
    {
        if (sfxSource != null && impactClip != null)
            sfxSource.PlayOneShot(impactClip);

        if (dustEffectPrefab != null)
        {
            GameObject dust = Instantiate(dustEffectPrefab, hitPoint, dustEffectPrefab.transform.rotation);
            Destroy(dust, 2f);   // Clean up so repeated hits can't pile up effects
        }

        if (cameraShake != null)
            cameraShake.Shake();
    }

    void PlayVehicleCrashFeedback(Vector3 hitPoint)
    {
        if (sfxSource != null)
        {
            if (vehicleCrashClip != null)
                sfxSource.PlayOneShot(vehicleCrashClip);
            else if (impactClip != null)
            {
                sfxSource.pitch = 0.7f;
                sfxSource.PlayOneShot(impactClip);
                sfxSource.pitch = 1f;
            }
        }

        if (dustEffectPrefab != null)
        {
            GameObject dust = Instantiate(dustEffectPrefab, hitPoint, dustEffectPrefab.transform.rotation);
            Destroy(dust, 2f);
        }

        if (cameraShake != null)
            cameraShake.Shake();
    }

    public void OnNearMiss()
    {
        OnNearMiss(null);
    }

    public void OnNearMiss(PlayerController who)
    {
        if (!IsGameActive || IsPaused)
            return;

        int index = ResolvePlayerIndex(who);
        if (eliminated[index])
            return;

        if (Time.timeSinceLevelLoad - lastNearMissTime[index] < nearMissCooldown)
            return;

        lastNearMissTime[index] = Time.timeSinceLevelLoad;
        AddClock(index, nearMissBonus);
        PlayNearMissSound(GetPlayer(index));
        StartBonusFeedback(index, nearMissBonus);
        UpdateTimerDisplay();
    }

    void PlayNearMissSound(PlayerController dodger)
    {
        if (nearMissClip == null)
            return;

        if (dodger == null)
            dodger = player;
        Vector3 position = dodger != null ? dodger.transform.position : transform.position;
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
    TMP_Text GetPenaltyPopup(int playerIndex) => playerIndex == 1 ? penaltyPopupP2 : penaltyPopupP1;

    TMP_Text GetTimerText(int playerIndex) => playerIndex == 1 ? timer2Text : timerText;

    Color GetTimerDefaultColor(int playerIndex) => playerIndex == 1 ? timerDefaultColorP2 : timerDefaultColorP1;

    // TwoPlayerUIStyler tints a "<Name>Bg" panel behind popups/the elimination banner; keep it
    // in sync whenever the text itself is toggled so it doesn't linger after the text hides.
    void SetHighlightPanelActive(TMP_Text label, bool active)
    {
        if (label == null)
            return;
        Transform panel = label.transform.parent.Find(label.gameObject.name + "Bg");
        if (panel != null)
            panel.gameObject.SetActive(active);
    }

    // Hit (−s) and near-miss (+s) share one popup per player; cancel any in-flight
    // feedback before starting a new one so overlapping coroutines can't fight.
    void StartPenaltyFeedback(int playerIndex, float seconds)
    {
        StopPopupFeedback(playerIndex);
        penaltyFeedbackRoutines[playerIndex] = StartCoroutine(PenaltyFeedback(playerIndex, seconds));
    }

    void StartBonusFeedback(int playerIndex, float seconds)
    {
        StopPopupFeedback(playerIndex);
        bonusFeedbackRoutines[playerIndex] = StartCoroutine(BonusFeedback(playerIndex, seconds));
    }

    void StopPopupFeedback(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex > 1)
            return;
        if (penaltyFeedbackRoutines[playerIndex] != null)
        {
            StopCoroutine(penaltyFeedbackRoutines[playerIndex]);
            penaltyFeedbackRoutines[playerIndex] = null;
        }
        if (bonusFeedbackRoutines[playerIndex] != null)
        {
            StopCoroutine(bonusFeedbackRoutines[playerIndex]);
            bonusFeedbackRoutines[playerIndex] = null;
        }
    }

    IEnumerator PenaltyFeedback(int playerIndex, float seconds)
    {
        var popup = GetPenaltyPopup(playerIndex);
        var timer = GetTimerText(playerIndex);
        if (popup != null)
        {
            popup.text = "-" + Mathf.RoundToInt(seconds) + "s";
            popup.color = PlayerColors.PenaltyRed;
            popup.gameObject.SetActive(true);
            SetHighlightPanelActive(popup, true);
        }
        if (timer != null)
            timer.color = PlayerColors.PenaltyRed;

        yield return new WaitForSeconds(0.6f);

        if (popup != null)
        {
            popup.gameObject.SetActive(false);
            SetHighlightPanelActive(popup, false);
            popup.color = playerIndex == 1 ? penaltyPopupDefaultColorP2 : penaltyPopupDefaultColorP1;
        }
        if (timer != null)
            timer.color = GetTimerDefaultColor(playerIndex);
        penaltyFeedbackRoutines[playerIndex] = null;
    }

    IEnumerator BonusFeedback(int playerIndex, float seconds)
    {
        var popup = GetPenaltyPopup(playerIndex);
        if (popup != null)
        {
            popup.text = "+" + Mathf.RoundToInt(seconds) + "s";
            popup.color = PlayerColors.BonusGreen;
            popup.gameObject.SetActive(true);
            SetHighlightPanelActive(popup, true);
        }

        yield return new WaitForSeconds(0.6f);

        if (popup != null)
        {
            popup.gameObject.SetActive(false);
            SetHighlightPanelActive(popup, false);
            popup.color = playerIndex == 1 ? penaltyPopupDefaultColorP2 : penaltyPopupDefaultColorP1;
        }
        bonusFeedbackRoutines[playerIndex] = null;
    }

    IEnumerator ShowEliminationBanner(int playerIndex)
    {
        if (eliminationBannerText == null)
            yield break;

        eliminationBannerText.text = PlayerColors.GetFullLabel(playerIndex) + " IS OUT!";
        eliminationBannerText.color = PlayerColors.GetColor(playerIndex);
        eliminationBannerText.gameObject.SetActive(true);
        SetHighlightPanelActive(eliminationBannerText, true);
        if (sfxSource != null && impactClip != null)
            sfxSource.PlayOneShot(impactClip);

        yield return new WaitForSeconds(1.5f);
        eliminationBannerText.gameObject.SetActive(false);
        SetHighlightPanelActive(eliminationBannerText, false);
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            string p1Label = IsTwoPlayerMode ? "P1: " : "Time: ";
            timerText.text = eliminated[0] && IsTwoPlayerMode
                ? "P1: OUT"
                : p1Label + Mathf.CeilToInt(Mathf.Max(displayedTimer, 0f));
        }

        if (timer2Text != null && IsTwoPlayerMode)
            timer2Text.text = eliminated[1]
                ? "P2: OUT"
                : "P2: " + Mathf.CeilToInt(Mathf.Max(displayedTimer2, 0f));
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
        if (!IsGameActive || IsPaused)
            return;

        bool showingPenalty = (penaltyPopupP1 != null && penaltyPopupP1.gameObject.activeSelf)
            || (penaltyPopupP2 != null && penaltyPopupP2.gameObject.activeSelf);
        if (showingPenalty)
            return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f);
        if (timerText != null && !eliminated[0] && timeRemaining <= lowTimeWarningThreshold)
            timerText.color = Color.Lerp(timerDefaultColorP1, lowTimeWarningColor, pulse);
        if (timer2Text != null && IsTwoPlayerMode && !eliminated[1] && timeRemaining2 <= lowTimeWarningThreshold)
            timer2Text.color = Color.Lerp(timerDefaultColorP2, lowTimeWarningColor, pulse);
    }

    void OnEnable()
    {
        InputModeWatcher.ModeChanged += UpdateControlsHint;
        MobileControlsUI.TouchControlsChanged += UpdateControlsHint;
        MobileControlsUI.TouchControlsChanged += UpdateTouchControlsButtonLabel;
        OrientationManager.OrientationPreferenceChanged += UpdateRotationButtonLabel;
        RearViewCameraController.RearViewPreferenceChanged += UpdateRearViewButtonLabel;
    }

    void OnDisable()
    {
        InputModeWatcher.ModeChanged -= UpdateControlsHint;
        MobileControlsUI.TouchControlsChanged -= UpdateControlsHint;
        MobileControlsUI.TouchControlsChanged -= UpdateTouchControlsButtonLabel;
        OrientationManager.OrientationPreferenceChanged -= UpdateRotationButtonLabel;
        RearViewCameraController.RearViewPreferenceChanged -= UpdateRearViewButtonLabel;
    }

    // Hint reflects whatever device the player last used (see InputModeWatcher)
    // and the touch-controls toggle (see MobileControlsUI), so a phone shows
    // touch instructions and a Steam Deck shows gamepad ones.
    void UpdateControlsHint()
    {
        if (controlsHintText == null)
            return;

        if (IsTwoPlayerMode)
        {
            if (MobileControlsUI.TouchControlsActive)
            {
                controlsHintText.text = "Dodge Obstacles\nP1 left stick, P2 right stick\nTap II to pause";
                return;
            }

            controlsHintText.text = InputModeWatcher.Mode == InputMode.Gamepad
                ? "Dodge Obstacles\nP1 left stick, P2 right stick\nEsc/Start pause"
                : "Dodge Obstacles\nP1 WASD, P2 Arrows\nEsc pause";
            return;
        }

        // The on-screen stick wins over the raw input mode: it can be forced on
        // from any device via the in-game toggle, or forced off on a phone.
        if (MobileControlsUI.TouchControlsActive)
        {
            controlsHintText.text = "Dodge Obstacles\nDrag stick to steer\nTap II to pause";
            return;
        }

        switch (InputModeWatcher.Mode)
        {
            case InputMode.Touch:
                controlsHintText.text = "Dodge Obstacles\nTouch controls off\nToggle them below";
                break;
            case InputMode.Gamepad:
                controlsHintText.text = "Dodge Obstacles\nLeft stick steer\nStart pause";
                break;
            default:
                controlsHintText.text = "Dodge Obstacles\nWASD steer\nEsc pause";
                break;
        }
    }

    void UpdateMusicButtonLabel()
    {
        if (musicButtonLabel != null && musicSource != null)
            musicButtonLabel.text = musicSource.mute ? "MUSIC: OFF" : "MUSIC: ON";
    }

    void UpdateRotationButtonLabel()
    {
        if (rotationButtonLabel != null)
            rotationButtonLabel.text = OrientationManager.ButtonLabel;
    }

    void UpdateTouchControlsButtonLabel()
    {
        if (touchControlsButtonLabel != null)
            touchControlsButtonLabel.text = MobileControlsUI.TouchControlsActive
                ? "TOUCH CONTROLS: ON" : "TOUCH CONTROLS: OFF";
    }

    void UpdateRearViewButtonLabel()
    {
        if (rearViewButtonLabel != null)
            rearViewButtonLabel.text = RearViewCameraController.ButtonLabel;
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
        displayedTimer = 0f;
        if (IsTwoPlayerMode)
        {
            timeRemaining2 = 0f;
            displayedTimer2 = 0f;
        }
        UpdateTimerDisplay();

        if (IsTwoPlayerMode)
            ShowTwoPlayerGameOver();
        else
            ShowSinglePlayerGameOver();

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
            ResolvePlayers();
        BeginPlayerExit(0);
        if (IsTwoPlayerMode)
            BeginPlayerExit(1);
    }

    void ShowSinglePlayerGameOver()
    {
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
    }

    // Competitive result: whoever survived longer wins. The leaderboard, NEW BEST
    // banner, and fireworks stay single-player only so records remain comparable.
    void ShowTwoPlayerGameOver()
    {
        bool tie = Mathf.Approximately(survivalTime[0], survivalTime[1]);
        string headline = tie
            ? "It's a Tie!"
            : survivalTime[0] > survivalTime[1] ? "Player 1 Wins!" : "Player 2 Wins!";

        if (!tie)
            SessionScore.RecordWin(survivalTime[0] > survivalTime[1] ? 0 : 1);

        if (runTimeText != null)
        {
            runTimeText.text = headline;
            runTimeText.color = tie ? Color.white : PlayerColors.GetColor(survivalTime[0] > survivalTime[1] ? 0 : 1);
        }

        string statsText = "P1: " + Mathf.FloorToInt(survivalTime[0]) + "s"
            + "\nP2: " + Mathf.FloorToInt(survivalTime[1]) + "s"
            + "\nDodged: " + rocksDodged
            + "\nHits: " + hitsTaken
            + "\n" + SessionScore.FormatLine();

        if (gameOverStatsText != null)
            gameOverStatsText.text = statsText;
        else if (runTimeText != null)
            runTimeText.text += "\n" + statsText;

        if (newBestText != null)
            newBestText.SetActive(false);

        if (!tie && fireworksPrefab != null)
        {
            GameObject fx = Instantiate(fireworksPrefab, new Vector3(0f, 1f, 2.5f), fireworksPrefab.transform.rotation);
            Destroy(fx, 6f);
        }
    }

    // Legacy no-arg overload: finish whichever vehicle is still mid exit-drive
    // (must not assume Player 1 — P2 can finish first in 2P).
    public void OnVehicleExitComplete()
    {
        PlayerController exited = null;
        if (players != null)
        {
            foreach (var p in players)
            {
                if (p != null && p.IsExiting)
                {
                    exited = p;
                    break;
                }
            }
        }
        OnVehicleExitComplete(exited != null ? exited : player);
    }

    public void OnVehicleExitComplete(PlayerController exited)
    {
        exitingVehicles = Mathf.Max(0, exitingVehicles - 1);
        IsVehicleExiting = exitingVehicles > 0;
        if (exited != null)
        {
            exited.enabled = false;
            exited.gameObject.SetActive(false);
        }
    }
}
