using System;
using UnityEngine;

// Owns the player's preferred in-run camera view — normal top-down follow or a
// low rear-chase pose — applied via GameplayCameraDirector.ApplyViewMode and
// persisted across sessions. Toggled by the pause menu's VIEW button.
public static class RearViewManager
{
    public enum ViewMode
    {
        Normal = 0,
        RearChase = 1,
        // Retained for PlayerPrefs compatibility; never offered in the pause UI.
        // Stored ISO values are migrated to Normal on load.
        Isometric = 2,
    }

    private const int SelectableModeCount = 2;

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
                // ISO was removed from the pause VIEW cycle; treat legacy saves as Normal.
                if (stored == (int)ViewMode.Isometric)
                    stored = (int)ViewMode.Normal;
                viewModePref = Mathf.Clamp(stored, 0, SelectableModeCount - 1);
            }
            return (ViewMode)viewModePref;
        }
    }

    // Back-compat: true whenever a non-normal view is active.
    public static bool RearViewEnabled => CurrentMode != ViewMode.Normal;

    public static string ButtonLabel => CurrentMode == ViewMode.RearChase
        ? "VIEW: REAR"
        : "VIEW: NORMAL";

    // Pause VIEW button: Normal <-> Rear only.
    public static void CycleView()
    {
        TogglePreference();
    }

    // Flips between Normal and RearChase.
    public static void TogglePreference()
    {
        SetMode(CurrentMode == ViewMode.Normal ? ViewMode.RearChase : ViewMode.Normal);
    }

    private static void SetMode(ViewMode mode)
    {
        if (mode == ViewMode.Isometric)
            mode = ViewMode.Normal;

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
