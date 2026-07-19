using System;
using UnityEngine;

// Owns the player's preferred in-run camera view — normal top-down follow,
// a low rear-chase pose, or an elevated rear isometric pose — applied via
// GameplayCameraDirector.ApplyViewMode and persisted across sessions. Cycled
// by the pause menu's VIEW button (see GameManager.CycleView).
public static class RearViewManager
{
    public enum ViewMode
    {
        Normal = 0,
        RearChase = 1,
        Isometric = 2,
    }

    private const int ModeCount = 3;

    // Key name kept from the original on/off toggle for save compatibility.
    public const string RearViewPrefKey = "RearViewEnabled";
    private const int PrefUnloaded = int.MinValue; // Sentinel: PlayerPrefs not read yet

    private static int viewModePref = PrefUnloaded;

    // Raised when the player cycles the VIEW button, so the pause menu's
    // button label (GameManager) can re-render without polling.
    public static event Action RearViewPreferenceChanged;

    public static ViewMode CurrentMode
    {
        get
        {
            if (viewModePref == PrefUnloaded)
            {
                int stored = PlayerPrefs.GetInt(RearViewPrefKey, (int)ViewMode.Normal);
                viewModePref = Mathf.Clamp(stored, 0, ModeCount - 1);
            }
            return (ViewMode)viewModePref;
        }
    }

    // Back-compat: true whenever a non-normal view is active.
    public static bool RearViewEnabled => CurrentMode != ViewMode.Normal;

    public static string ButtonLabel => CurrentMode switch
    {
        ViewMode.RearChase => "VIEW: REAR",
        ViewMode.Isometric => "VIEW: ISO",
        _ => "VIEW: NORMAL",
    };

    public static void CycleView()
    {
        SetMode((ViewMode)(((int)CurrentMode + 1) % ModeCount));
    }

    // Back-compat entry point for callers/tests expecting a simple on/off
    // toggle: flips between Normal and RearChase only.
    public static void TogglePreference()
    {
        SetMode(CurrentMode == ViewMode.Normal ? ViewMode.RearChase : ViewMode.Normal);
    }

    private static void SetMode(ViewMode mode)
    {
        viewModePref = (int)mode;
        PlayerPrefs.SetInt(RearViewPrefKey, viewModePref);
        PlayerPrefs.Save();
        RearViewPreferenceChanged?.Invoke();
        ApplyToCameraDirector();
    }

    public static void ApplyToCameraDirector()
    {
        var director = Camera.main != null
            ? Camera.main.GetComponent<GameplayCameraDirector>()
            : UnityEngine.Object.FindAnyObjectByType<GameplayCameraDirector>();
        director?.ApplyViewMode(CurrentMode);
    }
}
