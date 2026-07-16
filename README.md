# My Game: Disaster

## Overview
`My Game: Disaster` is a Unity 3D arcade-style endless driving game built with **Unity 6000.4.7f1**.
The player steers a vehicle across a scrolling road, dodges rocks, and survives as long as possible before the timer runs out.
The project uses the new Input System, URP-compatible lighting, and keyboard-driven controls (WASD and arrow keys).

## Requirements
- **Unity Editor:** `6000.4.7f1` (Unity 6) — see `ProjectSettings/ProjectVersion.txt`
- **Platforms tested in editor:** macOS (primary development target)
- **Input:** New Input System package (movement actions on the player prefab)

## Opening the Project
1. Clone or download this repository.
2. Open **Unity Hub** → **Add** → select the project folder (`My Game - Disaster`).
3. Open the project with Unity **6000.4.7f1** (Hub should offer the correct version from `ProjectVersion.txt`).
4. When Unity finishes importing, open the main scene: `Assets/Scenes/My Game.unity`.

## Playing in the Editor
1. Press **Play** in the Unity Editor.
2. On the start screen, pick a vehicle with **&lt;** / **&gt;** (or **A** / **D**).
3. Click **Drive** to begin a run.
4. Steer with **WASD** or **arrow keys**; avoid rocks (each hit costs time).
5. Skim past rocks without hitting for a **+2s** near-miss bonus (with sound feedback).
6. Press **Esc** to pause (credits appear in the top-right of the pause overlay).
7. When time runs out, the vehicle drives off screen, then the game over panel appears — click **Retry** to reload.

## Building a Player
The playable scene is already listed in build settings.

1. Open **File → Build Profiles** (or **Build Settings** on older layouts).
2. Confirm `Assets/Scenes/My Game.unity` is in **Scenes In Build** and enabled.
3. Choose a target platform (e.g. **macOS**, **Windows**, **WebGL**).
4. Click **Build** or **Build And Run**.
5. For distribution, use the platform-specific player settings under **Edit → Project Settings → Player** (company name, product name, icons, resolution).

Deployment target is **Unity Play**: builds run in **Unity Build Automation** (Unity Cloud) and are shared/published to the browser from there — see [`.github/CICD_SETUP.md`](.github/CICD_SETUP.md) for the dashboard setup and the **Publish → WebGL Project** (WebGL Publisher) flow. GitHub Actions runs tests and CI artifact builds only.

Web builds use the custom **DisasterDrive** WebGL template (`Assets/WebGLTemplates/DisasterDrive/`), selected in Player Settings and every Web build profile. On mobile browsers it locks the screen to the preferred orientation (landscape by default; lock engages in fullscreen where the browser supports it) and shows a "Please Rotate Your Device" overlay when the device is held the wrong way — iOS Safari can't lock, so the overlay is the fallback there. Build file names are injected via template macros, so product renames or compression changes can't break loading. The pause menu's **ROTATION** toggle switches the preferred orientation (landscape ↔ portrait) on any device; the choice persists via `PlayerPrefs` (`PreferredOrientation`) and, on the web, `localStorage`, so the page applies it again on the next load.

## Project Submission
Submit all three items required by the rubric:

1. **Unity package** — Assets → Export Package → include `Assets/`, `ProjectSettings/`, and `Packages/manifest.json` (exclude `Library/`).
2. **Build folder** — standalone player from **File → Build Profiles**.
3. **Reflection** — [`PROJECT_REFLECTION.md`](PROJECT_REFLECTION.md)

Optional supporting docs: [`DESIGN_DEVIATIONS.md`](DESIGN_DEVIATIONS.md), [`PROJECT_RUBRIC.md`](PROJECT_RUBRIC.md).

## Running Tests
Edit Mode tests live in `Assets/Tests/`.

1. Open **Window → General → Test Runner**.
2. Select the **EditMode** tab.
3. Run all tests in:
   - `VehicleSelectorTests`
   - `VehicleExitTests`
   - `GameManagerGameplayTests`
   - `GroundScrollerTests`
   - `InputModeWatcherTests`
   - `MobileControlsToggleTests`
   - `PlayerControllerControlSchemeTests`
   - `GameManagerPauseInputTests`
   - `OrientationPreferenceTests`

Tests cover vehicle selection, dirt emitters, game-over exit drive, near-miss scoring, pause, leaderboard, ground scroll gating, and core gameplay rules.

### Mobile & gamepad input tests

Gamepad and touch input is **simulated** with the Input System's `InputTestFixture` — no controller or touchscreen hardware is needed. Edit Mode suites cover input-mode detection (`InputModeWatcherTests`), the touch-controls toggle preference (`MobileControlsToggleTests`), the screen-orientation preference behind the pause menu's ROTATION toggle (`OrientationPreferenceTests`), the per-scheme movement bindings including left stick and d-pad (`PlayerControllerControlSchemeTests`), and the Esc/Start pause hotkeys (`GameManagerPauseInputTests`). The Play Mode suite `OrientationToggleE2ETests` verifies the runtime-built ROTATION button in the pause menu. The Play Mode suite `MobileAndGamepadE2ETests` drives the real scene with a simulated gamepad and touchscreen: stick/d-pad movement, Start-button pause and resume, touch-mode switching, the on-screen stick and the persistent top-left pause button, the TOUCH CONTROLS toggle (top left, under the controls hints, on both the start screen and during runs), and the controls-hint text.

Enabling this required listing `com.unity.inputsystem` under `testables` in `Packages/manifest.json`, which also makes the Input System package's **own** tests appear in Test Runner — they can be ignored (or filtered out by selecting only the `DisasterTests` / `DisasterPlayModeTests` assemblies).

### Rubric E2E (Play Mode + video)

Play Mode tests in `Assets/Tests/PlayMode/RubricE2ETests.cs` map directly to [`PROJECT_RUBRIC.md`](PROJECT_RUBRIC.md) (gameplay, audio, particles).

**Run with video capture (recommended before submit):**

1. Open `Assets/Scenes/My Game.unity`.
2. **Disaster → Run Rubric E2E with Video** (records every scenario).
3. Review videos at `TestResults/RubricE2E/<scenario>/video.webm` (e.g. `open TestResults/RubricE2E/01_start_screen_disaster_title/video.webm`).

Requires **ffmpeg** on your PATH for `.webm` encoding (`brew install ffmpeg`). Without ffmpeg, PNG frame sequences are still saved under each scenario’s `frames/` folder.

**Run without video:** Test Runner → **PlayMode** tab → run `RubricE2ETests` (faster; no screen capture). The same tab also runs `MobileAndGamepadE2ETests` (simulated gamepad/touch input; no video capture).

| Test | Rubric area |
|------|-------------|
| `01`–`08` | Gameplay improvements (start, drive, WASD, hit penalty, near-miss, pause, game over, rock spawn) |
| `09` | Music & sound (impact, music, click, near-miss clips) |
| `10` | Particle effects (dust, rubble, fireworks, vehicle dirt emitters) |

Unity MCP can also run Edit Mode tests when the Editor is connected.

## Key Features
- Vehicle selection screen with live model switching (choice saved via `PlayerPrefs`).
- Rear dirt particle emitters aligned to each vehicle's rear geometry.
- Camera-based screen bounds with wall clamping.
- Endless runner scroll (ground, trees, rocks) with difficulty ramping.
- Live run UI: wave number, dodge streak, low-time timer warning.
- Adaptive controls: keyboard, gamepad, and an on-screen virtual stick (bottom-left), with a persistent top-left stack — controller hints, a TOUCH CONTROLS toggle (start screen and in-game), and a pause button that stays available during runs for every input device; the toggle choice persists via `PlayerPrefs`.
- Near-miss bonus with sound and `+2s` popup.
- Game-over exit: vehicle drives off screen while the world keeps animating, then everything stops.
- Camera shake on rock impacts (screen-plane jitter).
- Start / pause / game-over flow managed by `GameManager`.

## Vehicle Selector Behavior
The `VehicleSelector` component:
- stores multiple vehicle visuals in `vehicleVisuals`
- activates exactly one vehicle at a time
- resizes the `BoxCollider` to fit the active visual
- repositions rear dirt emitters to match the active vehicle's rear geometry
- keeps dirt particles in local simulation space so velocities respect emitter orientation

## Important Files
- `Assets/Scripts/GameManager.cs` — timer, pause, game over, scoring, audio
- `Assets/Scripts/PlayerController.cs` — movement, bounds, exit drive
- `Assets/Scripts/VehicleSelector.cs` — vehicle picker and dirt emitters
- `Assets/Scripts/GroundScroller.cs` / `TreeScroller.cs` — world scroll
- `Assets/Scripts/WallBoundsUtility.cs` — shared side-wall range math
- `Assets/Tests/` — Edit Mode test suite
- `Assets/Scenes/My Game.unity` — main playable scene
- `PROJECT_REFLECTION.md` / `PROJECT_REFLECTION.md` — course reflections
- `STORYBOOK.md` — component behavior reference for AI agents

## Notes for Developers
- `VehicleSelector` depends on `GameManager` to gate selection input and emitter playback.
- `IsWorldAnimating` keeps scrollers and dirt spray running during the post-game-over vehicle exit.
- `MoveDown.ActiveRockCount` tracks live rocks for spawn cap checks.
- If you add vehicle prefabs, keep emitter positions behind the fitted collider's rear face.
- Avoid editing generated folders (`Library/`, `Temp/`, `Logs/`).
- Unity MCP is available in this project for editor automation and test runs.

## Credits
Same attribution shown on the pause screen (top-right):

| | |
|---|---|
| **Game** | Gilbert Anderson |
| **Music** | ["Climber"](https://thewzzard.com) by The Wzzard |
| **Sound & UI FX** | Unity Create With Code library |

Third-party vehicle and environment assets — see [`THIRD_PARTY_LICENSES.md`](THIRD_PARTY_LICENSES.md) for the full attribution list.
