using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

// On-screen touch controls: a virtual stick (bottom-left) and a pause button
// (top-right). The stick is an Input System OnScreenStick that feeds
// <Gamepad>/leftStick, so PlayerController's gamepad binding drives the vehicle
// with no extra plumbing. Everything is built in code at runtime and only shown
// while the game is running in Touch mode; the scene is untouched.
public class MobileControlsUI : MonoBehaviour
{
    private const float StickAreaSize = 340f;
    private const float StickHandleSize = 130f;
    private const float StickMovementRange = 105f;
    private const float PauseButtonSize = 110f;

    private GameManager gameManager;
    private GameObject stickRoot;
    private GameObject pauseRoot;
    private OnScreenStick onScreenStick;
    private bool stickDeviceIgnored;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("MobileControlsUI");
        DontDestroyOnLoad(go);
        go.AddComponent<MobileControlsUI>();
    }

    private void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        BuildUI();
        SetShown(false);
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager == null)
                return;
        }

        bool show = InputModeWatcher.Mode == InputMode.Touch && gameManager.IsGameActive;
        SetShown(show);

        // The stick's virtual gamepad device only exists while the stick is
        // enabled; register it as ignored as soon as it resolves so touch drags
        // don't get misread as real gamepad input.
        if (show && !stickDeviceIgnored && onScreenStick != null && onScreenStick.control != null)
        {
            InputModeWatcher.IgnoreDevice(onScreenStick.control.device);
            stickDeviceIgnored = true;
        }
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
        bool showPause = show || (InputModeWatcher.Mode == InputMode.Touch
                                  && gameManager != null && gameManager.IsPaused);
        if (pauseRoot != null && pauseRoot.activeSelf != showPause)
            pauseRoot.SetActive(showPause);
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

        // --- Pause button, top-right ---
        pauseRoot = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var pauseRect = (RectTransform)pauseRoot.transform;
        pauseRect.SetParent(canvasGo.transform, false);
        pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(1f, 1f);
        pauseRect.pivot = new Vector2(0.5f, 0.5f);
        pauseRect.anchoredPosition = new Vector2(-100f, -100f);
        pauseRect.sizeDelta = new Vector2(PauseButtonSize, PauseButtonSize);
        var pauseImage = pauseRoot.GetComponent<Image>();
        pauseImage.sprite = circle;
        pauseImage.color = new Color(0f, 0f, 0f, 0.35f);
        pauseRoot.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (gameManager != null)
                gameManager.TogglePause();
        });

        var pauseLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRect = (RectTransform)pauseLabelGo.transform;
        labelRect.SetParent(pauseRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var label = pauseLabelGo.GetComponent<TextMeshProUGUI>();
        label.text = "II";
        label.fontStyle = FontStyles.Bold;
        label.fontSize = 48f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 1f, 1f, 0.85f);
        label.raycastTarget = false;
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
