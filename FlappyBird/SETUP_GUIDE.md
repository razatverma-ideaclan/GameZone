# Flappy Bird Prototype — Unity 2D Setup Guide

Scripts are in `Assets/Scripts/`. Copy this whole `FlappyBird` folder's contents into your Unity project (or copy just the `Assets/Scripts` folder in).

## 1. Project Setup Steps

1. Open Unity Hub → New Project → **2D (Core)** template. Name it `FlappyBird`.
2. In Build Settings (`File > Build Settings`), for now leave platform as PC (you'll switch to Android/iOS/WebGL later — no code changes needed).
3. Copy the 5 scripts from `Assets/Scripts` in this folder into your project's `Assets/Scripts` folder.
4. Set Project Settings for mobile-friendly physics:
   - `Edit > Project Settings > Physics 2D` → leave gravity at default; per-object gravity is controlled in `BirdController`.
5. Create these tags (`Edit > Project Settings > Tags and Layers`): `Bird`, `Pipe`, `Ground`.

## 2. Scene Hierarchy

```
SampleScene
├── Main Camera
├── GameManager            (empty GameObject)
├── Background             (optional, sprite/color)
├── Ground                 (rectangle sprite, BoxCollider2D, Tag: Ground)
├── Bird                    (circle sprite, Rigidbody2D, CircleCollider2D, Tag: Bird)
├── PipeSpawner            (empty GameObject)
├── PipePair (Prefab)      — created once, then dragged into Assets/Prefabs
│   ├── PipeTop            (rectangle sprite, BoxCollider2D, Tag: Pipe)
│   ├── PipeBottom         (rectangle sprite, BoxCollider2D, Tag: Pipe)
│   └── ScoreZone          (empty child, BoxCollider2D [Is Trigger] between the two pipes)
└── Canvas (UI)
    ├── ScoreText          (Text, top center)
    ├── StartPanel
    │   └── StartText / "Tap to Play"
    └── GameOverPanel
        ├── GameOverText
        └── RestartButton
```

## 3. Required GameObjects and Components

| GameObject | Components | Notes |
|---|---|---|
| **Bird** | SpriteRenderer (circle sprite: `Assets > Create > Sprites > Circle` or use built-in `Knob`/`Circle`), Rigidbody2D, CircleCollider2D, `BirdController.cs` | Tag = `Bird`. Set Rigidbody2D **Body Type** starts as Kinematic (script switches it to Dynamic on game start). |
| **Ground** | SpriteRenderer (Square sprite, scaled into a long thin rectangle), BoxCollider2D | Tag = `Ground`. Position at bottom of screen. |
| **PipeTop / PipeBottom** | SpriteRenderer (Square sprite, scaled into a tall rectangle), BoxCollider2D | Tag = `Pipe`. `PipeBottom` faces up, `PipeTop` flipped/rotated 180° or just scaled and positioned above the gap. |
| **PipePair (parent)** | Empty GameObject holding PipeTop + PipeBottom + ScoreZone as children, `PipeMover.cs` on the parent | Save as a Prefab in `Assets/Prefabs`. |
| **ScoreZone** | BoxCollider2D (Is Trigger = ✅), `ScoreTrigger.cs` | Positioned in the gap between top/bottom pipes, thin on the X axis. |
| **PipeSpawner** | Empty GameObject, `PipeSpawner.cs` | Assign the PipePair prefab in the Inspector. |
| **GameManager** | Empty GameObject, `GameManager.cs` | Assign `Bird`, `PipeSpawner`, `ScoreText`, `StartPanel`, `GameOverPanel` references in Inspector. |
| **Canvas > ScoreText** | UI Text (or TextMeshPro) | Assign to GameManager's `scoreText` field. |
| **Canvas > StartPanel** | UI Panel/Image + Text | Assign to GameManager's `startPanel` field. |
| **Canvas > GameOverPanel** | UI Panel/Image + Text + Button | Assign to GameManager's `gameOverPanel` field. |
| **RestartButton** | Button | OnClick() → `GameManager.RestartGame()` |

## 4. Full C# Scripts

See `Assets/Scripts/`:
- `BirdController.cs`
- `PipeSpawner.cs`
- `PipeMover.cs`
- `ScoreTrigger.cs`
- `GameManager.cs`

## 5. Exactly Where to Attach Each Script

- **BirdController.cs** → on the `Bird` GameObject.
- **PipeMover.cs** → on the root `PipePair` GameObject (the prefab parent, not the individual pipe sprites).
- **ScoreTrigger.cs** → on the `ScoreZone` child of `PipePair`, on the collider set to "Is Trigger."
- **PipeSpawner.cs** → on the `PipeSpawner` empty GameObject in the scene. Drag the `PipePair` prefab into its `pipePairPrefab` field.
- **GameManager.cs** → on the `GameManager` empty GameObject in the scene. Drag in `Bird`, `PipeSpawner`, `ScoreText`, `StartPanel`, `GameOverPanel` references.

## 6. Recommended Tuning Values

Start here, then tweak by feel:

| Value | Recommended |
|---|---|
| Bird `gravityScale` | `1.5` |
| Bird `flapForce` | `5` |
| Pipe `speed` (PipeMover) | `3` |
| Pipe `spawnInterval` | `1.5` seconds |
| Pipe gap size (distance between PipeTop and PipeBottom) | `2.5` units |
| Gap Y range (`minGapY` / `maxGapY` on PipeSpawner) | `-2` to `2` |

Rule of thumb: if the bird feels too "floaty," raise `gravityScale`. If flaps feel too weak/strong, adjust `flapForce` in steps of 0.5. If the game feels too easy/hard, adjust `spawnInterval` and gap size together rather than one alone.

## 7. How to Test in Unity Editor

1. Press **Play**.
2. You should see the Start screen; click, tap, or press Space to begin.
3. Bird should fall under gravity; each click/tap/Space flaps it upward.
4. Pipes should spawn from the right and move left at a steady speed.
5. Passing through a pipe gap should increment the score display.
6. Hitting a pipe or the ground should trigger the Game Over panel.
7. Click Restart to reload the scene and confirm it resets to the Start screen with score at 0.
8. For mobile testing, use `Window > General > Device Simulator` to preview touch input, or build to an Android/iOS device.
9. For WebGL, switch platform in Build Settings to WebGL and do a test build — no code changes are required since input handling already checks mouse, touch, and keyboard.

## 8. Common Bugs and Fixes

| Bug | Likely Cause | Fix |
|---|---|---|
| Bird falls through the ground | Ground collider missing or not tagged `Ground` | Ensure BoxCollider2D is present (not trigger) and tag is exactly `Ground`. |
| Score doesn't increase | ScoreZone collider isn't set to "Is Trigger," or Bird isn't tagged `Bird` | Check both; also confirm `ScoreTrigger.cs` is on the ScoreZone child, not the pipe itself. |
| Game Over triggers immediately on start | Bird's Rigidbody2D is Dynamic before the game starts, so it falls into the ground pre-launch | Confirm `BirdController.Start()` sets `rb.bodyType = Kinematic` and that `EnableControl()` is only called from `GameManager.StartGame()`. |
| Pipes never destroy / pile up in the Hierarchy | `destroyXPosition` in `PipeMover` is set to a value the pipe never reaches (e.g., too far left, or camera view is narrower/wider than expected) | Adjust `destroyXPosition` to a few units left of the camera's visible left edge. |
| Bird flaps twice per tap on mobile | Both `Input.GetMouseButtonDown` and touch detection firing for the same tap | This is expected on some platforms since mouse input simulates touch in the Editor; it will not double-fire on an actual device. If it does, you can gate one path with `#if UNITY_EDITOR`. |
| Restart button does nothing | Button's OnClick() event not wired up, or GameManager reference missing | Select the RestartButton, in the OnClick() list drag the GameManager object and choose `GameManager.RestartGame`. |
| Pipes spawn overlapping the bird immediately | `spawnXPosition` too close to the bird's X position | Increase `spawnXPosition` so pipes start off-screen to the right. |
| Score keeps incrementing repeatedly for one pipe | `ScoreTrigger` not guarding against multiple triggers | Confirm the `scored` boolean guard in `ScoreTrigger.cs` is intact (it is, by default). |
