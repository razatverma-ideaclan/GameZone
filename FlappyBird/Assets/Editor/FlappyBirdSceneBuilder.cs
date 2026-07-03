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

        // 1. Synthesize retro 8-bit WAV sound effects
        CreateSynthAudioFiles();

        // Force-regenerate sprites
        AssetDatabase.DeleteAsset($"{SpriteFolder}/Square.png");
        
        // Delete all bird skin frames
        for (int s = 0; s < 3; s++)
        {
            AssetDatabase.DeleteAsset($"{SpriteFolder}/Bird_Up_{s}.png");
            AssetDatabase.DeleteAsset($"{SpriteFolder}/Bird_Mid_{s}.png");
            AssetDatabase.DeleteAsset($"{SpriteFolder}/Bird_Down_{s}.png");
        }

        AssetDatabase.DeleteAsset($"{SpriteFolder}/Background.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/Sky_Day.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/Sky_Sunset.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/Sky_Night.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/Sky_Dawn.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/ButtonPill.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/TapBubble.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/ResultCard.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/BronzeMedal.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/SilverMedal.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/GoldMedal.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/PlatinumMedal.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/MedalPlaceholder.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/GroundDirt.png");
        AssetDatabase.DeleteAsset($"{SpriteFolder}/Grass.png");
        EnsureFolders();

        Sprite squareSprite = GetOrCreateSprite("Square", 128, 128, (w, h) => GenerateSquareTexture(w), 128);

        // Generate 3 bird skins (Yellow = 0, Blue = 1, Red = 2) with 3 frames each
        Sprite[] birdUpSprites = new Sprite[3];
        Sprite[] birdMidSprites = new Sprite[3];
        Sprite[] birdDownSprites = new Sprite[3];
        for (int s = 0; s < 3; s++)
        {
            birdUpSprites[s] = GetOrCreateSprite($"Bird_Up_{s}", 320, 320, (w, h) => GenerateBirdTexture(w, s, 0), 320);
            birdMidSprites[s] = GetOrCreateSprite($"Bird_Mid_{s}", 320, 320, (w, h) => GenerateBirdTexture(w, s, 1), 320);
            birdDownSprites[s] = GetOrCreateSprite($"Bird_Down_{s}", 320, 320, (w, h) => GenerateBirdTexture(w, s, 2), 320);
        }

        // Generate 4 Sky Gradient Backdrops
        Sprite skyDaySprite = GetOrCreateSprite("Sky_Day", 256, 512, (w, h) => GenerateSkyGradient(w, h, new Color(0.3f, 0.62f, 0.87f), new Color(0.72f, 0.87f, 0.97f)), 256);
        Sprite skySunsetSprite = GetOrCreateSprite("Sky_Sunset", 256, 512, (w, h) => GenerateSkyGradient(w, h, new Color(0.2f, 0.1f, 0.35f), new Color(0.92f, 0.45f, 0.2f)), 256);
        Sprite skyNightSprite = GetOrCreateSprite("Sky_Night", 256, 512, (w, h) => GenerateSkyGradient(w, h, new Color(0.02f, 0.02f, 0.08f), new Color(0.08f, 0.12f, 0.25f)), 256);
        Sprite skyDawnSprite = GetOrCreateSprite("Sky_Dawn", 256, 512, (w, h) => GenerateSkyGradient(w, h, new Color(0.12f, 0.22f, 0.45f), new Color(0.92f, 0.65f, 0.65f)), 256);
        
        const float bgWorldWidth = 20f;
        Sprite backgroundSprite = GetOrCreateSprite("Background", 1536, 864, (w, h) => GenerateBackgroundTexture(w, h), 1536f / bgWorldWidth, FilterMode.Point);
        Sprite pillButtonSprite = GetOrCreateSprite("ButtonPill", 320, 120, (w, h) => GenerateRoundedRectTexture(w, h, 30, new Color(0.96f, 0.5f, 0.15f), new Color(0.38f, 0.15f, 0.02f), 8), 320);
        
        // Classic tan Flappy Bird results scoreboard card
        Sprite resultCardSprite = GetOrCreateSprite("ResultCard", 620, 350, (w, h) => GenerateRoundedRectTexture(w, h, 24, new Color(0.88f, 0.85f, 0.72f), new Color(0.48f, 0.45f, 0.35f), 8), 620);
        
        // Shiny metallic medals with gradient, shadow, and embossing
        Sprite bronzeMedalSprite = GetOrCreateSprite("BronzeMedal", 128, 128, (w, h) => GenerateMedalTexture(w, h, new Color(0.7f, 0.4f, 0.2f), new Color(0.9f, 0.6f, 0.4f)), 128);
        Sprite silverMedalSprite = GetOrCreateSprite("SilverMedal", 128, 128, (w, h) => GenerateMedalTexture(w, h, new Color(0.6f, 0.6f, 0.6f), new Color(0.95f, 0.95f, 0.95f)), 128);
        Sprite goldMedalSprite = GetOrCreateSprite("GoldMedal", 128, 128, (w, h) => GenerateMedalTexture(w, h, new Color(0.9f, 0.7f, 0.1f), new Color(1f, 0.93f, 0.5f)), 128);
        Sprite platinumMedalSprite = GetOrCreateSprite("PlatinumMedal", 128, 128, (w, h) => GenerateMedalTexture(w, h, new Color(0.5f, 0.8f, 0.9f), new Color(0.9f, 0.98f, 1f)), 128);
        Sprite medalPlaceholderSprite = GetOrCreateSprite("MedalPlaceholder", 128, 128, (w, h) => GenerateMedalPlaceholderTexture(w, h), 128);

        // Optimized UI navigation icons
        Sprite shopIcon = GetOrCreateSprite("ShopIcon", 128, 128, (w, h) => GenerateIconTexture("Shop", w, h), 128);
        Sprite heroesIcon = GetOrCreateSprite("HeroesIcon", 128, 128, (w, h) => GenerateIconTexture("Heroes", w, h), 128);
        Sprite missionsIcon = GetOrCreateSprite("MissionsIcon", 128, 128, (w, h) => GenerateIconTexture("Missions", w, h), 128);
        Sprite themesIcon = GetOrCreateSprite("ThemesIcon", 128, 128, (w, h) => GenerateIconTexture("Themes", w, h), 128);
        Sprite playIcon = GetOrCreateSprite("PlayIcon", 128, 128, (w, h) => GenerateIconTexture("Play", w, h), 128);
        Sprite homeIcon = GetOrCreateSprite("HomeIcon", 128, 128, (w, h) => GenerateIconTexture("Home", w, h), 128);

        Sprite pipeBodySprite = GetOrCreateSprite("PipeBody", 256, 256, (w, h) => GeneratePipeBodyTexture(w, h), 256, FilterMode.Point);
        Sprite pipeCapSprite = GetOrCreateSprite("PipeCap", 256, 256, (w, h) => GeneratePipeCapTexture(w, h), 256, FilterMode.Point);
        
        // Ground and grass with REPEAT wrapping enabled
        Sprite groundDirtSprite = GetOrCreateSprite("GroundDirt", 256, 256, (w, h) => GenerateGroundDirtTexture(w, h), 256, FilterMode.Point, TextureWrapMode.Repeat);
        Sprite grassSprite = GetOrCreateSprite("Grass", 256, 256, (w, h) => GenerateGrassTexture(w, h), 512, FilterMode.Point, TextureWrapMode.Repeat);

        AssetDatabase.Refresh(); // pick up the synthesized .wav clips generated outside the Editor
        
        AudioClip flapClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Flap.wav");
        AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ButtonClick.wav");
        AudioClip scoreClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Score.wav");
        AudioClip hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Hit.wav");
        AudioClip landClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Land.wav");

        // Start from a fresh empty scene so re-running this is safe.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Ensure visual theme ScriptableObjects and textures are generated
        ThemeData[] themeAssets = EnsureThemeSystem();
        GameObject themeMgrGO = new GameObject("ThemeManager");
        ThemeManager tm = themeMgrGO.AddComponent<ThemeManager>();
        tm.themes = themeAssets;

        BuildCamera();
        BuildBackground(backgroundSprite, skyDaySprite, skySunsetSprite, skyNightSprite, skyDawnSprite);
        GameObject ground = BuildGround(groundDirtSprite, grassSprite);
        GameObject bird = BuildBird(birdMidSprites, birdUpSprites, birdDownSprites, flapClip, hitClip, landClip);
        GameObject pipePairPrefab = BuildPipePairPrefab(pipeBodySprite, pipeCapSprite);
        GameObject pipeSpawnerGO = BuildPipeSpawner(pipePairPrefab);
        BuildEventSystem();
        Canvas canvas = BuildCanvas();
        GameObject scoreTextGO = BuildScoreText(canvas.transform);
        
        GameObject startHighScoreText;
        Button startButton, shopButton, heroesButton, missionsButton, themesButton, navPlayBtn;
        GameObject toastPanel, themeSelectorPanelRef;
        GameObject lobbyPanel, heroesPanel;
        UnityEngine.UI.Image playIconImageRef;
        GameObject startPanel = BuildStartPanel(canvas.transform, pillButtonSprite, resultCardSprite, goldMedalSprite, out startButton, out startHighScoreText, birdMidSprites[0], themeAssets, shopIcon, heroesIcon, missionsIcon, themesIcon, playIcon, homeIcon, out shopButton, out heroesButton, out missionsButton, out themesButton, out toastPanel, out themeSelectorPanelRef, out lobbyPanel, out heroesPanel, out playIconImageRef, out navPlayBtn);
        
        UnityEngine.UI.Image medalImage;
        GameObject newBestBadge;
        RectTransform resultCardTransform;
        GameObject gameOverScoreText, gameOverBestText;
        GameObject gameOverPanel = BuildGameOverPanel(canvas.transform, pillButtonSprite, resultCardSprite, out Button menuButton, out Button retryButton, out gameOverScoreText, out gameOverBestText, out medalImage, out newBestBadge, out resultCardTransform);
        
        GameObject gameManagerGO = BuildGameManager(bird, pipeSpawnerGO, scoreTextGO, startPanel, gameOverPanel, clickClip, scoreClip, gameOverScoreText, gameOverBestText, startHighScoreText, medalImage, bronzeMedalSprite, silverMedalSprite, goldMedalSprite, platinumMedalSprite, medalPlaceholderSprite, newBestBadge, resultCardTransform);

        // Wire the buttons now that GameManager exists.
        GameManager gm = gameManagerGO.GetComponent<GameManager>();
        gm.toastPanel = toastPanel;
        gm.themeSelectorPanel = themeSelectorPanelRef;
        gm.lobbyPanel = lobbyPanel;
        gm.heroesPanel = heroesPanel;
        gm.playIconImage = playIconImageRef;
        gm.playSprite = playIcon;
        gm.homeSprite = homeIcon;
        gm.shopButton = shopButton;
        gm.heroesButton = heroesButton;
        gm.missionsButton = missionsButton;
        gm.themesButton = themesButton;

        UnityEventTools.AddPersistentListener(startButton.onClick, gm.StartGame);
        UnityEventTools.AddPersistentListener(navPlayBtn.onClick, gm.StartGame);
        UnityEventTools.AddPersistentListener(shopButton.onClick, gm.OnShopClicked);
        UnityEventTools.AddPersistentListener(heroesButton.onClick, gm.OnHeroesClicked);
        UnityEventTools.AddPersistentListener(missionsButton.onClick, gm.OnMissionsClicked);
        UnityEventTools.AddPersistentListener(themesButton.onClick, gm.OnThemesClicked);
        
        UnityEventTools.AddPersistentListener(menuButton.onClick, gm.RestartGame);
        UnityEventTools.AddPersistentListener(retryButton.onClick, gm.RetryGame);

        // Wire the 3 cards inside heroesPanel
        for (int i = 0; i < 3; i++)
        {
            Transform cardTrans = heroesPanel.transform.Find("Grid/Card" + i);
            if (cardTrans != null)
            {
                Button cardBtn = cardTrans.GetComponent<Button>();
                if (cardBtn != null)
                {
                    UnityEventTools.AddIntPersistentListener(cardBtn.onClick, gm.SelectHero, i);
                }
            }
        }



        if (!Directory.Exists(SceneFolder))
        {
            Directory.CreateDirectory(SceneFolder);
        }
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorSceneManager.OpenScene(ScenePath);

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
        AddTag("Wall");
        AddTag("Ceiling");
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

    private static Sprite GetOrCreateSprite(string name, int width, int height, System.Func<int, int, Texture2D> generator, float pixelsPerUnit, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
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
        importer.wrapMode = wrapMode;
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

    private static Texture2D GenerateBirdTexture(int size, int skinIndex, int wingFrame)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color outline = new Color(0.25f, 0.14f, 0.02f);
        
        Color bodyColor = new Color(1f, 0.83f, 0.15f); // yellow
        Color bellyColor = new Color(1f, 0.95f, 0.6f); // light yellow
        Color wingColor = new Color(0.95f, 0.6f, 0.08f); // orange
        Color beakColor = new Color(0.95f, 0.35f, 0.05f); // red-orange

        if (skinIndex == 1) // Blue bird
        {
            bodyColor = new Color(0.2f, 0.6f, 0.95f);
            bellyColor = new Color(0.6f, 0.85f, 1f);
            wingColor = new Color(0.08f, 0.4f, 0.75f);
            beakColor = new Color(0.95f, 0.5f, 0.05f);
        }
        else if (skinIndex == 2) // Red bird
        {
            bodyColor = new Color(0.95f, 0.25f, 0.25f);
            bellyColor = new Color(1f, 0.6f, 0.6f);
            wingColor = new Color(0.75f, 0.08f, 0.08f);
            beakColor = new Color(0.95f, 0.65f, 0.05f);
        }

        Vector2 bodyCenter = new Vector2(size * 0.45f, size * 0.5f);
        float bodyRadius = size * 0.34f;
        float outlineRadius = bodyRadius + size * 0.03f;
        Vector2 bellyCenter = new Vector2(size * 0.4f, size * 0.35f);
        float bellyRadius = size * 0.21f;

        Vector2 wingCenter;
        float wingRx, wingRy;
        if (wingFrame == 0) // Up
        {
            wingCenter = new Vector2(size * 0.32f, size * 0.55f);
            wingRx = size * 0.14f;
            wingRy = size * 0.18f;
        }
        else if (wingFrame == 2) // Down
        {
            wingCenter = new Vector2(size * 0.32f, size * 0.41f);
            wingRx = size * 0.14f;
            wingRy = size * 0.18f;
        }
        else // Mid (1)
        {
            wingCenter = new Vector2(size * 0.32f, size * 0.48f);
            wingRx = size * 0.17f;
            wingRy = size * 0.13f;
        }

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

    private static Texture2D GenerateSkyGradient(int width, int height, Color topColor, Color bottomColor)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            Color col = Color.Lerp(bottomColor, topColor, t);
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, col);
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
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color dirtBase = new Color(0.85f, 0.68f, 0.38f);
        Color hatch = new Color(0.68f, 0.5f, 0.25f);
        Color lineColor = new Color(0.55f, 0.38f, 0.18f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = dirtBase;

                // Sediment bands (horizontal layers)
                int layer = y / 48;
                if (layer % 2 == 0)
                {
                    pixel = Color.Lerp(dirtBase, new Color(0.8f, 0.62f, 0.34f), 0.5f);
                }

                // Add diagonal hatch lines
                int diag = (x + y) % 32;
                if (diag < 2)
                {
                    pixel = hatch;
                }

                // Bold horizontal layers (lines)
                if (y == 24 || y == 25 || y == 96 || y == 97 || y == 160 || y == 161 || y == 220 || y == 221)
                {
                    pixel = lineColor;
                }

                tex.SetPixel(x, y, pixel);
            }
        }

        // Scattered pebbles/stones
        System.Random rng = new System.Random(101);
        int dotCount = (width * height) / 120;
        for (int i = 0; i < dotCount; i++)
        {
            int cx = rng.Next(0, width);
            int cy = rng.Next(0, height);
            int radius = rng.Next(2, 4);
            Color stoneColor = rng.Next(0, 2) == 0 ? lineColor : hatch;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius) continue;
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px < 0 || px >= width || py < 0 || py >= height) continue;
                    tex.SetPixel(px, py, stoneColor);
                }
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateGrassTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color grass = new Color(0.38f, 0.8f, 0.28f);
        Color grassHighlight = new Color(0.58f, 0.92f, 0.38f);
        Color grassShadow = new Color(0.24f, 0.6f, 0.16f);

        int toothWidth = Mathf.Max(4, width / 16);
        float toothHeight = height * 0.35f;
        float baseTop = height * 0.55f;

        for (int x = 0; x < width; x++)
        {
            float toothPhase = (x % toothWidth) / (float)toothWidth;
            float triangleHeight = (1f - Mathf.Abs(toothPhase - 0.5f) * 2f) * toothHeight;
            float edgeY = baseTop + triangleHeight;

            for (int y = 0; y < height; y++)
            {
                if (y > edgeY)
                {
                    tex.SetPixel(x, y, clear);
                }
                else if (y > edgeY - 6) // Highlight on the teeth tips
                {
                    tex.SetPixel(x, y, grassHighlight);
                }
                else if (y < 12) // Subtle shadow where the grass meets the dirt
                {
                    tex.SetPixel(x, y, grassShadow);
                }
                else
                {
                    tex.SetPixel(x, y, grass);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBackgroundTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color cloudColor = new Color(1f, 1f, 1f, 0.92f);
        Color farBuildingColor = new Color(0.68f, 0.82f, 0.9f, 0.75f);
        Color nearBuildingColor = new Color(0.78f, 0.88f, 0.93f, 0.9f);
        Color windowColor = new Color(0.92f, 0.95f, 0.75f, 0.5f);

        // Fill background as transparent initially
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        Vector2[] cloudCenters =
        {
            new Vector2(width * 0.16f, height * 0.86f),
            new Vector2(width * 0.55f, height * 0.90f),
            new Vector2(width * 0.85f, height * 0.80f)
        };
        float cloudScale = width * 0.045f;

        // Draw clouds on transparent background
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);
                foreach (Vector2 c in cloudCenters)
                {
                    bool inCloud =
                        IsInsideEllipse(p, c, cloudScale * 1.3f, cloudScale * 0.7f) ||
                        IsInsideEllipse(p, c + new Vector2(-cloudScale, -cloudScale * 0.1f), cloudScale * 0.8f, cloudScale * 0.55f) ||
                        IsInsideEllipse(p, c + new Vector2(cloudScale, -cloudScale * 0.05f), cloudScale * 0.8f, cloudScale * 0.55f);
                    if (inCloud)
                    {
                        tex.SetPixel(x, y, cloudColor);
                    }
                }
            }
        }

        Color distantBuildingColor = new Color(0.72f, 0.85f, 0.92f, 0.55f);

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

            // Randomly mix dark and light buildings (e.g., 35% chance to be a dark contrast block building)
            Color customColor;
            if (rng.NextDouble() < 0.35)
            {
                // Dark slate contrast color
                float darkVal = 0.28f + (float)rng.NextDouble() * 0.16f;
                // Mix in a bit of buildingColor tint to keep it cohesive
                customColor = new Color(
                    Mathf.Clamp01(darkVal * 0.6f + buildingColor.r * 0.4f),
                    Mathf.Clamp01(darkVal * 0.6f + buildingColor.g * 0.4f),
                    Mathf.Clamp01(darkVal * 0.6f + buildingColor.b * 0.4f),
                    buildingColor.a
                );
            }
            else
            {
                // Standard light building color with slight shift
                float rShift = ((float)rng.NextDouble() - 0.5f) * 0.15f;
                float gShift = ((float)rng.NextDouble() - 0.5f) * 0.15f;
                float bShift = ((float)rng.NextDouble() - 0.5f) * 0.15f;
                customColor = new Color(
                    Mathf.Clamp01(buildingColor.r + rShift),
                    Mathf.Clamp01(buildingColor.g + gShift),
                    Mathf.Clamp01(buildingColor.b + bShift),
                    buildingColor.a
                );
            }

            for (int x = x0; x < x1; x++)
            {
                for (int y = 0; y < yTop; y++)
                {
                    // Blend into the existing pixel (sky/cloud/farther building)
                    Color existing = tex.GetPixel(x, y);
                    Color pixel = Color.Lerp(existing, customColor, customColor.a);
                    pixel.a = 1f;

                    // Sparse window grid
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

    private static void BuildBackground(Sprite bgCloudsBuildings, Sprite skyDay, Sprite skySunset, Sprite skyNight, Sprite skyDawn)
    {
        // 1. Sky parent container
        GameObject skyParent = new GameObject("SkyBackground");
        skyParent.transform.position = Vector3.zero;
        WeatherController weather = skyParent.AddComponent<WeatherController>();

        // Day Sky
        GameObject daySky = new GameObject("Sky_Day");
        daySky.transform.SetParent(skyParent.transform);
        SpriteRenderer srDay = daySky.AddComponent<SpriteRenderer>();
        srDay.sprite = skyDay;
        srDay.sortingOrder = -110;
        daySky.transform.localScale = new Vector3(80f, 40f, 1f); // cover full screen
        daySky.transform.localPosition = Vector3.zero;
        weather.skyDay = srDay;

        // Sunset Sky
        GameObject sunsetSky = new GameObject("Sky_Sunset");
        sunsetSky.transform.SetParent(skyParent.transform);
        SpriteRenderer srSunset = sunsetSky.AddComponent<SpriteRenderer>();
        srSunset.sprite = skySunset;
        srSunset.sortingOrder = -109;
        sunsetSky.transform.localScale = new Vector3(80f, 40f, 1f);
        sunsetSky.transform.localPosition = Vector3.zero;
        weather.skySunset = srSunset;

        // Night Sky
        GameObject nightSky = new GameObject("Sky_Night");
        nightSky.transform.SetParent(skyParent.transform);
        SpriteRenderer srNight = nightSky.AddComponent<SpriteRenderer>();
        srNight.sprite = skyNight;
        srNight.sortingOrder = -108;
        nightSky.transform.localScale = new Vector3(80f, 40f, 1f);
        nightSky.transform.localPosition = Vector3.zero;
        weather.skyNight = srNight;

        // Dawn Sky
        GameObject dawnSky = new GameObject("Sky_Dawn");
        dawnSky.transform.SetParent(skyParent.transform);
        SpriteRenderer srDawn = dawnSky.AddComponent<SpriteRenderer>();
        srDawn.sprite = skyDawn;
        srDawn.sortingOrder = -107;
        dawnSky.transform.localScale = new Vector3(80f, 40f, 1f);
        dawnSky.transform.localPosition = Vector3.zero;
        weather.skyDawn = srDawn;

        // 2. Parallax Foreground (Clouds & Buildings with transparent sky background)
        GameObject bg = new GameObject("Background");
        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = bgCloudsBuildings;
        sr.sortingOrder = -100;
        bg.transform.position = new Vector3(0, 0, 0);

        GameObject bg2 = new GameObject("Background2");
        SpriteRenderer sr2 = bg2.AddComponent<SpriteRenderer>();
        sr2.sprite = bgCloudsBuildings;
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
        ground.transform.position = new Vector3(0, dirtTopY - dirtHeight / 2f, 0);

        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = dirtSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(tileWidth, dirtHeight);
        sr.color = Color.white;
        sr.sortingOrder = 10; // Ground renders in front of pipes (5)

        BoxCollider2D col = ground.AddComponent<BoxCollider2D>();
        col.size = new Vector2(tileWidth, dirtHeight);

        // Cosmetic grass strip with a jagged blade edge, sitting right on the dirt's top edge.
        GameObject grass = new GameObject("Grass");
        grass.transform.SetParent(ground.transform);
        SpriteRenderer grassSr = grass.AddComponent<SpriteRenderer>();
        grassSr.sprite = grassSprite;
        grassSr.drawMode = SpriteDrawMode.Tiled;
            grassSr.size = new Vector2(tileWidth, 0.5f);
        grassSr.color = Color.white;
        grassSr.sortingOrder = 11; // Grass renders in front of pipes (5)
        grass.transform.localPosition = new Vector3(0, dirtHeight / 2f, 0); // sits at the dirt's top edge

        return ground;
    }

    private static GameObject BuildBird(Sprite[] midSprites, Sprite[] upSprites, Sprite[] downSprites, AudioClip flapClip, AudioClip hitClip, AudioClip landClip)
    {
        GameObject bird = new GameObject("Bird");
        bird.tag = "Bird";
        SpriteRenderer sr = bird.AddComponent<SpriteRenderer>();
        sr.sprite = midSprites[0]; // default to yellow mid
        sr.sortingOrder = 30; // bird is on top of pipes (5) and ground (10/11)
        bird.transform.position = new Vector3(-1f, 0, 0); // dead-center vertically — keeps gameplay start position unchanged
        bird.transform.localScale = new Vector3(1.2f, 1.2f, 1); // larger — easier to see on tall/narrow mobile screens
        bird.AddComponent<Rigidbody2D>();
        CircleCollider2D col = bird.AddComponent<CircleCollider2D>();
        col.radius = 0.36f;
        
        BirdController birdController = bird.AddComponent<BirdController>();
        birdController.flapSound = flapClip;
        birdController.hitSound = hitClip;
        birdController.fallSound = landClip;
        
        // Setup skins struct in BirdController
        birdController.skins = new BirdController.BirdSkin[3];
        for (int i = 0; i < 3; i++)
        {
            birdController.skins[i] = new BirdController.BirdSkin();
            birdController.skins[i].flapSprites = new Sprite[] { upSprites[i], midSprites[i], downSprites[i] };
        }
        
        birdController.animationSpeed = 12f;

        // Particle Trail
        GameObject trail = new GameObject("Trail");
        trail.transform.SetParent(bird.transform);
        trail.transform.localPosition = new Vector3(-0.35f, 0f, 0f);
        ParticleSystem ps = trail.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = 0.4f;
        main.startSpeed = 0.6f;
        main.startSize = 0.12f;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var psRenderer = trail.GetComponent<ParticleSystemRenderer>();
        psRenderer.sortingOrder = 29; // just behind bird
        psRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

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
        scaler.referenceResolution = new Vector2(1080, 1920); // portrait reference
        canvasGO.AddComponent<CanvasScalerMatch>(); // added responsive scaler match component
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject BuildScoreText(Transform canvasTransform)
    {
        GameObject go = new GameObject("ScoreText");
        go.transform.SetParent(canvasTransform, false);
        Text text = go.AddComponent<Text>();
        text.text = "0";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 72;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.UpperRight;
        text.color = new Color(0.95f, 0.72f, 0.15f); // Beautiful Gold color text

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.18f, 0.22f, 1f); // Charcoal outline
        outline.effectDistance = new Vector2(4f, -4f);

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        shadow.effectDistance = new Vector2(2f, -4f);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f); // Anchor to top-right
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(300, 100);
        rt.anchoredPosition = new Vector2(-60, -60); // Offset to clear iPhone notch
        return go;
    }

    private static readonly Color[] TitlePalette =
    {
        new Color(0.15f, 0.15f, 0.18f) // Premium dark charcoal color
    };

    private static GameObject BuildStartPanel(Transform canvasTransform, Sprite buttonSprite, Sprite resultCardSprite, Sprite goldMedalSprite, out Button startButton, out GameObject startHighScoreText, Sprite birdMidSprite, ThemeData[] themeAssets, Sprite shopIcon, Sprite heroesIcon, Sprite missionsIcon, Sprite themesIcon, Sprite playIcon, Sprite homeIcon, out Button shopButton, out Button heroesButton, out Button missionsButton, out Button themesButton, out GameObject toastPanel, out GameObject themeSelectorPanelRef, out GameObject lobbyPanel, out GameObject heroesPanel, out UnityEngine.UI.Image playIconImageRef, out Button navPlayBtn)
    {
        GameObject panel = CreatePanel("StartPanel", canvasTransform, new Color(0, 0, 0, 0.0f));

        // --- Lobby State Panel (groups title, banner, high score badge) ---
        lobbyPanel = new GameObject("LobbyPanel");
        lobbyPanel.transform.SetParent(panel.transform, false);
        RectTransform lpRt = lobbyPanel.AddComponent<RectTransform>();
        lpRt.anchorMin = Vector2.zero;
        lpRt.anchorMax = Vector2.one;
        lpRt.offsetMin = Vector2.zero;
        lpRt.offsetMax = Vector2.zero;

        // Full-screen transparent button inside lobbyPanel to trigger tap-to-start
        GameObject startBtnGO = new GameObject("StartButton");
        startBtnGO.transform.SetParent(lobbyPanel.transform, false);
        Image bgTapImg = startBtnGO.AddComponent<Image>();
        bgTapImg.color = new Color(0, 0, 0, 0); // transparent

        RectTransform btnRt = startBtnGO.GetComponent<RectTransform>();
        btnRt.anchorMin = Vector2.zero;
        btnRt.anchorMax = Vector2.one;
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;

        startButton = startBtnGO.AddComponent<Button>();
        startBtnGO.transform.SetAsFirstSibling(); // render behind other elements

        // Colorful per-letter title near the top with floating animation
        GameObject titleGO = CreateColoredTitle("TitleText", lobbyPanel.transform, "FLAPPY BIRD", 110, new Vector2(0, 420));
        titleGO.AddComponent<UIFloat>().floatAmount = 12f;
        titleGO.GetComponent<UIFloat>().speed = 2f;

        // Optimized Floating World/Theme Banner
        GameObject worldBanner = new GameObject("WorldBanner");
        worldBanner.transform.SetParent(lobbyPanel.transform, false);
        Image wbImg = worldBanner.AddComponent<Image>();
        wbImg.sprite = resultCardSprite;
        wbImg.color = new Color(0.08f, 0.45f, 0.85f, 0.9f); // premium blue banner background
        
        Outline wbOutline = worldBanner.AddComponent<Outline>();
        wbOutline.effectColor = Color.white;
        wbOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform wbRt = worldBanner.GetComponent<RectTransform>();
        wbRt.sizeDelta = new Vector2(360, 80);
        wbRt.anchoredPosition = new Vector2(0, 220); // floating above bird character preview

        GameObject activeThemeLabel = CreateLabel("ActiveThemeLabel", worldBanner.transform, "WORLD: CLASSIC", 26, Vector2.zero);
        Text activeThemeTxt = activeThemeLabel.GetComponent<Text>();
        activeThemeTxt.color = Color.white;
        activeThemeTxt.fontStyle = FontStyle.Bold;
        
        RectTransform atlRt = activeThemeLabel.GetComponent<RectTransform>();
        atlRt.anchorMin = Vector2.zero;
        atlRt.anchorMax = Vector2.one;
        atlRt.offsetMin = Vector2.zero;
        atlRt.offsetMax = Vector2.zero;

        // High Score Badge (positioned below the central character)
        GameObject scoreBadge = new GameObject("HighScoreBadge");
        scoreBadge.transform.SetParent(lobbyPanel.transform, false);
        Image badgeImg = scoreBadge.AddComponent<Image>();
        badgeImg.sprite = resultCardSprite; // rounded card texture
        badgeImg.color = new Color(0.15f, 0.15f, 0.18f, 0.75f); // dark translucent charcoal
        
        RectTransform badgeRt = scoreBadge.GetComponent<RectTransform>();
        badgeRt.sizeDelta = new Vector2(280, 68);
        badgeRt.anchoredPosition = new Vector2(0, -100);

        // Small Gold Medal icon inside badge
        GameObject medalIcon = new GameObject("MedalIcon");
        medalIcon.transform.SetParent(scoreBadge.transform, false);
        Image medalImg = medalIcon.AddComponent<Image>();
        medalImg.sprite = goldMedalSprite;
        RectTransform medalRt = medalIcon.GetComponent<RectTransform>();
        medalRt.sizeDelta = new Vector2(42, 42);
        medalRt.anchoredPosition = new Vector2(-90, 0);

        // Best score text inside badge
        startHighScoreText = CreateLabel("StartHighScoreText", scoreBadge.transform, "BEST: 0", 34, new Vector2(25, 0));
        Text bestText = startHighScoreText.GetComponent<Text>();
        bestText.color = new Color(0.95f, 0.72f, 0.15f); // gold best text
        bestText.fontStyle = FontStyle.Bold;
        
        Outline bestOutline = startHighScoreText.GetComponent<Outline>();
        if (bestOutline == null) bestOutline = startHighScoreText.AddComponent<Outline>();
        bestOutline.effectColor = new Color(0.1f, 0.08f, 0.05f, 1f);
        bestOutline.effectDistance = new Vector2(2f, -2f);

        // Pulsing "TAP TO START" hint text on start screen
        GameObject tapStartGO = CreateLabel("TapToStartText", lobbyPanel.transform, "TAP TO START", 36, new Vector2(0, -300));
        Text tapText = tapStartGO.GetComponent<Text>();
        tapText.color = new Color(0.95f, 0.95f, 0.95f, 0.85f);
        tapText.fontStyle = FontStyle.Bold;
        
        Outline tapOutline = tapStartGO.GetComponent<Outline>();
        if (tapOutline == null) tapOutline = tapStartGO.AddComponent<Outline>();
        tapOutline.effectColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        tapOutline.effectDistance = new Vector2(2f, -2f);

        UIPulse pulse = tapStartGO.AddComponent<UIPulse>();
        pulse.scaleAmount = 0.06f;
        pulse.speed = 3f;

        // --- Interactive Heroes Selection Screen (Grid) ---
        heroesPanel = new GameObject("HeroesPanel");
        heroesPanel.transform.SetParent(panel.transform, false);
        RectTransform hpRt = heroesPanel.AddComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(0.5f, 0.5f);
        hpRt.anchorMax = new Vector2(0.5f, 0.5f);
        hpRt.pivot = new Vector2(0.5f, 0.5f);
        hpRt.sizeDelta = new Vector2(1000, 1400);
        hpRt.anchoredPosition = new Vector2(0, 80); // centered vertically above bottom bar

        // Header bar inside HeroesPanel
        GameObject hbGO = new GameObject("HeaderBar");
        hbGO.transform.SetParent(heroesPanel.transform, false);
        Image hbImg = hbGO.AddComponent<Image>();
        hbImg.sprite = resultCardSprite;
        hbImg.color = new Color(0.08f, 0.45f, 0.85f, 1f); // bright royal blue
        
        Outline hbOutline = hbGO.AddComponent<Outline>();
        hbOutline.effectColor = Color.white;
        hbOutline.effectDistance = new Vector2(2f, -2f);
        
        RectTransform hbRt = hbGO.GetComponent<RectTransform>();
        hbRt.sizeDelta = new Vector2(900, 80);
        hbRt.anchoredPosition = new Vector2(0, 620); // top header position

        GameObject hbTxtGO = CreateLabel("Text", hbGO.transform, "CLASSIC HEROES (1/3)", 28, Vector2.zero);
        hbTxtGO.GetComponent<Text>().color = Color.white;
        hbTxtGO.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform hbtRt = hbTxtGO.GetComponent<RectTransform>();
        hbtRt.anchorMin = Vector2.zero;
        hbtRt.anchorMax = Vector2.one;
        hbtRt.offsetMin = Vector2.zero;
        hbtRt.offsetMax = Vector2.zero;

        // Grid Container inside HeroesPanel
        GameObject gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(heroesPanel.transform, false);
        RectTransform gridRt = gridGO.AddComponent<RectTransform>();
        gridRt.sizeDelta = new Vector2(900, 1000);
        gridRt.anchoredPosition = new Vector2(0, 0); // centered

        GridLayoutGroup hGrid = gridGO.AddComponent<GridLayoutGroup>();
        hGrid.cellSize = new Vector2(270, 360);
        hGrid.spacing = new Vector2(30, 30);
        hGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        hGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        hGrid.childAlignment = TextAnchor.MiddleCenter;

        // Instantiate 3 character cards in grid
        for (int i = 0; i < 3; i++)
        {
            GameObject cardGO = new GameObject("Card" + i);
            cardGO.transform.SetParent(gridGO.transform, false);
            Image cardImg = cardGO.AddComponent<Image>();
            cardImg.sprite = resultCardSprite;
            cardImg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f); // dark card body

            Outline cardOutline = cardGO.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.35f, 0.35f, 0.4f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            cardGO.AddComponent<Button>();

            // Icon preview image
            GameObject iconGO = new GameObject("PreviewImage");
            iconGO.transform.SetParent(cardGO.transform, false);
            Image previewImg = iconGO.AddComponent<Image>();
            previewImg.preserveAspect = true;
            RectTransform iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(160, 160);
            iconRt.anchoredPosition = new Vector2(0, 40);

            // Name label text
            GameObject nameTxtGO = CreateLabel("NameText", cardGO.transform, "HERO", 20, new Vector2(0, -90));
            nameTxtGO.GetComponent<Text>().color = Color.white;
            nameTxtGO.GetComponent<Text>().fontStyle = FontStyle.Bold;
            RectTransform ntRt = nameTxtGO.GetComponent<RectTransform>();
            ntRt.sizeDelta = new Vector2(240, 50);

            // Selection Checkmark overlay badge
            GameObject checkGO = new GameObject("Checkmark");
            checkGO.transform.SetParent(cardGO.transform, false);
            Image checkImg = checkGO.AddComponent<Image>();
            checkImg.sprite = goldMedalSprite;
            RectTransform checkRt = checkGO.GetComponent<RectTransform>();
            checkRt.sizeDelta = new Vector2(50, 50);
            checkRt.anchoredPosition = new Vector2(90, 130); // top right of card

            checkGO.SetActive(false); // active if skin is selected
        }

        heroesPanel.SetActive(false);

        // --- Theme Selection Interface ---

        // Grid Panel Container for the buttons
        GameObject themePanelGO = new GameObject("ThemeSelectorPanel");
        themePanelGO.transform.SetParent(panel.transform, false);
        themeSelectorPanelRef = themePanelGO;
        
        RectTransform tpRt = themePanelGO.AddComponent<RectTransform>();
        tpRt.sizeDelta = new Vector2(850, 200);
        tpRt.anchoredPosition = new Vector2(0, -250); // positioned just above the bottom menu bar

        // Add a Grid Layout Group
        GridLayoutGroup grid = themePanelGO.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(180, 70);
        grid.spacing = new Vector2(12, 12);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;

        ThemeSelectorUI selectorUI = themePanelGO.AddComponent<ThemeSelectorUI>();
        selectorUI.themeButtons = new Button[7];

        string[] themeNames = { "Classic", "Space", "Football", "Dragon", "Fish", "Bee", "Ninja" };
        for (int i = 0; i < 7; i++)
        {
            GameObject btnGO = new GameObject(themeNames[i] + "Button");
            btnGO.transform.SetParent(themePanelGO.transform, false);
            
            Image btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = buttonSprite; // pill button shape
            btnImg.type = Image.Type.Simple;
            btnImg.color = Color.white;

            Outline btnOutline = btnGO.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.38f, 0.15f, 0.02f); // normal dark brown border
            btnOutline.effectDistance = new Vector2(2f, -2f);

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            Text txt = txtGO.AddComponent<Text>();
            txt.text = themeNames[i].ToUpper();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            Outline txtOutline = txtGO.AddComponent<Outline>();
            txtOutline.effectColor = new Color(0.15f, 0.15f, 0.18f); // dark charcoal outline
            txtOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            Button btn = btnGO.AddComponent<Button>();
            selectorUI.themeButtons[i] = btn;
        }

        // --- Bottom Navigation Bar ---
        GameObject bottomBar = new GameObject("BottomNavBar");
        bottomBar.transform.SetParent(panel.transform, false);
        Image barImg = bottomBar.AddComponent<Image>();
        barImg.sprite = resultCardSprite;
        barImg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f); // sleek dark charcoal
        
        RectTransform barRt = bottomBar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.5f, 0f);
        barRt.anchorMax = new Vector2(0.5f, 0f);
        barRt.pivot = new Vector2(0.5f, 0.5f);
        barRt.sizeDelta = new Vector2(980, 150);
        barRt.anchoredPosition = new Vector2(0, 110); // float elegantly

        Shadow barShadow = bottomBar.AddComponent<Shadow>();
        barShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        barShadow.effectDistance = new Vector2(0f, -8f);

        // 1. Shop Button
        GameObject shopGO = new GameObject("ShopButton");
        shopGO.transform.SetParent(bottomBar.transform, false);
        Image shopImg = shopGO.AddComponent<Image>();
        shopImg.sprite = shopIcon;
        shopButton = shopGO.AddComponent<Button>();
        RectTransform shopRt = shopGO.GetComponent<RectTransform>();
        shopRt.sizeDelta = new Vector2(100, 100);
        shopRt.anchoredPosition = new Vector2(-340, 0);

        GameObject shopTxt = CreateLabel("Label", shopGO.transform, "SHOP", 18, new Vector2(0, -50));
        shopTxt.GetComponent<Text>().color = new Color(0.9f, 0.9f, 0.9f);

        // 2. Heroes Button
        GameObject heroesGO = new GameObject("HeroesButton");
        heroesGO.transform.SetParent(bottomBar.transform, false);
        Image heroesImg = heroesGO.AddComponent<Image>();
        heroesImg.sprite = heroesIcon;
        heroesButton = heroesGO.AddComponent<Button>();
        RectTransform heroesRt = heroesGO.GetComponent<RectTransform>();
        heroesRt.sizeDelta = new Vector2(100, 100);
        heroesRt.anchoredPosition = new Vector2(-170, 0);

        GameObject heroesTxt = CreateLabel("Label", heroesGO.transform, "HEROES", 18, new Vector2(0, -50));
        heroesTxt.GetComponent<Text>().color = new Color(0.9f, 0.9f, 0.9f);

        // 3. Play Button (large circular center)
        GameObject playGO = new GameObject("PlayButton");
        playGO.transform.SetParent(bottomBar.transform, false);
        Image playImg = playGO.AddComponent<Image>();
        playImg.sprite = resultCardSprite;
        playImg.color = new Color(0.98f, 0.82f, 0.12f); // yellow
        navPlayBtn = playGO.AddComponent<Button>();
        RectTransform playRt = playGO.GetComponent<RectTransform>();
        playRt.sizeDelta = new Vector2(170, 170);
        playRt.anchoredPosition = new Vector2(0, 20); // overlaps top

        Outline playOutline = playGO.AddComponent<Outline>();
        playOutline.effectColor = Color.white;
        playOutline.effectDistance = new Vector2(3f, -3f);

        GameObject playIconGO = new GameObject("Icon");
        playIconGO.transform.SetParent(playGO.transform, false);
        Image pIconImg = playIconGO.AddComponent<Image>();
        pIconImg.sprite = playIcon;
        playIconImageRef = pIconImg;
        RectTransform piRt = playIconGO.GetComponent<RectTransform>();
        piRt.sizeDelta = new Vector2(90, 90);
        piRt.anchoredPosition = new Vector2(6, 0); // visual centering offset

        // 4. Missions Button
        GameObject missionsGO = new GameObject("MissionsButton");
        missionsGO.transform.SetParent(bottomBar.transform, false);
        Image missionsImg = missionsGO.AddComponent<Image>();
        missionsImg.sprite = missionsIcon;
        missionsButton = missionsGO.AddComponent<Button>();
        RectTransform missionsRt = missionsGO.GetComponent<RectTransform>();
        missionsRt.sizeDelta = new Vector2(100, 100);
        missionsRt.anchoredPosition = new Vector2(170, 0);

        GameObject missionsTxt = CreateLabel("Label", missionsGO.transform, "QUESTS", 18, new Vector2(0, -50));
        missionsTxt.GetComponent<Text>().color = new Color(0.9f, 0.9f, 0.9f);

        // 5. Themes Button
        GameObject themesGO = new GameObject("ThemesButton");
        themesGO.transform.SetParent(bottomBar.transform, false);
        Image themesImg = themesGO.AddComponent<Image>();
        themesImg.sprite = themesIcon;
        themesButton = themesGO.AddComponent<Button>();
        RectTransform themesRt = themesGO.GetComponent<RectTransform>();
        themesRt.sizeDelta = new Vector2(100, 100);
        themesRt.anchoredPosition = new Vector2(340, 0);

        GameObject themesTxt = CreateLabel("Label", themesGO.transform, "WORLDS", 18, new Vector2(0, -50));
        themesTxt.GetComponent<Text>().color = new Color(0.9f, 0.9f, 0.9f);

        // --- Fading Toast Notification Panel ---
        toastPanel = new GameObject("ToastPanel");
        toastPanel.transform.SetParent(canvasTransform, false);
        Image toastImg = toastPanel.AddComponent<Image>();
        toastImg.sprite = resultCardSprite;
        toastImg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        
        Outline toastOutline = toastPanel.AddComponent<Outline>();
        toastOutline.effectColor = new Color(0.95f, 0.72f, 0.15f, 0.8f); // gold outline
        toastOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform toastRt = toastPanel.GetComponent<RectTransform>();
        toastRt.sizeDelta = new Vector2(650, 90);
        toastRt.anchoredPosition = new Vector2(0, 640); // high center

        GameObject toastLabel = CreateLabel("Text", toastPanel.transform, "ALERT MESSAGE", 26, Vector2.zero);
        Text toastLabelText = toastLabel.GetComponent<Text>();
        toastLabelText.color = Color.white;
        toastLabelText.fontStyle = FontStyle.Bold;
        
        RectTransform tlRt = toastLabel.GetComponent<RectTransform>();
        tlRt.anchorMin = Vector2.zero;
        tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = Vector2.zero;
        tlRt.offsetMax = Vector2.zero;

        CanvasGroup cg = toastPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        toastPanel.SetActive(false);

        return panel;
    }

    private static GameObject BuildGameOverPanel(Transform canvasTransform, Sprite buttonSprite, Sprite resultCardSprite, out Button menuButton, out Button retryButton, out GameObject scoreValueText, out GameObject bestValueText, out UnityEngine.UI.Image medalImage, out GameObject newBestBadge, out RectTransform resultCardTransform)
    {
        // Dusk dark transparent background
        GameObject panel = CreatePanel("GameOverPanel", canvasTransform, new Color(0.15f, 0.1f, 0.05f, 0.6f));

        // Bold punchy "GAME OVER" text at the top
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

        // 1. Unified Result Card
        GameObject resultCard = new GameObject("ResultCard");
        resultCard.transform.SetParent(panel.transform, false);
        Image cardImg = resultCard.AddComponent<Image>();
        cardImg.sprite = resultCardSprite;
        cardImg.type = Image.Type.Simple;
        resultCardTransform = resultCard.GetComponent<RectTransform>();
        resultCardTransform.sizeDelta = new Vector2(650, 380);
        resultCardTransform.anchoredPosition = new Vector2(0, 50);

        Shadow cardShadow = resultCard.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        cardShadow.effectDistance = new Vector2(6f, -6f);

        // 2. Medal slot on the left of Result Card
        GameObject medalGO = new GameObject("Medal");
        medalGO.transform.SetParent(resultCard.transform, false);
        medalImage = medalGO.AddComponent<Image>();
        RectTransform medalRt = medalGO.GetComponent<RectTransform>();
        medalRt.sizeDelta = new Vector2(130, 130);
        medalRt.anchoredPosition = new Vector2(-160, -20);
        medalGO.SetActive(true); // Always active so placeholder shows immediately

        // 3. SCORE section on the right
        GameObject scoreLabel = CreateLabel("ScoreLabel", resultCard.transform, "SCORE", 28, new Vector2(160, 95));
        Text slText = scoreLabel.GetComponent<Text>();
        slText.color = new Color(0.4f, 0.25f, 0.15f);
        Outline slOutline = scoreLabel.GetComponent<Outline>();
        if (slOutline != null) Object.DestroyImmediate(slOutline);

        GameObject scoreVal = CreateLabel("ScoreValue", resultCard.transform, "0", 56, new Vector2(160, 35));
        scoreValueText = scoreVal;
        Text svText = scoreVal.GetComponent<Text>();
        svText.color = Color.white;
        svText.fontStyle = FontStyle.Bold;

        // 4. BEST section on the right
        GameObject bestLabel = CreateLabel("BestLabel", resultCard.transform, "BEST", 28, new Vector2(160, -45));
        Text blText = bestLabel.GetComponent<Text>();
        blText.color = new Color(0.4f, 0.25f, 0.15f);
        Outline blOutline = bestLabel.GetComponent<Outline>();
        if (blOutline != null) Object.DestroyImmediate(blOutline);

        GameObject bestVal = CreateLabel("BestValue", resultCard.transform, "0", 56, new Vector2(160, -105));
        bestValueText = bestVal;
        Text bvText = bestVal.GetComponent<Text>();
        bvText.color = Color.white;
        bvText.fontStyle = FontStyle.Bold;

        // 5. NEW Best badge next to the BEST score value
        GameObject newBadge = new GameObject("NewBestBadge");
        newBadge.transform.SetParent(resultCard.transform, false);
        Image badgeImg = newBadge.AddComponent<Image>();
        badgeImg.color = new Color(0.95f, 0.25f, 0.15f); // Retro red capsule
        RectTransform badgeRt = newBadge.GetComponent<RectTransform>();
        badgeRt.sizeDelta = new Vector2(80, 36);
        badgeRt.anchoredPosition = new Vector2(265, -105);
        newBestBadge = newBadge;

        GameObject badgeText = CreateLabel("Text", newBadge.transform, "NEW", 20);
        Text btText = badgeText.GetComponent<Text>();
        btText.color = Color.white;
        btText.fontStyle = FontStyle.Bold;
        Outline btOutline = badgeText.GetComponent<Outline>();
        if (btOutline != null) Object.DestroyImmediate(btOutline);
        newBadge.SetActive(false);

        // MENU (left) and RETRY (right) buttons below the card
        GameObject menuGO = new GameObject("MenuButton");
        menuGO.transform.SetParent(panel.transform, false);
        Image menuImg = menuGO.AddComponent<Image>();
        ApplyButtonLook(menuImg, buttonSprite, new Color(0.55f, 0.6f, 0.65f));
        RectTransform menuRt = menuGO.GetComponent<RectTransform>();
        menuRt.sizeDelta = new Vector2(260, 100);
        menuRt.anchoredPosition = new Vector2(-150, -220); // lowered slightly to clear the taller Results card
        menuButton = menuGO.AddComponent<Button>();
        GameObject menuLabel = CreateLabel("Text", menuGO.transform, "MENU", 38);
        Text menuLabelText = menuLabel.GetComponent<Text>();
        menuLabelText.color = Color.white;
        Outline menuLabelOutline = menuLabel.GetComponent<Outline>();
        if (menuLabelOutline == null) menuLabelOutline = menuLabel.AddComponent<Outline>();
        menuLabelOutline.effectColor = new Color(0.35f, 0.15f, 0.02f, 1f);
        menuLabelOutline.effectDistance = new Vector2(3f, -3f);

        GameObject retryGO = new GameObject("RetryButton");
        retryGO.transform.SetParent(panel.transform, false);
        Image retryImg = retryGO.AddComponent<Image>();
        ApplyButtonLook(retryImg, buttonSprite, new Color(0.4f, 0.82f, 0.4f));
        RectTransform retryRt = retryGO.GetComponent<RectTransform>();
        retryRt.sizeDelta = new Vector2(260, 100);
        retryRt.anchoredPosition = new Vector2(150, -220);
        retryButton = retryGO.AddComponent<Button>();
        GameObject retryLabel = CreateLabel("Text", retryGO.transform, "RETRY", 38);
        Text retryLabelText = retryLabel.GetComponent<Text>();
        retryLabelText.color = Color.white;
        Outline retryLabelOutline = retryLabel.GetComponent<Outline>();
        if (retryLabelOutline == null) retryLabelOutline = retryLabel.AddComponent<Outline>();
        retryLabelOutline.effectColor = new Color(0.35f, 0.15f, 0.02f, 1f);
        retryLabelOutline.effectDistance = new Vector2(3f, -3f);

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
    private static GameObject CreateColoredTitle(string name, Transform parent, string content, int fontSize, Vector2 centerPos)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = centerPos;
        rootRt.sizeDelta = new Vector2(1000, 200);

        Text text = root.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.95f, 0.55f, 0.1f); // Classic premium golden-orange

        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.18f, 0.22f, 1f); // Charcoal outline
        outline.effectDistance = new Vector2(4f, -4f);

        Shadow shadow = root.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        shadow.effectDistance = new Vector2(3f, -5f);

        return root;
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

    private static GameObject BuildGameManager(GameObject bird, GameObject pipeSpawnerGO, GameObject scoreTextGO, GameObject startPanel, GameObject gameOverPanel, AudioClip clickClip, AudioClip scoreClip, GameObject gameOverScoreText, GameObject gameOverBestText, GameObject startHighScoreText, UnityEngine.UI.Image medalImage, Sprite bronzeSprite, Sprite silverSprite, Sprite goldSprite, Sprite platinumSprite, Sprite placeholderSprite, GameObject newBestBadge, RectTransform resultCardTransform)
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
        gm.startHighScoreText = startHighScoreText;
        gm.medalImage = medalImage;
        gm.bronzeMedal = bronzeSprite;
        gm.silverMedal = silverSprite;
        gm.goldMedal = goldSprite;
        gm.platinumMedal = platinumSprite;
        gm.medalPlaceholder = placeholderSprite;
        gm.newBestBadge = newBestBadge;
        gm.resultCardTransform = resultCardTransform;

        // Attach ThemeApplier
        go.AddComponent<ThemeApplier>();

        // Add ThemeObstacleApplier to the generated prefab so newly spawned pipes apply the theme
        string prefabPath = $"{PrefabFolder}/PipePair.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            if (prefab.GetComponent<ThemeObstacleApplier>() == null)
            {
                prefab.AddComponent<ThemeObstacleApplier>();
                EditorUtility.SetDirty(prefab);
            }
        }

        return go;
    }

    private static Texture2D GenerateMedalTexture(int width, int height, Color baseColor, Color highlightColor)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color border = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        Vector2 center = new Vector2(width / 2f, height / 2f);
        float radius = width * 0.45f;
        float innerRadius = radius * 0.8f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float dist = Vector2.Distance(p, center);
                Color pixel = clear;

                if (dist <= radius)
                {
                    pixel = border;

                    if (dist <= innerRadius)
                    {
                        // Diagonal metallic gradient
                        float t = (x + y) / (float)(width + height);
                        pixel = Color.Lerp(baseColor, highlightColor, t);

                        // Shiny highlights
                        float diag = Mathf.Abs((x - y) / (float)width);
                        if (diag < 0.1f)
                        {
                            pixel = Color.Lerp(pixel, Color.white, 0.4f * (1f - diag / 0.1f));
                        }

                        // Center inner circle region
                        float centerDist = Vector2.Distance(p, center);
                        if (centerDist <= innerRadius * 0.5f)
                        {
                            pixel = Color.Lerp(pixel, highlightColor, 0.2f);
                        }
                    }
                }

                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        return tex;
    }

    private static void CreateSynthAudioFiles()
    {
        string audioDir = "Assets/Audio";
        if (!Directory.Exists(audioDir))
        {
            Directory.CreateDirectory(audioDir);
        }

        int sampleRate = 44100;

        // 1. ButtonClick.wav
        float clickDur = 0.05f;
        float[] clickSamples = GenerateSineSweep(sampleRate, clickDur, 600f, 400f, 0.4f, true);
        SaveWav(Path.Combine(audioDir, "ButtonClick.wav"), clickSamples, sampleRate);

        // 2. Flap.wav
        float flapDur = 0.12f;
        float[] flapSamples = GenerateSineSweep(sampleRate, flapDur, 350f, 750f, 0.4f, true);
        SaveWav(Path.Combine(audioDir, "Flap.wav"), flapSamples, sampleRate);

        // 3. Score.wav
        float[] scoreSamples = GenerateCoinSound(sampleRate);
        SaveWav(Path.Combine(audioDir, "Score.wav"), scoreSamples, sampleRate);

        // 4. Hit.wav
        float[] hitSamples = GenerateExplosionSound(sampleRate, 0.18f, 300f, 60f, 0.7f);
        SaveWav(Path.Combine(audioDir, "Hit.wav"), hitSamples, sampleRate);

        // 5. Land.wav
        float[] landSamples = GenerateThudSound(sampleRate, 0.25f, 150f, 40f, 0.8f);
        SaveWav(Path.Combine(audioDir, "Land.wav"), landSamples, sampleRate);
    }

    private static float[] GenerateSineSweep(int sampleRate, float duration, float startFreq, float endFreq, float maxVolume, bool linearDecay)
    {
        int count = (int)(sampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += 2f * Mathf.PI * freq / sampleRate;
            float vol = linearDecay ? Mathf.Lerp(maxVolume, 0f, t) : maxVolume;
            samples[i] = Mathf.Sin(phase) * vol;
        }
        return samples;
    }

    private static float[] GenerateCoinSound(int sampleRate)
    {
        float dur1 = 0.07f;
        float dur2 = 0.22f;
        int count1 = (int)(sampleRate * dur1);
        int count2 = (int)(sampleRate * dur2);
        float[] samples = new float[count1 + count2];

        float phase = 0f;
        // Tone 1: 950Hz
        for (int i = 0; i < count1; i++)
        {
            float t = (float)i / count1;
            phase += 2f * Mathf.PI * 950f / sampleRate;
            samples[i] = Mathf.Sin(phase) * 0.35f;
        }

        // Tone 2: 1300Hz
        for (int i = 0; i < count2; i++)
        {
            float t = (float)i / count2;
            phase += 2f * Mathf.PI * 1300f / sampleRate;
            float vol = Mathf.Lerp(0.35f, 0f, t);
            samples[count1 + i] = Mathf.Sin(phase) * vol;
        }
        return samples;
    }

    private static float[] GenerateExplosionSound(int sampleRate, float duration, float startFreq, float endFreq, float volume)
    {
        int count = (int)(sampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;
        System.Random rng = new System.Random(101);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float signal = Mathf.Sin(phase) * 0.5f + noise * 0.5f;

            float envelope = Mathf.Exp(-5f * t) * volume;
            samples[i] = signal * envelope;
        }
        return samples;
    }

    private static float[] GenerateThudSound(int sampleRate, float duration, float startFreq, float endFreq, float volume)
    {
        int count = (int)(sampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;
        System.Random rng = new System.Random(202);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float signal = Mathf.Sin(phase) * 0.8f + noise * 0.2f;

            float envelope = (1f - t) * volume; // linear decay
            samples[i] = signal * envelope;
        }
        return samples;
    }

    private static void SaveWav(string path, float[] samples, int sampleRate)
    {
        using (FileStream fs = new FileStream(path, FileMode.Create))
        {
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                int byteLength = samples.Length * 2; // 16-bit PCM

                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + byteLength);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);

                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(byteLength);

                for (int i = 0; i < samples.Length; i++)
                {
                    short s = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    bw.Write(s);
                }
            }
        }
    }

    private static Texture2D GenerateMedalPlaceholderTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color goldDashed = new Color(0.85f, 0.65f, 0.2f, 0.6f); // Premium gold dashed outline
        Color starColor = new Color(0.85f, 0.65f, 0.2f, 0.22f);  // Soft gold star fill

        Vector2 center = new Vector2(width / 2f, height / 2f);
        float radius = width * 0.45f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                Color pixel = clear;

                if (dist <= radius)
                {
                    if (dist >= radius - 4f)
                    {
                        // Dashed ring (12 segments around the circle)
                        float angle = Mathf.Atan2(y - center.y, x - center.x) * Mathf.Rad2Deg;
                        if (angle < 0) angle += 360f;
                        if ((angle % 30f) < 18f)
                        {
                            pixel = goldDashed;
                        }
                    }
                    else
                    {
                        // Draw a star shape in the center of the slot
                        if (IsPointInStar(x - center.x, y - center.y, radius * 0.55f))
                        {
                            pixel = starColor;
                        }
                    }
                }
                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        return tex;
    }

    private static bool IsPointInStar(float x, float y, float r)
    {
        float d = Mathf.Sqrt(x * x + y * y);
        if (d > r) return false;
        if (d < r * 0.4f) return true; // Inner core is solid

        float angle = Mathf.Atan2(y, x);
        if (angle < 0) angle += 2f * Mathf.PI;

        // 5-pointed symmetry
        float section = (2f * Mathf.PI) / 5f;
        float localAngle = angle % section;
        if (localAngle > section / 2f) localAngle = section - localAngle;

        float maxD = Mathf.Lerp(r, r * 0.4f, localAngle / (section / 2f));
        return d <= maxD;
    }

    // ---------- Theme System Support ----------

    private static ThemeData[] EnsureThemeSystem()
    {
        string folder = "Assets/ScriptableObjects/Themes";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Themes");
        }

        string[] names = { "Classic", "Space", "Football", "Dragon", "Fish", "Bee", "Ninja" };
        ThemeData[] themeAssets = new ThemeData[7];

        // Ensure directories for Sprites
        for (int i = 0; i < 7; i++)
        {
            string spriteDir = $"{SpriteFolder}/{names[i]}";
            if (!AssetDatabase.IsValidFolder(spriteDir))
            {
                AssetDatabase.CreateFolder(SpriteFolder, names[i]);
            }
        }

        for (int i = 0; i < 7; i++)
        {
            string path = $"{folder}/{names[i]}Theme.asset";
            ThemeData data = AssetDatabase.LoadAssetAtPath<ThemeData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ThemeData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.themeName = names[i];
            
            PopulateThemeAssets(data, i, names[i]);
            
            EditorUtility.SetDirty(data);
            themeAssets[i] = data;
        }
        AssetDatabase.SaveAssets();
        return themeAssets;
    }

    private static void PopulateThemeAssets(ThemeData data, int index, string name)
    {
        if (index == 0) // Classic
        {
            data.themeColor = new Color(0.53f, 0.81f, 0.92f); // Sky blue
            
            data.playerSprites = new Sprite[3];
            data.playerSprites[0] = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Bird_Up_0.png");
            data.playerSprites[1] = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Bird_Mid_0.png");
            data.playerSprites[2] = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Bird_Down_0.png");

            data.obstacleTopSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/PipeBody.png");
            data.obstacleBottomSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/PipeBody.png");
            data.obstacleTopCapSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/PipeCap.png");
            data.obstacleBottomCapSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/PipeCap.png");

            data.backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Background.png");
            data.groundDirtSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/GroundDirt.png");
            data.groundGrassSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Grass.png");

            data.flapSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Flap.wav");
            data.scoreSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Score.wav");
            data.hitSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Hit.wav");
            return;
        }

        // Setup theme colors
        if (index == 1) data.themeColor = new Color(0.04f, 0.04f, 0.12f); // Space dark
        else if (index == 2) data.themeColor = new Color(0.18f, 0.55f, 0.34f); // Football green
        else if (index == 3) data.themeColor = new Color(0.25f, 0.08f, 0.08f); // Dragon red
        else if (index == 4) data.themeColor = new Color(0.08f, 0.25f, 0.45f); // Fish teal
        else if (index == 5) data.themeColor = new Color(0.78f, 0.72f, 0.38f); // Bee yellow
        else if (index == 6) data.themeColor = new Color(0.1f, 0.1f, 0.15f); // Ninja black

        // Generate 3 unique skins for this theme
        data.playerSprites = new Sprite[3];
        for (int s = 0; s < 3; s++)
        {
            data.playerSprites[s] = GetOrCreateSprite($"{name}/Player_{s}", 320, 320, (w, h) => GenerateThemeSkinTexture(index, s, w, h), 320);
        }

        data.obstacleTopSprite = GetOrCreateSprite($"{name}/ObstacleTop", 256, 256, (w, h) => GenerateThemeTexture("ObstacleBody", index, w, h), 256, FilterMode.Point);
        data.obstacleBottomSprite = data.obstacleTopSprite;

        if (index == 2) // Football goals don't use caps
        {
            data.obstacleTopCapSprite = null;
            data.obstacleBottomCapSprite = null;
        }
        else
        {
            data.obstacleTopCapSprite = GetOrCreateSprite($"{name}/ObstacleCap", 256, 256, (w, h) => GenerateThemeTexture("ObstacleCap", index, w, h), 256, FilterMode.Point);
            data.obstacleBottomCapSprite = data.obstacleTopCapSprite;
        }

        data.backgroundSprite = GetOrCreateSprite($"{name}/Background", 1536, 864, (w, h) => GenerateThemeTexture("Background", index, w, h), 1536f / 20f, FilterMode.Point);
        data.groundDirtSprite = GetOrCreateSprite($"{name}/GroundDirt", 256, 256, (w, h) => GenerateThemeTexture("GroundDirt", index, w, h), 256, FilterMode.Point, TextureWrapMode.Repeat);
        data.groundGrassSprite = GetOrCreateSprite($"{name}/GroundGrass", 256, 256, (w, h) => GenerateThemeTexture("GroundGrass", index, w, h), 512, FilterMode.Point, TextureWrapMode.Repeat);

        // Fallbacks for sounds (use original classic clips)
        data.flapSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Flap.wav");
        data.scoreSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Score.wav");
        data.hitSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Hit.wav");
    }

    private static Texture2D GenerateThemeTexture(string type, int index, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, clear);

        Vector2 center = new Vector2(width / 2f, height / 2f);

        if (type == "Player")
        {
            if (index == 1) // Space (Rocket)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        if (IsInsideEllipse(p, new Vector2(width * 0.45f, height * 0.5f), width * 0.28f, height * 0.16f))
                        {
                            tex.SetPixel(x, y, new Color(0.75f, 0.75f, 0.8f)); // steel body
                        }
                        Vector2 a = new Vector2(width * 0.7f, height * 0.66f);
                        Vector2 b = new Vector2(width * 0.7f, height * 0.34f);
                        Vector2 c = new Vector2(width * 0.95f, height * 0.5f);
                        if (PointInTriangle(p, a, b, c))
                        {
                            tex.SetPixel(x, y, new Color(0.9f, 0.15f, 0.15f));
                        }
                        Vector2 fta = new Vector2(width * 0.25f, height * 0.66f);
                        Vector2 ftb = new Vector2(width * 0.15f, height * 0.82f);
                        Vector2 ftc = new Vector2(width * 0.35f, height * 0.5f);
                        if (PointInTriangle(p, fta, ftb, ftc))
                        {
                            tex.SetPixel(x, y, new Color(0.9f, 0.15f, 0.15f));
                        }
                        Vector2 fba = new Vector2(width * 0.25f, height * 0.34f);
                        Vector2 fbb = new Vector2(width * 0.15f, height * 0.18f);
                        Vector2 fbc = new Vector2(width * 0.35f, height * 0.5f);
                        if (PointInTriangle(p, fba, fbb, fbc))
                        {
                            tex.SetPixel(x, y, new Color(0.9f, 0.15f, 0.15f));
                        }
                        if (Vector2.Distance(p, new Vector2(width * 0.48f, height * 0.5f)) <= width * 0.06f)
                        {
                            tex.SetPixel(x, y, new Color(0.3f, 0.6f, 0.9f));
                        }
                        if (x < width * 0.18f && y > height * 0.42f && y < height * 0.58f)
                        {
                            tex.SetPixel(x, y, new Color(0.95f, 0.55f, 0.05f));
                        }
                    }
                }
            }
            else if (index == 2) // Football
            {
                float r = width * 0.35f;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        float dist = Vector2.Distance(p, center);
                        if (dist <= r)
                        {
                            float checker = Mathf.Sin(x * 0.15f) * Mathf.Sin(y * 0.15f);
                            if (checker > 0.1f || dist > r - 6f)
                            {
                                tex.SetPixel(x, y, new Color(0.12f, 0.12f, 0.12f));
                            }
                            else
                            {
                                tex.SetPixel(x, y, Color.white);
                            }
                        }
                    }
                }
            }
            else if (index == 3) // Dragon
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        if (IsInsideEllipse(p, new Vector2(width * 0.45f, height * 0.48f), width * 0.28f, height * 0.22f))
                        {
                            tex.SetPixel(x, y, new Color(0.85f, 0.2f, 0.2f));
                        }
                        if (IsInsideEllipse(p, new Vector2(width * 0.3f, height * 0.65f), width * 0.18f, height * 0.22f))
                        {
                            tex.SetPixel(x, y, new Color(0.95f, 0.55f, 0.1f));
                        }
                        if (x < width * 0.25f && y > height * 0.35f && y < height * 0.55f)
                        {
                            tex.SetPixel(x, y, new Color(0.85f, 0.2f, 0.2f));
                        }
                        if (Vector2.Distance(p, new Vector2(width * 0.62f, height * 0.54f)) <= width * 0.04f)
                        {
                            tex.SetPixel(x, y, Color.yellow);
                        }
                    }
                }
            }
            else if (index == 4) // Fish
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        if (IsInsideEllipse(p, new Vector2(width * 0.55f, height * 0.5f), width * 0.28f, height * 0.2f))
                        {
                            tex.SetPixel(x, y, new Color(0.95f, 0.45f, 0.1f));
                        }
                        Vector2 ta = new Vector2(width * 0.38f, height * 0.5f);
                        Vector2 tb = new Vector2(width * 0.15f, height * 0.72f);
                        Vector2 tc = new Vector2(width * 0.15f, height * 0.28f);
                        if (PointInTriangle(p, ta, tb, tc))
                        {
                            tex.SetPixel(x, y, new Color(0.95f, 0.65f, 0.15f));
                        }
                        if (Vector2.Distance(p, new Vector2(width * 0.68f, height * 0.56f)) <= width * 0.05f)
                        {
                            tex.SetPixel(x, y, Color.white);
                        }
                        if (Vector2.Distance(p, new Vector2(width * 0.7f, height * 0.56f)) <= width * 0.02f)
                        {
                            tex.SetPixel(x, y, Color.black);
                        }
                    }
                }
            }
            else if (index == 5) // Bee
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        if (IsInsideEllipse(p, new Vector2(width * 0.48f, height * 0.48f), width * 0.28f, height * 0.2f))
                        {
                            int stripe = (x / 25) % 2;
                            if (stripe == 0)
                            {
                                tex.SetPixel(x, y, new Color(0.95f, 0.85f, 0.1f));
                            }
                            else
                            {
                                tex.SetPixel(x, y, new Color(0.1f, 0.1f, 0.1f));
                            }
                        }
                        if (IsInsideEllipse(p, new Vector2(width * 0.4f, height * 0.7f), width * 0.12f, height * 0.22f))
                        {
                            tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.75f));
                        }
                    }
                }
            }
            else if (index == 6) // Ninja
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        if (Vector2.Distance(p, center) <= width * 0.32f)
                        {
                            tex.SetPixel(x, y, new Color(0.12f, 0.12f, 0.15f));
                            if (IsInsideEllipse(p, new Vector2(width * 0.56f, height * 0.54f), width * 0.14f, height * 0.08f))
                            {
                                tex.SetPixel(x, y, new Color(0.98f, 0.82f, 0.72f));
                                if (Vector2.Distance(p, new Vector2(width * 0.52f, height * 0.54f)) <= 3f ||
                                    Vector2.Distance(p, new Vector2(width * 0.6f, height * 0.54f)) <= 3f)
                                {
                                    tex.SetPixel(x, y, Color.black);
                                }
                            }
                        }
                        if (x > width * 0.1f && x < width * 0.25f && y > height * 0.44f && y < height * 0.54f)
                        {
                            tex.SetPixel(x, y, new Color(0.85f, 0.1f, 0.1f));
                        }
                    }
                }
            }
        }
        else if (type == "Background")
        {
            Color bottomColor = Color.black;
            Color topColor = Color.black;

            if (index == 1) { bottomColor = new Color(0.04f, 0.04f, 0.14f); topColor = new Color(0.01f, 0.01f, 0.04f); }
            else if (index == 2) { bottomColor = new Color(0.18f, 0.55f, 0.34f); topColor = new Color(0.05f, 0.15f, 0.1f); }
            else if (index == 3) { bottomColor = new Color(0.45f, 0.08f, 0.08f); topColor = new Color(0.1f, 0.04f, 0.04f); }
            else if (index == 4) { bottomColor = new Color(0.08f, 0.28f, 0.52f); topColor = new Color(0.04f, 0.12f, 0.25f); }
            else if (index == 5) { bottomColor = new Color(0.55f, 0.85f, 0.35f); topColor = new Color(0.48f, 0.81f, 0.95f); }
            else if (index == 6) { bottomColor = new Color(0.08f, 0.08f, 0.14f); topColor = new Color(0.02f, 0.02f, 0.05f); }

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                Color col = Color.Lerp(bottomColor, topColor, t);
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, col);
                    if (index == 1)
                    {
                        if ((x * 17 + y * 23) % 401 == 0 && (x % 3 == 0))
                        {
                            tex.SetPixel(x, y, Color.white);
                        }
                    }
                    else if (index == 2)
                    {
                        if (y > height * 0.85f && (x % 64 < 12))
                        {
                            tex.SetPixel(x, y, Color.white);
                        }
                    }
                    else if (index == 6)
                    {
                        if (y < height * 0.3f && (x * 13 + y * 7) % 199 == 0)
                        {
                            tex.SetPixel(x, y, new Color(0.95f, 0.85f, 0.2f));
                        }
                    }
                }
            }
        }
        else if (type == "GroundDirt")
        {
            Color dirtColor = Color.grey;
            if (index == 1) dirtColor = new Color(0.12f, 0.12f, 0.16f);
            else if (index == 2) dirtColor = new Color(0.38f, 0.22f, 0.08f);
            else if (index == 3) dirtColor = new Color(0.18f, 0.08f, 0.08f);
            else if (index == 4) dirtColor = new Color(0.85f, 0.72f, 0.45f);
            else if (index == 5) dirtColor = new Color(0.28f, 0.15f, 0.05f);
            else if (index == 6) dirtColor = new Color(0.25f, 0.16f, 0.1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color col = dirtColor;
                    if ((x + y * 7) % 19 == 0) col *= 0.85f;
                    tex.SetPixel(x, y, col);
                }
            }
        }
        else if (type == "GroundGrass")
        {
            Color grassColor = Color.green;
            if (index == 1) grassColor = new Color(0f, 0.95f, 0.95f);
            else if (index == 2) grassColor = Color.white;
            else if (index == 3) grassColor = new Color(0.95f, 0.25f, 0.05f);
            else if (index == 4) grassColor = new Color(0.1f, 0.65f, 0.35f);
            else if (index == 5) grassColor = new Color(0.95f, 0.85f, 0.15f);
            else if (index == 6) grassColor = new Color(0.45f, 0.45f, 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y > height - 12)
                    {
                        tex.SetPixel(x, y, grassColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                    }
                }
            }
        }
        else if (type == "ObstacleBody")
        {
            Color col = Color.green;
            if (index == 1) col = new Color(0.35f, 0.35f, 0.4f);
            else if (index == 2) col = Color.white;
            else if (index == 3) col = new Color(0.2f, 0.2f, 0.2f);
            else if (index == 4) col = new Color(0.85f, 0.35f, 0.5f);
            else if (index == 5) col = new Color(0.12f, 0.45f, 0.15f);
            else if (index == 6) col = new Color(0.38f, 0.25f, 0.15f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pCol = col;
                    if (x < 12 || x > width - 12) pCol *= 0.7f;
                    if (index == 1 && x > width / 2 - 8 && x < width / 2 + 8)
                    {
                        pCol = new Color(0f, 0.95f, 0.95f);
                    }
                    else if (index == 3 && (y % 32 < 4))
                    {
                        pCol = new Color(0.95f, 0.25f, 0.05f);
                    }
                    tex.SetPixel(x, y, pCol);
                }
            }
        }
        else if (type == "ObstacleCap")
        {
            Color col = Color.green;
            if (index == 1) col = new Color(0.45f, 0.45f, 0.5f);
            else if (index == 2) col = Color.white;
            else if (index == 3) col = new Color(0.3f, 0.3f, 0.3f);
            else if (index == 4) col = new Color(0.95f, 0.45f, 0.6f);
            else if (index == 5) col = new Color(0.95f, 0.72f, 0.1f);
            else if (index == 6) col = new Color(0.55f, 0.55f, 0.55f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pCol = col;
                    if (x < 8 || x > width - 8 || y < 8 || y > height - 8) pCol *= 0.6f;
                    if (index == 6)
                    {
                        Vector2 a = new Vector2(0, 0);
                        Vector2 b = new Vector2(width, 0);
                        Vector2 c = new Vector2(width / 2f, height);
                        if (!PointInTriangle(new Vector2(x, y), a, b, c))
                        {
                            pCol = clear;
                        }
                    }
                    tex.SetPixel(x, y, pCol);
                }
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateThemeSkinTexture(int index, int skin, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, clear);

        Vector2 center = new Vector2(width / 2f, height / 2f);

        if (index == 1) // Space
        {
            if (skin == 0) // Rocket
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        if (IsInsideEllipse(p, new Vector2(width * 0.45f, height * 0.5f), width * 0.28f, height * 0.16f))
                            tex.SetPixel(x, y, new Color(0.75f, 0.75f, 0.8f)); // steel body

                        // Red nose cone
                        Vector2 a = new Vector2(width * 0.7f, height * 0.66f);
                        Vector2 b = new Vector2(width * 0.7f, height * 0.34f);
                        Vector2 c = new Vector2(width * 0.95f, height * 0.5f);
                        if (PointInTriangle(p, a, b, c)) tex.SetPixel(x, y, new Color(0.9f, 0.15f, 0.15f));

                        // Engine fins
                        Vector2 fta = new Vector2(width * 0.25f, height * 0.66f);
                        Vector2 ftb = new Vector2(width * 0.15f, height * 0.82f);
                        Vector2 ftc = new Vector2(width * 0.35f, height * 0.5f);
                        if (PointInTriangle(p, fta, ftb, ftc)) tex.SetPixel(x, y, new Color(0.9f, 0.15f, 0.15f));

                        Vector2 fba = new Vector2(width * 0.25f, height * 0.34f);
                        Vector2 fbb = new Vector2(width * 0.15f, height * 0.18f);
                        Vector2 fbc = new Vector2(width * 0.35f, height * 0.5f);
                        if (PointInTriangle(p, fba, fbb, fbc)) tex.SetPixel(x, y, new Color(0.9f, 0.15f, 0.15f));

                        // Window
                        if (Vector2.Distance(p, new Vector2(width * 0.48f, height * 0.5f)) <= width * 0.06f)
                            tex.SetPixel(x, y, new Color(0.3f, 0.6f, 0.9f));
                    }
                }
            }
            else if (skin == 1) // Cosmic UFO
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        if (IsInsideEllipse(p, center, width * 0.38f, height * 0.12f))
                            tex.SetPixel(x, y, new Color(0.6f, 0.85f, 0.6f)); // green dome

                        if (Vector2.Distance(p, new Vector2(width * 0.5f, height * 0.5f)) <= width * 0.18f && y >= height * 0.5f)
                            tex.SetPixel(x, y, new Color(0.3f, 0.8f, 0.95f, 0.8f)); // dome glass
                        
                        if (y > height * 0.4f && y < height * 0.46f && (x % 32 < 8) && IsInsideEllipse(p, center, width * 0.38f, height * 0.12f))
                            tex.SetPixel(x, y, Color.yellow);
                    }
                }
            }
            else if (skin == 2) // Satellite
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        if (x > width * 0.38f && x < width * 0.62f && y > height * 0.38f && y < height * 0.62f)
                            tex.SetPixel(x, y, new Color(0.85f, 0.85f, 0.85f));

                        if ((x > width * 0.15f && x < width * 0.35f && y > height * 0.44f && y < height * 0.56f) ||
                            (x > width * 0.65f && x < width * 0.85f && y > height * 0.44f && y < height * 0.56f))
                        {
                            tex.SetPixel(x, y, new Color(0.12f, 0.3f, 0.8f));
                            if (x % 16 < 3 || y % 16 < 3) tex.SetPixel(x, y, Color.white);
                        }
                    }
                }
            }
        }
        else if (index == 2) // Football
        {
            float r = width * 0.35f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float dist = Vector2.Distance(p, center);
                    if (dist <= r)
                    {
                        if (skin == 0) // Soccer Ball
                        {
                            float checker = Mathf.Sin(x * 0.15f) * Mathf.Sin(y * 0.15f);
                            tex.SetPixel(x, y, (checker > 0.1f || dist > r - 6f) ? new Color(0.12f, 0.12f, 0.12f) : Color.white);
                        }
                        else if (skin == 1) // Basketball
                        {
                            Color orange = new Color(0.9f, 0.4f, 0.08f);
                            bool onCurve = Mathf.Abs(p.x - center.x - Mathf.Sin((p.y - center.y) * 0.05f) * 20f) < 4f ||
                                           Mathf.Abs(p.y - center.y) < 4f || dist > r - 5f;
                            tex.SetPixel(x, y, onCurve ? Color.black : orange);
                        }
                        else if (skin == 2) // Tennis Ball
                        {
                            Color lime = new Color(0.75f, 0.95f, 0.15f);
                            bool onSeam = Mathf.Abs(Mathf.Sin((p.x - p.y) * 0.04f) * 18f - (p.y - center.y)) < 4f || dist > r - 5f;
                            tex.SetPixel(x, y, onSeam ? Color.white : lime);
                        }
                    }
                }
            }
        }
        else if (index == 3) // Dragon
        {
            Color bodyCol = (skin == 0) ? new Color(0.85f, 0.2f, 0.2f) : (skin == 1 ? new Color(0.15f, 0.72f, 0.2f) : new Color(0.95f, 0.8f, 0.1f));
            Color wingCol = (skin == 0) ? new Color(0.95f, 0.55f, 0.1f) : (skin == 1 ? new Color(0.95f, 0.85f, 0.2f) : new Color(0.6f, 0.15f, 0.65f));
            Color eyeCol = (skin == 0) ? Color.yellow : (skin == 1 ? Color.red : Color.cyan);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (IsInsideEllipse(p, new Vector2(width * 0.45f, height * 0.48f), width * 0.28f, height * 0.22f))
                        tex.SetPixel(x, y, bodyCol);

                    if (IsInsideEllipse(p, new Vector2(width * 0.3f, height * 0.65f), width * 0.18f, height * 0.22f))
                        tex.SetPixel(x, y, wingCol);

                    if (x < width * 0.25f && y > height * 0.35f && y < height * 0.55f)
                        tex.SetPixel(x, y, bodyCol);

                    if (Vector2.Distance(p, new Vector2(width * 0.62f, height * 0.54f)) <= width * 0.04f)
                        tex.SetPixel(x, y, eyeCol);
                }
            }
        }
        else if (index == 4) // Fish
        {
            Color fishCol = (skin == 0) ? new Color(0.95f, 0.45f, 0.1f) : (skin == 1 ? new Color(0.48f, 0.55f, 0.6f) : new Color(0.95f, 0.6f, 0.75f));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (skin == 2)
                    {
                        if (x > width * 0.32f && x < width * 0.68f && y < height * 0.5f && (x % 16 < 4))
                        {
                            tex.SetPixel(x, y, new Color(0.95f, 0.6f, 0.75f, 0.8f));
                        }
                    }

                    if (IsInsideEllipse(p, new Vector2(width * 0.55f, height * 0.52f), width * 0.28f, height * 0.2f))
                        tex.SetPixel(x, y, fishCol);

                    if (skin < 2)
                    {
                        Vector2 ta = new Vector2(width * 0.38f, height * 0.5f);
                        Vector2 tb = new Vector2(width * 0.15f, height * 0.72f);
                        Vector2 tc = new Vector2(width * 0.15f, height * 0.28f);
                        if (PointInTriangle(p, ta, tb, tc)) tex.SetPixel(x, y, fishCol * 1.15f);
                    }

                    if (Vector2.Distance(p, new Vector2(width * 0.68f, height * 0.56f)) <= width * 0.05f)
                        tex.SetPixel(x, y, Color.white);
                    if (Vector2.Distance(p, new Vector2(width * 0.7f, height * 0.56f)) <= width * 0.02f)
                        tex.SetPixel(x, y, Color.black);
                }
            }
        }
        else if (index == 5) // Bee
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (skin == 0)
                    {
                        if (IsInsideEllipse(p, new Vector2(width * 0.48f, height * 0.48f), width * 0.28f, height * 0.2f))
                        {
                            int stripe = (x / 25) % 2;
                            tex.SetPixel(x, y, (stripe == 0) ? new Color(0.95f, 0.85f, 0.1f) : new Color(0.1f, 0.1f, 0.1f));
                        }
                        if (IsInsideEllipse(p, new Vector2(width * 0.4f, height * 0.7f), width * 0.12f, height * 0.22f))
                            tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.75f));
                    }
                    else if (skin == 1)
                    {
                        if (Vector2.Distance(p, center) <= width * 0.34f)
                        {
                            Color ladyCol = Color.red;
                            if (Vector2.Distance(p, new Vector2(width * 0.42f, height * 0.62f)) <= 5f ||
                                Vector2.Distance(p, new Vector2(width * 0.58f, height * 0.38f)) <= 5f ||
                                Vector2.Distance(p, new Vector2(width * 0.5f, height * 0.5f)) <= 6f ||
                                Mathf.Abs(p.x - center.x) < 2f)
                            {
                                ladyCol = Color.black;
                            }
                            tex.SetPixel(x, y, ladyCol);
                        }
                    }
                    else if (skin == 2)
                    {
                        if (IsInsideEllipse(p, center, width * 0.08f, height * 0.32f))
                            tex.SetPixel(x, y, new Color(0.15f, 0.15f, 0.15f));

                        if (IsInsideEllipse(p, new Vector2(width * 0.3f, height * 0.54f), width * 0.22f, height * 0.28f) ||
                            IsInsideEllipse(p, new Vector2(width * 0.7f, height * 0.54f), width * 0.22f, height * 0.28f))
                        {
                            if (tex.GetPixel(x, y).a == 0f) tex.SetPixel(x, y, new Color(0.95f, 0.4f, 0.85f));
                        }
                    }
                }
            }
        }
        else if (index == 6) // Ninja
        {
            Color grabCol = (skin == 0) ? new Color(0.12f, 0.12f, 0.15f) : (skin == 1 ? new Color(0.85f, 0.1f, 0.1f) : new Color(0.9f, 0.9f, 0.95f));
            Color bandCol = (skin == 0) ? new Color(0.85f, 0.1f, 0.1f) : (skin == 1 ? new Color(0.12f, 0.12f, 0.15f) : new Color(0.1f, 0.4f, 0.8f));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (Vector2.Distance(p, center) <= width * 0.32f)
                    {
                        tex.SetPixel(x, y, grabCol);
                        if (IsInsideEllipse(p, new Vector2(width * 0.56f, height * 0.54f), width * 0.14f, height * 0.08f))
                        {
                            tex.SetPixel(x, y, new Color(0.98f, 0.82f, 0.72f));
                            if (Vector2.Distance(p, new Vector2(width * 0.52f, height * 0.54f)) <= 3f ||
                                Vector2.Distance(p, new Vector2(width * 0.6f, height * 0.54f)) <= 3f)
                            {
                                tex.SetPixel(x, y, Color.black);
                            }
                        }
                    }
                    if (x > width * 0.1f && x < width * 0.25f && y > height * 0.44f && y < height * 0.54f)
                    {
                        tex.SetPixel(x, y, bandCol);
                    }
                }
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateIconTexture(string name, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

        if (name == "Home")
        {
            Color col = Color.white;
            Vector2 a = new Vector2(w * 0.5f, h * 0.85f);
            Vector2 b = new Vector2(w * 0.15f, h * 0.5f);
            Vector2 c = new Vector2(w * 0.85f, h * 0.5f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (PointInTriangle(p, a, b, c)) tex.SetPixel(x, y, col);
                    if (x > w * 0.25f && x < w * 0.75f && y > h * 0.15f && y <= h * 0.5f) tex.SetPixel(x, y, col);
                    if (x > w * 0.42f && x < w * 0.58f && y > h * 0.15f && y < h * 0.35f) tex.SetPixel(x, y, clear);
                }
            }
        }
        else if (name == "Shop")
        {
            Color brown = new Color(0.45f, 0.25f, 0.1f);
            Color gold = new Color(0.95f, 0.75f, 0.15f);
            for (int y = 15; y < h - 15; y++)
            {
                for (int x = 20; x < w - 20; x++)
                {
                    Color col = brown;
                    if (x < 26 || x > w - 27 || y < 21 || y > h - 22) col = gold;
                    if (x > w / 2 - 6 && x < w / 2 + 6 && y > h / 2 - 12 && y < h / 2 + 2) col = Color.black;
                    tex.SetPixel(x, y, col);
                }
            }
        }
        else if (name == "Heroes")
        {
            Color yellow = new Color(0.95f, 0.85f, 0.15f);
            Vector2 center = new Vector2(w / 2f, h / 2f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (Vector2.Distance(p, center) <= w * 0.35f)
                    {
                        Color col = yellow;
                        if (Vector2.Distance(p, new Vector2(w * 0.42f, h * 0.54f)) <= 4f ||
                            Vector2.Distance(p, new Vector2(w * 0.58f, h * 0.54f)) <= 4f)
                        {
                            col = Color.black;
                        }
                        if (x > w / 2 - 8 && x < w / 2 + 8 && y > h / 2 - 12 && y < h / 2 - 4)
                        {
                            col = new Color(0.95f, 0.35f, 0.05f);
                        }
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }
        else if (name == "Missions")
        {
            Color board = new Color(0.2f, 0.5f, 0.8f);
            Color paper = Color.white;
            for (int y = 15; y < h - 15; y++)
            {
                for (int x = 25; x < w - 25; x++)
                {
                    Color col = board;
                    if (x > 32 && x < w - 33 && y > 22 && y < h - 28) col = paper;
                    if (x > w / 2 - 12 && x < w / 2 + 12 && y > h - 26 && y < h - 18) col = Color.grey;
                    tex.SetPixel(x, y, col);
                }
            }
        }
        else if (name == "Themes")
        {
            Color blue = new Color(0.12f, 0.45f, 0.85f);
            Color green = new Color(0.15f, 0.65f, 0.25f);
            Vector2 center = new Vector2(w / 2f, h / 2f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (Vector2.Distance(p, center) <= w * 0.38f)
                    {
                        Color col = blue;
                        if (Mathf.Sin(x * 0.15f) + Mathf.Cos(y * 0.15f) > 0.3f) col = green;
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }
        else if (name == "Play")
        {
            Vector2 a = new Vector2(w * 0.32f, h * 0.72f);
            Vector2 b = new Vector2(w * 0.32f, h * 0.28f);
            Vector2 c = new Vector2(w * 0.72f, h * 0.5f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (PointInTriangle(new Vector2(x, y), a, b, c))
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
            }
        }

        tex.Apply();
        return tex;
    }
}
