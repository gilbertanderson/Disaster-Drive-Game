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

## Cursor Cloud specific instructions
- This is a pure Unity 6 (`6000.4.7f1`) Editor project. There is **no** offline/CLI build or test path: the game runs in the Editor, and both Edit Mode and Play Mode tests require the Unity Test Framework plus `UnityEngine`/`UnityEditor` assemblies (see `Assets/Tests/*/*.asmdef`). `dotnet`/`mono` alone cannot compile or run it because there are no standalone `UnityEngine.*` references without an Editor install.
- Running, building, or testing requires the Unity Editor `6000.4.7f1` (revision `f3c3c4248748`). It can run headless for tests via `Unity -batchmode -nographics -runTests -testPlatform EditMode|PlayMode -projectPath . -testResults <path>` and build via `-quit -batchmode -executeMethod`. No GPU is present (`/dev/dri` absent); `-nographics` is required.
- The Cloud VM's egress allowlist does **not** include Unity domains, so the Editor cannot be downloaded or licensed here by default. Getting a runnable environment needs BOTH of the following (currently blocking): (1) egress allowlist entries for `download.unity3d.com`, `public-cdn.cloud.unity3d.com`, `hub.unity3d.com`, `license.unity3d.com`, and `*.unity3d.com` / `*.unity.com` / `services.api.unity.com`; and (2) a Unity license — set `UNITY_EMAIL` + `UNITY_PASSWORD` (Personal) and `UNITY_SERIAL` (Pro/Plus) as secrets, or provide a `.ulf` activation file. These are consumed only during Editor activation, not by the update script.
- Do not put the Unity Editor download/activation in the startup update script — those network calls fail under the default egress policy and would break future pods. Perform Editor install/activation as an explicit step once the allowlist and license are in place.
- Because the Editor is unavailable, Unity restores its own package cache on first Editor launch; there is no `npm install`-style CLI dependency step for this repo. The `Library/`, `Temp/`, and `Logs/` folders are Editor-generated and git-ignored.
