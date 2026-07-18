using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

// On-screen touch controls: virtual sticks (P1 left / P2 right) and a
// persistent top-left controls hint. Sticks are Input System OnScreenStick
// controls that feed <Gamepad>/leftStick and <Gamepad>/rightStick, matching
// PlayerController's 1P/2P bindings. Everything is built in code at runtime;
// the scene is untouched. Pausing on touch devices is done by tapping the
// timer HUD (see GameManager/TimerPauseTapHandler), so this component has no
// pause button of its own.
//
// Whether the sticks appear is decided by the TOUCH CONTROLS toggle in the
// pause menu (a runtime-built pause-panel button, see
// GameManager.EnsureRuntimeUiRefs, which calls ToggleTouchControlsPref here):
// until the player uses it, the controls follow Touch mode automatically (the
// pre-toggle behavior); once toggled, the explicit on/off choice wins and is
// persisted.
public class MobileControlsUI : MonoBehaviour
{
    private const float StickAreaSize = 340f;
    private const float StickHandleSize = 130f;
    private const float StickMovementRange = 105f;
    private const float StickInsetX = 260f;
    private const float StickInsetY = 240f;
    private const float SafeAreaPadding = 24f;

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
            // A hardware keyboard attached to iOS Simulator is not evidence
            // that the player stopped using the phone's touch controls.
            if (Application.isMobilePlatform)
                return true;
            return InputModeWatcher.Mode == InputMode.Touch;
        }
    }

    private GameManager gameManager;
    private GameObject stickRoot;       // P1 left stick
    private GameObject stickRootP2;     // P2 right stick (2P only)
    private GameObject hintRoot;
    private TextMeshProUGUI hintLabel;
    private OnScreenStick onScreenStick;
    private OnScreenStick onScreenStickP2;
    private bool stickDeviceIgnored;
    private bool stickDeviceP2Ignored;
    private Canvas controlsCanvas;
    private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
    private Vector2Int lastScreenSize = new Vector2Int(-1, -1);

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
        SetLeftStickShown(false);
        SetRightStickShown(false);
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

        bool touchActive = TouchControlsActive && gameManager.IsGameActive && !gameManager.IsPaused;
        bool twoPlayer = gameManager.IsTwoPlayerMode;
        SetLeftStickShown(touchActive);
        SetRightStickShown(touchActive && twoPlayer);

        // The sticks' virtual gamepad device only exists while a stick is
        // enabled; register it as ignored as soon as it resolves so touch drags
        // don't get misread as real gamepad input.
        TryIgnoreStickDevice(onScreenStick, false);
        TryIgnoreStickDevice(onScreenStickP2, true);

        ApplySafeAreaInsets();
        RefreshHint();
    }

    private void TryIgnoreStickDevice(OnScreenStick stick, bool playerTwo)
    {
        if ((playerTwo ? stickDeviceP2Ignored : stickDeviceIgnored)
            || stick == null || stick.control == null)
            return;
        InputModeWatcher.IgnoreDevice(stick.control.device);
        if (playerTwo)
            stickDeviceP2Ignored = true;
        else
            stickDeviceIgnored = true;
    }

    private void SetLeftStickShown(bool show)
    {
        if (stickRoot == null || stickRoot.activeSelf == show)
            return;
        stickRoot.SetActive(show);
        if (show)
            TryIgnoreStickDevice(onScreenStick, false);
        // Disabling the stick destroys its virtual gamepad; the replacement
        // device created on the next show must be registered as ignored again.
        if (!show)
            stickDeviceIgnored = false;
    }

    private void SetRightStickShown(bool show)
    {
        if (stickRootP2 == null || stickRootP2.activeSelf == show)
            return;
        stickRootP2.SetActive(show);
        if (show)
            TryIgnoreStickDevice(onScreenStickP2, true);
        if (!show)
            stickDeviceP2Ignored = false;
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
            if (controlsOn)
                return "P1 left stick, P2 right stick\nTap the timer to pause";
            return mode == InputMode.Gamepad
                ? "P1 left stick, P2 right stick\nEsc / Start pauses"
                : "P1 WASD, P2 arrows\nEsc pauses";
        }
        if (controlsOn)
            return "Drag stick to steer\nTap the timer to pause";
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
        controlsCanvas = canvasGo.GetComponent<Canvas>();
        controlsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        controlsCanvas.sortingOrder = 100;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Sprite circle = CreateCircleSprite(128);

        // --- P1 virtual stick, bottom-left → <Gamepad>/leftStick ---
        stickRoot = BuildStick(canvasGo.transform, "StickArea", circle,
            new Vector2(0f, 0f), new Vector2(StickInsetX, StickInsetY),
            "<Gamepad>/leftStick", out onScreenStick);

        // --- P2 virtual stick, bottom-right → <Gamepad>/rightStick ---
        stickRootP2 = BuildStick(canvasGo.transform, "StickAreaP2", circle,
            new Vector2(1f, 0f), new Vector2(-StickInsetX, StickInsetY),
            "<Gamepad>/rightStick", out onScreenStickP2);

        // --- Controller hints, top-left (same spot as the start screen's own
        // hint text) ---
        hintRoot = new GameObject("ControlsHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        var hintRect = (RectTransform)hintRoot.transform;
        hintRect.SetParent(canvasGo.transform, false);
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0f, 1f);
        hintRect.pivot = new Vector2(0f, 1f);
        hintRect.anchoredPosition = new Vector2(40f, -64f);
        hintRect.sizeDelta = new Vector2(560f, 120f);
        hintLabel = hintRoot.GetComponent<TextMeshProUGUI>();
        hintLabel.fontSize = 32f;
        hintLabel.alignment = TextAlignmentOptions.TopLeft;
        hintLabel.color = new Color(1f, 1f, 1f, 0.75f);
        hintLabel.raycastTarget = false;
    }

    private static GameObject BuildStick(Transform parent, string name, Sprite circle,
        Vector2 anchor, Vector2 anchoredPosition, string controlPath, out OnScreenStick stick)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(OnScreenStickTouchArea));
        var areaRect = (RectTransform)root.transform;
        areaRect.SetParent(parent, false);
        areaRect.anchorMin = areaRect.anchorMax = anchor;
        areaRect.pivot = new Vector2(0.5f, 0.5f);
        areaRect.anchoredPosition = anchoredPosition;
        areaRect.sizeDelta = new Vector2(StickAreaSize, StickAreaSize);
        var areaImage = root.GetComponent<Image>();
        areaImage.sprite = circle;
        areaImage.color = new Color(1f, 1f, 1f, 0.18f);

        var handleGo = new GameObject("StickHandle", typeof(RectTransform), typeof(Image), typeof(OnScreenStick));
        var handleRect = (RectTransform)handleGo.transform;
        handleRect.SetParent(areaRect, false);
        handleRect.sizeDelta = new Vector2(StickHandleSize, StickHandleSize);
        var handleImage = handleGo.GetComponent<Image>();
        handleImage.sprite = circle;
        handleImage.color = new Color(1f, 1f, 1f, 0.55f);
        // The outer ring owns the raycast so every visible part of the control
        // starts a drag, not just the small center knob.
        handleImage.raycastTarget = false;
        stick = handleGo.GetComponent<OnScreenStick>();
        stick.controlPath = controlPath;
        stick.movementRange = StickMovementRange;
        // Isolated pointer actions prevent a virtual gamepad device change
        // from cancelling an in-progress touch on iOS.
        stick.useIsolatedInputActions = true;
        root.GetComponent<OnScreenStickTouchArea>().Initialize(stick);
        return root;
    }

    private void ApplySafeAreaInsets()
    {
        if (controlsCanvas == null || stickRoot == null || stickRootP2 == null)
            return;

        Rect safeArea = Screen.safeArea;
        var screenSize = new Vector2Int(Screen.width, Screen.height);
        if (safeArea == lastSafeArea && screenSize == lastScreenSize)
            return;

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;

        float scale = Mathf.Max(controlsCanvas.scaleFactor, 0.01f);
        float halfStick = StickAreaSize * 0.5f;
        float leftInset = safeArea.xMin / scale;
        float rightInset = (Screen.width - safeArea.xMax) / scale;
        float bottomInset = safeArea.yMin / scale;
        float leftX = Mathf.Max(StickInsetX, leftInset + halfStick + SafeAreaPadding);
        float rightX = Mathf.Max(StickInsetX, rightInset + halfStick + SafeAreaPadding);
        float y = Mathf.Max(StickInsetY, bottomInset + halfStick + SafeAreaPadding);

        ((RectTransform)stickRoot.transform).anchoredPosition = new Vector2(leftX, y);
        ((RectTransform)stickRootP2.transform).anchoredPosition = new Vector2(-rightX, y);
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
