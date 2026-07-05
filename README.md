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
5. Press **Esc** to pause (credits appear in the top-right of the pause overlay).
6. When time runs out, the vehicle drives off screen, then the game over panel appears — click **Retry** to reload.

## Building a Player
The playable scene is already listed in build settings.

1. Open **File → Build Profiles** (or **Build Settings** on older layouts).
2. Confirm `Assets/Scenes/My Game.unity` is in **Scenes In Build** and enabled.
3. Choose a target platform (e.g. **macOS**, **Windows**, **WebGL**).
4. Click **Build** or **Build And Run**.
5. For distribution, use the platform-specific player settings under **Edit → Project Settings → Player** (company name, product name, icons, resolution).

No custom pre-build scripts or Netlify/CI pipeline are configured — builds are produced from the Unity Editor.

## Running Tests
Edit Mode tests live in `Assets/Tests/`.

1. Open **Window → General → Test Runner**.
2. Select the **EditMode** tab.
3. Run **VehicleSelectorTests** and **VehicleExitTests**.

Tests cover vehicle selection, dirt emitter placement, and game-over exit state (`IsWorldAnimating`, off-screen drive).

## Key Features
- Vehicle selection screen with live model switching (choice saved via `PlayerPrefs`).
- Rear dirt particle emitters aligned to each vehicle's rear geometry.
- Camera-based screen bounds with wall clamping.
- Endless runner scroll (ground, trees, rocks) with difficulty ramping.
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
- `Assets/Scripts/GameManager.cs` — timer, pause, game over, scoring
- `Assets/Scripts/PlayerController.cs` — movement, bounds, exit drive
- `Assets/Scripts/VehicleSelector.cs` — vehicle picker and dirt emitters
- `Assets/Scripts/GroundScroller.cs` / `TreeScroller.cs` — world scroll
- `Assets/Tests/VehicleSelectorTests.cs` / `VehicleExitTests.cs`
- `Assets/Scenes/My Game.unity` — main playable scene
- `PROJECT_REFLECTION.md` — course reflection and design notes
- `STORYBOOK.md` — component behavior reference for AI agents

## Notes for Developers
- `VehicleSelector` depends on `GameManager` to gate selection input and emitter playback.
- `IsWorldAnimating` keeps scrollers and dirt spray running during the post-game-over vehicle exit.
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

Third-party vehicle and environment assets are credited in their respective `Assets/` package folders where applicable.
