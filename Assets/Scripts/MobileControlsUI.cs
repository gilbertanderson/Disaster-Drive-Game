using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

// On-screen touch controls: a virtual stick (bottom-left) and a persistent
// top-left stack — controls hint and pause button. The stick is an Input
// System OnScreenStick that feeds <Gamepad>/leftStick, so PlayerController's
// gamepad binding drives the vehicle with no extra plumbing. Everything is
// built in code at runtime; the scene is untouched.
//
// Whether the stick appears is decided by the TOUCH CONTROLS toggle in the
// pause menu (a runtime-built pause-panel button, see
// GameManager.EnsureRuntimeUiRefs, which calls ToggleTouchControlsPref here):
// until the player uses it, the controls follow Touch mode automatically (the
// pre-toggle behavior); once toggled, the explicit on/off choice wins and is
// persisted. The pause button is independent of the toggle: it stays
// available for every input device whenever a run is active or paused.
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

    // Raised when the player flips the toggle, so the controls hint and the
    // pause menu's button label (GameManager) can re-render without polling.
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
    private TextMeshProUGUI hintLabel;
    private OnScreenStick onScreenStick;
    private bool stickDeviceIgnored;

    // Last state the hint label was rendered for; avoids rebuilding the
    // string every frame.
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
        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (hintRoot != null) hintRoot.SetActive(false);
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

        // The pause button is part of the persistent top-left stack: reachable
        // for any input device during a run and while paused (the same button
        // resumes), independent of the touch-controls toggle.
        bool showPause = gameManager.IsGameActive || gameManager.IsPaused;
        if (pauseRoot != null && pauseRoot.activeSelf != showPause)
            pauseRoot.SetActive(showPause);

        // The stick's virtual gamepad device only exists while the stick is
        // enabled; register it as ignored as soon as it resolves so touch drags
        // don't get misread as real gamepad input.
        if (show && !stickDeviceIgnored && onScreenStick != null && onScreenStick.control != null)
        {
            InputModeWatcher.IgnoreDevice(onScreenStick.control.device);
            stickDeviceIgnored = true;
        }

        RefreshHint();
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
    }

    private void RefreshHint()
    {
        // The overlay hint only shows during runs — the start screen has its
        // own controls hint in the same top-left spot.
        bool showHint = gameManager.IsGameActive || gameManager.IsPaused;

        if (hintRoot != null && hintRoot.activeSelf != showHint)
            hintRoot.SetActive(showHint);
        if (!showHint)
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
            return "Drag stick to steer\nTap II or the timer to pause";
        if (mode == InputMode.Touch)
            return "Touch controls are off\nTap the timer to pause";
        return mode == InputMode.Gamepad
            ? "Left stick steers\nStart pauses"
            : "WASD steers\nEsc pauses";
    }

    // Entry point for the pause menu's TOUCH CONTROLS button (built by
    // GameManager.EnsureRuntimeUiRefs): flips the explicit on/off choice.
    public static void ToggleTouchControlsPref()
    {
        SetTouchControlsPref(TouchControlsActive ? PrefOff : PrefOn);
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

        // --- Pause button, top-left under the hints (persistent during runs) ---
        pauseRoot = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var pauseRect = (RectTransform)pauseRoot.transform;
        pauseRect.SetParent(canvasGo.transform, false);
        pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(0f, 1f);
        pauseRect.pivot = new Vector2(0.5f, 0.5f);
        pauseRect.anchoredPosition = new Vector2(220f, -330f);
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

        // --- Controller hints, top-left (same spot as the start screen's own
        // hint text) ---
        hintRoot = new GameObject("ControlsHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        var hintRect = (RectTransform)hintRoot.transform;
        hintRect.SetParent(canvasGo.transform, false);
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0f, 1f);
        hintRect.pivot = new Vector2(0f, 1f);
        hintRect.anchoredPosition = new Vector2(40f, -40f);
        hintRect.sizeDelta = new Vector2(560f, 120f);
        hintLabel = hintRoot.GetComponent<TextMeshProUGUI>();
        hintLabel.fontSize = 32f;
        hintLabel.alignment = TextAlignmentOptions.TopLeft;
        hintLabel.color = new Color(1f, 1f, 1f, 0.75f);
        hintLabel.raycastTarget = false;
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
}
