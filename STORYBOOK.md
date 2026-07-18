# Storybook: Gameplay UI and Vehicle Components

## Component: `VehicleSelector`

### Purpose
`VehicleSelector` manages the player vehicle selection UI and ensures the selected vehicle's visual representation and physics hitbox stay in sync.
It also manages rear dirt particle emitters so they remain positioned correctly behind the active vehicle.

### Behavior
- On `Awake()`, it loads the saved vehicle index from `PlayerPrefs`.
- Only the active vehicle visual in `vehicleVisuals` is enabled.
- The `BoxCollider` is resized to tightly fit the selected visual, ensuring collisions happen where the model appears.
- Dirt emitters are repositioned relative to the fitted collider using `PositionDirtEmitters()`.
- `Update()` only handles start-screen vehicle cycling and emitter playback state.
- Dirt spray direction is stable and depends on the emitter prefab orientation rather than steering input.

### Props
- `vehicleVisuals` - array of child vehicle GameObjects that can be selected.
- `dirtEmitters` - particle systems representing rear dirt spray.
- `showEmitterOrientationGizmos` - enables debug gizmos showing emitter right-facing direction.

### Interaction
- `NextVehicle()` and `PreviousVehicle()` are wired to start-screen buttons and keyboard input.
- Selected vehicle index persists via `PlayerPrefs`.
- The dirt emitters start and stop when the game enters and exits the active driving state.

### Why the emitter rotation fix matters
Previously, emitter behavior was being influenced by steering or runtime direction calculations, which caused the dirt spray to look wrong when the player turned.
The fix keeps emitter orientation tied to the emitter prefab's native orientation, with placement updated per selected vehicle.

## Test cases
- `Awake_SetsParticleSimulationSpaceToLocal` ensures each emitter is configured for local particle simulation space.
- `Apply_PlacesEmitterBehindTheSelectedVehicleOnly` ensures the selected visual is active and the emitter remains behind it.
- `Update_DoesNotChangeEmitterRotationFromDefault` makes sure runtime update logic does not alter emitter rotation.

## Notes for extension
- Add any new vehicle models to `vehicleVisuals` and ensure their rear geometry matches emitter placement expectations.
- If dirt behavior should change from a pure static orientation to a dynamic road-relative direction, the emitter placement logic can consume `GroundScroller.WorldMoveDirection` without altering local emitter rotation.
- Keep `simulationSpace` set to `Local` for the particle systems so their emitted velocities are relative to the emitter orientation.

## Component: `MobileControlsUI`

### Purpose
`MobileControlsUI` builds the runtime touch overlay without requiring scene-authored mobile controls.
It provides a bottom-left virtual stick and a top-left in-game controls hint.

### Behavior
- A `RuntimeInitializeOnLoadMethod` creates a persistent `MobileControlsUI` object after each scene load.
- The virtual stick is an Input System `OnScreenStick` bound to `<Gamepad>/leftStick`, so `PlayerController` can reuse the existing gamepad movement binding.
- The overlay hint is shown during active or paused runs; the start screen uses `GameManager.controlsHintText` instead.
- The touch overlay no longer owns a pause button or touch-controls toggle. Pausing is handled by the timer HUD, and the touch-controls toggle is cloned into the pause menu by `GameManager`.
- `TouchControlsActive` follows `InputMode.Touch` until the player makes an explicit choice. After that, the persisted `PlayerPrefs` key `TouchControlsEnabled` forces the stick on or off on any device.
- When the stick creates its virtual gamepad device, `MobileControlsUI` registers that device with `InputModeWatcher.IgnoreDevice()` so stick drags do not flip the input mode to gamepad.

### Public API
- `TouchControlsPrefKey` - `PlayerPrefs` key for the explicit touch-controls choice.
- `TouchControlsActive` - true when the on-screen stick should be visible during an active run.
- `TouchControlsChanged` - event raised after the toggle changes so `GameManager` can refresh hints and pause-menu labels.
- `ToggleTouchControlsPref()` - entry point used by the pause menu's **TOUCH CONTROLS** button.

### Test cases
- `MobileControlsToggleTests` covers auto/on/off preference behavior, persistence, event dispatch, and hint text variants.
- `MobileAndGamepadE2ETests` covers touch-mode activation, the virtual stick, the pause-menu toggle, and in-game hint updates in the real scene.

## Component: `TimerPauseTapHandler`

### Purpose
`TimerPauseTapHandler` makes the timer HUD itself the touch/click pause control, mirroring the keyboard **Esc** key and gamepad **Start** button.

### Behavior
- `GameManager.EnsureRuntimeUiRefs()` attaches the handler to `timerText` and `timer2Text` at startup and enables `raycastTarget` on those labels.
- The handler implements `IPointerClickHandler` and calls `GameManager.TogglePause()`.
- `TogglePause()` already guards on `IsGameActive`, so timer taps on the start screen or during the pre-run countdown do nothing.
- Tapping the timer while paused resumes the run, matching the Esc/Start toggle behavior.
- Runtime clones that are visually based on timer text but should not receive clicks (`countdownText`, `eliminationBannerText`, and penalty-popup clones) keep `raycastTarget = false`.

### Test cases
- `TimerPauseTapHandlerTests` covers active-run pause, no-op before gameplay starts, and second-tap resume.
- `MobileAndGamepadE2ETests.TouchTapOnTimer_TogglesPause_AndStaysReachableWhilePaused` verifies the timer can be tapped through the real EventSystem/GraphicRaycaster path.

## Runtime pause-menu buttons

`GameManager.EnsureRuntimeUiRefs()` clones the scene-authored music button to add pause-menu controls without changing the authored scene hierarchy:

| Runtime object | Purpose | Notes |
|---|---|---|
| `RotationButtonRuntime` | Toggles preferred Web orientation | Label comes from `OrientationManager.ButtonLabel`. |
| `TouchControlsButtonRuntime` | Toggles the on-screen stick on/off | Calls `MobileControlsUI.ToggleTouchControlsPref()` and updates between `TOUCH CONTROLS: ON` / `OFF`. |

Both clones replace their `Button.onClick` event with a fresh event before adding their own listener. This is required because instantiated buttons inherit the music button's persistent scene-wired `ToggleMusic` call.
