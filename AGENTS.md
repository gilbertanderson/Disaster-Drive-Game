# AI Agent Guidance for My Game: Disaster

## Project Overview
- Unity 6.0.4.7f1 project named `My Game: Disaster`.
- 3D arcade-style driving game built with Unity primitives, the new Input System, and URP-compatible scene setup.
- Core gameplay is implemented in `Assets/Scripts/` and tests are in `Assets/Tests/`.

## Key files and areas
- `README.md` — project overview, features, and test guidance.
- `STORYBOOK.md` — component behavior for `VehicleSelector` and dirt emitter logic.
- `PROJECT_REFLECTION.md` — design choices, gameplay mechanics, and Unity version.
- `Assets/Scripts/` — gameplay, input, camera, and particle emitter logic.
- `Assets/Tests/VehicleSelectorTests.cs` — existing Edit Mode tests for vehicle selection and emitter placement.
- `Assets/Prefabs/` — game object prefabs that should stay consistent with script logic.

## What AI agents should know
- This repository is a Unity project; changes should assume Unity Editor workflows rather than standard CLI build tools.
- Use Unity Test Runner for tests. `Assets/Tests/VehicleSelectorTests.cs` is the current test entry point.
- Preserve scene and prefab relationships in `Assets/` and avoid editing generated Unity files under `Library/`, `Temp/`, or `GeneratedAssets/`.
- `VehicleSelector` is responsible for selecting one vehicle visual at a time, resizing colliders, and positioning rear dirt emitters.
- The dirt emitter fix depends on keeping `ParticleSystem.simulationSpace` set to `Local` and letting the emitter prefab define its native orientation.
- `PlayerPrefs` is used to persist the selected vehicle index.

## Recommended agent behavior
- Prefer referencing `README.md`, `STORYBOOK.md`, and `PROJECT_REFLECTION.md` for feature and behavior intent.
- When making gameplay changes, check the relevant prefab and scene semantics before changing emitter placement or collider sizing.
- Avoid broad refactors that touch Unity-generated project files unless the user explicitly asks for project cleanup or migration.
- If asked to implement tests or verify behavior, recommend the Unity Editor Test Runner and note that there is no visible CLI test runner configured.

## Notes for future customization
- If the repo later gets additional AI customization files, preserve them and keep this guidance focused on project-level instructions.
- Keep this file minimal and actionable; link to existing workspace docs rather than duplicating their content.
