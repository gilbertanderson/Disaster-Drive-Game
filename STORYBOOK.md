# Storybook: Vehicle Selector and Dirt Emitters

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

### Interaction
- `NextVehicle()` and `PreviousVehicle()` are wired to start-screen buttons and keyboard input.
- Selected vehicle index persists via `PlayerPrefs`.
- The dirt emitters start and stop when the game enters and exits the active driving state.

### Why the emitter rotation fix matters
Previously, emitter behavior was being influenced by steering or runtime direction calculations, which caused the dirt spray to look wrong when the player turned.
The fix keeps emitter orientation tied to the emitter prefab's native orientation, with placement updated per selected vehicle.

## Test cases
- `Awake_SetsParticleSimulationSpaceToLocal` ensures each emitter is configured for local particle simulation space.
- `Apply_PlacesEmitterBehindTheSelectedVehicleOnly` ensures the selected visual is active and the emitter remains behind it.
- `Update_DoesNotChangeEmitterRotationFromDefault` makes sure runtime update logic does not alter emitter rotation.

## Notes for extension
- Add any new vehicle models to `vehicleVisuals` and ensure their rear geometry matches emitter placement expectations.
- If dirt behavior should change from a pure static orientation to a dynamic road-relative direction, the emitter placement logic can consume `GroundScroller.WorldMoveDirection` without altering local emitter rotation.
- Keep `simulationSpace` set to `Local` for the particle systems so their emitted velocities are relative to the emitter orientation.
