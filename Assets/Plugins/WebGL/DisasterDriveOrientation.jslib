// C# -> browser bridge for the pause menu's ROTATION toggle (see
// OrientationManager.cs). Prefers the DisasterDrive WebGL template's
// window.setDisasterOrientationPreference (which also persists the choice to
// localStorage, requests fullscreen, and manages the rotate overlay); falls
// back to a raw Screen Orientation API lock so builds made with the stock
// Unity template still get best-effort behavior.
mergeInto(LibraryManager.library, {
  DisasterDriveSetOrientation: function (landscape) {
    try {
      if (typeof window.setDisasterOrientationPreference === 'function') {
        window.setDisasterOrientationPreference(landscape ? 'landscape' : 'portrait');
      } else if (screen.orientation && screen.orientation.lock) {
        screen.orientation.lock(landscape ? 'landscape' : 'portrait').catch(function () {});
      }
    } catch (e) {
      console.warn('Orientation preference apply failed:', e);
    }
  }
});
