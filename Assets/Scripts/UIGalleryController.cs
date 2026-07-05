using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Dev-only "storybook" for the game's UI: shows every screen and state with mock
// data so panels can be reviewed and tweaked without playing through a run.
// Lives only in the UIGallery scene; ships nowhere. Cycle states with the arrow
// keys, the 1-9 keys, or by clicking any button.
public class UIGalleryController : MonoBehaviour
{
    private static readonly Color PenaltyRed = new Color(0.95f, 0.25f, 0.2f);

    private Canvas canvas;
    private TMP_Text timerText;
    private TMP_Text bestTimeText;
    private TMP_Text runTimeText;
    private TMP_Text penaltyPopupText;
    private TMP_Text newBestLabel;
    private TMP_Text musicButtonLabel;
    private GameObject startPanel;
    private GameObject gameOverPanel;
    private GameObject pausePanel;
    private GameObject newBestText;
    private TMP_Text banner;

    private Color timerDefaultColor;
    private readonly Dictionary<GameObject, Vector2> panelBasePositions = new Dictionary<GameObject, Vector2>();

    private int stateIndex;
    private (string name, System.Action apply)[] states;

    void Awake()
    {
        canvas = FindAnyObjectByType<Canvas>();

        timerText = FindText("TimerText");
        bestTimeText = FindText("BestTimeText");
        runTimeText = FindText("RunTimeText");
        penaltyPopupText = FindText("PenaltyPopupText");
        musicButtonLabel = FindText("MusicButton", "Label");
        startPanel = FindObject("StartPanel");
        gameOverPanel = FindObject("GameOverPanel");
        pausePanel = FindObject("PausePanel");
        newBestText = FindObject("NewBestText");
        if (newBestText != null)
            newBestLabel = newBestText.GetComponent<TMP_Text>();

        if (timerText != null)
            timerDefaultColor = timerText.color;
        foreach (var panel in new[] { startPanel, gameOverPanel, pausePanel })
        {
            var rt = panel != null ? panel.GetComponent<RectTransform>() : null;
            if (rt != null)
                panelBasePositions[panel] = rt.anchoredPosition;
        }

        states = new (string, System.Action)[]
        {
            ("Start / empty leaderboard", () =>
            {
                SetTimer("Time: 60", timerDefaultColor);
                SetText(bestTimeText, "Best Times:\n—");
                ShowPanel(startPanel);
            }),
            ("Start / full leaderboard", () =>
            {
                SetTimer("Time: 60", timerDefaultColor);
                SetText(bestTimeText, "Best Times:\n1. 96s\n2. 71s\n3. 64s\n4. 38s\n5. 12s");
                ShowPanel(startPanel);
            }),
            ("HUD / mid-run", () =>
            {
                SetTimer("Time: 47", timerDefaultColor);
            }),
            ("HUD / penalty hit", () =>
            {
                SetTimer("Time: 42", PenaltyRed);
                if (penaltyPopupText != null)
                {
                    penaltyPopupText.text = "-5s";
                    penaltyPopupText.gameObject.SetActive(true);
                }
            }),
            ("Game Over / no record", () =>
            {
                SetTimer("Time: 0", timerDefaultColor);
                SetText(runTimeText, "Time: 23s");
                ShowPanel(gameOverPanel);
            }),
            ("Game Over / top five", () =>
            {
                SetTimer("Time: 0", timerDefaultColor);
                SetText(runTimeText, "Time: 58s");
                if (newBestText != null) newBestText.SetActive(true);
                SetText(newBestLabel, "TOP 5! #3");
                ShowPanel(gameOverPanel);
            }),
            ("Game Over / new best", () =>
            {
                SetTimer("Time: 0", timerDefaultColor);
                SetText(runTimeText, "Time: 104s");
                if (newBestText != null) newBestText.SetActive(true);
                SetText(newBestLabel, "NEW BEST!");
                ShowPanel(gameOverPanel);
            }),
            ("Paused / music on", () =>
            {
                SetTimer("Time: 31", timerDefaultColor);
                SetText(musicButtonLabel, "MUSIC: ON");
                ShowPanel(pausePanel);
            }),
            ("Paused / music off", () =>
            {
                SetTimer("Time: 31", timerDefaultColor);
                SetText(musicButtonLabel, "MUSIC: OFF");
                ShowPanel(pausePanel);
            }),
        };
    }

    void Start()
    {
        CreateBanner();
        foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            button.onClick.AddListener(NextState);
        ApplyState(0);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.rightArrowKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            NextState();
        else if (kb.leftArrowKey.wasPressedThisFrame)
            ApplyState((stateIndex - 1 + states.Length) % states.Length);

        for (int i = 0; i < states.Length && i < 9; i++)
        {
            if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                ApplyState(i);
        }
    }

    public void NextState()
    {
        ApplyState((stateIndex + 1) % states.Length);
    }

    void ApplyState(int index)
    {
        stateIndex = index;
        StopAllCoroutines();
        ResetAll();
        states[index].apply();
        if (banner != null)
            banner.text = $"UI GALLERY  {index + 1}/{states.Length}  •  {states[index].name}\n← → / 1-9 / click any button to cycle";
    }

    // Put every mutable piece back to a known baseline so states are order-independent.
    void ResetAll()
    {
        foreach (var panel in new[] { startPanel, gameOverPanel, pausePanel })
        {
            if (panel == null)
                continue;
            panel.SetActive(false);
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = 1f;
            var rt = panel.GetComponent<RectTransform>();
            if (rt != null && panelBasePositions.TryGetValue(panel, out var basePos))
                rt.anchoredPosition = basePos;   // a cancelled UIPanelTransition can leave it offset
        }
        if (newBestText != null)
            newBestText.SetActive(false);
        if (penaltyPopupText != null)
            penaltyPopupText.gameObject.SetActive(false);
    }

    void ShowPanel(GameObject panel)
    {
        if (panel != null)
            StartCoroutine(UIPanelTransition.Show(panel));
    }

    void SetTimer(string text, Color color)
    {
        if (timerText == null)
            return;
        timerText.text = text;
        timerText.color = color;
    }

    static void SetText(TMP_Text target, string text)
    {
        if (target != null)
            target.text = text;
    }

    // Name lookup that also finds inactive objects (GameObject.Find can't).
    GameObject FindObject(params string[] path)
    {
        Transform current = null;
        foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == path[0])
            {
                current = t;
                break;
            }
        }
        if (current == null)
        {
            Debug.LogWarning($"UIGallery: '{path[0]}' not found under the Canvas");
            return null;
        }
        for (int i = 1; i < path.Length; i++)
        {
            current = current.Find(path[i]);
            if (current == null)
            {
                Debug.LogWarning($"UIGallery: '{string.Join("/", path)}' not found under the Canvas");
                return null;
            }
        }
        return current.gameObject;
    }

    TMP_Text FindText(params string[] path)
    {
        var go = FindObject(path);
        return go != null ? go.GetComponent<TMP_Text>() : null;
    }

    void CreateBanner()
    {
        var go = new GameObject("GalleryBanner", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 64f);
        rt.anchoredPosition = new Vector2(0f, -6f);
        banner = go.AddComponent<TextMeshProUGUI>();
        banner.fontSize = 20f;
        banner.alignment = TextAlignmentOptions.Top;
        banner.color = new Color(1f, 1f, 1f, 0.9f);
    }
}
