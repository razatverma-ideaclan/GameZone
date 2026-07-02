using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click scene builder for the Flappy Bird prototype.
/// Run from the menu: Tools > Flappy Bird > Build Scene.
/// Creates the Bird, Ground, Pipe prefab, PipeSpawner, UI Canvas, and
/// GameManager, wires all references, and saves a "SampleScene" scene.
/// Safe to re-run — it wipes and rebuilds the active scene each time.
/// </summary>
public static class FlappyBirdSceneBuilder
{
    private const string SpriteFolder = "Assets/Sprites";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string SceneFolder = "Assets/Scenes";
    private const string ScenePath = SceneFolder + "/SampleScene.unity";

    [MenuItem("Tools/Flappy Bird/Build Scene")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Flappy Bird", "Stopping Play Mode first. Click 'Tools > Flappy Bird > Build Scene' again once it stops.", "OK");
            EditorApplication.isPlaying = false;
            return;
        }

        EnsureFolders();
        EnsureTags();
        LockPortraitOrientation();

        // Force-regenerate only OUR OWN generated sprites every run (so a previous
        // broken import can't linger) — without touching anything else you've
        // added under Assets/Sprites, like a UI subfolder with real art in it.
        AssetDatabase.DeleteAsset($"{SpriteFolder}/Square.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/Bird.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/Background.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/ButtonPill.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/TapBubble.png");
        EnsureFolders();

        // Resolutions bumped well above the old values (2-3x) across the board —
        // on high-density mobile screens the old textures were being stretched
        // far past their native pixel size, which read as blur. Point filtering
        // on the structural/pixel-art pieces (pipes, ground, grass, background)
        // also removes the soft bilinear haze so edges stay crisp, closer to a
        // clean vector-style look even though these are still raster sprites
        // (true vector/SVG import would need Unity's separate Vector Graphics
        // package, which is a much bigger integration than this prototype needs).
        Sprite squareSprite = GetOrCreateSprite("Square", 128, 128, (w, h) => GenerateSquareTexture(w), 128);
        Sprite birdSprite = GetOrCreateSprite("Bird", 320, 320, (w, h) => GenerateBirdTexture(w), 320);
        const float bgWorldWidth = 20f;
        Sprite backgroundSprite = GetOrCreateSprite("Background", 1536, 864, (w, h) => GenerateBackgroundTexture(w, h), 1536f / bgWorldWidth, FilterMode.Point);
        Sprite pillButtonSprite = GetOrCreateSprite("ButtonPill", 320, 120, (w, h) => GenerateRoundedRectTexture(w, h, 30, new Color(0.35f, 0.78f, 0.95f), new Color(0.06f, 0.28f, 0.35f), 6), 320);
        Sprite tapBubbleSprite = GetOrCreateSprite("TapBubble", 220, 90, (w, h) => GenerateRoundedRectTexture(w, h, 40, new Color(0.95f, 0.4f, 0.15f), new Color(0.35f, 0.1f, 0.02f), 6), 220);
        Sprite scoreBadgeSprite = GetOrCreateSprite("ScoreBadge", 200, 150, (w, h) => GenerateRoundedRectTexture(w, h, 24, new Color(1f, 1f, 1f, 0.92f), new Color(0.15f, 0.15f, 0.15f, 0.85f), 5), 200);
        // Warm wooden-plank look for the Game Over score/best boxes.
        Sprite scorePlankSprite = GetOrCreateSprite("ScorePlank", 260, 200, (w, h) => GenerateRoundedRectTexture(w, h, 22, new Color(0.62f, 0.44f, 0.24f), new Color(0.28f, 0.16f, 0.06f), 7), 260);
        // All four kept perfectly square (bounds 1x1 at scale 1) so the existing
        // non-uniform transform.localScale stretching (unchanged from before)
        // produces exactly the intended world-space size, same as the old
        // flat-color "Square" sprite did — only the pixel content is new.
        Sprite pipeBodySprite = GetOrCreateSprite("PipeBody", 256, 256, (w, h) => GeneratePipeBodyTexture(w, h), 256, FilterMode.Point);
        Sprite pipeCapSprite = GetOrCreateSprite("PipeCap", 256, 256, (w, h) => GeneratePipeCapTexture(w, h), 256, FilterMode.Point);
        Sprite groundDirtSprite = GetOrCreateSprite("GroundDirt", 256, 256, (w, h) => GenerateGroundDirtTexture(w, h), 256, FilterMode.Point);
        Sprite grassSprite = GetOrCreateSprite("Grass", 256, 256, (w, h) => GenerateGrassTexture(w, h), 256, FilterMode.Point);
        AssetDatabase.Refresh(); // pick up the synthesized .wav clips generated outside the Editor
        // All sound effects below are our own synthesized clips — the earlier
        // downloaded Flap.ogg / ButtonClick.ogg are no longer used anywhere.
        AudioClip flapClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Flap.wav");
        AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ButtonClick.wav");
        AudioClip scoreClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Score.wav");
        AudioClip hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Hit.wav");
        AudioClip landClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Land.wav");

        // Start from a fresh empty scene so re-running this is safe.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera();
        BuildBackground(backgroundSprite);
        GameObject ground = BuildGround(groundDirtSprite, grassSprite);
        GameObject bird = BuildBird(birdSprite, flapClip, hitClip, landClip);
        GameObject pipePairPrefab = BuildPipePairPrefab(pipeBodySprite, pipeCapSprite);
        GameObject pipeSpawnerGO = BuildPipeSpawner(pipePairPrefab);
        BuildEventSystem();
        Canvas canvas = BuildCanvas();
        GameObject scoreTextGO = BuildScoreText(canvas.transform, scoreBadgeSprite);
        GameObject startPanel = BuildStartPanel(canvas.transform, pillButtonSprite, tapBubbleSprite, out Button startButton);
        GameObject gameOverPanel = BuildGameOverPanel(canvas.transform, pillButtonSprite, scorePlankSprite, out Button menuButton, out Button retryButton, out GameObject gameOverScoreText, out GameObject gameOverBestText);
        GameObject gameManagerGO = BuildGameManager(bird, pipeSpawnerGO, scoreTextGO, startPanel, gameOverPanel, clickClip, scoreClip, gameOverScoreText, gameOverBestText);

        // Wire the buttons now that GameManager exists.
        GameManager gm = gameManagerGO.GetComponent<GameManager>();
        UnityEventTools.AddPersistentListener(startButton.onClick, gm.StartGame);
        UnityEventTools.AddPersistentListener(menuButton.onClick, gm.RestartGame);
        UnityEventTools.AddPersistentListener(retryButton.onClick, gm.RetryGame);

        if (!Directory.Exists(SceneFolder))
        {
            Directory.CreateDirectory(SceneFolder);
        }
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        EditorUtility.DisplayDialog("Flappy Bird", "Scene built and saved to " + ScenePath + ".\nPress Play to test.", "OK");
    }

    // ---------- Folders / Tags ----------

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(SpriteFolder)) AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    private static void EnsureTags()
    {
        AddTag("Bird");
        AddTag("Pipe");
        AddTag("Ground");
    }

    private static void LockPortraitOrientation()
    {
        // Applies to Android/iOS builds. Forces strict portrait — no landscape rotation allowed.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
    }

    private static void AddTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return; // already exists
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    // ---------- Importing real downloaded assets (button art, sound clips) ----------

    /// <summary>
    /// Makes sure a PNG copied into the project (outside Unity's own import
    /// pipeline) is actually recognized and configured as a UI-ready Sprite.
    /// Returns null if the file isn't there yet — every call site handles that
    /// gracefully by simply leaving the default look in place.
    /// </summary>
    private static Sprite EnsureImportedAsSprite(string path)
    {
        AssetDatabase.Refresh(); // pick up files copied in from outside the Editor

        if (!File.Exists(path)) return null;

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) return null;

        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------- Sprite generation (original, procedurally drawn art — no external assets) ----------

    private static Sprite GetOrCreateSprite(string name, int width, int height, System.Func<int, int, Texture2D> generator, float pixelsPerUnit, FilterMode filterMode = FilterMode.Bilinear)
    {
        string path = $"{SpriteFolder}/{name}.png";

        Texture2D tex = generator(width, height);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single; // required or no Sprite sub-asset is generated
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = filterMode;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Texture2D GenerateSquareTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBirdTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color outline = new Color(0.25f, 0.14f, 0.02f);
        Color bodyColor = new Color(1f, 0.83f, 0.15f);
        Color bellyColor = new Color(1f, 0.95f, 0.6f);
        Color wingColor = new Color(0.95f, 0.6f, 0.08f);
        Color beakColor = new Color(0.95f, 0.35f, 0.05f);

        Vector2 bodyCenter = new Vector2(size * 0.45f, size * 0.5f);
        float bodyRadius = size * 0.34f;
        float outlineRadius = bodyRadius + size * 0.03f;
        Vector2 bellyCenter = new Vector2(size * 0.4f, size * 0.35f);
        float bellyRadius = size * 0.21f;
        Vector2 wingCenter = new Vector2(size * 0.32f, size * 0.48f);
        float wingRx = size * 0.17f;
        float wingRy = size * 0.13f;
        Vector2 eyeCenter = new Vector2(size * 0.6f, size * 0.68f);
        float eyeRadius = size * 0.155f;
        float eyeOutlineRadius = eyeRadius + size * 0.02f;
        Vector2 pupilCenter = eyeCenter + new Vector2(size * 0.04f, size * 0.02f);
        float pupilRadius = size * 0.06f;
        Vector2 beakTop = new Vector2(size * 0.72f, size * 0.58f);
        Vector2 beakBottom = new Vector2(size * 0.72f, size * 0.40f);
        Vector2 beakTip = new Vector2(size * 0.95f, size * 0.49f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                Color pixel = clear;
                bool beakArea = PointInTriangle(p, beakTop, beakBottom, beakTip) ||
                                 PointInTriangle(p, beakTop + Vector2.left * size * 0.02f, beakBottom + Vector2.left * size * 0.02f, beakTip);

                if (Vector2.Distance(p, bodyCenter) <= outlineRadius || beakArea)
                {
                    pixel = outline;

                    if (Vector2.Distance(p, bodyCenter) <= bodyRadius)
                    {
                        pixel = bodyColor;
                        if (IsInsideEllipse(p, wingCenter, wingRx, wingRy)) pixel = wingColor;
                        if (IsInsideEllipse(p, bellyCenter, bellyRadius, bellyRadius)) pixel = bellyColor;
                    }

                    if (PointInTriangle(p, beakTop, beakBottom, beakTip)) pixel = beakColor;
                }

                if (Vector2.Distance(p, eyeCenter) <= eyeOutlineRadius) pixel = outline;
                if (Vector2.Distance(p, eyeCenter) <= eyeRadius) pixel = Color.white;
                if (Vector2.Distance(p, pupilCenter) <= pupilRadius) pixel = new Color(0.05f, 0.05f, 0.05f);

                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Generates a rounded-rectangle sprite with a solid border, used for the
    /// Start/Restart buttons and the "TAP" hint bubble — gives real rounded
    /// corners instead of a flat rectangle, closer to a polished mobile UI look.
    /// </summary>
    private static Texture2D GenerateRoundedRectTexture(int width, int height, int radius, Color fill, Color border, int borderWidth)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inside = IsInsideRoundedRect(x, y, width, height, radius);
                bool insideInner = IsInsideRoundedRect(x, y, width, height, Mathf.Max(1, radius - borderWidth), borderWidth);
                Color pixel = clear;
                if (inside) pixel = insideInner ? fill : border;
                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        return tex;
    }

    private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius, int inset = 0)
    {
        float fx = x + 0.5f;
        float fy = y + 0.5f;
        float left = inset, right = width - inset, bottom = inset, top = height - inset;
        if (fx < left || fx > right || fy < bottom || fy > top) return false;

        float rx = Mathf.Min(radius, (right - left) / 2f);
        float ry = Mathf.Min(radius, (top - bottom) / 2f);

        if (fx < left + rx && fy < bottom + ry) return Vector2.Distance(new Vector2(fx, fy), new Vector2(left + rx, bottom + ry)) <= rx;
        if (fx > right - rx && fy < bottom + ry) return Vector2.Distance(new Vector2(fx, fy), new Vector2(right - rx, bottom + ry)) <= rx;
        if (fx < left + rx && fy > top - ry) return Vector2.Distance(new Vector2(fx, fy), new Vector2(left + rx, top - ry)) <= rx;
        if (fx > right - rx && fy > top - ry) return Vector2.Distance(new Vector2(fx, fy), new Vector2(right - rx, top - ry)) <= rx;
        return true;
    }

    /// <summary>
    /// Shaded green pipe body. Shading varies only by column (left highlight
    /// edge, mid base tone, right shadow edge) so the texture can be stretched
    /// to any height via transform.localScale without ever looking distorted.
    /// </summary>
    private static Texture2D GeneratePipeBodyTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color edgeDark = new Color(0.08f, 0.38f, 0.1f);
        Color highlight = new Color(0.55f, 0.88f, 0.35f);
        Color baseGreen = new Color(0.28f, 0.72f, 0.24f);
        Color shadow = new Color(0.13f, 0.48f, 0.14f);

        for (int x = 0; x < width; x++)
        {
            float u = (float)x / width;
            Color column;
            if (u < 0.06f) column = edgeDark;
            else if (u < 0.22f) column = highlight;
            else if (u < 0.78f) column = baseGreen;
            else if (u < 0.94f) column = shadow;
            else column = edgeDark;

            for (int y = 0; y < height; y++)
            {
                tex.SetPixel(x, y, column);
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// The pipe "lip" cap — same column shading as the body, plus a subtle
    /// horizontal bevel (a soft rim at the top, a soft shadow at the bottom)
    /// so it reads as a lip sticking out from the pipe without looking like a
    /// jarring, mismatched block of a different color from the body.
    /// </summary>
    private static Texture2D GeneratePipeCapTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        // Same palette as the body (not a separately-tuned, higher-contrast
        // set) so the cap blends smoothly into the pipe instead of standing out.
        Color edgeDark = new Color(0.08f, 0.38f, 0.1f);
        Color highlight = new Color(0.55f, 0.88f, 0.35f);
        Color baseGreen = new Color(0.28f, 0.72f, 0.24f);
        Color shadow = new Color(0.13f, 0.48f, 0.14f);

        for (int x = 0; x < width; x++)
        {
            float u = (float)x / width;
            Color columnBase;
            if (u < 0.04f || u > 0.96f) columnBase = edgeDark;
            else if (u < 0.16f) columnBase = highlight;
            else if (u < 0.84f) columnBase = baseGreen;
            else columnBase = shadow;

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                Color pixel = columnBase;
                if (v > 0.8f) pixel = Color.Lerp(columnBase, highlight, 0.25f); // soft top rim, not a hard contrast band
                if (v < 0.15f) pixel = Color.Lerp(columnBase, Color.black, 0.18f); // subtle bottom shadow rim
                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Tan/khaki dirt with a lighter "topsoil" band baked near the top of the
    /// texture (which lands right at the ground's visible top edge, no matter
    /// how much the sprite is stretched vertically to reach off-screen) plus
    /// subtle diagonal hatch marks for texture, like the classic game's dirt.
    /// </summary>
    private static Texture2D GenerateGroundDirtTexture(int width, int height)
    {
        // NOTE: only the top sliver of this texture is ever actually visible on
        // screen (the ground sprite is stretched way past the bottom of the
        // camera so it never runs out on tall phones) — so every pattern here
        // must repeat across the FULL height, not just live in one v-band, or
        // it never shows up in-game at all (this was the "flat ground, no
        // texture" bug: the old hatch/dots were confined to a band that ended
        // up entirely off-screen).
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color dirtBase = new Color(0.85f, 0.68f, 0.38f);
        Color hatch = new Color(0.62f, 0.46f, 0.22f);
        Color lineColor = new Color(0.55f, 0.4f, 0.18f);
        Color dotColor = new Color(0.6f, 0.44f, 0.2f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = dirtBase;

                // Diagonal hatch marks, repeating across the whole texture.
                int diag = (x + y) % 20;
                if (diag < 3) pixel = hatch;

                // A few bolder horizontal sediment lines for a "real ground" layered look.
                int stripe = y % 34;
                if (stripe == 0 || stripe == 1) pixel = lineColor;

                tex.SetPixel(x, y, pixel);
            }
        }

        // Scattered small round dots/pebbles over the sand, like real Flappy Bird's ground texture.
        System.Random rng = new System.Random(101);
        int dotCount = (width * height) / 180;
        for (int i = 0; i < dotCount; i++)
        {
            int cx = rng.Next(0, width);
            int cy = rng.Next(0, height); // scattered across the whole texture — the visible slice can be anywhere in it
            int radius = rng.Next(1, 3);
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius) continue;
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px < 0 || px >= width || py < 0 || py >= height) continue;
                    tex.SetPixel(px, py, dotColor);
                }
            }
        }

        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Grass strip with a sharp, solid jagged sawtooth top edge (transparent
    /// above it) — a single flat green, no soft shadow gradient along the
    /// edge, so it stays crisp instead of reading as a blurry line even when
    /// heavily stretched.
    /// </summary>
    private static Texture2D GenerateGrassTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color grass = new Color(0.38f, 0.8f, 0.28f);

        int toothWidth = Mathf.Max(4, width / 16);
        float toothHeight = height * 0.35f;
        float baseTop = height * 0.55f; // top of the flat grass body before the teeth start

        for (int x = 0; x < width; x++)
        {
            float toothPhase = (x % toothWidth) / (float)toothWidth; // 0..1 across one tooth
            float triangleHeight = (1f - Mathf.Abs(toothPhase - 0.5f) * 2f) * toothHeight;
            float edgeY = baseTop + triangleHeight;

            for (int y = 0; y < height; y++)
            {
                tex.SetPixel(x, y, y > edgeY ? clear : grass);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBackgroundTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color skyTop = new Color(0.30f, 0.62f, 0.87f);
        Color skyBottom = new Color(0.72f, 0.87f, 0.97f);
        Color cloudColor = new Color(1f, 1f, 1f, 0.95f);
        Color farBuildingColor = new Color(0.68f, 0.82f, 0.9f, 0.75f);
        Color nearBuildingColor = new Color(0.78f, 0.88f, 0.93f, 0.9f);
        Color windowColor = new Color(0.92f, 0.95f, 0.75f, 0.5f);

        Vector2[] cloudCenters =
        {
            new Vector2(width * 0.16f, height * 0.86f),
            new Vector2(width * 0.55f, height * 0.90f),
            new Vector2(width * 0.85f, height * 0.80f)
        };
        float cloudScale = width * 0.045f;

        // Sky gradient + clouds.
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            Color skyColor = Color.Lerp(skyBottom, skyTop, t);

            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);
                Color pixel = skyColor;

                foreach (Vector2 c in cloudCenters)
                {
                    bool inCloud =
                        IsInsideEllipse(p, c, cloudScale * 1.3f, cloudScale * 0.7f) ||
                        IsInsideEllipse(p, c + new Vector2(-cloudScale, -cloudScale * 0.1f), cloudScale * 0.8f, cloudScale * 0.55f) ||
                        IsInsideEllipse(p, c + new Vector2(cloudScale, -cloudScale * 0.05f), cloudScale * 0.8f, cloudScale * 0.55f);
                    if (inCloud) pixel = cloudColor;
                }

                tex.SetPixel(x, y, pixel);
            }
        }

        Color distantBuildingColor = new Color(0.72f, 0.85f, 0.92f, 0.55f);

        // Building counts pushed up further and packed tighter (less per-building
        // width variance) so several buildings are always visible even when the
        // camera's fixed-width crop is at its narrowest (tall phone aspect ratios).
        DrawSkyline(tex, width, height, distantBuildingColor, windowColor, seed: 5, baseHeightFrac: 0.1f, buildingCount: 26, jitter: 0.03f);
        DrawSkyline(tex, width, height, farBuildingColor, windowColor, seed: 11, baseHeightFrac: 0.17f, buildingCount: 20, jitter: 0.06f);
        DrawSkyline(tex, width, height, nearBuildingColor, windowColor, seed: 47, baseHeightFrac: 0.24f, buildingCount: 14, jitter: 0.1f);

        tex.Apply();
        return tex;
    }

    /// <summary>Draws one layer of a simple procedural city skyline with a scattering of lit windows.</summary>
    private static void DrawSkyline(Texture2D tex, int width, int height, Color buildingColor, Color windowColor, int seed, float baseHeightFrac, int buildingCount, float jitter)
    {
        System.Random rng = new System.Random(seed);
        float slotWidth = (float)width / buildingCount;

        for (int i = 0; i < buildingCount; i++)
        {
            float bWidth = slotWidth * (0.6f + (float)rng.NextDouble() * 0.35f);
            float bx = i * slotWidth + (slotWidth - bWidth) * 0.5f;
            float bHeight = height * (baseHeightFrac + (float)rng.NextDouble() * jitter * 2f);
            int x0 = Mathf.Clamp(Mathf.RoundToInt(bx), 0, width - 1);
            int x1 = Mathf.Clamp(Mathf.RoundToInt(bx + bWidth), 0, width);
            int yTop = Mathf.RoundToInt(bHeight);

            for (int x = x0; x < x1; x++)
            {
                for (int y = 0; y < yTop; y++)
                {
                    // Blend into the existing pixel (sky/cloud/farther building) so the
                    // building's alpha reads correctly baked into the final opaque texture.
                    Color existing = tex.GetPixel(x, y);
                    Color pixel = Color.Lerp(existing, buildingColor, buildingColor.a);
                    pixel.a = 1f;

                    // Sparse window grid — skip near edges so windows don't touch the building border.
                    int wx = x - x0;
                    int wy = y;
                    bool onWindowGridX = wx > 3 && wx % 7 < 3 && wx < (x1 - x0) - 3;
                    bool onWindowGridY = wy > 4 && wy % 9 < 4 && wy < yTop - 5;
                    if (onWindowGridX && onWindowGridY)
                    {
                        pixel = Color.Lerp(pixel, windowColor, windowColor.a);
                        pixel.a = 1f;
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }
        }
    }

    private static bool IsInsideEllipse(Vector2 p, Vector2 center, float rx, float ry)
    {
        float dx = (p.x - center.x) / rx;
        float dy = (p.y - center.y) / ry;
        return dx * dx + dy * dy <= 1f;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
        bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private static void BuildBackground(Sprite backgroundSprite)
    {
        // Two copies of the same sky/skyline sprite, scrolled slowly left as a
        // parallax layer (much slower than the ground/pipes) — always kept
        // adjacent and "cover"-scaled to the camera, so there's never a seam.
        GameObject bg = new GameObject("Background");
        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;
        sr.sortingOrder = -100;
        bg.transform.position = new Vector3(0, 0, 0);

        GameObject bg2 = new GameObject("Background2");
        SpriteRenderer sr2 = bg2.AddComponent<SpriteRenderer>();
        sr2.sprite = backgroundSprite;
        sr2.sortingOrder = -100;

        ScrollingBackground scroller = bg.AddComponent<ScrollingBackground>();
        scroller.tileB = bg2.transform;
        scroller.scrollSpeed = 0.6f;
    }

    // ---------- Scene objects ----------

    private const float CameraTargetHalfWidth = 9f; // fixed horizontal view width regardless of aspect ratio

    private static void BuildCamera()
    {
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f; // editor preview only; CameraFitWidth recalculates this at runtime
        cam.clearFlags = CameraClearFlags.SolidColor; // avoid the default skybox masking missing sprites
        cam.backgroundColor = new Color(0.53f, 0.81f, 0.92f); // sky blue
        camGO.transform.position = new Vector3(0, 0, -10);
        camGO.AddComponent<AudioListener>();

        CameraFitWidth fitWidth = camGO.AddComponent<CameraFitWidth>();
        fitWidth.targetHalfWidth = CameraTargetHalfWidth;
        // Slightly higher than the old cap of 7 — recovers more horizontal
        // width (so more of the skyline/buildings show) on tall phone aspect
        // ratios, without going back to the "zoomed out" look on very tall screens.
        fitWidth.maxOrthographicSize = 8f;
    }

    private static GameObject BuildGround(Sprite dirtSprite, Sprite grassSprite)
    {
        // Tall enough that it still reaches the bottom of the screen on very
        // tall portrait phones. Top edge moved lower (was -3.75) so the ground
        // takes up noticeably less of the screen, leaving more room to play.
        const float dirtHeight = 40f;
        const float dirtTopY = -5.6f;
        // Wider than the camera's fixed 18-unit visible width (CameraTargetHalfWidth * 2)
        // so two tiles side by side always fully cover the screen with room to spare.
        const float tileWidth = 20f;

        GameObject root = new GameObject("GroundRoot");

        GameObject tileA = BuildGroundTile("GroundTileA", dirtSprite, grassSprite, tileWidth, dirtHeight, dirtTopY);
        tileA.transform.SetParent(root.transform);
        GameObject tileB = BuildGroundTile("GroundTileB", dirtSprite, grassSprite, tileWidth, dirtHeight, dirtTopY);
        tileB.transform.SetParent(root.transform);

        GroundScroller scroller = root.AddComponent<GroundScroller>();
        scroller.tileA = tileA.transform;
        scroller.tileB = tileB.transform;
        scroller.tileWidth = tileWidth;
        scroller.scrollSpeed = 3f; // matches PipeMover's default speed so the floor and pipes move together

        return root;
    }

    /// <summary>One scrolling ground segment: dirt body (with collider + "Ground" tag) plus its grass cap.</summary>
    private static GameObject BuildGroundTile(string name, Sprite dirtSprite, Sprite grassSprite, float tileWidth, float dirtHeight, float dirtTopY)
    {
        GameObject ground = new GameObject(name);
        ground.tag = "Ground";
        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = dirtSprite; // tan/khaki dirt texture with a lighter topsoil band baked near the top edge
        sr.color = Color.white; // texture already has its final color — no tint
        sr.sortingOrder = -1;
        ground.transform.position = new Vector3(0, dirtTopY - dirtHeight / 2f, 0);
        ground.transform.localScale = new Vector3(tileWidth, dirtHeight, 1);
        ground.AddComponent<BoxCollider2D>();

        // Cosmetic grass strip with a jagged blade edge, sitting right on the dirt's top edge.
        GameObject grass = new GameObject("Grass");
        grass.transform.SetParent(ground.transform);
        SpriteRenderer grassSr = grass.AddComponent<SpriteRenderer>();
        grassSr.sprite = grassSprite;
        grassSr.color = Color.white;
        grassSr.sortingOrder = 0;
        grass.transform.localScale = new Vector3(1f, 0.5f / dirtHeight, 1f); // 0.5 world units tall
        grass.transform.localPosition = new Vector3(0, 0.5f, 0); // sits at the dirt's top edge (ratio-invariant)

        return ground;
    }

    private static GameObject BuildBird(Sprite birdSprite, AudioClip flapClip, AudioClip hitClip, AudioClip landClip)
    {
        GameObject bird = new GameObject("Bird");
        bird.tag = "Bird";
        SpriteRenderer sr = bird.AddComponent<SpriteRenderer>();
        sr.sprite = birdSprite;
        sr.sortingOrder = 10; // guaranteed to draw above background/ground/pipes on every aspect ratio
        bird.transform.position = new Vector3(-1f, 0, 0); // dead-center vertically — keeps gameplay start position unchanged
        bird.transform.localScale = new Vector3(1.2f, 1.2f, 1); // larger — easier to see on tall/narrow mobile screens
        bird.AddComponent<Rigidbody2D>();
        CircleCollider2D col = bird.AddComponent<CircleCollider2D>();
        col.radius = 0.36f;
        BirdController birdController = bird.AddComponent<BirdController>();
        birdController.flapSound = flapClip;
        birdController.hitSound = hitClip;
        birdController.fallSound = landClip;
        return bird;
    }

    private static GameObject BuildPipePairPrefab(Sprite pipeBodySprite, Sprite pipeCapSprite)
    {
        const float gapHeight = 2.5f;
        const float pipeHeight = 40f; // generous enough to reach top/bottom on tall portrait phones too
        const float pipeWidth = 1f;
        float half = gapHeight / 2f;

        GameObject root = new GameObject("PipePair");

        GameObject pipeBottom = new GameObject("PipeBottom");
        pipeBottom.transform.SetParent(root.transform);
        pipeBottom.tag = "Pipe";
        SpriteRenderer bottomSr = pipeBottom.AddComponent<SpriteRenderer>();
        bottomSr.sprite = pipeBodySprite; // shaded green body — safe to stretch vertically since shading is column-based only
        bottomSr.color = Color.white;
        bottomSr.sortingOrder = 5;
        pipeBottom.transform.localScale = new Vector3(pipeWidth, pipeHeight, 1);
        pipeBottom.transform.localPosition = new Vector3(0, -half - pipeHeight / 2f, 0);
        pipeBottom.AddComponent<BoxCollider2D>();

        GameObject pipeTop = new GameObject("PipeTop");
        pipeTop.transform.SetParent(root.transform);
        pipeTop.tag = "Pipe";
        SpriteRenderer topSr = pipeTop.AddComponent<SpriteRenderer>();
        topSr.sprite = pipeBodySprite;
        topSr.color = Color.white;
        topSr.sortingOrder = 5;
        pipeTop.transform.localScale = new Vector3(pipeWidth, pipeHeight, 1);
        pipeTop.transform.localPosition = new Vector3(0, half + pipeHeight / 2f, 0);
        pipeTop.AddComponent<BoxCollider2D>();

        // Cosmetic caps sitting right at the pipe mouths (classic pipe-lip look).
        GameObject capBottom = new GameObject("CapBottom");
        capBottom.transform.SetParent(root.transform);
        SpriteRenderer capBottomSr = capBottom.AddComponent<SpriteRenderer>();
        capBottomSr.sprite = pipeCapSprite;
        capBottomSr.color = Color.white;
        capBottomSr.sortingOrder = 6;
        capBottom.transform.localScale = new Vector3(pipeWidth * 1.3f, 0.35f, 1f);
        capBottom.transform.localPosition = new Vector3(0, -half, 0);

        GameObject capTop = new GameObject("CapTop");
        capTop.transform.SetParent(root.transform);
        SpriteRenderer capTopSr = capTop.AddComponent<SpriteRenderer>();
        capTopSr.sprite = pipeCapSprite;
        capTopSr.color = Color.white;
        capTopSr.sortingOrder = 6;
        capTop.transform.localScale = new Vector3(pipeWidth * 1.3f, -0.35f, 1f); // flipped vertically so its shading mirrors correctly on the underside
        capTop.transform.localPosition = new Vector3(0, half, 0);

        GameObject scoreZone = new GameObject("ScoreZone");
        scoreZone.transform.SetParent(root.transform);
        scoreZone.transform.localPosition = Vector3.zero;
        BoxCollider2D zoneCol = scoreZone.AddComponent<BoxCollider2D>();
        zoneCol.isTrigger = true;
        zoneCol.size = new Vector2(0.5f, gapHeight);
        scoreZone.AddComponent<ScoreTrigger>();

        PipeMover mover = root.AddComponent<PipeMover>();
        mover.speed = 3f;
        mover.destroyXPosition = -12f;

        if (!Directory.Exists(PrefabFolder)) Directory.CreateDirectory(PrefabFolder);
        string prefabPath = $"{PrefabFolder}/PipePair.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        return prefab;
    }

    private static GameObject BuildPipeSpawner(GameObject pipePairPrefab)
    {
        GameObject spawnerGO = new GameObject("PipeSpawner");
        PipeSpawner spawner = spawnerGO.AddComponent<PipeSpawner>();
        spawner.pipePairPrefab = pipePairPrefab;
        spawner.spawnInterval = 1.9f; // tighter, more consistent pacing — the previous gap felt uneven/delayed between pipes
        spawner.spawnXPosition = 10f;
        spawner.minGapY = -2f;
        spawner.maxGapY = 2f;
        return spawnerGO;
    }

    private static void BuildEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static Canvas BuildCanvas()
    {
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // portrait reference — matches the strict-portrait lock
        scaler.matchWidthOrHeight = 0f; // match width — width is the fixed/consistent dimension across phones
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject BuildScoreText(Transform canvasTransform, Sprite badgeSprite)
    {
        // Small rounded badge behind the score number instead of bare floating
        // text — reads clearly against any part of the sky/skyline.
        GameObject badge = new GameObject("ScoreBadge");
        badge.transform.SetParent(canvasTransform, false);
        Image badgeImg = badge.AddComponent<Image>();
        badgeImg.sprite = badgeSprite;
        badgeImg.type = Image.Type.Simple;
        RectTransform badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.5f, 1f);
        badgeRt.anchorMax = new Vector2(0.5f, 1f);
        badgeRt.pivot = new Vector2(0.5f, 1f);
        badgeRt.anchoredPosition = new Vector2(0, -70);
        badgeRt.sizeDelta = new Vector2(150, 110);

        GameObject go = new GameObject("ScoreText");
        go.transform.SetParent(badge.transform, false);
        Text text = go.AddComponent<Text>();
        text.text = "0";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 60;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.1f, 0.15f, 0.08f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private static readonly Color[] TitlePalette =
    {
        new Color(1f, 0.82f, 0.2f),   // yellow
        new Color(0.95f, 0.35f, 0.55f), // pink
        new Color(0.55f, 0.4f, 0.95f),  // purple
        new Color(0.55f, 0.85f, 0.3f),  // green
        new Color(0.95f, 0.35f, 0.55f), // pink
        new Color(0.55f, 0.4f, 0.95f),  // purple
    };

    private static GameObject BuildStartPanel(Transform canvasTransform, Sprite buttonSprite, Sprite tapBubbleSprite, out Button startButton)
    {
        GameObject panel = CreatePanel("StartPanel", canvasTransform, new Color(0, 0, 0, 0.0f));

        // Colorful per-letter title near the top, a pulsing "TAP" hint just under
        // the bird's idle spot, and the Start button lower down — leaves the
        // vertical center clear, which is exactly where the bird idles.
        CreateColoredTitle("TitleText", panel.transform, "FLAPPY BIRD", 78, new Vector2(0, 420));

        GameObject tapBubble = new GameObject("TapBubble");
        tapBubble.transform.SetParent(panel.transform, false);
        Image tapImg = tapBubble.AddComponent<Image>();
        tapImg.sprite = tapBubbleSprite;
        tapImg.type = Image.Type.Simple;
        RectTransform tapRt = tapBubble.GetComponent<RectTransform>();
        tapRt.sizeDelta = new Vector2(190, 78);
        tapRt.anchoredPosition = new Vector2(0, -190); // clear of the idling bird, well above the Start button
        tapBubble.AddComponent<UIPulse>();

        GameObject tapLabel = CreateLabel("Text", tapBubble.transform, "TAP", 40);
        Text tapText = tapLabel.GetComponent<Text>();
        tapText.color = Color.white;

        GameObject buttonGO = new GameObject("StartButton");
        buttonGO.transform.SetParent(panel.transform, false);
        Image img = buttonGO.AddComponent<Image>();
        ApplyButtonLook(img, buttonSprite, new Color(0.35f, 0.78f, 0.95f));
        RectTransform btnRt = buttonGO.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(340, 110);
        btnRt.anchoredPosition = new Vector2(0, -420); // near the bottom, well clear of the bird and the TAP hint
        startButton = buttonGO.AddComponent<Button>();

        GameObject label = CreateLabel("Text", buttonGO.transform, "START", 48);
        Text labelText = label.GetComponent<Text>();
        labelText.color = new Color(0.05f, 0.2f, 0.25f);
        Outline labelOutline = label.GetComponent<Outline>();
        if (labelOutline != null) Object.DestroyImmediate(labelOutline); // button already has contrast; skip the outline here

        return panel;
    }

    private static GameObject BuildGameOverPanel(Transform canvasTransform, Sprite buttonSprite, Sprite plankSprite, out Button menuButton, out Button retryButton, out GameObject scoreValueText, out GameObject bestValueText)
    {
        // Warm dusk-toned overlay instead of flat black, for a bit of atmosphere.
        GameObject panel = CreatePanel("GameOverPanel", canvasTransform, new Color(0.2f, 0.1f, 0.05f, 0.55f));

        // Bold red/orange "GAME OVER" heading with a heavy layered outline +
        // drop shadow, closer to a punchy pixel-game title than plain text.
        GameObject title = CreateLabel("GameOverText", panel.transform, "GAME OVER", 84, new Vector2(0, 480));
        Text titleText = title.GetComponent<Text>();
        titleText.color = new Color(0.95f, 0.25f, 0.15f);
        titleText.fontStyle = FontStyle.Bold;
        Outline titleOutline = title.GetComponent<Outline>();
        titleOutline.effectColor = new Color(0.35f, 0.05f, 0.02f, 1f);
        titleOutline.effectDistance = new Vector2(4f, -4f);
        Shadow titleShadow = title.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        titleShadow.effectDistance = new Vector2(3f, -7f);

        // Two wooden-plank boxes side by side: SCORE and BEST, label above value
        // (matches a classic "results card" look instead of one plain row list).
        GameObject scorePlank = CreatePlank(panel.transform, "SCORE", new Vector2(-150, 180), plankSprite, out scoreValueText);
        GameObject bestPlank = CreatePlank(panel.transform, "BEST", new Vector2(150, 180), plankSprite, out bestValueText);

        // MENU (left) and RETRY (right) — MENU fully resets to the Start screen,
        // RETRY jumps straight back into play.
        GameObject menuGO = new GameObject("MenuButton");
        menuGO.transform.SetParent(panel.transform, false);
        Image menuImg = menuGO.AddComponent<Image>();
        ApplyButtonLook(menuImg, buttonSprite, new Color(0.55f, 0.6f, 0.65f));
        RectTransform menuRt = menuGO.GetComponent<RectTransform>();
        menuRt.sizeDelta = new Vector2(260, 100);
        menuRt.anchoredPosition = new Vector2(-150, -160);
        menuButton = menuGO.AddComponent<Button>();
        GameObject menuLabel = CreateLabel("Text", menuGO.transform, "MENU", 38);
        Text menuLabelText = menuLabel.GetComponent<Text>();
        menuLabelText.color = new Color(0.08f, 0.1f, 0.12f);
        Outline menuLabelOutline = menuLabel.GetComponent<Outline>();
        if (menuLabelOutline != null) Object.DestroyImmediate(menuLabelOutline);

        GameObject retryGO = new GameObject("RetryButton");
        retryGO.transform.SetParent(panel.transform, false);
        Image retryImg = retryGO.AddComponent<Image>();
        ApplyButtonLook(retryImg, buttonSprite, new Color(0.4f, 0.82f, 0.4f));
        RectTransform retryRt = retryGO.GetComponent<RectTransform>();
        retryRt.sizeDelta = new Vector2(260, 100);
        retryRt.anchoredPosition = new Vector2(150, -160);
        retryButton = retryGO.AddComponent<Button>();
        GameObject retryLabel = CreateLabel("Text", retryGO.transform, "RETRY", 38);
        Text retryLabelText = retryLabel.GetComponent<Text>();
        retryLabelText.color = new Color(0.05f, 0.2f, 0.05f);
        Outline retryLabelOutline = retryLabel.GetComponent<Outline>();
        if (retryLabelOutline != null) Object.DestroyImmediate(retryLabelOutline);

        panel.SetActive(false);
        return panel;
    }

    /// <summary>One wooden-plank box: a small label ("SCORE"/"BEST") above a bold value number.</summary>
    private static GameObject CreatePlank(Transform parent, string label, Vector2 centerPos, Sprite plankSprite, out GameObject valueTextGO)
    {
        GameObject plank = new GameObject("Plank_" + label);
        plank.transform.SetParent(parent, false);
        Image plankImg = plank.AddComponent<Image>();
        plankImg.sprite = plankSprite;
        plankImg.type = Image.Type.Simple;
        RectTransform plankRt = plank.GetComponent<RectTransform>();
        plankRt.sizeDelta = new Vector2(260, 200);
        plankRt.anchoredPosition = centerPos;

        GameObject labelGO = CreateLabel("Label", plank.transform, label, 30, new Vector2(0, 45));
        Text labelText = labelGO.GetComponent<Text>();
        labelText.color = new Color(1f, 0.85f, 0.55f);
        Outline labelOutline = labelGO.GetComponent<Outline>();
        if (labelOutline != null) Object.DestroyImmediate(labelOutline);

        valueTextGO = CreateLabel("Value", plank.transform, "0", 56, new Vector2(0, -35));
        Text valueText = valueTextGO.GetComponent<Text>();
        valueText.color = Color.white;
        valueText.fontStyle = FontStyle.Bold;

        return plank;
    }

    /// <summary>
    /// Builds a title out of one colored Text object per letter (skipping the
    /// space), approximating the classic multi-color blocky logo look without
    /// using any copyrighted logo art.
    /// </summary>
    private static void CreateColoredTitle(string name, Transform parent, string content, int fontSize, Vector2 centerPos)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = centerPos;
        rootRt.sizeDelta = new Vector2(900, 150);

        float charWidth = fontSize * 0.72f;
        float spaceWidth = fontSize * 0.4f;
        float totalWidth = 0f;
        foreach (char c in content) totalWidth += c == ' ' ? spaceWidth : charWidth;

        float cursor = -totalWidth / 2f;
        int colorIndex = 0;
        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            float w = c == ' ' ? spaceWidth : charWidth;

            if (c != ' ')
            {
                GameObject letterGO = new GameObject("Letter_" + c + i);
                letterGO.transform.SetParent(root.transform, false);
                Text text = letterGO.AddComponent<Text>();
                text.text = c.ToString();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = fontSize;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = TitlePalette[colorIndex % TitlePalette.Length];
                Outline outline = letterGO.AddComponent<Outline>();
                outline.effectColor = new Color(0.15f, 0.08f, 0.02f, 0.9f);
                outline.effectDistance = new Vector2(3f, -3f);
                RectTransform rt = letterGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(charWidth * 1.3f, fontSize * 1.3f);
                rt.anchoredPosition = new Vector2(cursor + w / 2f, 0f);
                colorIndex++;
            }

            cursor += w;
        }
    }

    /// <summary>Uses the real downloaded button sprite if available, otherwise falls back to a plain color.</summary>
    private static void ApplyButtonLook(Image img, Sprite buttonSprite, Color fallbackColor)
    {
        if (buttonSprite != null)
        {
            img.sprite = buttonSprite;
            img.type = Image.Type.Simple; // no 9-slice border configured, so stretch as a whole image
            img.color = Color.white;
        }
        else
        {
            img.color = fallbackColor;
        }
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    private static GameObject CreateLabel(string name, Transform parent, string content, int fontSize, Vector2? offset = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600, 150);
        rt.anchoredPosition = offset ?? Vector2.zero;
        return go;
    }

    private static GameObject BuildGameManager(GameObject bird, GameObject pipeSpawnerGO, GameObject scoreTextGO, GameObject startPanel, GameObject gameOverPanel, AudioClip clickClip, AudioClip scoreClip, GameObject gameOverScoreText, GameObject gameOverBestText)
    {
        GameObject go = new GameObject("GameManager");
        GameManager gm = go.AddComponent<GameManager>();
        gm.bird = bird;
        gm.pipeSpawner = pipeSpawnerGO.GetComponent<PipeSpawner>();
        gm.scoreText = scoreTextGO;
        gm.startPanel = startPanel;
        gm.gameOverPanel = gameOverPanel;
        gm.buttonClickSound = clickClip;
        gm.scoreSound = scoreClip;
        gm.gameOverScoreText = gameOverScoreText;
        gm.gameOverBestText = gameOverBestText;
        return go;
    }
}
