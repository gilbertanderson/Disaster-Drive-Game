# Project 2 Reflection — "My Game: Disaster"

## What did you use to create the environment for your game?
The environment mixes Unity primitives with imported vehicle and rock assets. The
ground is a **Plane** with a scrolling UV texture (`GroundScroller`) so the road
appears to move under the vehicle. A **Directional Light** drives an evening-to-
daylight transition over each run, with fog and ambient colors lerped by
`GameManager`. Decorative **trees** scroll along the roadside via `TreeScroller`
and `TreeSpawnManager`. Scene objects are grouped under parent empties
(`Environment`, `Obstacles`, `UI`) to keep the hierarchy organized.

## What Prefabs did you create for your game? Which primitives represent each of these objects?
Interactive prefabs live in `Assets/Prefabs/`:

| Prefab        | Contents | What it represents |
|---------------|----------|--------------------|
| **Player**    | Vehicle shell + `VehicleSelector` with 8 swappable vehicle visuals, rear dirt emitters, `PlayerController`, and collider | The player-controlled vehicle |
| **Walls**     | **Cube** primitives | Side walls that narrow the lateral play area |
| **Obstacles** | Gameplay shell with `MoveDown`, `SphereCollider`, and a randomly swapped rock mesh (17 visual variants) | Rocks the player must dodge |
| **Goal**      | **Cube** (legacy from the original course; not used in the endless survival loop) | Original finish-line marker |

The ground itself is a **Plane** in the scene (not a prefab).

## Which primitive object is your player?
The player is no longer a single cube. The **Player** prefab is a shell whose
`VehicleSelector` activates one of eight imported vehicle models at a time. A
**BoxCollider** is resized to fit the active visual, and the vehicle moves with
a **Rigidbody** using `MovePosition` so collisions with rocks are respected.

## Describe how your player moves — in what directions, and what keys are used for input?
The player moves across the ground plane in **four directions** (forward,
backward, left, right, and diagonals). Input is read through the **new Input
System** as a 2D vector with two binding sets:

- **W / A / S / D**
- **Arrow keys**

Movement is projected onto the camera's screen-right and screen-forward axes so
controls match what the player sees. Direction is normalized so diagonals are
not faster, and movement runs in `FixedUpdate` with `Rigidbody.MovePosition`.

On the start screen the vehicle is stationary. After pressing **Drive**, the
timer counts down. When time reaches zero, the vehicle enters an **exit drive**
— it continues up-screen while the world keeps scrolling — before the game over
panel appears.

## What are the boundaries for the player? Where can and where can't they move?
- **Can move:** anywhere on the ground that is **visible on screen**, inset by the
  vehicle collider's half-extents and a small padding so the body never clips
  the edge. Lateral movement is also clamped to the inner faces of the **side
  walls**.
- **Can't move:** off-screen, through walls, or through rocks (physics collisions
  apply). Rotation is frozen and angular velocity is cleared each step so the
  vehicle stays upright.
- **Goal:** survive as long as possible. Each rock hit costs **5 seconds** on the
  timer. Close **near-misses** (rocks passing within a lateral gap without
  contact) award **+2 seconds**, with a short cooldown between awards.

## How does gameplay work beyond the original "reach the Goal" design?
The game is an **endless survival** arcade loop:

1. **Start screen** — pick a vehicle with **&lt;** / **&gt;** (saved via
   `PlayerPrefs`), then press **Drive**.
2. **Survive** — dodge scrolling rocks while a **countdown timer** ticks. Rocks
   spawn with random visuals, sizes, and slight speed jitter; difficulty ramps
   every 10 seconds (faster vehicle, faster rocks, shorter spawn interval).
3. **Hits** — each collision removes time, shakes the camera, and spawns dust.
4. **Near-misses** — skimming past a rock without hitting it adds time back.
5. **Game over** — when the timer hits zero, the vehicle drives off-screen, rocks
   and scrollers keep moving during the exit (`IsWorldAnimating`), then the game
   over panel shows survival time, dodges, hits, best streak, and wave reached.
   A **top-5 local leaderboard** is stored in `PlayerPrefs`.
6. **Retry** reloads the scene.

`IsWorldAnimating` is the shared contract: ground, trees, rocks (`MoveDown`), and
dirt emitters all scroll while a run is active **or** during the post-game-over
exit drive.

## What issues or challenges did you face completing this project?
- **Keeping the player on screen:** boundaries adapt to the camera each frame by
  projecting viewport corners into world space and clamping the vehicle position.
- **Physics stability:** freezing rotation and zeroing angular velocity stopped
  the vehicle from tipping on rock impacts.
- **Endless-runner illusion:** matching scroll speeds across ground UV offset,
  tree repositioning, and rock `MoveDown` speed required shared tuning via
  `GroundScroller.WorldSpeed`.
- **Vehicle variety:** eight models needed normalized colliders and rear dirt
  emitters aligned per vehicle; `ParticleSystem` simulation space stays **Local**
  so spray follows each model's orientation.
- **Game-over exit drive:** freezing everything at timer zero felt abrupt.
  `IsVehicleExiting` lets the world keep animating while the vehicle leaves the
  screen; rocks had to gate on `IsWorldAnimating` (not just `IsGameActive`) so
  they no longer freeze mid-air during the exit.
- **Camera shake:** jitter in world X/Y was invisible from a top-down camera;
  shaking in the camera's local right/up plane made impacts readable.
- **Rock visual swap:** a single obstacle shell swaps in one of 17 mesh prefabs
  at spawn, fits a `SphereCollider` to the mesh bounds, and disables child
  colliders on the visual so only the shell handles physics.

## What is the version number of Unity that you used to create your Project 2?
**Unity 6000.4.7f1** (Unity 6).
