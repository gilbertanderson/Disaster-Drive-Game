# My Game: Disaster

## Overview
`My Game: Disaster` is a Unity 3D arcade-style driving game built using Unity 6000.4.7f1.
The player drives a cube-shaped vehicle across a scrolling environment while avoiding obstacles and reaching the finish line.
The project uses the new Input System, a simple URP-compatible scene, and gameplay mechanics designed for a mobile or keyboard-driven experience.

## Key Features
- Vehicle selection screen with live model switching.
- Rear dirt particle emitters that align to each selected vehicle's rear geometry.
- Screen-bound movement with clamped camera-based limits.
- Dynamic obstacle collisions and a finish-line goal.
- Simple start/pause/game-over flow managed by `GameManager`.

## Vehicle Selector Behavior
The `VehicleSelector` component:
- stores multiple vehicle visuals in `vehicleVisuals`
- activates exactly one vehicle at a time
- resizes the `BoxCollider` to fit the active visual
- repositions the rear dirt emitters to match the active vehicle's rear geometry
- keeps dirt particles in local simulation space so particle velocities respect emitter orientation

## Emitter Orientation Fix
The dirt emitter logic now positions emitters behind each vehicle and relies on the emitter prefab's default orientation.
This avoids in-game steering-based rotation changes that previously caused the dirt effect to flip or appear inconsistent during player control.

## Tests Added
Added `Assets/Tests/VehicleSelectorTests.cs` to verify:
- `Awake()` configures dirt emitters for local particle simulation space
- `Apply()` selects the correct vehicle visual from saved PlayerPrefs
- emitters are placed behind the selected vehicle
- runtime `Update()` does not modify emitter rotation from the prefab default

## Running Tests
Use Unity Test Runner with Edit Mode tests enabled.
Open the Test Runner window and run `VehicleSelectorTests`.

## Notes for Developers
- `VehicleSelector` depends on `GameManager` to gate selection input and emitter playback.
- `GroundScroller` is referenced for emitter direction fallback, but current logic only uses it if needed for future orientation support.
- If you change emitter placement or add new vehicle prefabs, make sure the emitter positions still sit behind the rear of the fitted collider.

## Important Files
- `Assets/Scripts/VehicleSelector.cs`
- `Assets/Tests/VehicleSelectorTests.cs`
- `Assets/Scripts/PlayerController.cs`
- `Assets/Scripts/GroundScroller.cs`
- `PROJECT_REFLECTION.md`
