# Project 2 Reflection — "My Game: Disaster"

## What did you use to create the environment for your game?
The environment is built from Unity's built-in 3D primitives rather than imported
art. The ground is a **Plane** primitive that the player drives across, lit by a
single **Directional Light** and using a **Global Volume** for post-processing.
The scene objects are grouped under empty parent objects (`Enviornment`,
`Obstacles`, `Goal`) to keep the hierarchy organized. Objects are colored using
four simple Materials I created — **Blue, Red, Green, and Purple**.

## What Prefabs did you create for your game? Which primitives represent each of these objects?
I created four Prefabs, saved in a dedicated `Assets/Prefabs/` folder, covering
every interactive object in the game:

| Prefab        | Primitive(s) | What it represents                                                        |
|---------------|--------------|---------------------------------------------------------------------------|
| **Player**    | **Cube**     | The player-controlled vehicle (the `PlayerVehicle` cube + its controller) |
| **Walls**     | **Cube**     | The obstacle walls that block the player                                  |
| **Obstacles** | **Sphere**   | The rocks scattered along the path                                        |
| **Goal**      | **Cube**     | The finish line at the end of the course                                  |

The ground itself is a **Plane** primitive (part of the environment rather than an
interactive prefab).

## Which primitive object is your player?
The player is a **Cube** (the `PlayerVehicle` object, a child of the `Player`
object). It has a Rigidbody so it moves with physics and collides with the
obstacles.

## Describe how your player moves — in what directions, and what keys are used for input?
The player moves across the ground plane in **four directions: forward, backward,
left, and right** (and any diagonal combination of these). Input is read through
the new Input System as a 2D vector, with two sets of keys bound:

- **W / A / S / D**
- **Arrow keys (Up / Down / Left / Right)**

The horizontal input drives movement along the X axis and the vertical input
drives movement along the Z axis. The direction is normalized so moving
diagonally is not faster than moving straight, and movement is applied in
`FixedUpdate` with `Rigidbody.MovePosition` so collisions are respected.

## What are the boundaries for the player? Where can and where can't they move? What should and shouldn't they be able to do?
- **Can move:** anywhere on the ground plane that is **visible on screen**. The
  game calculates the camera's viewport edges and clamps the player's position so
  it always stays inside the camera's view (with a small padding so the cube never
  clips into the screen edge).
- **Can't move:** off-screen / past the visible edges of the playfield, and it
  **can't pass through the walls or rocks** because movement is physics-based.
- **Should be able to do:** drive freely around obstacles and reach the finish
  line.
- **Shouldn't be able to do:** leave the visible area, tip over, or spin out — its
  rotation is frozen (`FreezeRotation`) and any angular velocity from collisions is
  cancelled each physics step, so it stays upright and stable.

## What issues or challenges did you face completing this project?
- **Keeping the player on screen:** the trickiest part was making the boundaries
  adapt to the camera instead of hard-coding numbers. I solved this by projecting
  the camera's viewport corners into world space each frame and clamping the
  player's position to those limits.
- **Physics behaving oddly:** the cube would tip or spin when it hit obstacles.
  Freezing rotation on the Rigidbody and zeroing out angular velocity fixed it.
- **Consistent movement speed:** diagonal movement felt faster than straight
  movement until I normalized the input direction.
- **Supporting two control schemes:** I wanted both WASD and arrow keys, which
  meant adding both binding sets to the Input Action.

## What is the version number of Unity that you used to create your Project 2?
**Unity 6000.4.7f1** (Unity 6).
