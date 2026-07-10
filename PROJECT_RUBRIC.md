# Project 4 Rubric Checklist — CMST 315 (200 pts)

**Course:** Game Design I · **Game:** My Game: Disaster · **Unity:** 6000.4.7f1

> **Critical:** Rubric states the project earns **zero** if the Unity Package file is not attached.

---

## Rubric criteria

| # | Criterion | Pts | What graders look for |
|---|-----------|-----|------------------------|
| 1 | Gameplay improvements | 50 | All **recommended** improvements implemented, **or** deviations documented with justification |
| 2 | Music & sound effects | 50 | Per **Project Design Description** |
| 3 | Particle effects | 50 | Per **Project Design Description** |
| 4 | Submissions | 50 | **Unity Package** + **Build folder** + **Project Reflection** |

Grade bands: Excellent ≥180 · Very Good ≥160 · Good ≥140 · Needs Work ≥120

---

## Criterion 1 — Gameplay improvements (~45–50 / 50)

### Design doc + backlog (implemented)

| Recommendation | Status | Where |
|----------------|--------|-------|
| Keyboard / arrow vehicle control | Met | `PlayerController` |
| Rocks spawn from top, player dodges | Met | `SpawnManager`, `MoveDown` |
| Timer penalty on rock hit | Met | `GameManager.OnPlayerHit` |
| Vehicle accelerates every 10s | Met | `GameManager` difficulty ramp |
| Spawn rate / difficulty increases | Met | `SpawnManager.IncreaseDifficulty` |
| Random rock variety | Met | 12 `rockVisuals` prefabs |
| Impact sound on collision | Met | `impactClip` |

### Polish beyond original design (Project 4 scope)

| Feature | Status |
|---------|--------|
| 8-vehicle picker + `PlayerPrefs` | Implemented |
| Near-miss time bonus (+2s) | Implemented |
| Top-5 leaderboard | Implemented |
| Pause + music toggle | Implemented |
| Game-over exit drive (`IsWorldAnimating`) | Implemented |
| Camera shake on hit | Implemented |
| Evening → daylight lighting | Implemented |
| Edit Mode tests (21) | Implemented |

### Deviations requiring justification document

The game evolved from the design doc’s **2D lateral** prototype into a **3D endless survival** build. Per rubric: document why and how this improves (or equivalently delivers) the intended experience.

**Suggested file:** [`DESIGN_DEVIATIONS.md`](DESIGN_DEVIATIONS.md) (create before submit) covering:

- 4-direction movement vs. left/right only → richer dodging, matches camera view
- Countdown survival vs. reach-goal → fits endless rock stream
- 3D vehicles + scrolling plane vs. 2D sprites → final art requirement, same core loop
- Optional design-doc particles marked “none” → added dust, dirt, rubble, fireworks for feedback

**Estimated score:** 45–50 if deviations doc is included and play is solid.

---

## Criterion 2 — Music & sound (~48–50 / 50)

| Audio | Design doc | In game |
|-------|------------|---------|
| Impact on obstacle hit | Required | `impactClip` (honk) on `GameManager` |
| Background music | Not in original doc | `Climber.wav` — credited in pause UI |
| Rock destroy sound | — | `crushClip` on `Obstacles` prefab |
| UI click | — | `clickClip` on buttons via `GameManager.PlayClick` |
| Music mute | — | Pause overlay toggle + `PlayerPrefs` |

**Near-miss chirp:** `nearMissClip` on `GameManager` (pitched UI click, distinct from button clicks).

**Estimated score:** 48–50

---

## Criterion 3 — Particle effects (~48–50 / 50)

Design doc listed particles as optional / “none.” Project 4 rubric still grades particles at 50 pts — your build **exceeds** the minimum.

| Effect | Trigger | Asset |
|--------|---------|-------|
| Rear dirt spray | While driving / exit drive | `Player` prefab emitters |
| Hit dust burst | Vehicle hits rock | `dustEffectPrefab` |
| Rock rubble | Rock destroyed on hit | `destroyEffectPrefab` on `Obstacles` |
| Fireworks | New #1 best time | `fireworksPrefab` |

**Estimated score:** 48–50

---

## Criterion 4 — Submissions (~15–35 / 50 until complete)

| Required file | Status | Action |
|---------------|--------|--------|
| **Project Reflection** | Done | [`PROJECT_REFLECTION.md`](PROJECT_REFLECTION.md) |
| **Unity Package** | **Missing** | Export from Editor — **required or zero grade** |
| **Build folder** | **Missing** | File → Build Profiles → Build (macOS or Windows) |

### Export steps

**Unity package**

1. Unity → **Assets → Export Package…**
2. Include: `Assets/`, `Packages/manifest.json`, `ProjectSettings/`
3. Exclude: `Library/`, `Temp/`, `Logs/`
4. Name: e.g. `GilbertAnderson_Disaster_Project4.unitypackage`

**Build folder**

1. **File → Build Profiles** (or Build Settings)
2. Confirm `Assets/Scenes/My Game.unity` is in Scenes In Build
3. **Build** to a folder outside the project (e.g. `~/Desktop/Disaster_Build/`)
4. Zip the build folder for LMS upload if required

**Estimated score:** 50 only after all three are submitted.

---

## Estimated total

| State | Score |
|-------|-------|
| **Now** (no package, no build) | ~140–165 — submission criterion blocks Excellent |
| **After package + build + deviations doc** | ~180–195 (Excellent) |

---

## Pre-submit checklist

- [ ] Play-test: Drive → dodge → hit → near-miss → pause → game over → Retry
- [ ] Confirm impact sound, crush sound, music, and click all audible
- [ ] Confirm dirt, dust, and rubble particles visible in play mode
- [ ] Create `DESIGN_DEVIATIONS.md` (or add section to reflection)
- [ ] Export `.unitypackage`
- [ ] Produce build folder and test the standalone player
- [ ] Upload: package + build + `PROJECT_REFLECTION.md` (or paste into LMS)
