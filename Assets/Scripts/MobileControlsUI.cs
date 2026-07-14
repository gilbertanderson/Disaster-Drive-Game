using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

// On-screen touch controls: a virtual stick (bottom-left) and a pause button
// (bottom-right), plus a top-right block with a controls hint and, under it, a
// TOUCH CONTROLS toggle. The stick is an Input System OnScreenStick that feeds
// <Gamepad>/leftStick, so PlayerController's gamepad binding drives the vehicle
// with no extra plumbing. Everything is built in code at runtime and only shown
// while the game is running; the scene is untouched.
//
// Whether the stick and pause button appear is decided by the toggle: until the
// player uses it, the controls follow Touch mode automatically (the pre-toggle
// behavior); once toggled, the explicit on/off choice wins and is persisted.
public class MobileControlsUI : MonoBehaviour
{
    private const float StickAreaSize = 340f;
    private const float StickHandleSize = 130f;
    private const float StickMovementRange = 105f;
    private const float PauseButtonSize = 110f;

    public const string TouchControlsPrefKey = "TouchControlsEnabled";
    private const int PrefUnloaded = int.MinValue; // Sentinel: PlayerPrefs not read yet
    private const int PrefAuto = -1;               // No explicit choice: follow Touch mode
    private const int PrefOff = 0;
    private const int PrefOn = 1;

    private static int touchControlsPref = PrefUnloaded;

    // Raised when the player flips the toggle, so the start screen's controls
    // hint (GameManager) can re-render without polling.
    public static event Action TouchControlsChanged;

    // True when the on-screen controls should be driving the game: the player
    // forced them on, or made no explicit choice and the game is in Touch mode.
    public static bool TouchControlsActive
    {
        get
        {
            if (touchControlsPref == PrefUnloaded)
                touchControlsPref = PlayerPrefs.GetInt(TouchControlsPrefKey, PrefAuto);
            if (touchControlsPref == PrefOn)
                return true;
            if (touchControlsPref == PrefOff)
                return false;
            return InputModeWatcher.Mode == InputMode.Touch;
        }
    }

    private GameManager gameManager;
    private GameObject stickRoot;
    private GameObject pauseRoot;
    private GameObject hintRoot;
    private GameObject toggleRoot;
    private TextMeshProUGUI hintLabel;
    private TextMeshProUGUI toggleLabel;
    private OnScreenStick onScreenStick;
    private bool stickDeviceIgnored;

    // Last state the top-right labels were rendered for; avoids rebuilding the
    // strings every frame.
    private bool labelsRendered;
    private InputMode labelMode;
    private bool labelControlsOn;
    private bool labelTwoPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("MobileControlsUI");
        DontDestroyOnLoad(go);
        go.AddComponent<MobileControlsUI>();
    }

    private void Start()
    {
#if UNITY_EDITOR
        // Let mouse drags stand in for touches so the stick is testable in the
        // Editor without a touchscreen.
        UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
#endif
        gameManager = FindAnyObjectByType<GameManager>();
        EnsureEventSystem();
        BuildUI();
        SetShown(false);
        if (hintRoot != null) hintRoot.SetActive(false);
        if (toggleRoot != null) toggleRoot.SetActive(false);
    }

    // The stick and the buttons are pointer-driven UI; without an EventSystem
    // (some scenes may lack one) touches would silently do nothing.
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
            return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager == null)
                return;
        }

        bool show = TouchControlsActive && gameManager.IsGameActive;
        SetShown(show);

        // The stick's virtual gamepad device only exists while the stick is
        // enabled; register it as ignored as soon as it resolves so touch drags
        // don't get misread as real gamepad input.
        if (show && !stickDeviceIgnored && onScreenStick != null && onScreenStick.control != null)
        {
            InputModeWatcher.IgnoreDevice(onScreenStick.control.device);
            stickDeviceIgnored = true;
        }

        RefreshHintAndToggle();
    }

    private void SetShown(bool show)
    {
        if (stickRoot != null && stickRoot.activeSelf != show)
        {
            stickRoot.SetActive(show);
            // Disabling the stick destroys its virtual gamepad; the replacement
            // device created on the next show must be registered as ignored again.
            if (!show)
                stickDeviceIgnored = false;
        }
        // Keep pause reachable while paused so the same button resumes.
        bool showPause = show || (TouchControlsActive
                                  && gameManager != null && gameManager.IsPaused);
        if (pauseRoot != null && pauseRoot.activeSelf != showPause)
            pauseRoot.SetActive(showPause);
    }

    private void RefreshHintAndToggle()
    {
        // The hint and toggle live above the run, not the menus: the start
        // screen has its own controls hint, and the toggle would collide with
        // the start panel's top-right layout.
        bool runVisible = gameManager.IsGameActive || gameManager.IsPaused;
        bool showToggle = runVisible && TouchPlausible();

        if (hintRoot != null && hintRoot.activeSelf != runVisible)
            hintRoot.SetActive(runVisible);
        if (toggleRoot != null && toggleRoot.activeSelf != showToggle)
            toggleRoot.SetActive(showToggle);
        if (!runVisible)
            return;

        bool controlsOn = TouchControlsActive;
        bool twoPlayer = gameManager.IsTwoPlayerMode;
        InputMode mode = InputModeWatcher.Mode;
        if (labelsRendered && mode == labelMode
            && controlsOn == labelControlsOn && twoPlayer == labelTwoPlayer)
            return;
        labelsRendered = true;
        labelMode = mode;
        labelControlsOn = controlsOn;
        labelTwoPlayer = twoPlayer;

        if (toggleLabel != null)
            toggleLabel.text = controlsOn ? "TOUCH CONTROLS: ON" : "TOUCH CONTROLS: OFF";
        if (hintLabel != null)
            hintLabel.text = BuildHint(mode, controlsOn, twoPlayer);
    }

    private static string BuildHint(InputMode mode, bool controlsOn, bool twoPlayer)
    {
        if (twoPlayer)
        {
            return mode == InputMode.Gamepad
                ? "P1 WASD, P2 gamepad\nEsc / Start pauses"
                : "P1 WASD, P2 arrows\nEsc pauses";
        }
        if (controlsOn)
            return "Drag stick to steer\nTap II to pause";
        if (mode == InputMode.Touch)
            return "Touch controls are off\nTap the toggle below";
        return mode == InputMode.Gamepad
            ? "Left stick steers\nStart pauses"
            : "WASD steers\nEsc pauses";
    }

    // The toggle is pointless on hardware that can't tap it; only offer it when
    // touch input is plausible, or the player has used it before (so it can
    // always be turned back off/on).
    private static bool TouchPlausible()
    {
        if (touchControlsPref == PrefOn || touchControlsPref == PrefOff)
            return true;
        return Application.isMobilePlatform
            || Touchscreen.current != null
            || InputModeWatcher.Mode == InputMode.Touch;
    }

    private static void SetTouchControlsPref(int value)
    {
        touchControlsPref = value;
        PlayerPrefs.SetInt(TouchControlsPrefKey, value);
        TouchControlsChanged?.Invoke();
    }

    private void BuildUI()
    {
        // Own overlay canvas so we never disturb the game's authored canvas.
        var canvasGo = new GameObject("MobileControlsCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Sprite circle = CreateCircleSprite(128);

        // --- Virtual stick, bottom-left ---
        stickRoot = new GameObject("StickArea", typeof(RectTransform), typeof(Image));
        var areaRect = (RectTransform)stickRoot.transform;
        areaRect.SetParent(canvasGo.transform, false);
        areaRect.anchorMin = areaRect.anchorMax = new Vector2(0f, 0f);
        areaRect.pivot = new Vector2(0.5f, 0.5f);
        areaRect.anchoredPosition = new Vector2(260f, 240f);
        areaRect.sizeDelta = new Vector2(StickAreaSize, StickAreaSize);
        var areaImage = stickRoot.GetComponent<Image>();
        areaImage.sprite = circle;
        areaImage.color = new Color(1f, 1f, 1f, 0.18f);

        var handleGo = new GameObject("StickHandle", typeof(RectTransform), typeof(Image), typeof(OnScreenStick));
        var handleRect = (RectTransform)handleGo.transform;
        handleRect.SetParent(areaRect, false);
        handleRect.sizeDelta = new Vector2(StickHandleSize, StickHandleSize);
        handleGo.GetComponent<Image>().sprite = circle;
        handleGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.55f);
        onScreenStick = handleGo.GetComponent<OnScreenStick>();
        onScreenStick.controlPath = "<Gamepad>/leftStick";
        onScreenStick.movementRange = StickMovementRange;

        // --- Pause button, bottom-right (mirrors the stick, under the thumb) ---
        pauseRoot = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var pauseRect = (RectTransform)pauseRoot.transform;
        pauseRect.SetParent(canvasGo.transform, false);
        pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(1f, 0f);
        pauseRect.pivot = new Vector2(0.5f, 0.5f);
        pauseRect.anchoredPosition = new Vector2(-200f, 240f);
        pauseRect.sizeDelta = new Vector2(PauseButtonSize, PauseButtonSize);
        var pauseImage = pauseRoot.GetComponent<Image>();
        pauseImage.sprite = circle;
        pauseImage.color = new Color(0f, 0f, 0f, 0.35f);
        pauseRoot.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (gameManager != null)
                gameManager.TogglePause();
        });
        AddLabel(pauseRect, "II", 48f, TextAlignmentOptions.Center);

        // --- Controller hints, top-right ---
        hintRoot = new GameObject("ControlsHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        var hintRect = (RectTransform)hintRoot.transform;
        hintRect.SetParent(canvasGo.transform, false);
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.anchoredPosition = new Vector2(-40f, -30f);
        hintRect.sizeDelta = new Vector2(560f, 100f);
        hintLabel = hintRoot.GetComponent<TextMeshProUGUI>();
        hintLabel.fontSize = 32f;
        hintLabel.alignment = TextAlignmentOptions.TopRight;
        hintLabel.color = new Color(1f, 1f, 1f, 0.75f);
        hintLabel.raycastTarget = false;

        // --- Touch-controls toggle, top-right under the hints ---
        toggleRoot = new GameObject("TouchControlsToggle", typeof(RectTransform), typeof(Image), typeof(Button));
        var toggleRect = (RectTransform)toggleRoot.transform;
        toggleRect.SetParent(canvasGo.transform, false);
        toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1f, 1f);
        toggleRect.pivot = new Vector2(1f, 1f);
        toggleRect.anchoredPosition = new Vector2(-40f, -140f);
        toggleRect.sizeDelta = new Vector2(360f, 72f);
        var toggleImage = toggleRoot.GetComponent<Image>();
        toggleImage.sprite = CreateRoundedRectSprite(128, 64, 30);
        toggleImage.type = Image.Type.Sliced;
        toggleImage.color = new Color(0f, 0f, 0f, 0.35f);
        toggleRoot.GetComponent<Button>().onClick.AddListener(() =>
        {
            SetTouchControlsPref(TouchControlsActive ? PrefOff : PrefOn);
        });
        toggleLabel = AddLabel(toggleRect, "TOUCH CONTROLS: OFF", 28f, TextAlignmentOptions.Center);
    }

    private static TextMeshProUGUI AddLabel(RectTransform parent, string text,
        float fontSize, TextAlignmentOptions alignment)
    {
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRect = (RectTransform)labelGo.transform;
        labelRect.SetParent(parent, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontStyle = FontStyles.Bold;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = new Color(1f, 1f, 1f, 0.85f);
        label.raycastTarget = false;
        return label;
    }

    // Anti-aliased filled circle so the controls need no sprite assets.
    private static Sprite CreateCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f - 1f;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(r - d + 0.5f);   // 1px soft edge
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // Anti-aliased rounded rectangle with a 9-slice border sized to the corner
    // radius, so the toggle can stretch to any pill shape without distortion.
    private static Sprite CreateRoundedRectSprite(int width, int height, int radius)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(width * 0.5f, height * 0.5f);
        Vector2 inner = half - new Vector2(radius + 1f, radius + 1f);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Signed distance from the rounded-rect edge (negative inside).
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - inner;
                float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
                float dist = outside + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                float a = Mathf.Clamp01(-dist + 0.5f);   // 1px soft edge
                pixels[y * width + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        float border = radius + 2f;
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }
}
