using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Runtime polish for 2P HUD: bold timer text, tinted popup/banner backgrounds.
public class TwoPlayerUIStyler : MonoBehaviour
{
    private GameManager gameManager;
    private TMP_Text timerText;
    private TMP_Text timer2Text;
    private TMP_Text penaltyPopupP1;
    private TMP_Text penaltyPopupP2;
    private TMP_Text countdownText;
    private TMP_Text eliminationBannerText;

    // GameManager already holds resolved references to every one of these; passing them in
    // directly avoids GameObject.Find, which silently fails on objects that start inactive.
    public void Init(GameManager manager, TMP_Text timer, TMP_Text timer2, TMP_Text popupP1,
        TMP_Text popupP2, TMP_Text countdown, TMP_Text banner)
    {
        gameManager = manager;
        timerText = timer;
        timer2Text = timer2;
        penaltyPopupP1 = popupP1;
        penaltyPopupP2 = popupP2;
        countdownText = countdown;
        eliminationBannerText = banner;

        Refresh();
        StyleCountdown(countdownText);
        StyleBanner(eliminationBannerText);
    }

    // Re-syncs popup styling. Call again whenever GameManager moves or (de)activates the
    // timers, e.g. every SetTwoPlayerMode call.
    public void Refresh()
    {
        StyleTimer(timerText);
        StyleTimer(timer2Text);
        StylePopup(penaltyPopupP1, PlayerColors.P1);
        StylePopup(penaltyPopupP2, PlayerColors.P2);
    }

    void StyleTimer(TMP_Text label)
    {
        if (label == null)
            return;

        label.fontStyle = FontStyles.Bold;
    }

    void StylePopup(TMP_Text popup, Color accent)
    {
        if (popup == null)
            return;

        popup.fontStyle = FontStyles.Bold;
        popup.fontSize = Mathf.Max(popup.fontSize, 42f);
        var panel = CreatePanelBehind(popup, popup.gameObject.name + "Bg", accent, new Vector2(140f, 56f), 0.18f);
        panel.SetActive(popup.gameObject.activeSelf);
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
        var panel = CreatePanelBehind(banner, banner.gameObject.name + "Bg", new Color(0f, 0f, 0f, 0.55f), new Vector2(720f, 100f), 0.35f);
        panel.SetActive(banner.gameObject.activeSelf);
    }

    // Creates (or repositions, if it already exists) a tinted panel directly behind label,
    // sharing its anchor/pivot so it always sits in place even if label later moves.
    GameObject CreatePanelBehind(TMP_Text label, string name, Color accent, Vector2 size, float alpha = 0.22f)
    {
        Vector2 pos = label.rectTransform.anchoredPosition + new Vector2(0f, -8f);
        Transform existing = label.transform.parent.Find(name);
        if (existing != null)
        {
            existing.GetComponent<RectTransform>().anchoredPosition = pos;
            return existing.gameObject;
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(label.transform.parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = label.rectTransform.anchorMin;
        rt.anchorMax = label.rectTransform.anchorMax;
        rt.pivot = label.rectTransform.pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = new Color(accent.r, accent.g, accent.b, alpha);
        image.raycastTarget = false;
        return go;
    }
}
