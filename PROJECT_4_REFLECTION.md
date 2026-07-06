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
should still move.

**Syncing motion** across the ground texture, roadside trees, and rocks took a
lot of tuning. Rocks and trees had to match `GroundScroller.WorldSpeed` or the
run felt fake. **Camera shake** on rock hits was invisible until I shook along
the camera's local right/up axes instead of world X and Y.

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
keeping particle and scroll motion believable, testing the tricky parts, and
managing project size so I could actually ship a clean export.

I really enjoyed pushing the project further, and I still plan to add more
polish after submission.

## What is the version number of Unity that you used to create your Project 4?

I built Project 4 using **Unity 6000.4.7f1**, the same Unity 6 version I used
for Projects 1, 2, and 3.
