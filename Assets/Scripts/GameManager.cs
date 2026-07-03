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

    [Header("Difficulty")]
    [SerializeField] private float rampInterval = 10f;             // Seconds between difficulty bumps
    [SerializeField] private float playerSpeedIncrease = 0.75f;    // Added to the vehicle's speed each bump
    [SerializeField] private float rockSpeedIncrease = 0.5f;       // Added to newly spawned rocks' speed each bump
    [SerializeField] private float spawnIntervalMultiplier = 0.9f; // Spawn interval shrinks by this factor each bump

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject startPanel;      // Title, best time, vehicle picker, and Drive button
    [SerializeField] private GameObject gameOverPanel;   // GAME OVER text, run time, and Retry button
    [SerializeField] private GameObject pausePanel;      // PAUSED overlay with resume and music buttons
    [SerializeField] private TMP_Text bestTimeText;      // Longest-survival record shown on the start screen
    [SerializeField] private TMP_Text runTimeText;       // This playthrough's survival time, on the game over screen
    [SerializeField] private GameObject newBestText;     // "NEW BEST!" banner, shown when the record is beaten
    [SerializeField] private TMP_Text penaltyPopupText;  // "-5s" popup that flashes when a rock is hit
    [SerializeField] private TMP_Text musicButtonLabel;  // Pause overlay button label ("MUSIC: ON/OFF")

    [Header("Impact Feedback")]
    [SerializeField] private AudioClip impactClip;         // Honk played when the vehicle hits a rock
    [SerializeField] private GameObject dustEffectPrefab;  // Dust burst spawned at the point of impact

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;      // Looping background music
    [SerializeField] private AudioClip clickClip;          // UI button click
    [SerializeField] private float menuMusicVolume = 0.3f; // Music volume on the start/game over screens
    [SerializeField] private float playMusicVolume = 0.6f; // Music volume during a run

    [Header("Celebration")]
    [SerializeField] private GameObject fireworksPrefab;   // Spawned when a run sets a new best time

    // False on the start screen and after game over; the player and spawner only run while true.
    public bool IsGameActive { get; private set; }
    public bool IsPaused { get; private set; }

    private float timeRemaining;
    private float gameStartTime;   // Time.timeSinceLevelLoad when the Drive button was pressed
    private float nextRampTime;
    private AudioSource sfxSource;
    private PlayerController player;
    private SpawnManager spawnManager;
    private Color timerDefaultColor;

    void Start()
    {
        timeRemaining = startTime;
        sfxSource = GetComponent<AudioSource>();
        player = FindAnyObjectByType<PlayerController>();
        spawnManager = FindAnyObjectByType<SpawnManager>();

        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (newBestText != null) newBestText.SetActive(false);
        if (penaltyPopupText != null) penaltyPopupText.gameObject.SetActive(false);
        if (timerText != null) timerDefaultColor = timerText.color;

        UpdateLeaderboardDisplay();

        if (musicSource != null)
        {
            musicSource.mute = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
            musicSource.volume = menuMusicVolume;
        }
        UpdateMusicButtonLabel();
        UpdateTimerDisplay();
    }

    void Update()
    {
        // Esc pauses/resumes mid-run
        if (IsGameActive && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();

        if (!IsGameActive || IsPaused)
            return;

        timeRemaining -= Time.deltaTime;
        UpdateTimerDisplay();

        // Difficulty ramp: every rampInterval seconds the vehicle accelerates and the
        // rocks spawn sooner and travel faster, so dodging gets progressively harder.
        if (Time.timeSinceLevelLoad >= nextRampTime)
        {
            nextRampTime += rampInterval;
            if (player != null) player.speed += playerSpeedIncrease;
            if (spawnManager != null) spawnManager.IncreaseDifficulty(rockSpeedIncrease, spawnIntervalMultiplier);
        }

        if (timeRemaining <= 0f)
            GameOver();
    }

    // Wired to the start screen's Drive button.
    public void StartGame()
    {
        if (IsGameActive)
            return;

        IsGameActive = true;
        gameStartTime = Time.timeSinceLevelLoad;
        nextRampTime = gameStartTime + rampInterval;
        if (startPanel != null) startPanel.SetActive(false);
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
        if (pausePanel != null) pausePanel.SetActive(IsPaused);
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
        if (sfxSource != null && impactClip != null)
            sfxSource.PlayOneShot(impactClip);

        if (dustEffectPrefab != null)
        {
            GameObject dust = Instantiate(dustEffectPrefab, hitPoint, dustEffectPrefab.transform.rotation);
            Destroy(dust, 2f);   // Clean up so repeated hits can't pile up effects
        }

        StartCoroutine(PenaltyFeedback());
        UpdateTimerDisplay();
        if (timeRemaining <= 0f)
            GameOver();
    }

    // Flash a "-5s" popup and turn the timer red for a moment so the cost of a hit is obvious.
    IEnumerator PenaltyFeedback()
    {
        if (penaltyPopupText != null)
        {
            penaltyPopupText.text = "-" + Mathf.RoundToInt(hitPenalty) + "s";
            penaltyPopupText.gameObject.SetActive(true);
        }
        if (timerText != null) timerText.color = new Color(0.95f, 0.25f, 0.2f);

        yield return new WaitForSeconds(0.6f);

        if (penaltyPopupText != null) penaltyPopupText.gameObject.SetActive(false);
        if (timerText != null) timerText.color = timerDefaultColor;
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
            timerText.text = "Time: " + Mathf.CeilToInt(Mathf.Max(timeRemaining, 0f));
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
        if (runTimeText != null)
            runTimeText.text = "Time: " + Mathf.FloorToInt(survival) + "s";

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
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (musicSource != null) musicSource.volume = menuMusicVolume;
        if (spawnManager != null) spawnManager.StopSpawning();
        if (player != null) player.enabled = false;   // Freeze the vehicle where it stands
    }
}
