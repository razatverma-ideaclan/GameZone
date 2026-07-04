# Flappy Bird Multi-Theme Development & AI Guide

This document summarizes the architecture, key systems, and visual theme implementations added to the Flappy Bird Unity project. If a new AI assistant takes over, this guide provides the necessary context to continue development without breaking existing systems.

---

## 1. Core Architecture Overview

The codebase is split into gameplay scripts and a procedural editor scene builder:

### Gameplay Components
* **[GameManager.cs](file:///Users/razatverma/Documents/GameZone/FlappyBird/Assets/Scripts/GameManager.cs)**: Manages game states (Start, Playing, GameOver). Handles bottom navigation bar callbacks, toggles between the Lobby UI (`lobbyPanel`) and Heroes UI (`heroesPanel`), manages fading toast notifications, and coordinates the dual-state Play/Home button logic.
* **[BirdController.cs](file:///Users/razatverma/Documents/GameZone/FlappyBird/Assets/Scripts/BirdController.cs)**: Controls player physics, wing-flap animations, bobbing idle effects, and collision death. Holds a list of 3 selectable skins (`skins`) which can be selected via `SetSkin(index)`.
* **[ThemeData.cs](file:///Users/razatverma/Documents/GameZone/FlappyBird/Assets/Scripts/Themes/ThemeData.cs)**: A `ScriptableObject` storing visuals and audio for a theme. Holds 3 player sprites, obstacle/ground sprites, sound effects, background music, and solid sky colors.
* **[ThemeApplier.cs](file:///Users/razatverma/Documents/GameZone/FlappyBird/Assets/Scripts/Themes/ThemeApplier.cs)**: Applies the active `ThemeData` to all active elements in the scene (player skins, pipes, background sky camera color tints, ground dirt/grass, and audio).

### Editor Tools
* **[FlappyBirdSceneBuilder.cs](file:///Users/razatverma/Documents/GameZone/FlappyBird/Assets/Editor/FlappyBirdSceneBuilder.cs)**: An Editor script (**Tools > Flappy Bird > Build Scene**) that procedurally generates the entire scene hierarchy, draws 8-bit retro textures, synthesizes sound effect clips, creates ScriptableObject assets, instantiates UI buttons, and saves the active scene to `Assets/Scenes/SampleScene.unity`.

---

## 2. Implementations Completed Today (July 3, 2026)

### Fixed Particle Trail Black/Blue Square Bug
* **Issue**: The bird's particle trail rendered as a solid blocky square instead of soft cloud particles.
* **Fix**: Updated `psRenderer.material` in `FlappyBirdSceneBuilder.cs` to load Unity's built-in `Default-Particle.mat` resource instead of creating a generic untextured material.

### Lobby UI vs. Heroes Grid UI
* Created the **LobbyPanel** containing the floating per-letter title, high score badge, and floating World Name banner.
* Created the **HeroesPanel** containing a full-screen vertical container:
  * A **Header Bar** showing skin progression (e.g. `CLASSIC HEROES (1/3)`).
  * A **3-card Grid Layout** featuring a dark capsule card, a centered preview image of that theme's skin, a bold name label, and a gold medal selection checkmark.
* Clicking any card triggers `GameManager.SelectHero(i)`, which saves your choice and updates the checkmark.

### Dual-State Bottom Center Button (Play / Home)
* When in the Lobby, the center button displays a **Play arrow (`▶`)** and starts the gameplay.
* When you click the **HEROES** button in the bottom bar, the lobby UI and bird character hide, the Heroes screen opens, and the center button changes to a **Home icon (`🏠`)**.
* Clicking the **Home button (`🏠`)** returns you to the lobby, shows the central floating bird scaled up to a **large preview size (`2.0f`)** with your selected skin, and reverts the center button back to **Play (`▶`)**.

### 21 Unique Character Skins (3 per Theme)
* Designed a custom procedural texture painter (`GenerateThemeSkinTexture`) inside `FlappyBirdSceneBuilder.cs` that draws 3 theme-appropriate skins for all 7 themes:
  * **Classic**: Yellow Bird, Blue Bird, Red Bird (with animated flap-wings)
  * **Space**: Rocket, Cosmic UFO, Communication Satellite
  * **Football**: Soccer Ball, Basketball, Tennis Ball
  * **Dragon**: Red Drake, Emerald Dragon, Gold Wyvern
  * **Fish**: Goldfish, Bull Shark, Pink Jellyfish
  * **Bee**: Honey Bee, Ladybug, Butterfly
  * **Ninja**: Shadow Ninja, Crimson Ninja, Silver Shinobi

---

## 3. How to Rebuild and Test the Scene

If you modify the UI layout, procedural textures, or scene wiring:
1. Ensure Unity is **NOT in Play Mode** (rebuilding inside Play Mode will not persist changes).
2. Go to the Unity Editor top menu bar.
3. Select **Tools > Flappy Bird > Build Scene**.
4. The script will save the generated scene to `Assets/Scenes/SampleScene.unity`.
5. Press **Play** in Unity to run and test immediately.
