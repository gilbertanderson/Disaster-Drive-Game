using System.Collections.Generic;
using UnityEngine;

// Drives the start-of-run intro camera: a head-on shot of the vehicles held through
// 3-2-1, then a swoop up to the top-down gameplay pose on GO. Also owns the
// optional rear-view chase pose toggled from the pause menu.
public class GameplayCameraDirector : MonoBehaviour
{
    [SerializeField] private Transform introRig;
    [SerializeField] private Transform frontAnchor;
    [SerializeField] private Transform rearAnchor;
    [SerializeField] private Transform isoAnchor;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float beatDuration = 0.8f;

    private Vector3 gameplayPosition;
    private Quaternion gameplayRotation;
    private Vector3 rearPosition;
    private Quaternion rearRotation;
    private Vector3 isoPosition;
    private Quaternion isoRotation;
    private RearViewManager.ViewMode currentView = RearViewManager.ViewMode.Normal;
    private bool introActive;
    private bool introComplete;
    private int currentBeat = -1;
    private float beatElapsed;
    private Vector3 beatStartPosition;
    private Quaternion beatStartRotation;
    private Vector3 beatTargetPosition;
    private Quaternion beatTargetRotation;
    private bool beatInProgress;

    public bool RearViewActive => currentView != RearViewManager.ViewMode.Normal;

    void Awake()
    {
        CacheGameplayPose();
        CacheRearPose();
        CacheIsoPose();
        if (cameraShake == null)
            cameraShake = GetComponent<CameraShake>();
    }

    void Start()
    {
        ApplyViewMode(RearViewManager.CurrentMode);
    }

    void Update()
    {
        if (beatInProgress)
            TickBeat(Time.unscaledDeltaTime);
    }

    public void CacheGameplayPose()
    {
        gameplayPosition = transform.position;
        gameplayRotation = transform.rotation;
    }

    public void CacheRearPose()
    {
        if (rearAnchor != null)
        {
            rearPosition = rearAnchor.position;
            rearRotation = rearAnchor.rotation;
            return;
        }

        // Fallback when the scene anchor is missing: low chase cam behind the
        // playfield looking toward the vehicles (opposite the intro front shot).
        rearPosition = gameplayPosition + new Vector3(-10f, -6f, 0f);
        rearRotation = Quaternion.Euler(28f, 90f, 0f);
    }

    public void CacheIsoPose()
    {
        if (isoAnchor != null)
        {
            isoPosition = isoAnchor.position;
            isoRotation = isoAnchor.rotation;
            return;
        }

        // Fallback when the scene anchor is missing: elevated 3/4 view behind
        // and above the vehicle, higher and less steep than the rear chase
        // pose (Tesla-FSD-style rear isometric).
        isoPosition = gameplayPosition + new Vector3(-14f, 10f, 6f);
        isoRotation = Quaternion.Euler(45f, 55f, 0f);
    }

    // Back-compat entry point: true/false maps to RearChase/Normal. Prefer
    // ApplyViewMode for the full 3-way selector.
    public void ApplyRearView(bool rear)
    {
        ApplyViewMode(rear ? RearViewManager.ViewMode.RearChase : RearViewManager.ViewMode.Normal);
    }

    public void ApplyViewMode(RearViewManager.ViewMode mode)
    {
        if (introActive)
            return;

        currentView = mode;
        CacheRearPose();
        CacheIsoPose();

        Vector3 targetPos;
        Quaternion targetRot;
        switch (mode)
        {
            case RearViewManager.ViewMode.RearChase:
                targetPos = rearPosition;
                targetRot = rearRotation;
                break;
            case RearViewManager.ViewMode.Isometric:
                targetPos = isoPosition;
                targetRot = isoRotation;
                break;
            default:
                targetPos = gameplayPosition;
                targetRot = gameplayRotation;
                break;
        }
        transform.SetPositionAndRotation(targetPos, targetRot);

        if (cameraShake != null)
            cameraShake.SyncRestPosition();
    }

    public void StartIntroSequence(IReadOnlyList<Transform> vehicleRoots)
    {
        CacheGameplayPose();
        CacheRearPose();
        CacheIsoPose();

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

    void GetBeatTargetPose(int beatIndex, out Vector3 position, out Quaternion rotation)
    {
        // Beats 0-2 (3, 2, 1) hold the head-on front shot; beat 3 (GO) swoops to gameplay.
        if (beatIndex < 3)
        {
            position = frontAnchor.position;
            rotation = frontAnchor.rotation;
        }
        else if (currentView == RearViewManager.ViewMode.RearChase)
        {
            position = rearPosition;
            rotation = rearRotation;
        }
        else if (currentView == RearViewManager.ViewMode.Isometric)
        {
            position = isoPosition;
            rotation = isoRotation;
        }
        else
        {
            position = gameplayPosition;
            rotation = gameplayRotation;
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
            introActive = false;
            introComplete = true;
            if (cameraShake != null)
                cameraShake.SyncRestPosition();
        }
    }
}
