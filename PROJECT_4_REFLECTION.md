# Project 4 Reflection — "My Game: Disaster"

## What issues or challenges did you face completing this project?

Storage space was still my biggest headache. Unity 6, the full project, imported
vehicle and environment packs, and build/export steps kept eating disk space, and
that made it easy to export or back up the wrong version when I was trying to
free room.

Polish work added a lot of new problems on top of the basics from Project 3.
Getting **eight different vehicles** to feel like one fleet was harder than I
expected. Each model had a different pivot and size, so I had to normalize
scales, align them to a shared ground point, and resize the `BoxCollider` every
time the player switched vehicles. I also had **ghost slots** in the vehicle
array where a deleted prefab left a null entry, which broke cycling until I
compacted the list in code.

**Rear dirt emitters** were another rabbit hole. The spray looked wrong when I
tried to tie rotation to steering, so I kept the particles in local simulation
space and repositioned the emitters behind each vehicle's fitted collider
instead. Even then I had to align the emitter's right axis with the road scroll
direction so the dust kicked backward consistently for every vehicle.

Turning the game into an **endless survival loop** meant rethinking game over.
Stopping everything the moment the timer hit zero felt abrupt, so I added an exit
drive where the vehicle keeps moving off screen while rocks, trees, and the
ground keep scrolling. That required a shared `IsWorldAnimating` flag so
`MoveDown`, `GroundScroller`, and the dirt emitters all agreed on when the world
should still move. Once **two-player mode** existed, exit had to work per
vehicle instead of once globally, and I had to add an `exitViaBottom` option so
a player eliminated near the bottom of the screen exits downward instead of
snapping to the same top-exit path as everyone else.

**Syncing motion** across the ground texture, roadside trees, and rocks took a
lot of tuning. Rocks and trees had to match `GroundScroller.WorldSpeed` or the
run felt fake. **Camera shake** on rock hits was invisible until I shook along
the camera's local right/up axes instead of world X and Y. A related bug hid
in the **stonewall belt**: the code measuring spacing between wall segments was
probing a lower-detail LOD mesh instead of the LOD0 mesh actually shown up
close, so segments were spaced about 19% too far apart and left visible gaps.
I also had a **wheel-spin bug** where `TickWheelSpin` rebuilt each wheel's
rotation from scratch every frame, silently overwriting the baked FBX
import-correction rotation some vehicle models ship with — the fix was to
compose roll and steer on top of the cached base rotation instead of
replacing it.

Adding a second player was its own project. **Two-player mode** meant
reworking `GameManager` and `PlayerController` to run per-player timers,
control schemes, and elimination instead of one global game-over state, plus
preventing both players from picking the same vehicle. It also broke my
camera setup — a single fixed camera no longer made sense with two vehicles
moving independently, so I built a `GameplayCameraDirector` that frames both
players, repositions to their midpoint, and runs beat-synced transitions
during the countdown before the round starts.

Asset work continued to bite me. Some rock packs rendered **magenta in URP**, so
I had to drop incompatible meshes and stick to stylized prefabs that worked with
my pipeline. I used AI-assisted searching to find low-poly vehicles and rocks
that fit the military road theme after Asset Store browsing and my own attempts
did not pan out.

I also spent time on **UI polish** (vehicle name labels, pause/credits, penalty
popups, leaderboard), **near-miss bonuses**, and **Edit Mode tests** so vehicle
selection and the exit-drive behavior would not break again when I changed
things. A small Inspector typo in an older variable name is still there because
renaming it would have broken serialized references.

Overall the main lessons for Project 4 were about finishing a game, not just
getting one working: normalizing content that was never designed together,
keeping particle and scroll motion believable, extending single-player systems
to support two players without a rewrite, testing the tricky parts, and
managing project size so I could actually ship a clean export.

I really enjoyed pushing the project further, and I still plan to add more
polish after submission.

## What is the version number of Unity that you used to create your Project 4?

I built Project 4 using **Unity 6000.4.7f1**, the same Unity 6 version I used
for Projects 1, 2, and 3.
