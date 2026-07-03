using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Adds button press feedback only; scene colors are left as authored in the editor.
public class UIModernizer : MonoBehaviour
{
    void Awake()
    {
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            if (btn.GetComponent<UIButtonFeedback>() == null)
                btn.gameObject.AddComponent<UIButtonFeedback>();
        }
    }
}

public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 baseScale;
    private Coroutine pulse;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pulse != null) StopCoroutine(pulse);
        transform.localScale = baseScale * 0.94f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (pulse != null) StopCoroutine(pulse);
        pulse = StartCoroutine(ResetScale());
    }

    IEnumerator ResetScale()
    {
        float t = 0f;
        Vector3 start = transform.localScale;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(start, baseScale, t / 0.12f);
            yield return null;
        }
        transform.localScale = baseScale;
        pulse = null;
    }
}

// Fade + slide for Start / Game Over / Pause panels.
public static class UIPanelTransition
{
    const float Duration = 0.32f;
    const float SlideOffset = 36f;

    static CanvasGroup EnsureCanvasGroup(GameObject panel)
    {
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = panel.AddComponent<CanvasGroup>();
        return cg;
    }

    public static IEnumerator Hide(GameObject panel)
    {
        if (panel == null || !panel.activeSelf)
            yield break;

        var cg = EnsureCanvasGroup(panel);
        var rt = panel.GetComponent<RectTransform>();
        Vector2 basePos = rt != null ? rt.anchoredPosition : Vector2.zero;
        float t = 0f;

        while (t < Duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / Duration);
            cg.alpha = 1f - p;
            if (rt != null)
                rt.anchoredPosition = basePos + Vector2.up * (SlideOffset * p);
            yield return null;
        }

        panel.SetActive(false);
        cg.alpha = 1f;
        if (rt != null)
            rt.anchoredPosition = basePos;
    }

    public static IEnumerator Show(GameObject panel)
    {
        if (panel == null)
            yield break;

        panel.SetActive(true);
        var cg = EnsureCanvasGroup(panel);
        var rt = panel.GetComponent<RectTransform>();
        Vector2 basePos = rt != null ? rt.anchoredPosition : Vector2.zero;
        Vector2 startPos = basePos + Vector2.down * SlideOffset;
        if (rt != null)
            rt.anchoredPosition = startPos;
        cg.alpha = 0f;

        float t = 0f;
        while (t < Duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / Duration);
            cg.alpha = p;
            if (rt != null)
                rt.anchoredPosition = Vector2.Lerp(startPos, basePos, p);
            yield return null;
        }

        cg.alpha = 1f;
        if (rt != null)
            rt.anchoredPosition = basePos;
    }
}
