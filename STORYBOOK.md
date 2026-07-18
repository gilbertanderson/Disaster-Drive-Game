# Storybook: Gameplay Components

This file documents runtime gameplay contracts that are easy to break from the
Inspector because several systems are wired together by convention. Keep it in
sync with `Assets/Scripts/` and the matching Edit/Play Mode tests.

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
- `playerIndex` - synced from the local `PlayerController` in `Awake()` so P1
  and P2 save different `PlayerPrefs` keys.

### Interaction
- `NextVehicle()` and `PreviousVehicle()` are wired to start-screen buttons and keyboard input.
- Selected vehicle index persists via `PlayerPrefs`.
- In two-player mode, `EnsureDistinctFrom()` prevents both selectors from using
  the same vehicle. Auto-deduping does **not** persist P2's temporary pick, so a
  saved P2 choice is only written after the player explicitly cycles P2.
- The dirt emitters start and stop when the game enters and exits the active driving state.

### Why the emitter rotation fix matters
Previously, emitter behavior was being influenced by steering or runtime direction calculations, which caused the dirt spray to look wrong when the player turned.
The fix keeps emitter orientation tied to the emitter prefab's native orientation, with placement updated per selected vehicle.

## Test cases
- `Awake_SetsParticleSimulationSpaceToLocal` ensures each emitter is configured for local particle simulation space.
- `Apply_PlacesEmitterBehindTheSelectedVehicleOnly` ensures the selected visual is active and the emitter remains behind it.
- `Update_DoesNotChangeEmitterRotationFromDefault` makes sure runtime update logic does not alter emitter rotation.
- `EnsureDistinctFrom_DoesNotRewriteSavedVehicleChoice` protects the 2P auto-dedupe persistence rule.

## Notes for extension
- Add any new vehicle models to `vehicleVisuals` and ensure their rear geometry matches emitter placement expectations.
- If dirt behavior should change from a pure static orientation to a dynamic road-relative direction, the emitter placement logic can consume `GroundScroller.WorldMoveDirection` without altering local emitter rotation.
- Keep `simulationSpace` set to `Local` for the particle systems so their emitted velocities are relative to the emitter orientation.

## Component: `PlayerController` input schemes

### Purpose
`PlayerController` owns movement, wall bounds, vehicle-vehicle separation, and
the control scheme currently assigned by `GameManager`.

### Binding matrix
`ApplyControlScheme()` replaces and disposes the previous `InputAction`; do not
layer extra bindings on the serialized prefab action at runtime.

| Mode | Scheme | Keyboard | Gamepad / virtual stick |
|------|--------|----------|--------------------------|
| 1P | `WasdAndArrows` | WASD and arrows | left stick and d-pad |
| 2P P1 | `WasdAndLeftStick` | WASD only | left stick and d-pad |
| 2P P2 | `ArrowsAndRightStick` | arrows only | right stick only |
| Disabled P2 | `DisableMovementInput()` | none | none |

Touch controls are built by `MobileControlsUI` as Input System `OnScreenStick`
controls:

- P1 stick: bottom-left, feeds `<Gamepad>/leftStick`.
- P2 stick: bottom-right, only shown in 2P, feeds `<Gamepad>/rightStick`.
- The virtual gamepad device is registered with `InputModeWatcher.IgnoreDevice()`
  so touch drags do not switch UI hints to gamepad mode or mask a physical
  Start-button pause press.

### Movement constraints
- Movement only runs while `GameManager.IsWorldAnimating` is true. Start screen
  and pause zero input and velocities; exit drive uses its own path.
- Analog input magnitude scales speed, so partial stick deflection is slower
  than full deflection. Values below `movementDeadzone` zero movement.
- Bounds combine camera-visible limits and the inner faces of `Walls`.
  `WallBoundsUtility` only reports success when it finds colliders on both
  sides of the reference position; callers keep fallback ranges otherwise.
- In 2P, vehicles are solid against active racers but ignore eliminated/exiting
  vehicles so a loser leaving the screen cannot pin or block the survivor.

### Tests
- `PlayerControllerControlSchemeTests` covers the binding matrix.
- `VehicleExitTests` covers exit-drive and world-animation behavior.
- `TwoPlayerModeTests` covers vehicle collision, elimination, and duplicate
  vehicle prevention.

## System: two-player scoring and collisions

### Public calls
`MoveDown` and `PlayerController` report gameplay events to `GameManager`:

- `OnPlayerHit(hitPoint, who)` charges the hit player's clock. The older
  parameterless overload still charges P1 for legacy callers/tests.
- `OnNearMiss(who)` adds the near-miss bonus to the dodging player's clock.
- `OnVehicleCollision(hitPoint, a, b)` resolves P1/P2 vehicle crashes.
- `IsPlayerInteractable(who)` is the shared gate for "still racing": active in
  the hierarchy, not eliminated, and not currently exiting.

### Rock hits and near-misses
- Rock-hit penalties are charged to the `PlayerController` on the collider that
  was hit. Eliminated or exiting vehicles are ignored so they do not consume
  rocks meant for a survivor.
- Near-miss cooldowns are per player (`lastNearMissTime[0]` and `[1]`), so P2
  can earn a bonus while P1 is still cooling down.
- `MoveDown` awards a near-miss after the rock passes a racer along the travel
  axis. In 2P it chooses the closest lateral gap among interactable racers the
  rock has already passed.
- Each rock only evaluates near-miss once: after a successful award or after it
  clears the nearest racer with a wide gap.

### Vehicle-vehicle crashes
- Crashes only count in two-player mode while both racers are interactable.
- `vehicleCollisionCooldown` dedupes both sides of the same contact scrape.
- Rammer blame compares each vehicle's approach speed **toward the other
  vehicle**. The faster approaching player loses `vehicleCrashRammerPenalty`
  seconds; the other loses `vehicleCrashVictimPenalty`; near ties lose
  `vehicleCrashTiePenalty` each.
- Each player has one popup slot. Starting new penalty/bonus feedback cancels any
  in-flight popup coroutine for that player, preventing overlapping text/color
  fights.

### Tests
- `OnNearMiss_CooldownIsPerPlayer` protects independent near-miss timers.
- `OnPlayerHit_ChargesOnlyTheHitPlayersClock` protects rock-hit ownership.
- `OnVehicleCollision_ChargesRammerMoreThanVictim` protects approach-direction
  blame.
- `OnVehicleCollision_IgnoresRepeatWithinCooldown` protects contact deduping.

## System: spawning and wall bounds

### Purpose
`SpawnManager` owns obstacle cadence and rock presentation; `MoveDown` owns each
rock's travel, dodge credit, hit response, and cleanup.

### Runtime contracts
- Rocks spawn just beyond the camera's top edge (`CacheSpawnX()`), not from a
  hand-tuned constant.
- Lateral spawn range comes from `WallBoundsUtility.TryGetPaddedRange()`. If the
  scene does not contain a low-Z and high-Z wall collider, the spawner keeps its
  serialized fallback range instead of trusting a half-resolved bound.
- Spawning uses three lanes. In 2P, `PickSpawnZ()` alternates between the lower
  and upper halves of the wall range so both racers see traffic.
- Runtime builds cannot use `AssetDatabase`; the scene `SpawnManager` must have
  `rockVisuals` assigned. If no usable visuals are available, startup logs a
  warning and falls back to the obstacle shell list where possible.
- `MoveDown.ActiveRockCount` is the spawn cap source of truth. Destroying rocks
  off-screen or after a hit frees capacity for the next spawn.

### Tests
- `GameManagerGameplayTests` covers dodge/near-miss scoring.
- `VehicleExitTests` covers rocks continuing while `IsWorldAnimating` is true.
- `RubricE2ETests` covers the visible rock spawn and gameplay rubric flows.
