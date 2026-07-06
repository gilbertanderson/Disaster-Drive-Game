using System.Collections;
using UnityEngine;

// Brief camera jitter on impact; attach to Main Camera.
public class CameraShake : MonoBehaviour
{
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float magnitude = 0.4f;

    private Vector3 restLocalPos;
    private Coroutine shakeRoutine;

    void Awake()
    {
        restLocalPos = transform.localPosition;
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
            transform.localPosition = restLocalPos + transform.right * x + transform.up * y;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = restLocalPos;
        shakeRoutine = null;
    }
}
