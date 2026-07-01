# Subway Surfer Prototype — Unity 3D Setup Guide

A simple-shapes 3D endless runner: capsule player, cube obstacles, plane ground. No external assets.

## Fastest Path: One-Click Scene Build

This folder is already a valid Unity project (`ProjectSettings/`, `Packages/manifest.json`).

1. Unity Hub → **Open** → select this `SubwaySurfer` folder.
2. Wait for the Editor to open (first launch resolves packages — needs internet).
3. Menu bar → **Tools > Subway Surfer > Build Scene**.
4. This creates the Player (capsule + CharacterController + follow camera), Ground, three obstacle prefabs (red = must switch lanes, orange = must jump, purple = must slide), the ObstacleSpawner, UI (score, start panel, game-over panel + restart), and GameManager — fully wired — and saves `Assets/Scenes/SampleScene.unity`.
5. Press **Play**.

Re-run the menu item any time to rebuild the scene from scratch.

## Controls

- **Lane switch:** Left/Right arrow keys, A/D, or swipe left/right.
- **Jump:** Up arrow, W, Space, or swipe up.
- **Slide:** Down arrow, S, or swipe down.

## Required GameObjects and Components

| GameObject | Components | Notes |
|---|---|---|
| **Player** | Capsule mesh, `CharacterController`, `PlayerController.cs` | No tag needed. Camera is a child of this object so it follows automatically. |
| **Main Camera** | Camera, AudioListener | Child of Player, offset above/behind, angled down. |
| **Ground** | Plane mesh | Long thin plane covering all 3 lanes. |
| **BarrierObstacle / LowObstacle / HighObstacle (prefabs)** | Cube mesh, `BoxCollider` (Is Trigger ✅) | Tag = `Obstacle`. Sized/positioned so Low can be jumped over and High can be slid under; Barrier spans full height and must be avoided by lane switch. |
| **ObstacleSpawner** | Empty GameObject, `ObstacleSpawner.cs` | References Player transform + the 3 obstacle prefabs. |
| **GameManager** | Empty GameObject, `GameManager.cs` | References Player, ObstacleSpawner, ScoreText, StartPanel, GameOverPanel. |
| **Canvas > ScoreText / StartPanel / GameOverPanel** | UI Text / Image / Button | Same pattern as the FlappyBird project. |

## Recommended Tuning Values

| Value | Recommended |
|---|---|
| `laneDistance` | `3` |
| `forwardSpeed` (starting) | `8` |
| `speedIncreaseRate` | `0.15` per second |
| `jumpForce` | `9` |
| `gravity` | `-25` |
| `slideDuration` | `0.7` seconds |
| `spawnGap` (distance between obstacle rows) | `14` |
| `spawnLookahead` | `40` |

## How to Test

1. Press Play. You'll see the Start screen ("Tap to Run").
2. Click/tap or press Space to start running.
3. Use lane-switch, jump, and slide to avoid the three obstacle colors.
4. Hitting any obstacle without the right action triggers Game Over.
5. Score increases automatically based on distance traveled.
6. Click Restart to reload and confirm it resets to the Start screen.

## Common Bugs and Fixes

| Bug | Likely Cause | Fix |
|---|---|---|
| Player falls through the ground | Ground plane's mesh collider missing (shouldn't happen with `CreatePrimitive(PrimitiveType.Plane)`, but check if you replaced it) | Re-add a `MeshCollider` to Ground, not a trigger. |
| Player never dies on obstacles | Obstacle `BoxCollider` isn't set to Is Trigger, or missing `Obstacle` tag | Check both on all 3 prefabs. |
| Jump/slide feels wrong | `jumpForce`/`gravity`/`slideDuration` need tuning for your taste | Adjust in small increments and retest. |
| Camera doesn't follow | Camera isn't parented to Player | Re-run **Tools > Subway Surfer > Build Scene**, or manually drag Main Camera under Player in the Hierarchy. |
| Restart does nothing | Button OnClick() not wired to `GameManager.RestartGame` | Re-run the scene builder, or wire it manually in the Inspector. |
