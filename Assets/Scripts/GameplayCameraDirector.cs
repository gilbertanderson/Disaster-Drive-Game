using System.Collections;
using UnityEngine;

// Drives the start-of-run intro camera: cinematic anchor on Drive, then back to gameplay top-down view.
public class GameplayCameraDirector : MonoBehaviour
{
    enum IntroPhase { Idle, IntroIn, Hold, Return, Done }

    [SerializeField] private Transform introAnchor;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float introInDuration = 0.5f;
    [SerializeField] private float introHoldDuration = 1.7f;
    [SerializeField] private float returnDuration = 1f;

    private Vector3 gameplayPosition;
    private Quaternion gameplayRotation;
    private IntroPhase phase = IntroPhase.Idle;
    private float phaseElapsed;
    private Vector3 phaseStartPosition;
    private Quaternion phaseStartRotation;
    private Coroutine introRoutine;

    void Awake()
    {
        CacheGameplayPose();
        if (cameraShake == null)
            cameraShake = GetComponent<CameraShake>();
    }

    public void CacheGameplayPose()
    {
        gameplayPosition = transform.position;
        gameplayRotation = transform.rotation;
    }

    public IEnumerator PlayIntroSequence()
    {
        if (introAnchor == null)
            yield break;

        if (introRoutine != null)
            StopCoroutine(introRoutine);

        if (cameraShake != null)
            cameraShake.StopAndReset();

        BeginPhase(IntroPhase.IntroIn);
        while (phase != IntroPhase.Done)
        {
            phaseElapsed += Time.unscaledDeltaTime;
            TickIntro();
            yield return null;
        }

        introRoutine = null;
    }

    public void StartIntroSequence()
    {
        if (introRoutine != null)
            StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(PlayIntroSequence());
    }

    // Advances the intro timeline by a fixed delta; used by edit-mode tests.
    internal void SimulateIntroStep(float deltaTime)
    {
        if (introAnchor == null)
        {
            phase = IntroPhase.Done;
            return;
        }

        if (phase == IntroPhase.Idle)
            BeginPhase(IntroPhase.IntroIn);

        phaseElapsed += deltaTime;
        TickIntro();
    }

    internal bool IsIntroComplete => phase == IntroPhase.Done || introAnchor == null;

    void BeginPhase(IntroPhase next)
    {
        phase = next;
        phaseElapsed = 0f;
        phaseStartPosition = transform.position;
        phaseStartRotation = transform.rotation;
    }

    void TickIntro()
    {
        switch (phase)
        {
            case IntroPhase.IntroIn:
                if (AdvanceLerp(introAnchor.position, introAnchor.rotation, introInDuration))
                    BeginPhase(IntroPhase.Hold);
                break;
            case IntroPhase.Hold:
                transform.SetPositionAndRotation(introAnchor.position, introAnchor.rotation);
                if (phaseElapsed >= introHoldDuration)
                    BeginPhase(IntroPhase.Return);
                break;
            case IntroPhase.Return:
                if (AdvanceLerp(gameplayPosition, gameplayRotation, returnDuration))
                {
                    phase = IntroPhase.Done;
                    if (cameraShake != null)
                        cameraShake.SyncRestPosition();
                }
                break;
        }
    }

    bool AdvanceLerp(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        if (duration <= 0f)
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            return true;
        }

        float t = Mathf.Clamp01(phaseElapsed / duration);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        transform.position = Vector3.Lerp(phaseStartPosition, targetPosition, eased);
        transform.rotation = Quaternion.Slerp(phaseStartRotation, targetRotation, eased);
        return t >= 1f;
    }
}
