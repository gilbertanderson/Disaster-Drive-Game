using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Runtime polish for 2P HUD: tinted timer panels, styled popups, and game-over accents.
public class TwoPlayerUIStyler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text timer2Text;
    [SerializeField] private TMP_Text penaltyPopupP1;
    [SerializeField] private TMP_Text penaltyPopupP2;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text eliminationBannerText;
    void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
        if (timerText == null)
            timerText = GameObject.Find("TimerText")?.GetComponent<TMP_Text>();
        if (timer2Text == null)
            timer2Text = GameObject.Find("Timer2Text")?.GetComponent<TMP_Text>();
        if (penaltyPopupP1 == null)
            penaltyPopupP1 = GameObject.Find("PenaltyPopupText")?.GetComponent<TMP_Text>();
        if (penaltyPopupP2 == null)
            penaltyPopupP2 = GameObject.Find("PenaltyPopupP2Runtime")?.GetComponent<TMP_Text>();
        if (countdownText == null)
            countdownText = GameObject.Find("CountdownTextRuntime")?.GetComponent<TMP_Text>();
        if (eliminationBannerText == null)
            eliminationBannerText = GameObject.Find("EliminationBannerRuntime")?.GetComponent<TMP_Text>();

        StyleTimerPanel(timerText, PlayerColors.P1, -220f);
        StyleTimerPanel(timer2Text, PlayerColors.P2, 220f);
        StylePopup(penaltyPopupP1, PlayerColors.P1);
        StylePopup(penaltyPopupP2, PlayerColors.P2);
        StyleCountdown(countdownText);
        StyleBanner(eliminationBannerText);
    }

    void StyleTimerPanel(TMP_Text label, Color accent, float panelOffsetX)
    {
        if (label == null)
            return;

        label.fontStyle = FontStyles.Bold;
        var panel = CreatePanelBehind(label, "TimerPanel", accent, new Vector2(260f, 72f), panelOffsetX);
        panel.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
    }

    void StylePopup(TMP_Text popup, Color accent)
    {
        if (popup == null)
            return;

        popup.fontStyle = FontStyles.Bold;
        popup.fontSize = Mathf.Max(popup.fontSize, 42f);
        CreatePanelBehind(popup, popup.name + "Bg", accent, new Vector2(140f, 56f), 0f, 0.18f);
    }

    void StyleCountdown(TMP_Text countdown)
    {
        if (countdown == null)
            return;

        countdown.fontStyle = FontStyles.Bold;
        countdown.fontSize = Mathf.Max(countdown.fontSize, 96f);
        countdown.alignment = TextAlignmentOptions.Center;
    }

    void StyleBanner(TMP_Text banner)
    {
        if (banner == null)
            return;

        banner.fontStyle = FontStyles.Bold;
        banner.fontSize = Mathf.Max(banner.fontSize, 54f);
        banner.alignment = TextAlignmentOptions.Center;
        CreatePanelBehind(banner, "EliminationBannerBg", new Color(0f, 0f, 0f, 0.55f), new Vector2(720f, 100f), 0f, 0.35f);
    }

    GameObject CreatePanelBehind(TMP_Text label, string name, Color accent, Vector2 size, float offsetX, float alpha = 0.22f)
    {
        Transform existing = label.transform.parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(label.transform.parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = label.rectTransform.anchorMin;
        rt.anchorMax = label.rectTransform.anchorMax;
        rt.pivot = label.rectTransform.pivot;
        rt.anchoredPosition = label.rectTransform.anchoredPosition + new Vector2(offsetX, -8f);
        rt.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = new Color(accent.r, accent.g, accent.b, alpha);
        image.raycastTarget = false;
        return go;
    }
}
