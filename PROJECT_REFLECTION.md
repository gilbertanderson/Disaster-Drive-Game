# Project Reflection — "My Game: Disaster"

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


| Prefab        | Contents                                                                                                                 | What it represents                           |
| ------------- | ------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------- |
| **Player**    | Vehicle shell + `VehicleSelector` with 8 swappable vehicle visuals, rear dirt emitters, `PlayerController`, and collider | The player-controlled vehicle                |
| **Walls**     | **Cube** primitives with gravel roadside material                                                                        | Side walls that narrow the lateral play area |
| **Obstacles** | Gameplay shell with `MoveDown`, `SphereCollider`, and a randomly swapped rock mesh (17 visual variants)                  | Rocks the player must dodge                  |
| **Goal**      | **Cube** (legacy from the original course; not used in the endless survival loop)                                        | Original finish-line marker                  |


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

On the start screen the vehicle is stationary. A **vehicle name label** shows the
current pick (short names, at most two words). After pressing **Drive**, the
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

1. **Start screen** — pick a vehicle with **<** / **>** (saved via
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

The biggest challenge I ran into was still **storage space on my computer**, since
Unity projects, the editor itself, and the build and export process all eat up
disk space quickly. I exported the wrong version because of that issue.

The second biggest challenge was **getting obstacles to spawn in the right
place**. I did not want them appearing outside the walls or clipping through
them, so instead of just picking a random spot I had `SpawnManager` look at where
the side walls actually are and figure out the range to spawn between them, with
a little padding so the rocks stay inside the play area.

**Player movement and wall collisions** also gave me trouble. I spent a good
amount of time getting the player to move the way I wanted while making sure it
could not pass through or get stuck on the side walls. Mass and `Rigidbody`
alone were not enough — I ended up clamping position in `PlayerController` and
clearing velocity each step so collisions could not shove the vehicle out of
bounds.

Another small thing that tripped me up was a **typo in one of my variable
names** in a script. Once I had it wired up in the Inspector, renaming it would
have broken the connection, so I left the misspelled name in place rather than
risk breaking everything.

I also had trouble **finding assets in the Unity Asset Store** that were low-poly, free, and compatible with my game. Some textures and materials came out magenta, which meant they were incompatible with my render pipeline, URP (universal render pipeline). I eventually found compatible vehicles and rocks that matched the theme. I used AI-assisted searching too, after failing to generate useful ones on my own; though I did create a night sky that I ended up scrapping for just lighting tweaks.

After selecting a vehicle in the UI, the run starts with smaller obstacles
approaching the player that **scale larger at random**, with difficulty also increasing every 10 seconds. Overall the main lessons were about spacing out obstacle spawns over time, reworking the X/Y/Z axises with gravity since I chose to change the game's plane originally and it would have broken too many things to change it back, syncing spawns with the moving ground plane, picking random obstacles from an array and removing them when they leave the screen, and getting game boundaries to work reliably.

The core design-doc promises top-down driving, rock avoidance, timer penalties, difficulty ramp, Disaster title, game over on timeout, impact audio are all met. Deviations expand presentation and replay value while keeping the same skill-based loop. No deviation was made to reduce scope; each addresses play feel, final assets, or Project 4 polish requirements. I really enjoyed this project, and I plan to add a good bit of polish.

## What is the version number of Unity that you used to create your Project 3?

I built the final Project using **Unity 6000.4.7f1**, the same Unity 6 version I used for Project 1, 2, and 3.
