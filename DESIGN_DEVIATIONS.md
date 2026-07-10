# Design Deviations — Project 4 Justification

**Game:** My Game: Disaster  
**Author:** Gilbert Anderson  
**Unity:** 6000.4.7f1

This document explains where the shipped game diverges from the original Project Design Document and course prototype, and why those changes improve player experience (or achieve the same goals with comparable effort).

---

## 1. Four-direction movement instead of left/right only

**Design doc:** Keyboard and arrow keys move the player left and right.  
**Shipped:** WASD and arrows move in four directions (including forward/back and diagonals), projected onto the camera’s screen axes.

**Justification:** The camera is angled top-down, not pure 2D. Letting the player steer in all directions on the ground plane matches what they see on screen, makes dodging rocks feel more natural, and does not reduce difficulty because rocks still stream from the top of the field.

---

## 2. Endless survival instead of reaching a goal

**Design doc:** Avoid rocks; timer decreases on hits; game ends when time runs out.  
**Shipped:** No finish-line goal. The player survives as long as possible on a scrolling road with a countdown timer, dodge stats, and a local leaderboard.

**Justification:** Random rock spawning fits an endless loop better than a single goal cube. The timer, hit penalty, and game-over flow from the design doc are unchanged — only the win condition shifted from “reach the goal” to “survive longest,” which supports replay and difficulty ramping every 10 seconds.

---

## 3. 3D models and physics instead of 2D sprites and Rigidbody2D

**Design doc backlog:** 2D vehicle sprite, rock prefab with BoxCollider2D.  
**Shipped:** Imported 3D military vehicles and stylized rock meshes with 3D `Rigidbody`, `MovePosition`, and fitted colliders.

**Justification:** Final art assets replaced primitives as required by later project milestones. 3D meshes read better at scale, support varied rock silhouettes, and still deliver the same interactions: spawn, move down-screen, collide, destroy. `Rigidbody` movement with frozen rotation solved tipping and boundary issues that pure mass/gravity did not.

---

## 4. Particle effects beyond “optional / none”

**Design doc:** Impact sound required; particles listed as optional with no implementation planned.  
**Shipped:** Rear dirt emitters, hit dust, rock rubble on destroy, fireworks on a new best time.

**Justification:** Particles communicate speed, impacts, and rewards without extra UI reading. They reinforce the off-road military theme and make hits and near-misses easier to read during fast scrolling. Implementation cost was low using Unity’s particle prefabs already in the Create With Code library.

---

## 5. Additional audio beyond a single impact clip

**Design doc:** One impact sound on obstacle collision.  
**Shipped:** Impact honk, rock crush on destroy, UI click feedback, near-miss reward chirp, looping background music with mute toggle.

**Justification:** Music sets pace for a survival arcade loop. Separate crush and click sounds help the player distinguish player-hit vs. rock-cleared vs. menu actions. All clips are short, mixed at reasonable volumes, and credited in the pause screen.

---

## 6. Vehicle selection and persistence

**Not in original design doc.**  
**Shipped:** Eight vehicles on the start screen, name label, choice saved in `PlayerPrefs`.

**Justification:** Reusing multiple free vehicle assets gives variety without new mechanics. Normalizing scale and collider fit keeps gameplay fair. This is polish that increases replay value with minimal scope creep.

---

## 7. Near-miss bonus, pause, and exit drive

**Not in original design doc.**  
**Shipped:** +2s for close dodges, Esc pause, vehicle drives off-screen after game over while the world keeps scrolling.

**Justification:** Near-misses reward skillful play without removing challenge. Pause is expected on desktop builds. The exit drive avoids an abrupt freeze when the timer hits zero and sells the “drive away” moment before the game over panel.

---

## Summary

Core design-doc promises — top-down driving, rock avoidance, timer penalties, difficulty ramp, Disaster title, game over on timeout, impact audio — are all met. Deviations expand presentation and replay value while keeping the same skill-based loop. No deviation was made to reduce scope; each addresses play feel, final assets, or Project 4 polish requirements.
