using System;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

// Owns the player's preferred screen orientation (landscape by default,
// portrait via the pause menu's ROTATION toggle) and applies it per platform:
// native mobile through Screen.orientation/autorotation, WebGL through a
// .jslib call into the page (which locks the orientation, requests fullscreen
// where the lock needs it, and drives the "please rotate" overlay). Desktop
// and the Editor have nothing to rotate, so Apply is a no-op there while the
// preference, label, and event still work — which is what the tests exercise.
public static class OrientationManager
{
    public const string OrientationPrefKey = "PreferredOrientation";
    private const int PrefUnloaded = int.MinValue; // Sentinel: PlayerPrefs not read yet
    private const int PrefPortrait = 0;
    private const int PrefLandscape = 1;

    private static int orientationPref = PrefUnloaded;

    // Raised when the player flips the ROTATION toggle, so the pause menu's
    // button label (GameManager) can re-render without polling.
    public static event Action OrientationPreferenceChanged;

    public static bool LandscapePreferred
    {
        get
        {
            if (orientationPref == PrefUnloaded)
                orientationPref = PlayerPrefs.GetInt(OrientationPrefKey, PrefLandscape);
            return orientationPref != PrefPortrait;
        }
    }

    public static string ButtonLabel =>
        LandscapePreferred ? "ORIENTATION: LANDSCAPE" : "ORIENTATION: PORTRAIT";

    public static void TogglePreference()
    {
        SetOrientationPref(LandscapePreferred ? PrefPortrait : PrefLandscape);
    }

    private static void SetOrientationPref(int value)
    {
        orientationPref = value;
        PlayerPrefs.SetInt(OrientationPrefKey, value);
        PlayerPrefs.Save();
        OrientationPreferenceChanged?.Invoke();
        Apply();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyOnStartup()
    {
        Apply();
    }

    public static void Apply()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        DisasterDriveSetOrientation(LandscapePreferred ? 1 : 0);
#elif UNITY_ANDROID || UNITY_IOS
        bool landscape = LandscapePreferred;
        Screen.autorotateToLandscapeLeft = landscape;
        Screen.autorotateToLandscapeRight = landscape;
        Screen.autorotateToPortrait = !landscape;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.orientation = ScreenOrientation.AutoRotation;
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DisasterDriveSetOrientation(int landscape);
#endif
}
