using System;
using System.Collections.Generic;
using UnityEngine;

// Drives the start-of-run intro camera: a head-on shot of the vehicles held through
// 3-2-1, then a swoop up to the top-down gameplay pose on GO. Also owns the optional
// rear-view gameplay pose toggled from the pause menu.
public class GameplayCameraDirector : MonoBehaviour
{
    public const string RearViewPrefKey = "RearViewCameraEnabled";

    [SerializeField] private Transform introRig;
    [SerializeField] private Transform frontAnchor;
    [SerializeField] private Transform rearAnchor;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float beatDuration = 0.8f;

    private Vector3 topDownPosition;
    private Quaternion topDownRotation;
    private bool introActive;
    private bool introComplete;
    private int currentBeat = -1;
    private float beatElapsed;
    private Vector3 beatStartPosition;
    private Quaternion beatStartRotation;
    private Vector3 beatTargetPosition;
    private Quaternion beatTargetRotation;
    private bool beatInProgress;
    private static int rearViewPref = int.MinValue;

    // Raised when the pause-menu rear-view toggle flips so the button label can refresh.
    public static event Action RearViewChanged;

    public static bool RearViewEnabled
    {
        get
        {
            if (rearViewPref == int.MinValue)
                rearViewPref = PlayerPrefs.GetInt(RearViewPrefKey, 0);
            return rearViewPref == 1;
        }
    }

    public static string RearViewButtonLabel =>
        RearViewEnabled ? "REAR VIEW: ON" : "REAR VIEW: OFF";

    public static void ToggleRearViewPref()
    {
        SetRearViewPref(RearViewEnabled ? 0 : 1);
    }

    public static void SetRearViewPref(int value)
    {
        rearViewPref = value;
        PlayerPrefs.SetInt(RearViewPrefKey, value);
        PlayerPrefs.Save();
        RearViewChanged?.Invoke();
    }

    // Edit Mode / Play Mode tests reset the static pref cache between cases.
    internal static void ResetRearViewPrefCacheForTests()
    {
        rearViewPref = int.MinValue;
    }

    void Awake()
    {
        // Scene-authored Main Camera transform is the top-down gameplay pose.
        topDownPosition = transform.position;
        topDownRotation = transform.rotation;
        if (cameraShake == null)
            cameraShake = GetComponent<CameraShake>();
    }

    void OnEnable()
    {
        RearViewChanged += ApplyPreferredGameplayPose;
    }

    void OnDisable()
    {
        RearViewChanged -= ApplyPreferredGameplayPose;
    }

    void Update()
    {
        if (beatInProgress)
            TickBeat(Time.unscaledDeltaTime);
    }

    public void CacheGameplayPose()
    {
        // Only refresh the stored top-down pose while that pose is the live one.
        // If rear view is already applied, the live transform is the rear anchor and
        // must not overwrite the return path back to top-down.
        if (RearViewEnabled && HasRearAnchor())
            return;

        topDownPosition = transform.position;
        topDownRotation = transform.rotation;
    }

    public void StartIntroSequence(IReadOnlyList<Transform> vehicleRoots)
    {
        CacheGameplayPose();

        if (cameraShake != null)
            cameraShake.StopAndReset();

        PositionRigAtFocalPoint(vehicleRoots);

        introActive = HasValidAnchors();
        introComplete = !introActive;
        currentBeat = -1;
        beatInProgress = false;
        beatElapsed = 0f;

        // Open on the front shot with a hard cut so the countdown never pans across the track.
        if (introActive)
            transform.SetPositionAndRotation(frontAnchor.position, frontAnchor.rotation);
    }

    public void PlayCountdownBeat(int beatIndex)
    {
        if (!introActive || beatIndex < 0 || beatIndex > 3)
            return;

        beatStartPosition = transform.position;
        beatStartRotation = transform.rotation;
        GetBeatTargetPose(beatIndex, out beatTargetPosition, out beatTargetRotation);
        currentBeat = beatIndex;
        beatElapsed = 0f;
        beatInProgress = true;
    }

    // Snaps (or keeps) the camera on the player's preferred gameplay pose once the
    // intro is done, or immediately when the pause-menu toggle flips mid-run.
    public void ApplyPreferredGameplayPose()
    {
        if (introActive && !introComplete)
            return;

        GetPreferredGameplayPose(out Vector3 position, out Quaternion rotation);
        transform.SetPositionAndRotation(position, rotation);
        if (cameraShake != null)
            cameraShake.SyncRestPosition();
    }

    internal bool IsIntroComplete => introComplete || !HasValidAnchors();

    internal int CurrentBeat => currentBeat;

    // Advances the active beat lerp; used by edit-mode tests.
    internal void SimulateBeatStep(float deltaTime)
    {
        if (!introActive)
        {
            introComplete = true;
            return;
        }

        if (!beatInProgress && currentBeat < 0)
            PlayCountdownBeat(0);

        if (beatInProgress)
            TickBeat(deltaTime);
    }

    internal void SimulateFullIntro(float stepDelta = 0.05f)
    {
        StartIntroSequence(null);
        for (int beat = 0; beat < 4; beat++)
        {
            PlayCountdownBeat(beat);
            float elapsed = 0f;
            while (beatInProgress && elapsed <= beatDuration + stepDelta)
            {
                SimulateBeatStep(stepDelta);
                elapsed += stepDelta;
            }
        }
    }

    bool HasValidAnchors()
    {
        return introRig != null && frontAnchor != null;
    }

    bool HasRearAnchor()
    {
        return rearAnchor != null;
    }

    void PositionRigAtFocalPoint(IReadOnlyList<Transform> vehicleRoots)
    {
        if (introRig == null || vehicleRoots == null || vehicleRoots.Count == 0)
            return;

        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Transform root in vehicleRoots)
        {
            if (root == null)
                continue;
            sum += root.position;
            count++;
        }

        if (count == 0)
            return;

        Vector3 midpoint = sum / count;
        Vector3 rigPosition = introRig.position;
        introRig.position = new Vector3(midpoint.x, rigPosition.y, midpoint.z);
    }

    void GetPreferredGameplayPose(out Vector3 position, out Quaternion rotation)
    {
        if (RearViewEnabled && HasRearAnchor())
        {
            position = rearAnchor.position;
            rotation = rearAnchor.rotation;
            return;
        }

        position = topDownPosition;
        rotation = topDownRotation;
    }

    void GetBeatTargetPose(int beatIndex, out Vector3 position, out Quaternion rotation)
    {
        // Beats 0-2 (3, 2, 1) hold the head-on front shot; beat 3 (GO) swoops to gameplay.
        if (beatIndex < 3)
        {
            position = frontAnchor.position;
            rotation = frontAnchor.rotation;
        }
        else
        {
            GetPreferredGameplayPose(out position, out rotation);
        }
    }

    void TickBeat(float deltaTime)
    {
        beatElapsed += deltaTime;
        float t = beatDuration <= 0f ? 1f : Mathf.Clamp01(beatElapsed / beatDuration);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        transform.position = Vector3.Lerp(beatStartPosition, beatTargetPosition, eased);
        transform.rotation = Quaternion.Slerp(beatStartRotation, beatTargetRotation, eased);

        if (t < 1f)
            return;

        transform.SetPositionAndRotation(beatTargetPosition, beatTargetRotation);
        beatInProgress = false;

        if (currentBeat == 3)
        {
            introComplete = true;
            if (cameraShake != null)
                cameraShake.SyncRestPosition();
        }
    }
}
