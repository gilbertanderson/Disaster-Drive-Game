using UnityEngine;
using UnityEngine.EventSystems;

// Lets the player tap/click the timer HUD to pause/resume, mirroring the Esc
// key. Attached to the timer labels at runtime by GameManager
// (EnsureRuntimeUiRefs) — not scene-authored.
public class TimerPauseTapHandler : MonoBehaviour, IPointerClickHandler
{
    private GameManager gameManager;

    public void Init(GameManager manager) => gameManager = manager;

    public void OnPointerClick(PointerEventData eventData)
    {
        // TogglePause is a no-op before gameplay starts (IsGameActive guard),
        // so taps on the start screen and during the countdown do nothing.
        gameManager?.TogglePause();
    }
}
