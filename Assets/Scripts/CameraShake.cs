using System.Collections;
using UnityEngine;

// Brief camera jitter on impact; attach to Main Camera.
public class CameraShake : MonoBehaviour
{
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float magnitude = 0.4f;

    private Vector3 restLocalPos;
    private Vector3 appliedShakeOffset;   // Offset currently baked into localPosition by an in-flight shake
    private Coroutine shakeRoutine;

    void Awake()
    {
        restLocalPos = transform.localPosition;
    }

    public void SyncRestPosition()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        // Subtract any in-flight shake offset so a shake that straddles the
        // sync (e.g. a late hit still decaying when a new run's intro ends)
        // can't get baked into the rest pose.
        transform.localPosition -= appliedShakeOffset;
        appliedShakeOffset = Vector3.zero;
        restLocalPos = transform.localPosition;
    }

    public void StopAndReset()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        appliedShakeOffset = Vector3.zero;
        transform.localPosition = restLocalPos;
    }

    public void Shake()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float decay = 1f - (elapsed / duration);
            float x = Random.Range(-1f, 1f) * magnitude * decay;
            float y = Random.Range(-1f, 1f) * magnitude * decay;
            appliedShakeOffset = transform.right * x + transform.up * y;
            transform.localPosition = restLocalPos + appliedShakeOffset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        appliedShakeOffset = Vector3.zero;
        transform.localPosition = restLocalPos;
        shakeRoutine = null;
    }
}
