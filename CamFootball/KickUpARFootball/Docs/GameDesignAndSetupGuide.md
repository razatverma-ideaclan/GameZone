# KickUp AR Football — MVP Implementation Guide

## 1. Implementation Plan

Build order for the MVP:

1. Create the Unity 2D project and folder structure (`Assets/Scripts`, `Assets/Sprites`, `Assets/Audio`).
2. Set up the camera background (works in Editor via fallback color, on-device via `WebCamTexture`).
3. Create the Ball GameObject with physics (`Rigidbody2D`) and `BallController`.
4. Add `KickInputManager` and confirm mouse-click kicking works in the Editor.
5. Wire up `GameManager` and `UIManager` with the three screens (Start, Gameplay, Game Over).
6. Add `AudioManager` with empty AudioClip slots.
7. Build Infinite Mode end-to-end and playtest until the kick feels good.
8. Add the Basket/Goal object and `BasketGoalController` for Basket Mode.
9. Add PlayerPrefs best-score saving.
10. Test on an Android device, then move on to polish (sprites, sounds, UI styling).

All seven scripts described below are already written and placed in `Assets/Scripts/`.

## 2. Scene Hierarchy

```
Main Camera
CameraBackground        (Quad or RawImage showing camera feed / fallback)
Canvas
  StartPanel
    TitleText
    InstructionText
    InfiniteModeButton
    BasketModeButton
  GameplayPanel
    ScoreText
    BestScoreText
    TimerText
  GameOverPanel
    GameOverText
    FinalScoreText
    BestScoreGameOverText
    RestartButton
    MainMenuButton
Ball
BasketGoal
Managers
  GameManager
  UIManager
  AudioManager
  CameraBackgroundManager
  KickInputManager
```

## 3. Unity Setup Instructions

### 3.1 Project setup
1. Create a new Unity project using the **2D (URP or Built-in, either works)** template.
2. Copy the `Assets/Scripts` folder from this delivery into your project's `Assets` folder.
3. Create empty folders: `Assets/Sprites`, `Assets/Audio`.

### 3.2 Camera Background
Two options — pick one:

**Option A (recommended, simplest): UI RawImage**
1. In `Canvas`, create a `RawImage` named `CameraBackground`, stretch it to fill the whole screen (anchor min 0,0 / max 1,1), and move it to be the **first child** of Canvas so it renders behind everything else.
2. Add the `CameraBackgroundManager` script to a Managers object (or directly on this RawImage).
3. Assign the RawImage to the `backgroundRawImage` field.

**Option B: World-space Quad**
1. Create a `Quad` GameObject named `CameraBackground`, scale/position it to fill the camera's view, and put it on a layer rendered behind the Ball.
2. Add `CameraBackgroundManager`, assign its `Renderer` to `backgroundRenderer`.

Either way, leave `fallbackColor` at its default so the Editor shows a plain green background when no webcam is present.

### 3.3 Ball
1. Create a GameObject named `Ball`.
2. Add components: `SpriteRenderer` (assign a circle sprite, e.g. Unity's built-in `Knob` or `Circle` sprite as a placeholder), `Rigidbody2D`, `CircleCollider2D`.
3. Set the **Tag** to `Ball` (create this tag if it doesn't exist — needed by `BasketGoalController`).
4. Add the `BallController` script.
5. On `Rigidbody2D`: set **Gravity Scale** to match the script's `gravityScale` field (1.5 by default — the script sets this automatically at runtime, but setting it in the Inspector too avoids a one-frame pop). Set **Collision Detection** to `Continuous` to avoid tunneling through the goal trigger at high speed.

### 3.4 Basket/Goal (Basket Mode only)
1. Create a GameObject named `BasketGoal`.
2. Add a `SpriteRenderer` (placeholder: a wide rectangle sprite) and a `BoxCollider2D` with **Is Trigger** checked.
3. Position it near the top of the screen (see recommended values below).
4. Add the `BasketGoalController` script, assign `ballController` (drag the `Ball` object in).
5. Leave this object active in the scene; `GameManager` will enable/disable it based on the selected mode.

### 3.5 Managers
1. Create an empty GameObject named `Managers`.
2. Add child empty GameObjects (or just add all scripts directly onto `Managers` — either works): `GameManager`, `UIManager`, `AudioManager`, `CameraBackgroundManager`, `KickInputManager` (skip this if you attached it under 3.2).
3. Add the corresponding script to each.

### 3.6 UI Canvas
1. Create a `Canvas` (Screen Space - Overlay) with an `EventSystem` (Unity adds one automatically).
2. Build the three panels and their children as listed in the hierarchy above, using Unity's default `Text` and `Button` UI elements.
3. Set `GameOverPanel` and `GameplayPanel` inactive by default in the Editor (Start Panel active) — `UIManager` controls visibility at runtime, so the initial state just needs to be sensible for editing.

## 4. Inspector Assignment Guide

**GameManager**
- `uiManager` → the `UIManager` component
- `ballController` → the `Ball` object's `BallController`
- `basketGoalController` → the `BasketGoal` object's `BasketGoalController`

**CameraBackgroundManager**
- `backgroundRawImage` (or `backgroundRenderer`) → your chosen background object
- `fallbackColor` → leave default or pick a field-green color
- `fallbackTexture` → optional placeholder image

**BallController**
- Tune `kickForce`, `horizontalSwipeForce`, `gravityScale`, bounds fields (see recommended values below)
- `startPosition` → near bottom-center, e.g. `(0, -2, 0)`

**KickInputManager**
- `ballController` → the `Ball` object's `BallController`

**BasketGoalController**
- `ballController` → the `Ball` object's `BallController`
- `ballTag` → `Ball`
- `useTimer` → checked for Mode 2's optional 60s timer

**UIManager**
- `startPanel`, `gameplayPanel`, `gameOverPanel` → the three Canvas panels
- All `Text` fields → their matching UI Text objects
- All `Button` fields → their matching UI Button objects
(No manual OnClick() wiring needed — `UIManager.Start()` wires buttons in code. You may also wire them manually in the Inspector if preferred; both work.)

**AudioManager**
- Leave AudioClip fields empty for now; assign `kickSound`, `scoreSound`, `gameOverSound`, `buttonClickSound`, `backgroundMusic` once you have audio files.

## 5. Recommended Gameplay Values

| Setting | Recommended value |
|---|---|
| Gravity scale | 1.5 |
| Kick force (upward impulse) | 12 |
| Horizontal swipe force | 4 |
| Ball size (scale) | 0.5–0.7 world units diameter |
| Ball start position | (0, -2, 0) |
| Side bounce strength | 0.6 (60% velocity retained) |
| Side bounds padding | 0.3 |
| Basket/goal size | ~2 wide x 0.6 tall |
| Basket/goal position | (0, 3.5, 0) — near top of screen |
| Basket Mode timer | 60 seconds |
| Input detection radius | 1.5 world units (~80px minimum on screen) |
| Game over bottom boundary | y = -6 (adjust to just below the visible screen for your camera size/orthographic size) |

These are starting points — orthographic camera size and screen aspect ratio will affect what "near top" and "below screen" mean in world units, so playtest and adjust `gameOverBottomY` and the basket's Y position to match your camera setup.

## 6. Testing Guide

1. **Test in Editor with mouse**: Press Play, click and hold near the ball, drag, and release. A short click = tap (kicks straight up); a longer drag = swipe (adds horizontal direction).
2. **Test on Android with touch**: Build and deploy to a device (see Section 7). Tap/swipe near the ball exactly as with the mouse — `KickInputManager` automatically switches to touch input on device via `#if UNITY_EDITOR` conditional compilation.
3. **Check camera permission**: On first launch, Android should prompt for camera access. If it doesn't, verify `Player Settings > Android > Other Settings` has no manifest issues, and check `Assets/Plugins/Android/AndroidManifest.xml` includes `<uses-permission android:name="android.permission.CAMERA" />` (Unity's `Permission.RequestUserPermission` call handles the runtime prompt, but the manifest entry must exist too — see Section 7.2).
4. **Test camera background failure**: In the Editor (no webcam), confirm the fallback color/texture shows instead of a black screen — this is expected and by design.
5. **Debug ball not moving**: Confirm `Rigidbody2D` is present and not set to `Kinematic`, confirm `gravityScale` isn't 0, and confirm `GameManager.CurrentState` is `Playing` (kicks are ignored outside Playing state).
6. **Debug tap/swipe not detecting**: Confirm `KickInputManager.ballController` is assigned, and increase `inputDetectionRadius` if taps near the ball aren't registering.
7. **Debug score not increasing**: In Infinite Mode, score increases on every `Kick()` call — confirm `GameManager.Instance` isn't null and `CurrentMode` is set correctly. In Basket Mode, confirm the Ball has the `Ball` tag and the goal's collider has `Is Trigger` checked.
8. **Debug game over not triggering**: Confirm `gameOverBottomY` is above where the ball actually goes off-screen for your camera's orthographic size, and check the Console for null reference errors on `GameManager.Instance`.

## 7. Android Build Guide

1. **Build settings**: `File > Build Settings > Android > Switch Platform`. Under `Player Settings`:
   - Minimum API Level: 24+ (Android 7.0) recommended.
   - Scripting Backend: IL2CPP, Target Architectures: ARM64.
2. **Camera permission**: Unity auto-generates a manifest, but for camera access ensure `Player Settings > Publishing Settings > Custom Main Manifest` is enabled if you need to manually add `<uses-permission android:name="android.permission.CAMERA" />` and `<uses-feature android:name="android.hardware.camera" android:required="true" />`. The `CameraBackgroundManager` script also requests permission at runtime via `UnityEngine.Android.Permission`.
3. **Screen orientation**: Set to **Portrait** (`Player Settings > Resolution and Presentation > Orientation`) — this matches typical one-handed mobile juggling gameplay.
4. **Resolution / aspect handling**: Use a `Canvas Scaler` set to **Scale With Screen Size**, reference resolution ~1080x1920, Match slider around 0.5 to balance width/height scaling across devices.
5. **Responsive UI**: Anchor all UI elements properly (e.g. score text anchored top-left/top-center, buttons anchored center) rather than using fixed pixel positions, so layouts adapt to different phone aspect ratios.

## 8. Future Foot Detection Plan

The architecture already isolates input from gameplay so this upgrade won't require rewriting the game:

1. Add a computer-vision/pose-detection package (e.g. a Unity ML/pose plugin or a native Android/iOS pose SDK) that reads the camera feed already being displayed by `CameraBackgroundManager`.
2. Create a new script, `FootDetectionInputManager.cs`, following the same pattern as `KickInputManager.cs`: detect foot/keypoint movement, compute a kick direction and force, then call `ballController.Kick(direction, forceMultiplier)` — the exact same method Signature used today.
3. Swap `KickInputManager` for `FootDetectionInputManager` on the `Managers` GameObject (or run both side-by-side and let the player choose an input mode in settings).
4. No changes are needed to `BallController`, `GameManager`, `UIManager`, `BasketGoalController`, or `AudioManager` — they only know about `Kick()` calls, not where those calls come from.
