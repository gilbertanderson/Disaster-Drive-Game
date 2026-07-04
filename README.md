# Disaster Drive Game

A Unity project built with URP for a top-down driving game where the player navigates through obstacles and decorative scenery.

## Overview

- Vehicle selection and driving gameplay.
- Procedural obstacles and decorative tree scrolling.
- Runtime lighting transitions from evening through sunset to daylight.
- Dirt particle effects behind vehicles.

## Setup

1. Open `My Game - Disaster.slnx` in Unity 2024+ or newer compatible with URP.
2. Allow Unity to refresh and compile scripts.
3. Open the main scene from `Assets/Scenes` if not already loaded.

## Key Files

- `Assets/Scripts/GameManager.cs` — game flow, timer, and lighting transition logic.
- `Assets/Scripts/GroundScroller.cs` — road texture scrolling and world movement direction.
- `Assets/Scripts/TreeScroller.cs` — decorative tree motion alongside the road.
- `Assets/Scripts/VehicleSelector.cs` — vehicle picker and dirt particle emitter positioning.
- `Assets/Scripts/SpawnManager.cs` — obstacle spawning and difficulty scaling.

## Controls

- `A` / left arrow: previous vehicle on the start screen.
- `D` / right arrow: next vehicle on the start screen.
- Drive button on start screen: begin the game.

## Notes

- This project uses local particle simulation space for dirt emitters so the particle velocity follows emitter rotation.
- Tree scrolling speed is linked to ground speed via `GroundScroller`.
- Lighting is controlled in `GameManager` and gradually brightens over time.

## Troubleshooting

- If walls appear magenta, verify URP-compatible materials are assigned.
- If particle effects look wrong, check emitter orientation and `simulationSpace` settings.
