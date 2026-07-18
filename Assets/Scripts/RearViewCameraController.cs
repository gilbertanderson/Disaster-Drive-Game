using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Optional rear-facing inset camera. It renders only while a run is active and
// the pause-menu preference is enabled, leaving the main gameplay camera and
// camera-relative steering unchanged.
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class RearViewCameraController : MonoBehaviour
{
    public const string RearViewPrefKey = "RearViewCameraEnabled";

    private const int PrefUnloaded = int.MinValue;
    private const int PrefOff = 0;
    private const int PrefOn = 1;
    private const int TextureWidth = 640;
    private const int TextureHeight = 240;

    private static int rearViewPref = PrefUnloaded;

    public static event Action RearViewPreferenceChanged;

    [SerializeField] private float cameraHeight = 3.5f;
    [SerializeField] private float cameraLead = 2f;
    [SerializeField] private float lookDistance = 14f;

    private Camera sourceCamera;
    private Camera rearCamera;
    private RenderTexture rearTexture;
    private GameObject viewRoot;
    private Canvas viewCanvas;
    private GameManager gameManager;
    private PlayerController[] players;
    private Vector3 behindDirection;
    private bool viewVisible;

    public static bool RearViewEnabled
    {
        get
        {
            if (rearViewPref == PrefUnloaded)
                rearViewPref = PlayerPrefs.GetInt(RearViewPrefKey, PrefOff);
            return rearViewPref == PrefOn;
        }
    }

    public static string ButtonLabel => RearViewEnabled
        ? "REAR VIEW: ON"
        : "REAR VIEW: OFF";

    public static void TogglePreference()
    {
        SetRearViewPref(RearViewEnabled ? PrefOff : PrefOn);
    }

    private static void SetRearViewPref(int value)
    {
        rearViewPref = value;
        PlayerPrefs.SetInt(RearViewPrefKey, value);
        PlayerPrefs.Save();
        RearViewPreferenceChanged?.Invoke();
    }

    private void Awake()
    {
        sourceCamera = GetComponent<Camera>();
        behindDirection = ScreenEdgeUtility.ComputeTravelDirection(sourceCamera);
        behindDirection.y = 0f;
        if (behindDirection.sqrMagnitude < 0.001f)
            behindDirection = Vector3.left;
        else
            behindDirection.Normalize();
    }

    private void OnEnable()
    {
        RearViewPreferenceChanged += ApplyVisibility;
    }

    private void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        RefreshPlayers();
        BuildView();
        ApplyVisibility();
    }

    private void OnDisable()
    {
        RearViewPreferenceChanged -= ApplyVisibility;
    }

    private void LateUpdate()
    {
        ApplyVisibility();
        if (!viewVisible || rearCamera == null)
            return;

        if (!TryGetVehicleMidpoint(out Vector3 midpoint))
        {
            RefreshPlayers();
            if (!TryGetVehicleMidpoint(out midpoint))
                return;
        }

        // Place the lens just ahead of the vehicle row and point it toward the
        // road behind. This makes passed obstacles visible without changing the
        // main top-down view or steering coordinate system.
        Vector3 cameraPosition = midpoint - behindDirection * cameraLead + Vector3.up * cameraHeight;
        Vector3 lookTarget = midpoint + behindDirection * lookDistance;
        rearCamera.transform.SetPositionAndRotation(
            cameraPosition,
            Quaternion.LookRotation(lookTarget - cameraPosition, Vector3.up));
    }

    private void RefreshPlayers()
    {
        players = FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private bool TryGetVehicleMidpoint(out Vector3 midpoint)
    {
        midpoint = Vector3.zero;
        if (players == null)
            return false;

        int count = 0;
        foreach (PlayerController player in players)
        {
            if (player == null || !player.gameObject.activeInHierarchy || player.IsExiting)
                continue;
            midpoint += player.transform.position;
            count++;
        }

        if (count == 0)
            return false;
        midpoint /= count;
        return true;
    }

    private void BuildView()
    {
        if (sourceCamera == null || rearCamera != null)
            return;

        var cameraObject = new GameObject("RearViewCamera");
        cameraObject.transform.SetParent(transform, false);
        rearCamera = cameraObject.AddComponent<Camera>();
        rearCamera.CopyFrom(sourceCamera);
        rearTexture = new RenderTexture(TextureWidth, TextureHeight, 16, RenderTextureFormat.ARGB32)
        {
            name = "RearViewCameraTexture",
            antiAliasing = 1
        };
        rearTexture.Create();
        rearCamera.targetTexture = rearTexture;
        rearCamera.enabled = false;

        var canvasObject = new GameObject(
            "RearViewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        viewCanvas = canvasObject.GetComponent<Canvas>();
        viewCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        viewCanvas.overrideSorting = true;
        viewCanvas.sortingOrder = -5;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        viewRoot = new GameObject("RearViewPanel", typeof(RectTransform), typeof(Image));
        var panelRect = (RectTransform)viewRoot.transform;
        panelRect.SetParent(canvasObject.transform, false);
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-60f, -64f);
        panelRect.sizeDelta = new Vector2(520f, 210f);
        var panelImage = viewRoot.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.05f, 0.07f, 0.94f);
        panelImage.raycastTarget = false;

        var imageObject = new GameObject("RearViewImage", typeof(RectTransform), typeof(RawImage));
        var imageRect = (RectTransform)imageObject.transform;
        imageRect.SetParent(panelRect, false);
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(6f, 6f);
        imageRect.offsetMax = new Vector2(-6f, -6f);
        var image = imageObject.GetComponent<RawImage>();
        image.texture = rearTexture;
        image.raycastTarget = false;

        var labelObject = new GameObject("RearViewLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRect = (RectTransform)labelObject.transform;
        labelRect.SetParent(panelRect, false);
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(14f, -10f);
        labelRect.sizeDelta = new Vector2(200f, 34f);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "REAR VIEW";
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.raycastTarget = false;

        viewRoot.SetActive(false);
    }

    private void ApplyVisibility()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        bool shouldShow = RearViewEnabled
            && gameManager != null
            && gameManager.IsGameActive;
        if (viewVisible == shouldShow)
            return;

        viewVisible = shouldShow;
        if (rearCamera != null)
            rearCamera.enabled = shouldShow;
        if (viewRoot != null)
            viewRoot.SetActive(shouldShow);
    }

    private void OnDestroy()
    {
        if (rearTexture != null)
        {
            rearTexture.Release();
            Destroy(rearTexture);
        }
        if (viewCanvas != null)
            Destroy(viewCanvas.gameObject);
    }
}
