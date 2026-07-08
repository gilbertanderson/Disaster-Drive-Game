using System.Collections.Generic;
using UnityEngine;

// Drives the start-of-run intro camera: beat-synced cinematic shots during 3-2-1-GO, then top-down gameplay.
public class GameplayCameraDirector : MonoBehaviour
{
    [SerializeField] private Transform introRig;
    [SerializeField] private Transform frontAnchor;
    [SerializeField] private Transform leftAnchor;
    [SerializeField] private Transform behindAnchor;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float beatDuration = 0.8f;

    private Vector3 gameplayPosition;
    private Quaternion gameplayRotation;
    private bool introActive;
    private bool introComplete;
    private int currentBeat = -1;
    private float beatElapsed;
    private Vector3 beatStartPosition;
    private Quaternion beatStartRotation;
    private Vector3 beatTargetPosition;
    private Quaternion beatTargetRotation;
    private bool beatInProgress;

    void Awake()
    {
        CacheGameplayPose();
        if (cameraShake == null)
            cameraShake = GetComponent<CameraShake>();
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
        return introRig != null && frontAnchor != null && leftAnchor != null && behindAnchor != null;
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
        switch (beatIndex)
        {
            case 0:
                position = frontAnchor.position;
                rotation = frontAnchor.rotation;
                break;
            case 1:
                position = leftAnchor.position;
                rotation = leftAnchor.rotation;
                break;
            case 2:
                position = behindAnchor.position;
                rotation = behindAnchor.rotation;
                break;
            default:
                position = gameplayPosition;
                rotation = gameplayRotation;
                break;
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
