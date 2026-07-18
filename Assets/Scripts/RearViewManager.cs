using System;
using UnityEngine;

// Owns the player's rear-view camera preference and applies it through
// GameplayCameraDirector. Toggled from the pause menu; persisted in PlayerPrefs.
public static class RearViewManager
{
    public const string RearViewPrefKey = "RearViewEnabled";
    private const int PrefUnloaded = int.MinValue;
    private const int PrefOff = 0;
    private const int PrefOn = 1;

    private static int rearViewPref = PrefUnloaded;

    public static event Action RearViewPreferenceChanged;

    public static bool RearViewEnabled
    {
        get
        {
            if (rearViewPref == PrefUnloaded)
                rearViewPref = PlayerPrefs.GetInt(RearViewPrefKey, PrefOff);
            return rearViewPref == PrefOn;
        }
    }

    public static string ButtonLabel =>
        RearViewEnabled ? "REAR VIEW: ON" : "REAR VIEW: OFF";

    public static void TogglePreference()
    {
        SetRearViewPref(RearViewEnabled ? PrefOff : PrefOn);
    }

    private static void SetRearViewPref(int value)
    {
        rearViewPref = value;
        PlayerPrefs.SetInt(RearViewPrefKey, value);
        PlayerPrefs.Save();
        RearViewPreferenceChanged?.Invoke();
        ApplyToCameraDirector();
    }

    public static void ApplyToCameraDirector()
    {
        var director = Camera.main != null
            ? Camera.main.GetComponent<GameplayCameraDirector>()
            : null;
        if (director == null)
            director = UnityEngine.Object.FindAnyObjectByType<GameplayCameraDirector>();
        director?.ApplyRearView(RearViewEnabled);
    }
}
