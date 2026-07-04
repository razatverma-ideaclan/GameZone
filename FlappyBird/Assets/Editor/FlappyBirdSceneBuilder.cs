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
        EnsureLayers();
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

        // Coins & power-ups
        Sprite coinSprite = GetOrCreateSprite("Coin", 128, 128, (w, h) => GenerateCoinTexture(w, h), 128);
        Sprite magnetSprite = GetOrCreateSprite("Powerup_Magnet", 128, 128, (w, h) => GenerateMagnetTexture(w, h), 128);
        Sprite boostSprite = GetOrCreateSprite("Powerup_Boost", 128, 128, (w, h) => GenerateBoostTexture(w, h), 128);
        Sprite doubleSprite = GetOrCreateSprite("Powerup_Double", 128, 128, (w, h) => GenerateDoubleTexture(w, h), 128);
        Sprite hammerSprite = GetOrCreateSprite("Powerup_Hammer", 128, 128, (w, h) => GenerateHammerTexture(w, h), 128);

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
        GameObject itemSpawnerGO = BuildItemSpawner(coinSprite, magnetSprite, boostSprite, doubleSprite, hammerSprite);
        BuildEventSystem();
        Canvas canvas = BuildCanvas();
        GameObject scoreTextGO = BuildScoreText(canvas.transform);

        GameObject coinsTextGO, magnetTimerGO, boostTimerGO, doubleTimerGO, hammerChargesGO;
        BuildPowerupHUD(canvas.transform, coinSprite, magnetSprite, boostSprite, doubleSprite, hammerSprite, out coinsTextGO, out magnetTimerGO, out boostTimerGO, out doubleTimerGO, out hammerChargesGO);

        GameObject startHighScoreText;
        Button startButton, shopButton, heroesButton, missionsButton, themesButton, navPlayBtn;
        GameObject toastPanel, themeSelectorPanelRef;
        GameObject lobbyPanel, heroesPanel;
        UnityEngine.UI.Image playIconImageRef;
        Text activeThemeLabelText;
        GameObject shopPanel, questsPanel, leaderboardPanel, screenBackdrop;
        GameObject starterMagnetWidget, starterBoostWidget, starterDoubleWidget, starterHammerWidget;
        Button bestScoreButton;
        GameObject startPanel = BuildStartPanel(canvas.transform, pillButtonSprite, resultCardSprite, goldMedalSprite, out startButton, out startHighScoreText, birdMidSprites, themeAssets, shopIcon, heroesIcon, missionsIcon, themesIcon, playIcon, homeIcon, out shopButton, out heroesButton, out missionsButton, out themesButton, out toastPanel, out themeSelectorPanelRef, out lobbyPanel, out heroesPanel, out playIconImageRef, out navPlayBtn, out activeThemeLabelText, out shopPanel, out questsPanel, out leaderboardPanel, out bestScoreButton, out screenBackdrop, out starterMagnetWidget, out starterBoostWidget, out starterDoubleWidget, out starterHammerWidget, coinSprite, magnetSprite, boostSprite, doubleSprite, hammerSprite);
        
        UnityEngine.UI.Image medalImage;
        GameObject newBestBadge;
        RectTransform resultCardTransform;
        GameObject gameOverScoreText, gameOverBestText, gameOverCoinsText;
        GameObject gameOverPanel = BuildGameOverPanel(canvas.transform, pillButtonSprite, resultCardSprite, coinSprite, out Button menuButton, out Button retryButton, out gameOverScoreText, out gameOverBestText, out gameOverCoinsText, out medalImage, out newBestBadge, out resultCardTransform);
        
        GameObject gameManagerGO = BuildGameManager(bird, pipeSpawnerGO, itemSpawnerGO, scoreTextGO, startPanel, gameOverPanel, clickClip, scoreClip, gameOverScoreText, gameOverBestText, gameOverCoinsText, startHighScoreText, medalImage, bronzeMedalSprite, silverMedalSprite, goldMedalSprite, platinumMedalSprite, medalPlaceholderSprite, newBestBadge, resultCardTransform);

        // Wire the buttons now that GameManager exists.
        GameManager gm = gameManagerGO.GetComponent<GameManager>();
        gm.toastPanel = toastPanel;
        gm.themeSelectorPanel = themeSelectorPanelRef;
        gm.lobbyPanel = lobbyPanel;
        gm.heroesPanel = heroesPanel;
        gm.shopPanel = shopPanel;
        gm.questsPanel = questsPanel;
        gm.leaderboardPanel = leaderboardPanel;
        gm.screenBackdrop = screenBackdrop;
        gm.starterMagnetWidget = starterMagnetWidget;
        gm.starterBoostWidget = starterBoostWidget;
        gm.starterDoubleWidget = starterDoubleWidget;
        gm.starterHammerWidget = starterHammerWidget;
        gm.coinsText = coinsTextGO;
        gm.magnetTimerText = magnetTimerGO;
        gm.boostTimerText = boostTimerGO;
        gm.doubleTimerText = doubleTimerGO;
        gm.hammerChargesWidget = hammerChargesGO;
        gm.playIconImage = playIconImageRef;
        gm.playSprite = playIcon;
        gm.homeSprite = homeIcon;
        gm.shopButton = shopButton;
        gm.heroesButton = heroesButton;
        gm.missionsButton = missionsButton;
        gm.themesButton = themesButton;
        gm.centerButton = navPlayBtn;

        ThemeApplier themeApplierRef = gameManagerGO.GetComponent<ThemeApplier>();
        if (themeApplierRef != null) themeApplierRef.themeNameLabel = activeThemeLabelText;

        // Attach and wire the customizable MenuThemeConfig component to the Canvas
        MenuThemeConfig themeConfig = canvas.gameObject.AddComponent<MenuThemeConfig>();
        if (startPanel != null)
        {
            Transform navBarTrans = startPanel.transform.Find("BottomNavBar");
            if (navBarTrans != null)
            {
                themeConfig.bottomBarBg = navBarTrans.GetComponent<Image>();
                
                Transform playBtnTrans = navBarTrans.Find("PlayButton");
                if (playBtnTrans != null) themeConfig.centerPlayButtonImage = playBtnTrans.GetComponent<Image>();
                
                Transform indTrans = navBarTrans.Find("ActiveIndicator");
                if (indTrans != null) themeConfig.activeIndicatorImage = indTrans.GetComponent<Image>();
            }
        }
        if (lobbyPanel != null)
        {
            Transform leadTrans = lobbyPanel.transform.Find("LeaderboardButton");
            if (leadTrans != null) themeConfig.leaderboardButtonImage = leadTrans.GetComponent<Image>();
            
            Transform vertTrans = lobbyPanel.transform.Find("VerticalModeButton");
            if (vertTrans != null) themeConfig.verticalClimbButtonImage = vertTrans.GetComponent<Image>();
            
            Transform badgeTrans = lobbyPanel.transform.Find("HighScoreBadge");
            if (badgeTrans != null) themeConfig.highScoreBadgeImage = badgeTrans.GetComponent<Image>();
        }
        themeConfig.ApplyTheme();

        UnityEventTools.AddPersistentListener(startButton.onClick, gm.StartGame);
        UnityEventTools.AddPersistentListener(navPlayBtn.onClick, gm.OnCenterNavClicked);
        UnityEventTools.AddPersistentListener(shopButton.onClick, gm.OnShopClicked);
        UnityEventTools.AddPersistentListener(heroesButton.onClick, gm.OnHeroesClicked);
        UnityEventTools.AddPersistentListener(missionsButton.onClick, gm.OnMissionsClicked);
        UnityEventTools.AddPersistentListener(themesButton.onClick, gm.OnThemesClicked);
        UnityEventTools.AddPersistentListener(bestScoreButton.onClick, gm.OnLeaderboardClicked);
        UnityEventTools.AddIntPersistentListener(starterMagnetWidget.GetComponent<Button>().onClick, gm.ToggleArmedStarter, (int)GameManager.StarterPower.Magnet);
        UnityEventTools.AddIntPersistentListener(starterBoostWidget.GetComponent<Button>().onClick, gm.ToggleArmedStarter, (int)GameManager.StarterPower.Boost);
        UnityEventTools.AddIntPersistentListener(starterDoubleWidget.GetComponent<Button>().onClick, gm.ToggleArmedStarter, (int)GameManager.StarterPower.Double);
        UnityEventTools.AddIntPersistentListener(starterHammerWidget.GetComponent<Button>().onClick, gm.ToggleArmedStarter, (int)GameManager.StarterPower.Hammer);

        UnityEventTools.AddPersistentListener(menuButton.onClick, gm.RestartGame);
        UnityEventTools.AddPersistentListener(retryButton.onClick, gm.RetryGame);

        // Wire all 27 hero cards (9 worlds x 3 skins) inside heroesPanel to the global selector
        string[] wireThemeNames = { "Classic", "Space", "Football", "Dragon", "Fish", "Bee", "Ninja", "Mario", "Mars" };
        for (int t = 0; t < 9; t++)
        {
            for (int s = 0; s < 3; s++)
            {
                Transform cardTrans = heroesPanel.transform.Find("ScrollView/Viewport/Content/" + wireThemeNames[t] + "Card" + s);
                if (cardTrans != null)
                {
                    Button cardBtn = cardTrans.GetComponent<Button>();
                    if (cardBtn != null)
                    {
                        UnityEventTools.AddIntPersistentListener(cardBtn.onClick, gm.SelectHeroGlobal, t * 3 + s);
                    }
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

    /// <summary>
    /// Physics layers (distinct from tags) so Physics2D.IgnoreLayerCollision can suppress real
    /// collider contact between the bird and pipes during Boost — skipping Die() alone isn't
    /// enough, since the Rigidbody2D/BoxCollider2D contact still physically shoves the bird.
    /// </summary>
    private static void EnsureLayers()
    {
        AddLayer("Bird");
        AddLayer("Obstacle");
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

    /// <summary>Unlike tags, the layers array is a fixed 32-slot array — find the first empty user slot (0-7 are Unity built-ins) instead of inserting.</summary>
    private static void AddLayer(string name)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 0; i < layersProp.arraySize; i++)
        {
            if (layersProp.GetArrayElementAtIndex(i).stringValue == name) return; // already exists
        }

        for (int i = 8; i < layersProp.arraySize; i++)
        {
            SerializedProperty slot = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
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

        bg.AddComponent<BackgroundAnimator>();
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
        ground.layer = LayerMask.NameToLayer("Obstacle"); // so Boost's Bird/Obstacle layer-ignore also covers the ground
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
        bird.layer = LayerMask.NameToLayer("Bird");
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

        // Boost Storm — swirling energy ring around the bird, only active during Fast Boost.
        GameObject storm = new GameObject("BoostStorm");
        storm.transform.SetParent(bird.transform, false);
        ParticleSystem stormPs = storm.AddComponent<ParticleSystem>();

        var stormMain = stormPs.main;
        stormMain.duration = 1f;
        stormMain.loop = true;
        stormMain.startLifetime = 0.6f;
        stormMain.startSpeed = 0f; // orbital velocity module drives all movement
        stormMain.startSize = 0.1f;
        stormMain.gravityModifier = 0f;
        stormMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        stormMain.startColor = new Color(0.6f, 0.85f, 1f, 0.8f);

        var stormEmission = stormPs.emission;
        stormEmission.rateOverTime = 20f;

        var stormShape = stormPs.shape;
        stormShape.shapeType = ParticleSystemShapeType.Circle;
        stormShape.radius = 0.5f;

        var stormVelocity = stormPs.velocityOverLifetime;
        stormVelocity.enabled = true;
        stormVelocity.orbitalZ = 6f; // spins particles around the bird in the 2D view plane

        var stormRenderer = storm.GetComponent<ParticleSystemRenderer>();
        stormRenderer.sortingOrder = 31; // in front of the bird for a wraparound look
        stormRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

        storm.SetActive(false); // toggled on/off by BirdController.SetBoostTrailActive

        // Boost Comet — sharp stretched streaks trailing behind, only active during Fast Boost.
        GameObject comet = new GameObject("BoostComet");
        comet.transform.SetParent(bird.transform, false);
        comet.transform.localPosition = new Vector3(-0.3f, 0f, 0f);
        ParticleSystem cometPs = comet.AddComponent<ParticleSystem>();

        var cometMain = cometPs.main;
        cometMain.duration = 1f;
        cometMain.loop = true;
        cometMain.startLifetime = 0.25f;
        cometMain.startSpeed = 3f;
        cometMain.startSize = 0.18f;
        cometMain.gravityModifier = 0f;
        cometMain.simulationSpace = ParticleSystemSimulationSpace.World;
        cometMain.startColor = new Color(1f, 0.95f, 0.6f);

        var cometEmission = cometPs.emission;
        cometEmission.rateOverTime = 8f;

        var cometShape = cometPs.shape;
        cometShape.shapeType = ParticleSystemShapeType.Cone;
        cometShape.angle = 3f;
        cometShape.radius = 0.05f;
        cometShape.rotation = new Vector3(0f, -90f, 0f); // points emission along -X, trailing behind the bird

        var cometColorOverLifetime = cometPs.colorOverLifetime;
        cometColorOverLifetime.enabled = true;
        Gradient cometGrad = new Gradient();
        cometGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.6f, 0.1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        cometColorOverLifetime.color = new ParticleSystem.MinMaxGradient(cometGrad);

        var cometRenderer = comet.GetComponent<ParticleSystemRenderer>();
        cometRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        cometRenderer.lengthScale = 4f;
        cometRenderer.velocityScale = 0.3f;
        cometRenderer.sortingOrder = 28; // behind the spark trail
        cometRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

        comet.SetActive(false); // toggled on/off by BirdController.SetBoostTrailActive

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
        pipeBottom.layer = LayerMask.NameToLayer("Obstacle");
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
        pipeTop.layer = LayerMask.NameToLayer("Obstacle");
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

    private static GameObject BuildItemSpawner(Sprite coinSprite, Sprite magnetSprite, Sprite boostSprite, Sprite doubleSprite, Sprite hammerSprite)
    {
        GameObject spawnerGO = new GameObject("ItemSpawner");
        ItemSpawner spawner = spawnerGO.AddComponent<ItemSpawner>();
        spawner.spawnInterval = 1.9f; // matches PipeSpawner's interval
        spawner.initialDelay = 0.95f; // half the interval, so items land between pipe spawns
        spawner.spawnXPosition = 10f;
        spawner.minY = -3.5f;
        spawner.maxY = 3.5f;
        spawner.coinSprite = coinSprite;
        spawner.magnetSprite = magnetSprite;
        spawner.boostSprite = boostSprite;
        spawner.doubleSprite = doubleSprite;
        spawner.hammerSprite = hammerSprite;
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

    /// <summary>Top-left coin counter (mirrors the top-right score display) plus three stacked, initially-hidden active power-up timer labels below it.</summary>
    private static void BuildPowerupHUD(Transform canvasTransform, Sprite coinIcon, Sprite magnetIcon, Sprite boostIcon, Sprite doubleIcon, Sprite hammerIcon, out GameObject coinsTextGO, out GameObject magnetTimerGO, out GameObject boostTimerGO, out GameObject doubleTimerGO, out GameObject hammerChargesGO)
    {
        GameObject coinBadge = new GameObject("CoinsBadge");
        coinBadge.transform.SetParent(canvasTransform, false);
        RectTransform badgeRt = coinBadge.AddComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0f, 1f);
        badgeRt.anchorMax = new Vector2(0f, 1f);
        badgeRt.pivot = new Vector2(0f, 1f);
        badgeRt.sizeDelta = new Vector2(220, 100);
        badgeRt.anchoredPosition = new Vector2(30, -60); // mirrors ScoreText's top-right offset

        GameObject coinIconGO = new GameObject("CoinIcon");
        coinIconGO.transform.SetParent(coinBadge.transform, false);
        UnityEngine.UI.Image coinIconImg = coinIconGO.AddComponent<UnityEngine.UI.Image>();
        coinIconImg.sprite = coinIcon;
        RectTransform coinIconRt = coinIconGO.GetComponent<RectTransform>();
        coinIconRt.anchorMin = new Vector2(0f, 0.5f);
        coinIconRt.anchorMax = new Vector2(0f, 0.5f);
        coinIconRt.pivot = new Vector2(0f, 0.5f);
        coinIconRt.sizeDelta = new Vector2(56, 56);
        coinIconRt.anchoredPosition = Vector2.zero;

        coinsTextGO = new GameObject("CoinsText");
        coinsTextGO.transform.SetParent(coinBadge.transform, false);
        Text coinsText = coinsTextGO.AddComponent<Text>();
        coinsText.text = "0";
        coinsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        coinsText.fontSize = 48;
        coinsText.fontStyle = FontStyle.Bold;
        coinsText.alignment = TextAnchor.MiddleLeft;
        coinsText.color = new Color(0.98f, 0.82f, 0.15f);
        Outline coinsOutline = coinsTextGO.AddComponent<Outline>();
        coinsOutline.effectColor = new Color(0.18f, 0.18f, 0.22f, 1f);
        coinsOutline.effectDistance = new Vector2(3f, -3f);
        RectTransform coinsTextRt = coinsTextGO.GetComponent<RectTransform>();
        coinsTextRt.anchorMin = new Vector2(0f, 0.5f);
        coinsTextRt.anchorMax = new Vector2(0f, 0.5f);
        coinsTextRt.pivot = new Vector2(0f, 0.5f);
        coinsTextRt.sizeDelta = new Vector2(140, 70);
        coinsTextRt.anchoredPosition = new Vector2(64, 0);

        magnetTimerGO = BuildTimerIndicator(canvasTransform, "MagnetTimer", magnetIcon, -170);
        boostTimerGO = BuildTimerIndicator(canvasTransform, "BoostTimer", boostIcon, -220);
        doubleTimerGO = BuildTimerIndicator(canvasTransform, "DoubleTimer", doubleIcon, -270);
        hammerChargesGO = BuildTimerIndicator(canvasTransform, "HammerCharges", hammerIcon, -320);
    }

    /// <summary>Icon + countdown number (no text label) for an active power-up, shown only while it's running.</summary>
    private static GameObject BuildTimerIndicator(Transform canvasTransform, string name, Sprite icon, float yOffset)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(canvasTransform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(140, 60);
        rt.anchoredPosition = new Vector2(0, yOffset);

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(go.transform, false);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = icon;
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(48, 48);
        iconRt.anchoredPosition = new Vector2(-35, 0);

        GameObject numberGO = new GameObject("Number");
        numberGO.transform.SetParent(go.transform, false);
        Text text = numberGO.AddComponent<Text>();
        text.text = "";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 34;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        Outline outline = numberGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.1f, 0.12f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        RectTransform numberRt = numberGO.GetComponent<RectTransform>();
        numberRt.sizeDelta = new Vector2(80, 50);
        numberRt.anchoredPosition = new Vector2(20, 0);

        go.SetActive(false); // only shown while its power-up is active
        return go;
    }

    /// <summary>
    /// Lobby starter-power icon+count widget. Tapping arms it (GameManager.ToggleArmedStarter),
    /// which highlights it via the Outline until the run starts or it's tapped again to un-arm.
    /// </summary>
    private static GameObject BuildStarterWidget(Transform parent, Sprite icon, Vector2 position)
    {
        GameObject go = new GameObject("StarterWidget");
        go.transform.SetParent(parent, false);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.18f, 0.85f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(70, 70);
        rt.anchoredPosition = position;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.35f, 0.4f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(go.transform, false);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = icon;
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(48, 48);
        iconRt.anchoredPosition = new Vector2(0, 4);

        GameObject countGO = new GameObject("Count");
        countGO.transform.SetParent(go.transform, false);
        Text countText = countGO.AddComponent<Text>();
        countText.text = "x0";
        countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countText.fontSize = 20;
        countText.fontStyle = FontStyle.Bold;
        countText.alignment = TextAnchor.MiddleCenter;
        countText.color = new Color(0.95f, 0.72f, 0.15f);
        Outline countOutline = countGO.AddComponent<Outline>();
        countOutline.effectColor = new Color(0.1f, 0.1f, 0.12f, 1f);
        countOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform countRt = countGO.GetComponent<RectTransform>();
        countRt.sizeDelta = new Vector2(70, 26);
        countRt.anchoredPosition = new Vector2(0, -26);

        return go;
    }

    private static readonly Color[] TitlePalette =
    {
        new Color(0.15f, 0.15f, 0.18f) // Premium dark charcoal color
    };

    private static GameObject BuildStartPanel(Transform canvasTransform, Sprite buttonSprite, Sprite resultCardSprite, Sprite goldMedalSprite, out Button startButton, out GameObject startHighScoreText, Sprite[] birdMidSprites, ThemeData[] themeAssets, Sprite shopIcon, Sprite heroesIcon, Sprite missionsIcon, Sprite themesIcon, Sprite playIcon, Sprite homeIcon, out Button shopButton, out Button heroesButton, out Button missionsButton, out Button themesButton, out GameObject toastPanel, out GameObject themeSelectorPanelRef, out GameObject lobbyPanel, out GameObject heroesPanel, out UnityEngine.UI.Image playIconImageRef, out Button navPlayBtn, out Text activeThemeLabelText, out GameObject shopPanel, out GameObject questsPanel, out GameObject leaderboardPanel, out Button bestScoreButton, out GameObject screenBackdrop, out GameObject starterMagnetWidget, out GameObject starterBoostWidget, out GameObject starterDoubleWidget, out GameObject starterHammerWidget, Sprite coinSprite, Sprite magnetSprite, Sprite boostSprite, Sprite doubleSprite, Sprite hammerSprite)
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

        // Dedicated Leaderboard corner button — a clear, labeled tap target
        // (separate from the small BEST badge, which isn't an obvious button).
        GameObject leaderboardBtnGO = new GameObject("LeaderboardButton");
        leaderboardBtnGO.transform.SetParent(lobbyPanel.transform, false);
        Image leaderboardBtnImg = leaderboardBtnGO.AddComponent<Image>();
        leaderboardBtnImg.sprite = goldMedalSprite;
        bestScoreButton = leaderboardBtnGO.AddComponent<Button>();
        bestScoreButton.transition = Selectable.Transition.None;
        RectTransform leaderboardBtnRt = leaderboardBtnGO.GetComponent<RectTransform>();
        leaderboardBtnRt.sizeDelta = new Vector2(100, 100);
        leaderboardBtnRt.anchoredPosition = new Vector2(400, 650); // clear above the wide "FLAPPY BIRD" title

        GameObject leaderboardBtnLabel = CreateLabel("Label", leaderboardBtnGO.transform, "RANKS", 20, new Vector2(0, -62));
        leaderboardBtnLabel.GetComponent<Text>().color = new Color(0.9f, 0.9f, 0.9f);

        // World name is no longer shown on the main Lobby screen (it has its own dedicated
        // Worlds screen now), so ThemeApplier has no label to update here.
        activeThemeLabelText = null;

        // High Score Badge (positioned below the central character, clear of the bird preview)
        GameObject scoreBadge = new GameObject("HighScoreBadge");
        scoreBadge.transform.SetParent(lobbyPanel.transform, false);
        Image badgeImg = scoreBadge.AddComponent<Image>();
        badgeImg.sprite = resultCardSprite; // rounded card texture
        badgeImg.color = new Color(0.15f, 0.15f, 0.18f, 0.75f); // dark translucent charcoal

        RectTransform badgeRt = scoreBadge.GetComponent<RectTransform>();
        badgeRt.sizeDelta = new Vector2(280, 68);
        badgeRt.anchoredPosition = new Vector2(0, -140); // moved up to clear the starter-power widget row below it

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

        // Starter Power widgets — small icon+count buttons. Tapping one arms it (consumes a charge)
        // to auto-apply at the start of the next run; tapping the same one again un-arms/refunds it.
        starterMagnetWidget = BuildStarterWidget(lobbyPanel.transform, magnetSprite, new Vector2(-130, -250));
        starterBoostWidget = BuildStarterWidget(lobbyPanel.transform, boostSprite, new Vector2(-43, -250));
        starterDoubleWidget = BuildStarterWidget(lobbyPanel.transform, doubleSprite, new Vector2(43, -250));
        starterHammerWidget = BuildStarterWidget(lobbyPanel.transform, hammerSprite, new Vector2(130, -250));

        // Pulsing "TAP TO START" hint text on start screen
        GameObject tapStartGO = CreateLabel("TapToStartText", lobbyPanel.transform, "TAP TO START", 52, new Vector2(0, -300));
        Text tapText = tapStartGO.GetComponent<Text>();
        tapText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // bulky, retro game font
        tapText.color = Color.white;
        tapText.fontStyle = FontStyle.Bold;
        
        Outline tapOutline = tapStartGO.GetComponent<Outline>();
        if (tapOutline == null) tapOutline = tapStartGO.AddComponent<Outline>();
        tapOutline.effectColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        tapOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow tapShadow = tapStartGO.AddComponent<Shadow>();
        tapShadow.effectColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        tapShadow.effectDistance = new Vector2(2f, -2f);

        UIPulse pulse = tapStartGO.AddComponent<UIPulse>();
        pulse.scaleAmount = 0.05f;
        pulse.speed = 3f;

        // --- Shared dim backdrop behind every non-Lobby screen. Without this, the live gameplay
        // background (whose color depends on whichever world theme is currently active — bright
        // yellow for Bee, fiery orange for Dragon, etc.) shows through and clashes with whatever
        // screen is on top. GameManager toggles this alongside CurrentScreen != Lobby.
        screenBackdrop = new GameObject("ScreenBackdrop");
        screenBackdrop.transform.SetParent(panel.transform, false);
        Image backdropImg = screenBackdrop.AddComponent<Image>();
        backdropImg.color = new Color(0.05f, 0.05f, 0.08f, 0.88f);
        RectTransform backdropRt = screenBackdrop.GetComponent<RectTransform>();
        backdropRt.anchorMin = Vector2.zero;
        backdropRt.anchorMax = Vector2.one;
        backdropRt.offsetMin = Vector2.zero;
        backdropRt.offsetMax = Vector2.zero;
        screenBackdrop.SetActive(false);

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

        GameObject hbTxtGO = CreateLabel("Text", hbGO.transform, "HEROES (1/27)", 28, Vector2.zero);
        hbTxtGO.GetComponent<Text>().color = Color.white;
        hbTxtGO.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform hbtRt = hbTxtGO.GetComponent<RectTransform>();
        hbtRt.anchorMin = Vector2.zero;
        hbtRt.anchorMax = Vector2.one;
        hbtRt.offsetMin = Vector2.zero;
        hbtRt.offsetMax = Vector2.zero;

        // Scrollable roster: every hero from every world, not just the currently active one.
        GameObject scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(heroesPanel.transform, false);
        RectTransform scrollRt = scrollGO.AddComponent<RectTransform>();
        scrollRt.sizeDelta = new Vector2(900, 1100);
        scrollRt.anchoredPosition = new Vector2(0, -60);

        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform viewportRt = viewportGO.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportGO.AddComponent<RectMask2D>();

        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRt = contentGO.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;

        GridLayoutGroup hGrid = contentGO.AddComponent<GridLayoutGroup>();
        hGrid.cellSize = new Vector2(260, 300);
        hGrid.spacing = new Vector2(20, 20);
        hGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        hGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        hGrid.childAlignment = TextAnchor.UpperCenter;
        hGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        hGrid.constraintCount = 3;

        ContentSizeFitter contentFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;
        scrollRect.viewport = viewportRt;

        string[] heroThemeNames = { "Classic", "Space", "Football", "Dragon", "Fish", "Bee", "Ninja", "Mario", "Mars" };
        string[][] heroNames = new string[][]
        {
            new [] { "YELLOW HERO", "BLUE HERO", "RED HERO" },
            new [] { "ROCKET SPEEDER", "COSMIC UFO", "COMM SATELLITE" },
            new [] { "SOCCER BALL", "BASKETBALL", "TENNIS BALL" },
            new [] { "RED DRAKE", "EMERALD DRAGON", "GOLD WYVERN" },
            new [] { "GOLDFISH", "BULL SHARK", "PINK JELLYFISH" },
            new [] { "HONEY BEE", "LADYBUG", "BUTTERFLY" },
            new [] { "SHADOW NINJA", "CRIMSON NINJA", "SILVER SHINOBI" },
            new [] { "JUMP MAN", "GREEN PLUMBER", "PRINCESS PEACH" },
            new [] { "ASTRONAUT", "MARS ROVER", "CRYSTAL EXPLORER" }
        };

        // Instantiate all 9 worlds x 3 skins = 27 hero cards
        for (int t = 0; t < 9; t++)
        {
            for (int s = 0; s < 3; s++)
            {
                GameObject cardGO = new GameObject(heroThemeNames[t] + "Card" + s);
                cardGO.transform.SetParent(contentGO.transform, false);

                // --- NO opaque background panel: card is fully transparent ---
                // A very subtle rounded border gives visual separation without a dark box.
                Image cardImg = cardGO.AddComponent<Image>();
                cardImg.sprite = resultCardSprite;
                cardImg.color = new Color(1f, 1f, 1f, 0f); // fully transparent fill

                // Faint coloured outline ring using the world theme colour
                Outline cardOutline = cardGO.AddComponent<Outline>();
                Color worldTint = themeAssets != null && t < themeAssets.Length ? themeAssets[t].themeColor : new Color(0.5f, 0.5f, 0.6f);
                cardOutline.effectColor = new Color(worldTint.r, worldTint.g, worldTint.b, 0.55f);
                cardOutline.effectDistance = new Vector2(3f, -3f);

                cardGO.AddComponent<Button>();

                // --- Subtle dark shadow disc behind the character icon ---
                // Ensures light-coloured characters are always visible on any theme background.
                GameObject shadowGO = new GameObject("ShadowDisc");
                shadowGO.transform.SetParent(cardGO.transform, false);
                Image shadowImg = shadowGO.AddComponent<Image>();
                // Use a circular sprite by drawing it as a white image and tinting it dark
                shadowImg.color = new Color(0f, 0f, 0f, 0.22f);
                RectTransform shadowRt = shadowGO.GetComponent<RectTransform>();
                shadowRt.sizeDelta = new Vector2(155, 155);
                shadowRt.anchoredPosition = new Vector2(3f, 46f); // slightly offset down-right for drop-shadow feel

                // --- Character icon preview ---
                GameObject iconGO = new GameObject("PreviewImage");
                iconGO.transform.SetParent(cardGO.transform, false);
                Image previewImg = iconGO.AddComponent<Image>();
                previewImg.preserveAspect = true;
                if (t == 0)
                {
                    if (birdMidSprites != null && s < birdMidSprites.Length) previewImg.sprite = birdMidSprites[s];
                }
                else if (themeAssets != null && t < themeAssets.Length && themeAssets[t].playerSprites != null && s < themeAssets[t].playerSprites.Length)
                {
                    previewImg.sprite = themeAssets[t].playerSprites[s];
                }
                RectTransform iconRt = iconGO.GetComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(160, 160);
                iconRt.anchoredPosition = new Vector2(0, 50);

                // Hero name label — white with subtle dark shadow for legibility
                GameObject nameTxtGO = CreateLabel("NameText", cardGO.transform, heroNames[t][s], 17, new Vector2(0, -75));
                Text nameText = nameTxtGO.GetComponent<Text>();
                nameText.color = Color.white;
                nameText.fontStyle = FontStyle.Bold;
                // Add text shadow for contrast on light backgrounds
                Shadow nameShadow = nameTxtGO.AddComponent<Shadow>();
                nameShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
                nameShadow.effectDistance = new Vector2(1f, -1f);
                RectTransform ntRt = nameTxtGO.GetComponent<RectTransform>();
                ntRt.sizeDelta = new Vector2(240, 36);


                // Selection Checkmark overlay badge (top right)
                GameObject checkGO = new GameObject("Checkmark");
                checkGO.transform.SetParent(cardGO.transform, false);
                Image checkImg = checkGO.AddComponent<Image>();
                checkImg.sprite = goldMedalSprite;
                RectTransform checkRt = checkGO.GetComponent<RectTransform>();
                checkRt.sizeDelta = new Vector2(40, 40);
                checkRt.anchoredPosition = new Vector2(95, 115);

                checkGO.SetActive(false); // activated by GameManager when hero is selected
            }
        }

        heroesPanel.SetActive(false);

        // --- Interactive Worlds Selection Screen (Grid) ---
        // Full-screen sibling of HeroesPanel (same construction pattern) instead of a small
        // floating button grid — previously this overlaid the still-visible lobby/tap-to-start.
        GameObject worldsPanel = new GameObject("WorldsPanel");
        worldsPanel.transform.SetParent(panel.transform, false);
        themeSelectorPanelRef = worldsPanel;

        RectTransform wpRt = worldsPanel.AddComponent<RectTransform>();
        wpRt.anchorMin = new Vector2(0.5f, 0.5f);
        wpRt.anchorMax = new Vector2(0.5f, 0.5f);
        wpRt.pivot = new Vector2(0.5f, 0.5f);
        wpRt.sizeDelta = new Vector2(1000, 1400);
        wpRt.anchoredPosition = new Vector2(0, 80); // centered vertically above bottom bar

        // Header bar inside WorldsPanel
        GameObject wHbGO = new GameObject("HeaderBar");
        wHbGO.transform.SetParent(worldsPanel.transform, false);
        Image wHbImg = wHbGO.AddComponent<Image>();
        wHbImg.sprite = resultCardSprite;
        wHbImg.color = new Color(0.08f, 0.45f, 0.85f, 1f); // bright royal blue

        Outline wHbOutline = wHbGO.AddComponent<Outline>();
        wHbOutline.effectColor = Color.white;
        wHbOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform wHbRt = wHbGO.GetComponent<RectTransform>();
        wHbRt.sizeDelta = new Vector2(900, 80);
        wHbRt.anchoredPosition = new Vector2(0, 620); // top header position

        GameObject wHbTxtGO = CreateLabel("Text", wHbGO.transform, "SELECT WORLD", 28, Vector2.zero);
        wHbTxtGO.GetComponent<Text>().color = Color.white;
        wHbTxtGO.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform wHbtRt = wHbTxtGO.GetComponent<RectTransform>();
        wHbtRt.anchorMin = Vector2.zero;
        wHbtRt.anchorMax = Vector2.one;
        wHbtRt.offsetMin = Vector2.zero;
        wHbtRt.offsetMax = Vector2.zero;

        // Grid Container inside WorldsPanel
        GameObject wGridGO = new GameObject("Grid");
        wGridGO.transform.SetParent(worldsPanel.transform, false);
        RectTransform wGridRt = wGridGO.AddComponent<RectTransform>();
        wGridRt.sizeDelta = new Vector2(900, 1000);
        wGridRt.anchoredPosition = new Vector2(0, 0);

        GridLayoutGroup wGrid = wGridGO.AddComponent<GridLayoutGroup>();
        wGrid.cellSize = new Vector2(260, 300);
        wGrid.spacing = new Vector2(20, 20);
        wGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        wGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        wGrid.childAlignment = TextAnchor.UpperCenter;
        wGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        wGrid.constraintCount = 3;

        ThemeSelectorUI selectorUI = worldsPanel.AddComponent<ThemeSelectorUI>();
        selectorUI.themeButtons = new Button[9];

        string[] themeNames = { "Classic", "Space", "Football", "Dragon", "Fish", "Bee", "Ninja", "Mario", "Mars" };
        for (int i = 0; i < 9; i++)
        {
            GameObject cardGO = new GameObject(themeNames[i] + "Card");
            cardGO.transform.SetParent(wGridGO.transform, false);

            // Transparent card background (just like Heroes cards)
            Image cardImg = cardGO.AddComponent<Image>();
            cardImg.sprite = resultCardSprite;
            cardImg.color = new Color(1f, 1f, 1f, 0f); // fully transparent fill

            // Card outline border styled with world theme color
            Outline cardOutline = cardGO.AddComponent<Outline>();
            Color worldTint = themeAssets != null && i < themeAssets.Length ? themeAssets[i].themeColor : new Color(0.5f, 0.5f, 0.6f);
            cardOutline.effectColor = new Color(worldTint.r, worldTint.g, worldTint.b, 0.55f);
            cardOutline.effectDistance = new Vector2(3f, -3f);

            Button btn = cardGO.AddComponent<Button>();
            selectorUI.themeButtons[i] = btn;

            // Preview thumbnail (this world's background art)
            GameObject previewGO = new GameObject("PreviewImage");
            previewGO.transform.SetParent(cardGO.transform, false);
            Image previewImg = previewGO.AddComponent<Image>();
            previewImg.preserveAspect = true;
            if (themeAssets != null && i < themeAssets.Length) previewImg.sprite = themeAssets[i].backgroundSprite;
            
            // Add a subtle dark frame to the preview image
            Outline previewOutline = previewGO.AddComponent<Outline>();
            previewOutline.effectColor = new Color(0f, 0f, 0f, 0.5f);
            previewOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform previewRt = previewGO.GetComponent<RectTransform>();
            previewRt.sizeDelta = new Vector2(220, 130);
            previewRt.anchoredPosition = new Vector2(0, 55);

            // Name label - white with shadow for contrast
            GameObject wNameTxtGO = CreateLabel("NameText", cardGO.transform, themeNames[i].ToUpper(), 22, new Vector2(0, -70));
            Text wNameText = wNameTxtGO.GetComponent<Text>();
            wNameText.color = Color.white;
            wNameText.fontStyle = FontStyle.Bold;

            Shadow wNameShadow = wNameTxtGO.AddComponent<Shadow>();
            wNameShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            wNameShadow.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform wNtRt = wNameTxtGO.GetComponent<RectTransform>();
            wNtRt.sizeDelta = new Vector2(240, 50);

            // Selection checkmark overlay badge (same treatment as Heroes cards)
            GameObject wCheckGO = new GameObject("Checkmark");
            wCheckGO.transform.SetParent(cardGO.transform, false);
            Image wCheckImg = wCheckGO.AddComponent<Image>();
            wCheckImg.sprite = goldMedalSprite;
            RectTransform wCheckRt = wCheckGO.GetComponent<RectTransform>();
            wCheckRt.sizeDelta = new Vector2(46, 46);
            wCheckRt.anchoredPosition = new Vector2(95, 115); // top right of card

            wCheckGO.SetActive(false); // active if world is selected
        }

        worldsPanel.SetActive(false);

        // --- Shop / Quests placeholder screens ---
        shopPanel = BuildStarterPowerShopPanel(panel.transform, resultCardSprite, buttonSprite, coinSprite, magnetSprite, boostSprite, doubleSprite, hammerSprite);
        questsPanel = BuildUpgradeShopPanel(panel.transform, resultCardSprite, buttonSprite, coinSprite, magnetSprite, boostSprite, doubleSprite);

        leaderboardPanel = BuildLeaderboardPanel(panel.transform, resultCardSprite, buttonSprite);

        // --- Bottom Navigation Bar ---
        GameObject bottomBar = new GameObject("BottomNavBar");
        bottomBar.transform.SetParent(panel.transform, false);
        Image barImg = bottomBar.AddComponent<Image>();
        barImg.sprite = resultCardSprite;
        barImg.color = new Color(0.04f, 0.05f, 0.07f, 0.92f); // darker, sleek translucent glass
        
        RectTransform barRt = bottomBar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.5f, 0f);
        barRt.anchorMax = new Vector2(0.5f, 0f);
        barRt.pivot = new Vector2(0.5f, 0.5f);
        barRt.sizeDelta = new Vector2(900, 120);
        barRt.anchoredPosition = new Vector2(0, 80); // float elegantly closer to the bottom
        
        Shadow barShadow = bottomBar.AddComponent<Shadow>();
        barShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        barShadow.effectDistance = new Vector2(0f, -6f);

        Outline barOutline = bottomBar.AddComponent<Outline>();
        barOutline.effectColor = new Color(1f, 1f, 1f, 0.12f); // very clean, thin, subtle outline border
        barOutline.effectDistance = new Vector2(1f, -1f);

        GameObject indicatorGO = new GameObject("ActiveIndicator");
        indicatorGO.transform.SetParent(bottomBar.transform, false);
        Image indImg = indicatorGO.AddComponent<Image>();
        indImg.sprite = resultCardSprite;
        indImg.color = new Color(1f, 1f, 1f, 0.16f); // soft translucent capsule overlay
        
        RectTransform indRt = indicatorGO.GetComponent<RectTransform>();
        indRt.anchorMin = new Vector2(0.5f, 0.5f);
        indRt.anchorMax = new Vector2(0.5f, 0.5f);
        indRt.pivot = new Vector2(0.5f, 0.5f);
        indRt.sizeDelta = new Vector2(100, 90);
        indRt.anchoredPosition = new Vector2(0, 0);

        // 1. Shop Button
        GameObject shopGO = new GameObject("ShopButton");
        shopGO.transform.SetParent(bottomBar.transform, false);
        Image shopImg = shopGO.AddComponent<Image>();
        shopImg.sprite = resultCardSprite;
        shopImg.color = Color.clear;
        shopButton = shopGO.AddComponent<Button>();
        shopButton.transition = Selectable.Transition.None;
        RectTransform shopRt = shopGO.GetComponent<RectTransform>();
        shopRt.sizeDelta = new Vector2(80, 80);
        shopRt.anchoredPosition = new Vector2(-300, 0);

        GameObject shopIconGO = new GameObject("Icon");
        shopIconGO.transform.SetParent(shopGO.transform, false);
        Image sIconImg = shopIconGO.AddComponent<Image>();
        sIconImg.sprite = shopIcon;
        RectTransform sIconRt = shopIconGO.GetComponent<RectTransform>();
        sIconRt.sizeDelta = new Vector2(62, 62);
        sIconRt.anchoredPosition = Vector2.zero;

        // 2. Heroes Button
        GameObject heroesGO = new GameObject("HeroesButton");
        heroesGO.transform.SetParent(bottomBar.transform, false);
        Image heroesImg = heroesGO.AddComponent<Image>();
        heroesImg.sprite = resultCardSprite;
        heroesImg.color = Color.clear;
        heroesButton = heroesGO.AddComponent<Button>();
        heroesButton.transition = Selectable.Transition.None;
        RectTransform heroesRt = heroesGO.GetComponent<RectTransform>();
        heroesRt.sizeDelta = new Vector2(80, 80);
        heroesRt.anchoredPosition = new Vector2(-150, 0);

        GameObject heroesIconGO = new GameObject("Icon");
        heroesIconGO.transform.SetParent(heroesGO.transform, false);
        Image hIconImg = heroesIconGO.AddComponent<Image>();
        hIconImg.sprite = heroesIcon;
        RectTransform hIconRt = heroesIconGO.GetComponent<RectTransform>();
        hIconRt.sizeDelta = new Vector2(62, 62);
        hIconRt.anchoredPosition = Vector2.zero;

        // 3. Play Button (Center Home/Play)
        GameObject playGO = new GameObject("PlayButton");
        playGO.transform.SetParent(bottomBar.transform, false);
        Image playImg = playGO.AddComponent<Image>();
        playImg.sprite = resultCardSprite;
        playImg.color = Color.clear;
        navPlayBtn = playGO.AddComponent<Button>();
        navPlayBtn.transition = Selectable.Transition.None;
        RectTransform playRt = playGO.GetComponent<RectTransform>();
        playRt.sizeDelta = new Vector2(80, 80);
        playRt.anchoredPosition = new Vector2(0, 0);

        GameObject playIconGO = new GameObject("Icon");
        playIconGO.transform.SetParent(playGO.transform, false);
        Image pIconImg = playIconGO.AddComponent<Image>();
        pIconImg.sprite = playIcon;
        playIconImageRef = pIconImg;
        RectTransform piRt = playIconGO.GetComponent<RectTransform>();
        piRt.sizeDelta = new Vector2(62, 62);
        piRt.anchoredPosition = Vector2.zero;

        // 4. Missions Button
        GameObject missionsGO = new GameObject("MissionsButton");
        missionsGO.transform.SetParent(bottomBar.transform, false);
        Image missionsImg = missionsGO.AddComponent<Image>();
        missionsImg.sprite = resultCardSprite;
        missionsImg.color = Color.clear;
        missionsButton = missionsGO.AddComponent<Button>();
        missionsButton.transition = Selectable.Transition.None;
        RectTransform missionsRt = missionsGO.GetComponent<RectTransform>();
        missionsRt.sizeDelta = new Vector2(80, 80);
        missionsRt.anchoredPosition = new Vector2(150, 0);

        GameObject missionsIconGO = new GameObject("Icon");
        missionsIconGO.transform.SetParent(missionsGO.transform, false);
        Image mIconImg = missionsIconGO.AddComponent<Image>();
        mIconImg.sprite = missionsIcon;
        RectTransform mIconRt = missionsIconGO.GetComponent<RectTransform>();
        mIconRt.sizeDelta = new Vector2(62, 62);
        mIconRt.anchoredPosition = Vector2.zero;

        // 5. Themes Button
        GameObject themesGO = new GameObject("ThemesButton");
        themesGO.transform.SetParent(bottomBar.transform, false);
        Image themesImg = themesGO.AddComponent<Image>();
        themesImg.sprite = resultCardSprite;
        themesImg.color = Color.clear;
        themesButton = themesGO.AddComponent<Button>();
        themesButton.transition = Selectable.Transition.None;
        RectTransform themesRt = themesGO.GetComponent<RectTransform>();
        themesRt.sizeDelta = new Vector2(80, 80);
        themesRt.anchoredPosition = new Vector2(300, 0);

        GameObject themesIconGO = new GameObject("Icon");
        themesIconGO.transform.SetParent(themesGO.transform, false);
        Image tIconImg = themesIconGO.AddComponent<Image>();
        tIconImg.sprite = themesIcon;
        RectTransform tIconRt = themesIconGO.GetComponent<RectTransform>();
        tIconRt.sizeDelta = new Vector2(62, 62);
        tIconRt.anchoredPosition = Vector2.zero;

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

    /// <summary>Full-screen placeholder used by Shop/Quests until those systems are built — same construction pattern as WorldsPanel/HeroesPanel.</summary>
    private static GameObject BuildLeaderboardPanel(Transform parent, Sprite resultCardSprite, Sprite buttonSprite)
    {
        GameObject leaderboardPanel = new GameObject("LeaderboardPanel");
        leaderboardPanel.transform.SetParent(parent, false);
        RectTransform rt = leaderboardPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1000, 1400);
        rt.anchoredPosition = new Vector2(0, 80);

        LeaderboardUI board = leaderboardPanel.AddComponent<LeaderboardUI>();

        // Header bar
        GameObject hbGO = new GameObject("HeaderBar");
        hbGO.transform.SetParent(leaderboardPanel.transform, false);
        Image hbImg = hbGO.AddComponent<Image>();
        hbImg.sprite = resultCardSprite;
        hbImg.color = new Color(0.08f, 0.45f, 0.85f, 1f);
        Outline hbOutline = hbGO.AddComponent<Outline>();
        hbOutline.effectColor = Color.white;
        hbOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform hbRt = hbGO.GetComponent<RectTransform>();
        hbRt.sizeDelta = new Vector2(900, 80);
        hbRt.anchoredPosition = new Vector2(0, 620);

        GameObject hbTxtGO = CreateLabel("Text", hbGO.transform, "LEADERBOARD", 28, Vector2.zero);
        hbTxtGO.GetComponent<Text>().color = Color.white;
        hbTxtGO.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform hbtRt = hbTxtGO.GetComponent<RectTransform>();
        hbtRt.anchorMin = Vector2.zero;
        hbtRt.anchorMax = Vector2.one;
        hbtRt.offsetMin = Vector2.zero;
        hbtRt.offsetMax = Vector2.zero;

        // --- Signed-out group ---
        GameObject signedOutGroup = new GameObject("SignedOutGroup");
        signedOutGroup.transform.SetParent(leaderboardPanel.transform, false);
        RectTransform sogRt = signedOutGroup.AddComponent<RectTransform>();
        sogRt.anchorMin = new Vector2(0.5f, 0.5f);
        sogRt.anchorMax = new Vector2(0.5f, 0.5f);
        sogRt.sizeDelta = Vector2.zero;
        sogRt.anchoredPosition = Vector2.zero;

        GameObject infoGO = CreateLabel("InfoText", signedOutGroup.transform, "Sign in with Google to save your\nbest score to the global leaderboard!", 24, new Vector2(0, 500));
        infoGO.GetComponent<Text>().color = new Color(0.85f, 0.85f, 0.85f);
        infoGO.GetComponent<RectTransform>().sizeDelta = new Vector2(760, 90);

        GameObject signInGO = new GameObject("SignInButton");
        signInGO.transform.SetParent(signedOutGroup.transform, false);
        Image signInImg = signInGO.AddComponent<Image>();
        ApplyButtonLook(signInImg, buttonSprite, new Color(0.85f, 0.25f, 0.2f));
        RectTransform signInRt = signInGO.GetComponent<RectTransform>();
        signInRt.sizeDelta = new Vector2(480, 90);
        signInRt.anchoredPosition = new Vector2(0, 390);
        Button signInBtn = signInGO.AddComponent<Button>();
        signInBtn.transition = Selectable.Transition.None;
        GameObject signInLabelGO = CreateLabel("Text", signInGO.transform, "SIGN IN WITH GOOGLE", 26);
        Text signInLabelText = signInLabelGO.GetComponent<Text>();
        signInLabelText.color = Color.white;
        signInLabelText.fontStyle = FontStyle.Bold;

        // --- Signed-in group ---
        GameObject signedInGroup = new GameObject("SignedInGroup");
        signedInGroup.transform.SetParent(leaderboardPanel.transform, false);
        RectTransform sigRt = signedInGroup.AddComponent<RectTransform>();
        sigRt.anchorMin = new Vector2(0.5f, 0.5f);
        sigRt.anchorMax = new Vector2(0.5f, 0.5f);
        sigRt.sizeDelta = Vector2.zero;
        sigRt.anchoredPosition = Vector2.zero;

        GameObject nameDisplayGO = CreateLabel("NameText", signedInGroup.transform, "Player", 28, new Vector2(0, 520));
        Text nameDisplayText = nameDisplayGO.GetComponent<Text>();
        nameDisplayText.color = new Color(0.95f, 0.72f, 0.15f);
        nameDisplayText.fontStyle = FontStyle.Bold;

        GameObject nameFieldGO = new GameObject("NameInputField");
        nameFieldGO.transform.SetParent(signedInGroup.transform, false);
        Image nameFieldImg = nameFieldGO.AddComponent<Image>();
        nameFieldImg.color = new Color(1f, 1f, 1f, 0.9f);
        RectTransform nameFieldRt = nameFieldGO.GetComponent<RectTransform>();
        nameFieldRt.sizeDelta = new Vector2(540, 70);
        nameFieldRt.anchoredPosition = new Vector2(-80, 430);
        InputField nameInput = nameFieldGO.AddComponent<InputField>();

        GameObject nameFieldTextGO = new GameObject("Text");
        nameFieldTextGO.transform.SetParent(nameFieldGO.transform, false);
        Text nameFieldText = nameFieldTextGO.AddComponent<Text>();
        nameFieldText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameFieldText.fontSize = 26;
        nameFieldText.color = Color.black;
        nameFieldText.alignment = TextAnchor.MiddleLeft;
        nameFieldText.supportRichText = false;
        RectTransform nameFieldTextRt = nameFieldTextGO.GetComponent<RectTransform>();
        nameFieldTextRt.anchorMin = Vector2.zero;
        nameFieldTextRt.anchorMax = Vector2.one;
        nameFieldTextRt.offsetMin = new Vector2(16, 4);
        nameFieldTextRt.offsetMax = new Vector2(-16, -4);
        nameInput.textComponent = nameFieldText;
        nameInput.characterLimit = 20;

        GameObject saveNameGO = new GameObject("SaveNameButton");
        saveNameGO.transform.SetParent(signedInGroup.transform, false);
        Image saveNameImg = saveNameGO.AddComponent<Image>();
        ApplyButtonLook(saveNameImg, buttonSprite, new Color(0.4f, 0.82f, 0.4f));
        RectTransform saveNameRt = saveNameGO.GetComponent<RectTransform>();
        saveNameRt.sizeDelta = new Vector2(160, 70);
        saveNameRt.anchoredPosition = new Vector2(300, 430);
        Button saveNameBtn = saveNameGO.AddComponent<Button>();
        saveNameBtn.transition = Selectable.Transition.None;
        GameObject saveNameLabelGO = CreateLabel("Text", saveNameGO.transform, "SAVE", 24);
        saveNameLabelGO.GetComponent<Text>().color = Color.white;

        GameObject signOutGO = new GameObject("SignOutButton");
        signOutGO.transform.SetParent(signedInGroup.transform, false);
        Image signOutImg = signOutGO.AddComponent<Image>();
        ApplyButtonLook(signOutImg, buttonSprite, new Color(0.55f, 0.6f, 0.65f));
        RectTransform signOutRt = signOutGO.GetComponent<RectTransform>();
        signOutRt.sizeDelta = new Vector2(220, 60);
        signOutRt.anchoredPosition = new Vector2(0, 340);
        Button signOutBtn = signOutGO.AddComponent<Button>();
        signOutBtn.transition = Selectable.Transition.None;
        GameObject signOutLabelGO = CreateLabel("Text", signOutGO.transform, "SIGN OUT", 22);
        signOutLabelGO.GetComponent<Text>().color = Color.white;

        // --- Global / Country scope tabs ---
        GameObject globalTabGO = new GameObject("GlobalTabButton");
        globalTabGO.transform.SetParent(leaderboardPanel.transform, false);
        Image globalTabImg = globalTabGO.AddComponent<Image>();
        globalTabImg.sprite = resultCardSprite;
        globalTabImg.color = new Color(0.95f, 0.72f, 0.15f);
        RectTransform globalTabRt = globalTabGO.GetComponent<RectTransform>();
        globalTabRt.sizeDelta = new Vector2(230, 64);
        globalTabRt.anchoredPosition = new Vector2(-125, 250);
        Button globalTabBtn = globalTabGO.AddComponent<Button>();
        globalTabBtn.transition = Selectable.Transition.None;
        GameObject globalTabLabelGO = CreateLabel("Text", globalTabGO.transform, "GLOBAL", 24);
        globalTabLabelGO.GetComponent<Text>().color = Color.white;
        globalTabLabelGO.GetComponent<Text>().fontStyle = FontStyle.Bold;

        GameObject countryTabGO = new GameObject("CountryTabButton");
        countryTabGO.transform.SetParent(leaderboardPanel.transform, false);
        Image countryTabImg = countryTabGO.AddComponent<Image>();
        countryTabImg.sprite = resultCardSprite;
        countryTabImg.color = new Color(0.25f, 0.25f, 0.3f);
        RectTransform countryTabRt = countryTabGO.GetComponent<RectTransform>();
        countryTabRt.sizeDelta = new Vector2(230, 64);
        countryTabRt.anchoredPosition = new Vector2(125, 250);
        Button countryTabBtn = countryTabGO.AddComponent<Button>();
        countryTabBtn.transition = Selectable.Transition.None;
        GameObject countryTabLabelGO = CreateLabel("Text", countryTabGO.transform, "MY COUNTRY", 22);
        countryTabLabelGO.GetComponent<Text>().color = Color.white;
        countryTabLabelGO.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // --- Scrollable ranked list ---
        GameObject scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(leaderboardPanel.transform, false);
        RectTransform scrollRt = scrollGO.AddComponent<RectTransform>();
        scrollRt.sizeDelta = new Vector2(900, 700);
        scrollRt.anchoredPosition = new Vector2(0, -150);

        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform viewportRt = viewportGO.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportGO.AddComponent<RectMask2D>();

        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRt = contentGO.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.spacing = 4f;

        ContentSizeFitter contentFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;
        scrollRect.viewport = viewportRt;

        GameObject emptyLabelGO = CreateLabel("EmptyLabel", leaderboardPanel.transform, "No scores yet — be the first!", 26, new Vector2(0, -150));
        emptyLabelGO.GetComponent<Text>().color = new Color(0.7f, 0.7f, 0.7f);
        emptyLabelGO.SetActive(false);

        // Wire LeaderboardUI references
        board.signedOutGroup = signedOutGroup;
        board.signInButton = signInBtn;
        board.signInButtonLabel = signInLabelText;
        board.signedInGroup = signedInGroup;
        board.nameText = nameDisplayText;
        board.nameInputField = nameInput;
        board.saveNameButton = saveNameBtn;
        board.signOutButton = signOutBtn;
        board.globalTabButton = globalTabBtn;
        board.countryTabButton = countryTabBtn;
        board.globalTabBg = globalTabImg;
        board.countryTabBg = countryTabImg;
        board.listContent = contentRt;
        board.emptyListLabel = emptyLabelGO;

        leaderboardPanel.SetActive(false);
        return leaderboardPanel;
    }

    private static GameObject BuildUpgradeShopPanel(Transform parent, Sprite resultCardSprite, Sprite buttonSprite, Sprite coinSprite, Sprite magnetSprite, Sprite boostSprite, Sprite doubleSprite)
    {
        GameObject shopPanel = new GameObject("UpgradeShopPanel");
        shopPanel.transform.SetParent(parent, false);
        RectTransform rt = shopPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1000, 1400);
        rt.anchoredPosition = new Vector2(0, 80);

        PowerupUpgradeUI shop = shopPanel.AddComponent<PowerupUpgradeUI>();

        // Header bar
        GameObject hbGO = new GameObject("HeaderBar");
        hbGO.transform.SetParent(shopPanel.transform, false);
        Image hbImg = hbGO.AddComponent<Image>();
        hbImg.sprite = resultCardSprite;
        hbImg.color = new Color(0.08f, 0.45f, 0.85f, 1f);
        Outline hbOutline = hbGO.AddComponent<Outline>();
        hbOutline.effectColor = Color.white;
        hbOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform hbRt = hbGO.GetComponent<RectTransform>();
        hbRt.sizeDelta = new Vector2(900, 80);
        hbRt.anchoredPosition = new Vector2(0, 620);

        GameObject hbTxtGO = CreateLabel("Text", hbGO.transform, "UPGRADES SHOP", 28, Vector2.zero);
        hbTxtGO.GetComponent<Text>().color = Color.white;
        hbTxtGO.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform hbtRt = hbTxtGO.GetComponent<RectTransform>();
        hbtRt.anchorMin = Vector2.zero;
        hbtRt.anchorMax = Vector2.one;
        hbtRt.offsetMin = Vector2.zero;
        hbtRt.offsetMax = Vector2.zero;

        // Total coins display
        GameObject coinsRow = new GameObject("CoinsRow");
        coinsRow.transform.SetParent(shopPanel.transform, false);
        RectTransform coinsRowRt = coinsRow.AddComponent<RectTransform>();
        coinsRowRt.anchorMin = new Vector2(0.5f, 0.5f);
        coinsRowRt.anchorMax = new Vector2(0.5f, 0.5f);
        coinsRowRt.sizeDelta = Vector2.zero;
        coinsRowRt.anchoredPosition = new Vector2(0, 500);

        GameObject coinsIconGO = new GameObject("CoinIcon");
        coinsIconGO.transform.SetParent(coinsRow.transform, false);
        Image coinsIconImg = coinsIconGO.AddComponent<Image>();
        coinsIconImg.sprite = coinSprite;
        RectTransform coinsIconRt = coinsIconGO.GetComponent<RectTransform>();
        coinsIconRt.sizeDelta = new Vector2(56, 56);
        coinsIconRt.anchoredPosition = new Vector2(-60, 0);
        UIFloat coinsFloat = coinsIconGO.AddComponent<UIFloat>();
        coinsFloat.floatAmount = 4f;
        coinsFloat.speed = 3f;

        GameObject coinsValueGO = CreateLabel("CoinsValue", coinsRow.transform, "0", 44, new Vector2(30, 0));
        Text coinsValueText = coinsValueGO.GetComponent<Text>();
        coinsValueText.color = new Color(0.98f, 0.82f, 0.15f);
        coinsValueText.fontStyle = FontStyle.Bold;

        // Three upgrade rows
        Button magnetBtn, boostBtn, doubleBtn;
        Text magnetLevel, magnetSegments, magnetDuration, magnetCost;
        Text boostLevel, boostSegments, boostDuration, boostCost;
        Text doubleLevel, doubleSegments, doubleDuration, doubleCost;

        BuildUpgradeRow(shopPanel.transform, resultCardSprite, buttonSprite, magnetSprite, "MAGNET", 320,
            out magnetBtn, out magnetLevel, out magnetSegments, out magnetDuration, out magnetCost);
        BuildUpgradeRow(shopPanel.transform, resultCardSprite, buttonSprite, boostSprite, "GO FAST", 80,
            out boostBtn, out boostLevel, out boostSegments, out boostDuration, out boostCost);
        BuildUpgradeRow(shopPanel.transform, resultCardSprite, buttonSprite, doubleSprite, "DOUBLE COINS", -160,
            out doubleBtn, out doubleLevel, out doubleSegments, out doubleDuration, out doubleCost);

        shop.totalCoinsText = coinsValueText;
        shop.magnetRow = new PowerupUpgradeUI.UpgradeRow { levelText = magnetLevel, segmentsText = magnetSegments, durationText = magnetDuration, costText = magnetCost, upgradeButton = magnetBtn };
        shop.boostRow = new PowerupUpgradeUI.UpgradeRow { levelText = boostLevel, segmentsText = boostSegments, durationText = boostDuration, costText = boostCost, upgradeButton = boostBtn };
        shop.doubleRow = new PowerupUpgradeUI.UpgradeRow { levelText = doubleLevel, segmentsText = doubleSegments, durationText = doubleDuration, costText = doubleCost, upgradeButton = doubleBtn };

        shopPanel.SetActive(false);
        return shopPanel;
    }

    private static void BuildUpgradeRow(Transform parent, Sprite resultCardSprite, Sprite buttonSprite, Sprite icon, string name, float yPos,
        out Button upgradeButton, out Text levelText, out Text segmentsText, out Text durationText, out Text costText)
    {
        GameObject card = new GameObject(name.Replace(" ", "") + "Row");
        card.transform.SetParent(parent, false);
        Image cardImg = card.AddComponent<Image>();
        cardImg.sprite = resultCardSprite;
        cardImg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.35f, 0.35f, 0.4f);
        cardOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(900, 200);
        cardRt.anchoredPosition = new Vector2(0, yPos);

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(card.transform, false);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = icon;
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(100, 100);
        iconRt.anchoredPosition = new Vector2(-370, 0);

        GameObject nameGO = CreateLabel("Name", card.transform, name, 26, new Vector2(-160, 60));
        nameGO.GetComponent<Text>().color = Color.white;
        nameGO.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.sizeDelta = new Vector2(340, 50);

        GameObject levelGO = CreateLabel("Level", card.transform, "LEVEL 1/5", 20, new Vector2(-160, 15));
        levelText = levelGO.GetComponent<Text>();
        levelText.color = new Color(0.85f, 0.85f, 0.85f);
        RectTransform levelRt = levelGO.GetComponent<RectTransform>();
        levelRt.sizeDelta = new Vector2(340, 40);

        GameObject segmentsGO = CreateLabel("Segments", card.transform, "■ □ □ □ □", 22, new Vector2(-160, -25));
        segmentsText = segmentsGO.GetComponent<Text>();
        segmentsText.color = new Color(0.95f, 0.72f, 0.15f);
        RectTransform segmentsRt = segmentsGO.GetComponent<RectTransform>();
        segmentsRt.sizeDelta = new Vector2(340, 40);

        GameObject durationGO = CreateLabel("Duration", card.transform, "5.0s DURATION", 18, new Vector2(-160, -60));
        durationText = durationGO.GetComponent<Text>();
        durationText.color = new Color(0.7f, 0.7f, 0.7f);
        RectTransform durationRt = durationGO.GetComponent<RectTransform>();
        durationRt.sizeDelta = new Vector2(340, 40);

        GameObject upgradeGO = new GameObject("UpgradeButton");
        upgradeGO.transform.SetParent(card.transform, false);
        Image upgradeImg = upgradeGO.AddComponent<Image>();
        ApplyButtonLook(upgradeImg, buttonSprite, new Color(0.95f, 0.72f, 0.15f));
        RectTransform upgradeRt = upgradeGO.GetComponent<RectTransform>();
        upgradeRt.sizeDelta = new Vector2(200, 70);
        upgradeRt.anchoredPosition = new Vector2(330, 25);
        upgradeButton = upgradeGO.AddComponent<Button>();
        upgradeButton.transition = Selectable.Transition.None;

        GameObject upgradeLabelGO = CreateLabel("Text", upgradeGO.transform, "UPGRADE", 22);
        upgradeLabelGO.GetComponent<Text>().color = new Color(0.15f, 0.1f, 0.02f);
        upgradeLabelGO.GetComponent<Text>().fontStyle = FontStyle.Bold;

        GameObject costGO = CreateLabel("Cost", card.transform, "100 COINS", 20, new Vector2(330, -35));
        costText = costGO.GetComponent<Text>();
        costText.color = new Color(0.95f, 0.72f, 0.15f);
        RectTransform costRt = costGO.GetComponent<RectTransform>();
        costRt.sizeDelta = new Vector2(220, 40);
    }

    private static GameObject BuildStarterPowerShopPanel(Transform parent, Sprite resultCardSprite, Sprite buttonSprite, Sprite coinSprite, Sprite magnetSprite, Sprite boostSprite, Sprite doubleSprite, Sprite hammerSprite)
    {
        GameObject shopPanel = new GameObject("StarterPowerShopPanel");
        shopPanel.transform.SetParent(parent, false);
        RectTransform rt = shopPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1000, 1400);
        rt.anchoredPosition = new Vector2(0, 80);

        StarterPowerShopUI shop = shopPanel.AddComponent<StarterPowerShopUI>();

        // Header bar
        GameObject hbGO = new GameObject("HeaderBar");
        hbGO.transform.SetParent(shopPanel.transform, false);
        Image hbImg = hbGO.AddComponent<Image>();
        hbImg.sprite = resultCardSprite;
        hbImg.color = new Color(0.08f, 0.45f, 0.85f, 1f);
        Outline hbOutline = hbGO.AddComponent<Outline>();
        hbOutline.effectColor = Color.white;
        hbOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform hbRt = hbGO.GetComponent<RectTransform>();
        hbRt.sizeDelta = new Vector2(900, 80);
        hbRt.anchoredPosition = new Vector2(0, 620);

        GameObject hbTxtGO = CreateLabel("Text", hbGO.transform, "SHOP", 28, Vector2.zero);
        hbTxtGO.GetComponent<Text>().color = Color.white;
        hbTxtGO.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform hbtRt = hbTxtGO.GetComponent<RectTransform>();
        hbtRt.anchorMin = Vector2.zero;
        hbtRt.anchorMax = Vector2.one;
        hbtRt.offsetMin = Vector2.zero;
        hbtRt.offsetMax = Vector2.zero;

        // Total coins display
        GameObject coinsRow = new GameObject("CoinsRow");
        coinsRow.transform.SetParent(shopPanel.transform, false);
        RectTransform coinsRowRt = coinsRow.AddComponent<RectTransform>();
        coinsRowRt.anchorMin = new Vector2(0.5f, 0.5f);
        coinsRowRt.anchorMax = new Vector2(0.5f, 0.5f);
        coinsRowRt.sizeDelta = Vector2.zero;
        coinsRowRt.anchoredPosition = new Vector2(0, 500);

        GameObject coinsIconGO = new GameObject("CoinIcon");
        coinsIconGO.transform.SetParent(coinsRow.transform, false);
        Image coinsIconImg = coinsIconGO.AddComponent<Image>();
        coinsIconImg.sprite = coinSprite;
        RectTransform coinsIconRt = coinsIconGO.GetComponent<RectTransform>();
        coinsIconRt.sizeDelta = new Vector2(56, 56);
        coinsIconRt.anchoredPosition = new Vector2(-60, 0);
        UIFloat coinsFloat = coinsIconGO.AddComponent<UIFloat>();
        coinsFloat.floatAmount = 4f;
        coinsFloat.speed = 3f;

        GameObject coinsValueGO = CreateLabel("CoinsValue", coinsRow.transform, "0", 44, new Vector2(30, 0));
        Text coinsValueText = coinsValueGO.GetComponent<Text>();
        coinsValueText.color = new Color(0.98f, 0.82f, 0.15f);
        coinsValueText.fontStyle = FontStyle.Bold;

        // Four buy rows — 20 coins each, one charge per purchase
        Button magnetBtn, boostBtn, doubleBtn, hammerBtn;
        Text magnetCount, boostCount, doubleCount, hammerCount;

        BuildStarterShopRow(shopPanel.transform, resultCardSprite, buttonSprite, magnetSprite, "MAGNET", 340, out magnetBtn, out magnetCount);
        BuildStarterShopRow(shopPanel.transform, resultCardSprite, buttonSprite, boostSprite, "GO FAST", 150, out boostBtn, out boostCount);
        BuildStarterShopRow(shopPanel.transform, resultCardSprite, buttonSprite, doubleSprite, "DOUBLE COINS", -40, out doubleBtn, out doubleCount);
        BuildStarterShopRow(shopPanel.transform, resultCardSprite, buttonSprite, hammerSprite, "HAMMER", -230, out hammerBtn, out hammerCount);

        shop.totalCoinsText = coinsValueText;
        shop.magnetRow = new StarterPowerShopUI.BuyRow { countText = magnetCount, buyButton = magnetBtn };
        shop.boostRow = new StarterPowerShopUI.BuyRow { countText = boostCount, buyButton = boostBtn };
        shop.doubleRow = new StarterPowerShopUI.BuyRow { countText = doubleCount, buyButton = doubleBtn };
        shop.hammerRow = new StarterPowerShopUI.BuyRow { countText = hammerCount, buyButton = hammerBtn };

        shopPanel.SetActive(false);
        return shopPanel;
    }

    private static void BuildStarterShopRow(Transform parent, Sprite resultCardSprite, Sprite buttonSprite, Sprite icon, string name, float yPos,
        out Button buyButton, out Text countText)
    {
        GameObject card = new GameObject(name.Replace(" ", "") + "BuyRow");
        card.transform.SetParent(parent, false);
        Image cardImg = card.AddComponent<Image>();
        cardImg.sprite = resultCardSprite;
        cardImg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.35f, 0.35f, 0.4f);
        cardOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(900, 170);
        cardRt.anchoredPosition = new Vector2(0, yPos);

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(card.transform, false);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = icon;
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(90, 90);
        iconRt.anchoredPosition = new Vector2(-370, 20);

        GameObject nameGO = CreateLabel("Name", card.transform, name, 26, new Vector2(-160, 40));
        nameGO.GetComponent<Text>().color = Color.white;
        nameGO.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.sizeDelta = new Vector2(340, 50);

        GameObject countGO = CreateLabel("Count", card.transform, "x0", 30, new Vector2(-160, -25));
        countText = countGO.GetComponent<Text>();
        countText.color = new Color(0.95f, 0.72f, 0.15f);
        countText.fontStyle = FontStyle.Bold;
        RectTransform countRt = countGO.GetComponent<RectTransform>();
        countRt.sizeDelta = new Vector2(340, 50);

        GameObject buyGO = new GameObject("BuyButton");
        buyGO.transform.SetParent(card.transform, false);
        Image buyImg = buyGO.AddComponent<Image>();
        ApplyButtonLook(buyImg, buttonSprite, new Color(0.2f, 0.75f, 0.35f));
        RectTransform buyRt = buyGO.GetComponent<RectTransform>();
        buyRt.sizeDelta = new Vector2(220, 80);
        buyRt.anchoredPosition = new Vector2(330, 0);
        buyButton = buyGO.AddComponent<Button>();
        buyButton.transition = Selectable.Transition.None;

        GameObject buyLabelGO = CreateLabel("Text", buyGO.transform, "BUY (" + GameManager.StarterPurchaseCost + ")", 22);
        buyLabelGO.GetComponent<Text>().color = Color.white;
        buyLabelGO.GetComponent<Text>().fontStyle = FontStyle.Bold;
    }

    private static GameObject BuildGameOverPanel(Transform canvasTransform, Sprite buttonSprite, Sprite resultCardSprite, Sprite coinIcon, out Button menuButton, out Button retryButton, out GameObject scoreValueText, out GameObject bestValueText, out GameObject coinsValueText, out UnityEngine.UI.Image medalImage, out GameObject newBestBadge, out RectTransform resultCardTransform)
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
        resultCardTransform.sizeDelta = new Vector2(650, 460);
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

        // 4b. COINS earned this run, tucked under the medal
        GameObject coinsIconGO = new GameObject("CoinsIcon");
        coinsIconGO.transform.SetParent(resultCard.transform, false);
        Image coinsIconImg = coinsIconGO.AddComponent<Image>();
        coinsIconImg.sprite = coinIcon;
        RectTransform coinsIconRt = coinsIconGO.GetComponent<RectTransform>();
        coinsIconRt.sizeDelta = new Vector2(36, 36);
        coinsIconRt.anchoredPosition = new Vector2(-185, -170);

        GameObject coinsVal = CreateLabel("CoinsValue", resultCard.transform, "0", 32, new Vector2(-135, -170));
        coinsValueText = coinsVal;
        Text cvText = coinsVal.GetComponent<Text>();
        cvText.color = new Color(0.98f, 0.82f, 0.15f);
        cvText.fontStyle = FontStyle.Bold;
        cvText.alignment = TextAnchor.MiddleLeft;
        RectTransform coinsValRt = coinsVal.GetComponent<RectTransform>();
        coinsValRt.sizeDelta = new Vector2(100, 50);

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
        menuRt.anchoredPosition = new Vector2(-150, -260); // lowered to clear the taller Results card (grew to fit the COINS row)
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
        retryRt.anchoredPosition = new Vector2(150, -260);
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
        text.raycastTarget = false; // decorative title shouldn't block tap-to-start or nearby buttons

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
        // Caption text should never intercept clicks meant for a parent/neighboring button —
        // without this, this label's default 600x150 hitbox can extend past its small parent
        // button and steal clicks from an adjacent button (e.g. GameOver's MENU/RETRY pair).
        text.raycastTarget = false;
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

    private static GameObject BuildGameManager(GameObject bird, GameObject pipeSpawnerGO, GameObject itemSpawnerGO, GameObject scoreTextGO, GameObject startPanel, GameObject gameOverPanel, AudioClip clickClip, AudioClip scoreClip, GameObject gameOverScoreText, GameObject gameOverBestText, GameObject gameOverCoinsText, GameObject startHighScoreText, UnityEngine.UI.Image medalImage, Sprite bronzeSprite, Sprite silverSprite, Sprite goldSprite, Sprite platinumSprite, Sprite placeholderSprite, GameObject newBestBadge, RectTransform resultCardTransform)
    {
        GameObject go = new GameObject("GameManager");
        GameManager gm = go.AddComponent<GameManager>();
        gm.bird = bird;
        gm.pipeSpawner = pipeSpawnerGO.GetComponent<PipeSpawner>();
        gm.itemSpawner = itemSpawnerGO.GetComponent<ItemSpawner>();
        gm.scoreText = scoreTextGO;
        gm.startPanel = startPanel;
        gm.gameOverPanel = gameOverPanel;
        gm.buttonClickSound = clickClip;
        gm.scoreSound = scoreClip;
        gm.gameOverScoreText = gameOverScoreText;
        gm.gameOverBestText = gameOverBestText;
        gm.gameOverCoinsText = gameOverCoinsText;
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

        // Attach Firebase-backed auth + leaderboard singletons
        go.AddComponent<AuthManager>();
        go.AddComponent<LeaderboardManager>();

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

    private static Texture2D GenerateCoinTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color gold = new Color(0.98f, 0.82f, 0.15f);
        Color darkGold = new Color(0.72f, 0.55f, 0.06f);
        Color highlight = new Color(1f, 0.95f, 0.7f, 0.55f);

        Vector2 center = new Vector2(width / 2f, height / 2f);
        float radius = width * 0.42f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                Color pixel = clear;

                if (dist <= radius)
                {
                    pixel = dist >= radius - width * 0.06f ? darkGold : gold;

                    // Vertical slot line through the middle
                    if (Mathf.Abs(x - center.x) <= width * 0.035f && Mathf.Abs(y - center.y) <= height * 0.28f)
                    {
                        pixel = darkGold;
                    }

                    // Diagonal reflection streak
                    float diag = (x - center.x) - (y - center.y);
                    if (diag > width * 0.05f && diag < width * 0.15f)
                    {
                        pixel = Color.Lerp(pixel, highlight, highlight.a);
                    }
                }
                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateMagnetTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color red = new Color(0.82f, 0.15f, 0.12f);
        Color silver = new Color(0.82f, 0.84f, 0.88f);

        Vector2 center = new Vector2(width / 2f, height * 0.42f);
        float outerRadius = width * 0.32f;
        float innerRadius = width * 0.18f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = clear;
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg; // -180..180, 0 = right, -90 = up

                bool inRing = dist <= outerRadius && dist >= innerRadius;
                // Horseshoe: ring for the bottom half + a bit past horizontal on each side, open at the top.
                bool inArc = !(angle > -160f && angle < -20f); // exclude the top opening
                if (inRing && inArc)
                {
                    pixel = red;
                }

                // Silver pole tips (the two open ends of the horseshoe, pointing up)
                bool inLeftPole = x >= center.x - outerRadius && x <= center.x - innerRadius && y >= center.y - outerRadius * 1.4f && y <= center.y;
                bool inRightPole = x <= center.x + outerRadius && x >= center.x + innerRadius && y >= center.y - outerRadius * 1.4f && y <= center.y;
                if ((inLeftPole || inRightPole) && y <= center.y - outerRadius * 0.85f)
                {
                    pixel = silver;
                }

                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateHammerTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color steel = new Color(0.55f, 0.58f, 0.62f);
        Color steelDark = new Color(0.35f, 0.38f, 0.42f);
        Color wood = new Color(0.55f, 0.35f, 0.15f);
        Color woodDark = new Color(0.35f, 0.2f, 0.08f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = clear;

                // Handle: diagonal wooden shaft
                float handleWidth = width * 0.12f;
                float t = (float)y / height;
                float handleCenterX = width * 0.55f - t * width * 0.15f;
                if (Mathf.Abs(x - handleCenterX) < handleWidth * 0.5f && y < height * 0.72f)
                {
                    pixel = (x < handleCenterX) ? woodDark : wood;
                }

                // Head: rectangular steel block near the top
                if (y >= height * 0.58f && y <= height * 0.85f && x >= width * 0.18f && x <= width * 0.78f)
                {
                    pixel = (y > height * 0.75f) ? steelDark : steel;
                }

                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBoostTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color gold = new Color(1f, 0.82f, 0.1f);
        Color darkGold = new Color(0.85f, 0.55f, 0.02f);

        // Classic lightning bolt as two overlapping triangles.
        Vector2 a = new Vector2(width * 0.58f, height * 0.95f);
        Vector2 b = new Vector2(width * 0.32f, height * 0.48f);
        Vector2 c = new Vector2(width * 0.55f, height * 0.48f);

        Vector2 d = new Vector2(width * 0.42f, height * 0.05f);
        Vector2 e = new Vector2(width * 0.68f, height * 0.52f);
        Vector2 f = new Vector2(width * 0.45f, height * 0.52f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);
                Color pixel = clear;
                if (PointInTriangle(p, a, b, c) || PointInTriangle(p, d, e, f))
                {
                    pixel = (y > height * 0.5f) ? darkGold : gold;
                }
                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateDoubleTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color purple = new Color(0.55f, 0.2f, 0.85f);
        Color lightPurple = new Color(0.82f, 0.6f, 1f);
        Color glow = new Color(0.7f, 0.4f, 1f, 0.35f);

        Vector2 center = new Vector2(width / 2f, height / 2f);
        float radius = width * 0.38f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                Color pixel = clear;

                if (dist <= radius * 1.25f && dist > radius)
                {
                    pixel = glow; // soft outer glow
                }
                else if (dist <= radius)
                {
                    pixel = Color.Lerp(lightPurple, purple, Mathf.Clamp01(dist / radius));

                    // Two accent rings to read as "double" without needing bitmap text.
                    float ring1 = Mathf.Abs(dist - radius * 0.4f);
                    float ring2 = Mathf.Abs(dist - radius * 0.7f);
                    if (ring1 < width * 0.02f || ring2 < width * 0.02f)
                    {
                        pixel = lightPurple;
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

        string[] names = { "Classic", "Space", "Football", "Dragon", "Fish", "Bee", "Ninja", "Mario", "Mars" };
        ThemeData[] themeAssets = new ThemeData[9];

        // Ensure directories for Sprites
        for (int i = 0; i < 9; i++)
        {
            string spriteDir = $"{SpriteFolder}/{names[i]}";
            if (!AssetDatabase.IsValidFolder(spriteDir))
            {
                AssetDatabase.CreateFolder(SpriteFolder, names[i]);
            }
        }

        for (int i = 0; i < 9; i++)
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
        else if (index == 7) data.themeColor = new Color(0.36f, 0.58f, 0.98f); // Mario sky blue
        else if (index == 8) data.themeColor = new Color(0.28f, 0.10f, 0.06f); // Mars dark red

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
        else if (index == 7) // Mario: Bricks on top, Green Warp Pipe on bottom
        {
            data.obstacleTopSprite = GetOrCreateSprite($"{name}/ObstacleTopBricks", 256, 256, (w, h) => GenerateThemeTexture("MarioBricks", index, w, h), 256, FilterMode.Point);
            data.obstacleBottomSprite = GetOrCreateSprite($"{name}/ObstacleBottomPipe", 256, 256, (w, h) => GenerateThemeTexture("MarioPipeBody", index, w, h), 256, FilterMode.Point);
            data.obstacleTopCapSprite = null; // Bricks don't need caps
            data.obstacleBottomCapSprite = GetOrCreateSprite($"{name}/ObstacleBottomCap", 256, 256, (w, h) => GenerateThemeTexture("MarioPipeCap", index, w, h), 256, FilterMode.Point);
        }
        else if (index == 8) // Mars: Dark rocky spire columns
        {
            data.obstacleTopSprite    = GetOrCreateSprite($"{name}/ObstacleTop",    256, 256, (w, h) => GenerateThemeTexture("ObstacleBody", index, w, h), 256, FilterMode.Point);
            data.obstacleBottomSprite = data.obstacleTopSprite;
            data.obstacleTopCapSprite    = GetOrCreateSprite($"{name}/ObstacleCap", 256, 256, (w, h) => GenerateThemeTexture("ObstacleCap",  index, w, h), 256, FilterMode.Point);
            data.obstacleBottomCapSprite = data.obstacleTopCapSprite;
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

        if (type == "MarioBricks")
        {
            Color brickCol = new Color(0.85f, 0.35f, 0.12f);
            Color brickShadow = new Color(0.5f, 0.15f, 0.05f);
            Color brickHighlight = new Color(0.98f, 0.6f, 0.45f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int rowHeight = height / 4;
                    int row = y / rowHeight;
                    int colWidth = width / 2;
                    int colOffset = (row % 2 == 0) ? 0 : colWidth / 2;

                    int localY = y % rowHeight;
                    int localX = (x + colOffset) % colWidth;

                    if (localY == 0 || localY == rowHeight - 1 || localX == 0 || localX == colWidth - 1)
                    {
                        tex.SetPixel(x, y, brickShadow);
                    }
                    else if (localY == 1 || localX == 1)
                    {
                        tex.SetPixel(x, y, brickHighlight);
                    }
                    else
                    {
                        tex.SetPixel(x, y, brickCol);
                    }
                }
            }
            tex.Apply();
            return tex;
        }
        else if (type == "MarioPipeBody")
        {
            Color pipeGreen = new Color(0.0f, 0.75f, 0.0f);
            Color pipeHighlight = new Color(0.5f, 0.95f, 0.5f);
            Color pipeShadow = new Color(0.0f, 0.4f, 0.0f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pCol = pipeGreen;
                    float ratio = (float)x / width;

                    if (ratio < 0.15f) pCol = Color.Lerp(pipeShadow, pipeHighlight, ratio / 0.15f);
                    else if (ratio < 0.35f) pCol = Color.Lerp(pipeHighlight, pipeGreen, (ratio - 0.15f) / 0.2f);
                    else if (ratio > 0.7f) pCol = Color.Lerp(pipeGreen, pipeShadow, (ratio - 0.7f) / 0.3f);

                    if (x == (int)(width * 0.22f) || x == (int)(width * 0.28f) || x == (int)(width * 0.78f))
                    {
                        pCol = pipeShadow;
                    }

                    if (x < 12 || x > width - 12) pCol = pipeShadow;

                    tex.SetPixel(x, y, pCol);
                }
            }
            tex.Apply();
            return tex;
        }
        else if (type == "MarioPipeCap")
        {
            Color pipeGreen = new Color(0.0f, 0.75f, 0.0f);
            Color pipeHighlight = new Color(0.5f, 0.95f, 0.5f);
            Color pipeShadow = new Color(0.0f, 0.4f, 0.0f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pCol = pipeGreen;
                    float ratio = (float)x / width;

                    if (ratio < 0.15f) pCol = Color.Lerp(pipeShadow, pipeHighlight, ratio / 0.15f);
                    else if (ratio < 0.35f) pCol = Color.Lerp(pipeHighlight, pipeGreen, (ratio - 0.15f) / 0.2f);
                    else if (ratio > 0.7f) pCol = Color.Lerp(pipeGreen, pipeShadow, (ratio - 0.7f) / 0.3f);

                    if (x == (int)(width * 0.22f) || x == (int)(width * 0.28f) || x == (int)(width * 0.78f))
                    {
                        pCol = pipeShadow;
                    }

                    if (x < 8 || x > width - 8 || y < 12 || y > height - 12) pCol = pipeShadow;

                    tex.SetPixel(x, y, pCol);
                }
            }
            tex.Apply();
            return tex;
        }

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
            else if (index == 7) { bottomColor = new Color(0.36f, 0.58f, 0.98f); topColor = new Color(0.36f, 0.58f, 0.98f); } // Mario sky blue
            else if (index == 8) { bottomColor = new Color(0.30f, 0.12f, 0.06f); topColor = new Color(0.06f, 0.04f, 0.10f); } // Mars: dark red-brown ground to deep indigo sky

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                Color col = Color.Lerp(bottomColor, topColor, t);
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, col);
                    if (index == 1)
                    {
                        // Determine star presence via deterministic hash
                        float h1 = Mathf.Abs(Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f % 1f);
                        float h2 = Mathf.Abs(Mathf.Sin(x * 269.5f + y * 183.3f) * 43758.5453f % 1f);

                        // White cross-shaped plus sparkle stars — sparse, only brightest
                        if (h1 > 0.9985f)
                        {
                            float brightness = 0.75f + h2 * 0.25f;
                            Color starCol = new Color(brightness, brightness, brightness, 1f);
                            tex.SetPixel(x, y, starCol);
                            if (x > 0) tex.SetPixel(x - 1, y, starCol * 0.5f);
                            if (x < width - 1) tex.SetPixel(x + 1, y, starCol * 0.5f);
                            if (y > 0) tex.SetPixel(x, y - 1, starCol * 0.5f);
                            if (y < height - 1) tex.SetPixel(x, y + 1, starCol * 0.5f);
                        }
                        // Occasional cyan pixel dot accent
                        else if (h1 > 0.9975f && h2 > 0.6f)
                        {
                            tex.SetPixel(x, y, new Color(0.2f, 0.9f, 1f, 0.85f));
                        }
                        // Occasional purple pixel dot accent
                        else if (h1 > 0.9968f && h2 < 0.35f)
                        {
                            tex.SetPixel(x, y, new Color(0.7f, 0.3f, 1f, 0.80f));
                        }
                        // Dim small background dot stars — reduced count
                        else if (h1 > 0.994f)
                        {
                            float dim = 0.25f + h2 * 0.35f;
                            tex.SetPixel(x, y, new Color(dim, dim, dim + 0.08f, 1f));
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
                    else if (index == 7) // Mario retro hills with black outline and black oval eyes
                    {
                        // Define three distinct green hills across the screen
                        float[] centers = { 250f, 750f, 1250f };
                        float[] halfWidths = { 100f, 150f, 120f };
                        float[] hillHeights = { 160f, 220f, 180f };

                        bool insideHill = false;
                        bool isOutline = false;
                        bool isEye = false;

                        for (int h = 0; h < 3; h++)
                        {
                            float dx = x - centers[h];
                            float hw = halfWidths[h];
                            float hh = hillHeights[h];
                            
                            if (dx >= -hw && dx <= hw)
                            {
                                float ratio = dx / hw;
                                float currentHillY = (1f - ratio * ratio) * hh;
                                if (y < currentHillY)
                                {
                                    insideHill = true;
                                    if (y > currentHillY - 4f)
                                    {
                                        isOutline = true;
                                    }
                                    
                                    // Eye calculations near the peak of the hill
                                    float eyeYCenter = hh * 0.75f;
                                    if (Mathf.Abs(y - eyeYCenter) < 12f)
                                    {
                                        if (Mathf.Abs(dx - 8f) < 3f || Mathf.Abs(dx + 8f) < 3f)
                                        {
                                            isEye = true;
                                        }
                                    }
                                }
                            }
                        }

                        if (isOutline || isEye)
                        {
                            tex.SetPixel(x, y, Color.black);
                        }
                        else if (insideHill)
                        {
                            tex.SetPixel(x, y, new Color(0.0f, 0.72f, 0.0f)); // classic green
                        }
                    }
                    // Mars: glowing amber/orange sun on horizon + jagged dark rock spire silhouettes
                    else if (index == 8)
                    {
                        // Sun glow — large warm circle at horizontal center-bottom
                        float sunX = width * 0.5f;
                        float sunY = height * 0.32f;
                        float distSun = Mathf.Sqrt((x - sunX)*(x - sunX)*0.6f + (y - sunY)*(y - sunY));

                        // Atmospheric glow falloff
                        float glow = 1f - Mathf.Clamp01(distSun / (height * 0.38f));
                        Color skyCol = tex.GetPixel(x, y);
                        Color glowColor = new Color(0.95f, 0.55f, 0.18f) * (glow * glow * 1.4f);
                        Color blended = new Color(
                            Mathf.Clamp01(skyCol.r + glowColor.r),
                            Mathf.Clamp01(skyCol.g + glowColor.g * 0.5f),
                            Mathf.Clamp01(skyCol.b + glowColor.b * 0.1f));
                        tex.SetPixel(x, y, blended);

                        // Crisp white sun disc
                        if (distSun < height * 0.07f)
                            tex.SetPixel(x, y, new Color(1f, 0.95f, 0.78f));

                        // Jagged dark rock spire silhouettes on left and right
                        // Left cluster
                        float[] lSpireX = { width*0.04f, width*0.10f, width*0.16f, width*0.22f, width*0.02f, width*0.13f };
                        float[] lSpireH = { height*0.62f, height*0.75f, height*0.58f, height*0.50f, height*0.45f, height*0.68f };
                        float[] lSpireW = { width*0.04f, width*0.05f, width*0.04f, width*0.035f, width*0.03f, width*0.04f };
                        // Right cluster
                        float[] rSpireX = { width*0.78f, width*0.85f, width*0.90f, width*0.96f, width*0.82f, width*0.93f };
                        float[] rSpireH = { height*0.58f, height*0.72f, height*0.60f, height*0.48f, height*0.50f, height*0.65f };
                        float[] rSpireW = { width*0.04f, width*0.05f, width*0.04f, width*0.035f, width*0.03f, width*0.04f };

                        bool inSpire = false;
                        for (int s = 0; s < 6; s++)
                        {
                            // Left spires: jagged triangle shape
                            float dx = Mathf.Abs(x - lSpireX[s]);
                            float spireEdge = lSpireH[s] * (1f - dx / lSpireW[s]);
                            if (dx < lSpireW[s] && y < spireEdge) { inSpire = true; break; }
                            // Right spires
                            dx = Mathf.Abs(x - rSpireX[s]);
                            spireEdge = rSpireH[s] * (1f - dx / rSpireW[s]);
                            if (dx < rSpireW[s] && y < spireEdge) { inSpire = true; break; }
                        }
                        if (inSpire)
                        {
                            float darkT = (float)y / height;
                            tex.SetPixel(x, y, new Color(
                                Mathf.Lerp(0.18f, 0.08f, darkT),
                                Mathf.Lerp(0.10f, 0.04f, darkT),
                                Mathf.Lerp(0.12f, 0.06f, darkT)));
                        }

                        // Sparse stars in upper half
                        if (y > height * 0.55f)
                        {
                            float h1 = Mathf.Abs(Mathf.Sin(x * 197.3f + y * 311.7f) * 43758.5453f % 1f);
                            if (h1 > 0.997f) tex.SetPixel(x, y, new Color(1f, 0.9f, 0.8f, 0.9f));
                        }
                    }
                }
            }
        }
        else if (type == "GroundDirt")
        {
            Color dirtBase = new Color(0.85f, 0.68f, 0.38f);
            Color hatch = new Color(0.68f, 0.5f, 0.25f);
            Color lineColor = new Color(0.55f, 0.38f, 0.18f);

            if (index == 1) // Space
            {
                dirtBase = new Color(0.12f, 0.12f, 0.16f);
                hatch = new Color(0.18f, 0.18f, 0.24f);
                lineColor = new Color(0.08f, 0.08f, 0.1f);
            }
            else if (index == 2) // Football
            {
                dirtBase = new Color(0.38f, 0.22f, 0.08f);
                hatch = new Color(0.48f, 0.3f, 0.15f);
                lineColor = new Color(0.28f, 0.15f, 0.05f);
            }
            else if (index == 3) // Dragon
            {
                dirtBase = new Color(0.18f, 0.08f, 0.08f);
                hatch = new Color(0.28f, 0.12f, 0.12f);
                lineColor = new Color(0.1f, 0.03f, 0.03f);
            }
            else if (index == 4) // Fish
            {
                dirtBase = new Color(0.85f, 0.72f, 0.45f);
                hatch = new Color(0.92f, 0.8f, 0.55f);
                lineColor = new Color(0.7f, 0.58f, 0.32f);
            }
            else if (index == 5) // Bee
            {
                dirtBase = new Color(0.32f, 0.2f, 0.06f);
                hatch = new Color(0.42f, 0.28f, 0.1f);
                lineColor = new Color(0.22f, 0.12f, 0.03f);
            }
            else if (index == 6) // Ninja
            {
                dirtBase = new Color(0.2f, 0.18f, 0.16f);
                hatch = new Color(0.28f, 0.25f, 0.22f);
                lineColor = new Color(0.12f, 0.1f, 0.08f);
            }
            else if (index == 7) // Mario
            {
                Color baseCol = new Color(0.85f, 0.35f, 0.12f);       // orange-brown
                Color darkLine = new Color(0.12f, 0.05f, 0.02f);      // dark brown border
                Color highlightCol = new Color(0.98f, 0.85f, 0.7f);   // white/beige highlight
                Color shadowCol = new Color(0.55f, 0.2f, 0.05f);      // dark brown shadow

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int bx = x % 32;
                        int by = y % 32;

                        Color px = baseCol;

                        if (bx == 0 || bx == 31 || by == 0 || by == 31)
                        {
                            px = darkLine;
                        }
                        else if ((bx <= 3 && by >= 29) || (bx <= 3 && bx == (31 - by)) || (by >= 29 && bx == by))
                        {
                            px = highlightCol;
                        }
                        else if (bx >= 28 || by <= 3)
                        {
                            px = shadowCol;
                        }
                        else if ((bx >= 12 && bx <= 14 && by >= 10 && by <= 22) || 
                                 (by >= 12 && by <= 14 && bx >= 10 && bx <= 22) ||
                                 (bx >= 20 && bx <= 22 && by >= 16 && by <= 26) ||
                                 (by >= 20 && by <= 22 && bx >= 16 && bx <= 26))
                        {
                            px = darkLine;
                        }

                        tex.SetPixel(x, y, px);
                    }
                }
                tex.Apply();
                return tex;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = dirtBase;

                    // Sediment bands
                    int layer = y / 48;
                    if (layer % 2 == 0)
                    {
                        pixel = Color.Lerp(dirtBase, hatch, 0.4f);
                    }

                    // Diagonal hatches
                    int diag = (x + y) % 32;
                    if (diag < 2)
                    {
                        pixel = hatch;
                    }

                    // Layer divider lines
                    if (y == 24 || y == 25 || y == 96 || y == 97 || y == 160 || y == 161 || y == 220 || y == 221)
                    {
                        pixel = lineColor;
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            // Pebble stones
            System.Random rng = new System.Random(index * 23 + 101);
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
        }
        else if (type == "GroundGrass")
        {
            Color grass = new Color(0.38f, 0.8f, 0.28f);
            Color grassHighlight = new Color(0.58f, 0.92f, 0.38f);
            Color grassShadow = new Color(0.24f, 0.6f, 0.16f);

            if (index == 1) // Space
            {
                grass = new Color(0f, 0.75f, 0.75f);
                grassHighlight = new Color(0f, 0.95f, 0.95f);
                grassShadow = new Color(0f, 0.5f, 0.5f);
            }
            else if (index == 2) // Football
            {
                grass = new Color(0.15f, 0.65f, 0.15f);
                grassHighlight = new Color(0.35f, 0.85f, 0.35f);
                grassShadow = new Color(0.05f, 0.45f, 0.05f);
            }
            else if (index == 3) // Dragon
            {
                grass = new Color(0.85f, 0.2f, 0.05f);
                grassHighlight = new Color(0.98f, 0.45f, 0.15f);
                grassShadow = new Color(0.6f, 0.1f, 0.02f);
            }
            else if (index == 4) // Fish
            {
                grass = new Color(0.1f, 0.65f, 0.35f);
                grassHighlight = new Color(0.3f, 0.82f, 0.55f);
                grassShadow = new Color(0.05f, 0.45f, 0.2f);
            }
            else if (index == 5) // Bee
            {
                grass = new Color(0.9f, 0.75f, 0.12f);
                grassHighlight = new Color(0.98f, 0.88f, 0.25f);
                grassShadow = new Color(0.7f, 0.55f, 0.05f);
            }
            else if (index == 6) // Ninja
            {
                grass = new Color(0.35f, 0.45f, 0.35f);
                grassHighlight = new Color(0.5f, 0.6f, 0.5f);
                grassShadow = new Color(0.2f, 0.3f, 0.2f);
            }
            else if (index == 8) // Mars: jagged dark rocky rim with faint blue crystal accents
            {
                Color rockBase  = new Color(0.22f, 0.08f, 0.04f); // dark maroon rock
                Color rockDark  = new Color(0.12f, 0.04f, 0.02f);
                Color crystalTip = new Color(0.15f, 0.65f, 0.80f); // blue crystal glint

                int toothW = Mathf.Max(6, width / 14);
                float toothH = height * 0.40f;
                float baseT  = height * 0.50f;

                for (int x = 0; x < width; x++)
                {
                    float phase = (x % toothW) / (float)toothW;
                    // Irregular jagged spire shape (not smooth triangle)
                    float jagged = (1f - Mathf.Abs(phase - 0.5f) * 2f);
                    jagged = Mathf.Pow(jagged, 0.6f); // sharper peaks
                    float edgeY = baseT + jagged * toothH;

                    // Add per-spire noise using sine
                    edgeY += Mathf.Sin(x * 0.3f) * 6f;

                    for (int y = 0; y < height; y++)
                    {
                        if (y > edgeY)
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        else if (y > edgeY - 5f)
                        {
                            // Crystal tip glow on top of tallest spires
                            tex.SetPixel(x, y, jagged > 0.7f ? crystalTip : rockDark);
                        }
                        else
                        {
                            tex.SetPixel(x, y, rockBase);
                        }
                    }
                }
                tex.Apply();
                return tex;
            }
            else if (index == 7) // Mario: custom repeating blocky tiles with a flat top black rim
            {
                Color baseCol = new Color(0.85f, 0.35f, 0.12f);       // orange-brown
                Color darkLine = new Color(0.12f, 0.05f, 0.02f);      // dark brown border
                Color highlightCol = new Color(0.98f, 0.85f, 0.7f);   // white/beige highlight
                Color shadowCol = new Color(0.55f, 0.2f, 0.05f);      // dark brown shadow

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int bx = x % 32;
                        int by = y % 32;

                        Color px = baseCol;

                        if (y >= height - 4)
                        {
                            px = darkLine;
                        }
                        else if (bx == 0 || bx == 31 || by == 0 || by == 31)
                        {
                            px = darkLine;
                        }
                        else if ((bx <= 3 && by >= 29) || (bx <= 3 && bx == (31 - by)) || (by >= 29 && bx == by))
                        {
                            px = highlightCol;
                        }
                        else if (bx >= 28 || by <= 3)
                        {
                            px = shadowCol;
                        }
                        else if ((bx >= 12 && bx <= 14 && by >= 10 && by <= 22) || 
                                 (by >= 12 && by <= 14 && bx >= 10 && bx <= 22) ||
                                 (bx >= 20 && bx <= 22 && by >= 16 && by <= 26) ||
                                 (by >= 20 && by <= 22 && bx >= 16 && bx <= 26))
                        {
                            px = darkLine;
                        }

                        tex.SetPixel(x, y, px);
                    }
                }
                tex.Apply();
                return tex;
            }

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
                        tex.SetPixel(x, y, Color.clear);
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
        }
        else if (type == "ObstacleBody")
        {
            if (index == 1) // Space asteroid rock column
            {

                // Asteroid-rock pillar with teal glow trim on the inner edge. Brightened well above the
                // near-black starfield background — the original near-black rock tones made the whole
                // obstacle "wipe out" (blend invisibly) against Space's dark backdrop.
                Color rockBase  = new Color(0.24f, 0.28f, 0.40f); // lit charcoal rock
                Color rockMid   = new Color(0.34f, 0.39f, 0.53f); // mid rock tone
                Color rockLight = new Color(0.48f, 0.54f, 0.68f); // lighter rock vein
                Color rockDark  = new Color(0.15f, 0.17f, 0.24f); // shadow crevice (still visible, not near-black)
                Color tealGlow  = new Color(0.05f, 0.75f, 0.80f); // teal inner glow edge
                Color tealFade  = new Color(0.03f, 0.40f, 0.48f); // teal glow fade

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color px;

                        // Teal glow strip on the inner (right) edge
                        if (x >= width - 6)
                            px = x >= width - 2 ? tealGlow : tealFade;
                        else
                        {
                            // Rocky noise using two sine waves to fake craggy texture
                            float noise1 = Mathf.Sin(x * 0.31f + y * 0.17f) * Mathf.Cos(y * 0.27f - x * 0.13f);
                            float noise2 = Mathf.Sin(x * 0.09f + y * 0.43f);
                            float rock = (noise1 + noise2) * 0.5f;

                            if (rock > 0.35f)       px = rockLight;
                            else if (rock > 0.05f)  px = rockMid;
                            else if (rock < -0.35f) px = rockDark;
                            else                    px = rockBase;

                            // Dark left-edge shadow bevel
                            if (x < 8)
                                px = Color.Lerp(rockDark, px, x / 8f);
                        }
                        tex.SetPixel(x, y, px);
                    }
                }
                tex.Apply();
                return tex;
            }

            Color col = Color.green;
            if (index == 1) col = new Color(0.35f, 0.35f, 0.4f);
            else if (index == 2) col = Color.white;
            else if (index == 3) col = new Color(0.2f, 0.2f, 0.2f);
            else if (index == 4) col = new Color(0.85f, 0.35f, 0.5f);
            else if (index == 5) col = new Color(0.12f, 0.45f, 0.15f);
            else if (index == 6) col = new Color(0.38f, 0.25f, 0.15f);
            else if (index == 8) // Mars: dark rock spire with blue crystal veins
            {
                Color spireBase  = new Color(0.18f, 0.10f, 0.08f);
                Color spireMid   = new Color(0.24f, 0.14f, 0.10f);
                Color spireLight = new Color(0.32f, 0.18f, 0.12f);
                Color spireDark  = new Color(0.10f, 0.05f, 0.04f);
                Color crystal    = new Color(0.10f, 0.65f, 0.85f);
                Color crystalDim = new Color(0.06f, 0.35f, 0.50f);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float n1 = Mathf.Sin(x * 0.27f + y * 0.19f) * Mathf.Cos(y * 0.23f - x * 0.11f);
                        float n2 = Mathf.Sin(x * 0.07f + y * 0.41f);
                        float rock = (n1 + n2) * 0.5f;
                        Color px = rock > 0.35f ? spireLight : (rock > 0.05f ? spireMid : (rock < -0.35f ? spireDark : spireBase));
                        float vein = Mathf.Sin(x * 0.18f - y * 0.28f + 1.5f);
                        if (vein > 0.82f) px = crystal;
                        else if (vein > 0.72f) px = crystalDim;
                        if (x < 10) px = Color.Lerp(spireDark, px, x / 10f);
                        if (x >= width - 5) px = x >= width - 2 ? crystal : crystalDim;
                        tex.SetPixel(x, y, px);
                    }
                }
                tex.Apply();
                return tex;
            }

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
            if (index == 1) // Space asteroid rock cap — glowing teal inner rim
            {
                // Brightened to match the ObstacleBody fix above — see comment there.
                Color rockBase  = new Color(0.26f, 0.30f, 0.42f);
                Color rockMid   = new Color(0.36f, 0.42f, 0.55f);
                Color rockLight = new Color(0.50f, 0.56f, 0.70f);
                Color rockDark  = new Color(0.16f, 0.18f, 0.25f);
                Color tealGlow  = new Color(0.05f, 0.85f, 0.90f);
                Color tealMid   = new Color(0.03f, 0.55f, 0.62f);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color px;
                        bool topEdge    = (y > height - 10);
                        bool bottomEdge = (y < 10);
                        bool sideEdge   = (x < 8 || x > width - 8);

                        if (topEdge)
                        {
                            // Top inner glow edge — bright teal bar across full width
                            float edgeFade = (y - (height - 10f)) / 10f;
                            px = Color.Lerp(tealMid, tealGlow, edgeFade);
                        }
                        else if (bottomEdge || sideEdge)
                        {
                            px = rockDark;
                        }
                        else
                        {
                            float noise1 = Mathf.Sin(x * 0.28f + y * 0.19f) * Mathf.Cos(y * 0.23f - x * 0.11f);
                            float noise2 = Mathf.Sin(x * 0.11f + y * 0.37f);
                            float rock = (noise1 + noise2) * 0.5f;

                            if (rock > 0.35f)       px = rockLight;
                            else if (rock > 0.05f)  px = rockMid;
                            else if (rock < -0.35f) px = rockDark;
                            else                    px = rockBase;
                        }
                        tex.SetPixel(x, y, px);
                    }
                }
                tex.Apply();
                return tex;
            }

            Color col = Color.green;
            if (index == 1) col = new Color(0.45f, 0.45f, 0.5f);
            else if (index == 2) col = Color.white;
            else if (index == 3) col = new Color(0.3f, 0.3f, 0.3f);
            else if (index == 4) col = new Color(0.95f, 0.45f, 0.6f);
            else if (index == 5) col = new Color(0.95f, 0.72f, 0.1f);
            else if (index == 6) col = new Color(0.55f, 0.55f, 0.55f);
            else if (index == 8) // Mars: glowing blue crystal cluster cap
            {
                Color cBright = new Color(0.20f, 0.85f, 1.00f);
                Color cMid    = new Color(0.10f, 0.55f, 0.78f);
                Color cDark   = new Color(0.04f, 0.28f, 0.42f);
                Color rBase   = new Color(0.18f, 0.08f, 0.06f);
                Color rDark   = new Color(0.08f, 0.04f, 0.03f);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color px = rDark;
                        int numSpikes = 5;
                        for (int k = 0; k < numSpikes; k++)
                        {
                            float cx = width * (k + 0.5f) / numSpikes;
                            float halfW = width * 0.07f;
                            float dx = Mathf.Abs(x - cx);
                            if (dx < halfW)
                            {
                                float spikeH = height * (0.55f + 0.35f * (1f - Mathf.Abs(k - numSpikes/2f) / (numSpikes/2f)));
                                float taperT = 1f - dx / halfW;
                                if (y < spikeH * taperT)
                                    px = taperT > 0.6f ? cBright : (taperT > 0.3f ? cMid : cDark);
                            }
                        }
                        if (y < height * 0.22f) px = rBase;
                        tex.SetPixel(x, y, px);
                    }
                }
                tex.Apply();
                return tex;
            }

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
        else if (index == 7) // Mario
        {
            Color primaryCol = (skin == 0) ? new Color(0.9f, 0.1f, 0.1f) : (skin == 1 ? new Color(0.1f, 0.75f, 0.15f) : new Color(0.95f, 0.45f, 0.65f)); // red cap, green cap, or pink dress
            Color overallsCol = (skin == 0) ? new Color(0.1f, 0.35f, 0.85f) : (skin == 1 ? new Color(0.08f, 0.25f, 0.6f) : new Color(0.95f, 0.85f, 0.15f)); // blue/gold
            Color hairCol = (skin == 2) ? new Color(0.98f, 0.88f, 0.25f) : new Color(0.35f, 0.22f, 0.12f); // blonde/brown

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    // Body/Face circle
                    if (Vector2.Distance(p, center) <= width * 0.32f)
                    {
                        tex.SetPixel(x, y, new Color(0.98f, 0.82f, 0.72f)); // skin tone
                        
                        // Eyes
                        if (Vector2.Distance(p, new Vector2(width * 0.58f, height * 0.54f)) <= 4f ||
                            Vector2.Distance(p, new Vector2(width * 0.66f, height * 0.54f)) <= 4f)
                        {
                            tex.SetPixel(x, y, Color.black);
                        }

                        // Cap (drawn on the upper half)
                        if (y > height * 0.65f && skin < 2)
                        {
                            tex.SetPixel(x, y, primaryCol);
                        }
                    }

                    // Overalls / Dress at the bottom
                    if (Vector2.Distance(p, center) <= width * 0.32f && y < height * 0.38f)
                    {
                        tex.SetPixel(x, y, overallsCol);
                    }

                    // Crown for Princess Peach (skin 2)
                    if (skin == 2 && y > height * 0.65f && y < height * 0.82f && x > width * 0.4f && x < width * 0.6f)
                    {
                        int tx = x - (int)(width * 0.4f);
                        int tw = (int)(width * 0.2f);
                        if (y < height * 0.72f || (tx % (tw/2) < 4))
                        {
                            tex.SetPixel(x, y, new Color(0.98f, 0.82f, 0.1f)); // gold
                        }
                    }

                    // Hair
                    if (skin == 2 && Vector2.Distance(p, center) <= width * 0.34f && x < width * 0.45f)
                    {
                        tex.SetPixel(x, y, hairCol);
                    }
                }
            }
        }
        else if (index == 8) // Mars: 3 themed skins
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x, y);

                    if (skin == 0) // Astronaut — white spacesuit with gold visor
                    {
                        // Helmet large circle
                        if (Vector2.Distance(p, new Vector2(width * 0.5f, height * 0.65f)) <= width * 0.26f)
                        {
                            tex.SetPixel(x, y, new Color(0.92f, 0.92f, 0.93f));
                            // Gold visor strip
                            if (y > height * 0.64f && y < height * 0.76f &&
                                x > width * 0.30f && x < width * 0.70f)
                                tex.SetPixel(x, y, new Color(0.95f, 0.72f, 0.15f));
                            // Visor reflection shine
                            if (y > height * 0.67f && y < height * 0.72f &&
                                x > width * 0.34f && x < width * 0.42f)
                                tex.SetPixel(x, y, new Color(1f, 0.95f, 0.6f, 0.75f));
                        }
                        // Body suit ellipse lower
                        if (IsInsideEllipse(p, new Vector2(width * 0.5f, height * 0.32f), width * 0.24f, height * 0.18f))
                            tex.SetPixel(x, y, new Color(0.88f, 0.88f, 0.90f));
                        // Backpack rectangle
                        if (x < width * 0.28f && y > height * 0.28f && y < height * 0.50f)
                            tex.SetPixel(x, y, new Color(0.70f, 0.70f, 0.74f));
                        // NASA blue stripe
                        if (y > height * 0.40f && y < height * 0.44f &&
                            x > width * 0.35f && x < width * 0.65f)
                            tex.SetPixel(x, y, new Color(0.12f, 0.35f, 0.85f));
                    }
                    else if (skin == 1) // Mars Rover — boxy body + solar panels + wheels
                    {
                        // Main rover body
                        if (x > width * 0.20f && x < width * 0.80f &&
                            y > height * 0.40f && y < height * 0.62f)
                            tex.SetPixel(x, y, new Color(0.72f, 0.62f, 0.48f));
                        // 3 wheels
                        float[] wX = { 0.28f, 0.50f, 0.72f };
                        for (int ww = 0; ww < 3; ww++)
                        {
                            if (Vector2.Distance(p, new Vector2(width*wX[ww], height*0.30f)) <= width*0.09f)
                                tex.SetPixel(x, y, new Color(0.22f, 0.20f, 0.16f));
                            if (Vector2.Distance(p, new Vector2(width*wX[ww], height*0.30f)) <= width*0.05f)
                                tex.SetPixel(x, y, new Color(0.38f, 0.32f, 0.24f));
                        }
                        // Solar panels left + right
                        if (y > height * 0.60f && y < height * 0.76f)
                        {
                            if ((x > width * 0.10f && x < width * 0.32f) ||
                                (x > width * 0.68f && x < width * 0.90f))
                            {
                                tex.SetPixel(x, y, new Color(0.10f, 0.28f, 0.78f));
                                if (x % 10 < 2 || y % 8 < 1)
                                    tex.SetPixel(x, y, new Color(0.06f, 0.18f, 0.52f));
                            }
                        }
                        // Camera mast
                        if (x > width*0.48f && x < width*0.52f && y > height*0.62f && y < height*0.85f)
                            tex.SetPixel(x, y, new Color(0.52f, 0.48f, 0.40f));
                        if (Vector2.Distance(p, new Vector2(width*0.5f, height*0.85f)) <= width*0.05f)
                            tex.SetPixel(x, y, new Color(0.82f, 0.82f, 0.85f));
                    }
                    else // skin == 2: Crystal Explorer — dark teal armor + blue crystal backpack
                    {
                        // Body suit dark teal
                        if (Vector2.Distance(p, new Vector2(width*0.5f, height*0.60f)) <= width*0.24f)
                            tex.SetPixel(x, y, new Color(0.08f, 0.28f, 0.42f));
                        // Helmet visor glowing cyan
                        if (Vector2.Distance(p, new Vector2(width*0.5f, height*0.68f)) <= width*0.18f && y > height * 0.60f)
                            tex.SetPixel(x, y, new Color(0.10f, 0.72f, 0.95f, 0.88f));
                        // Crystal backpack — 3 spikes on left side
                        for (int k = 0; k < 3; k++)
                        {
                            float bx = width * (0.20f - k * 0.04f);
                            float by = height * (0.50f + k * 0.06f);
                            float bh = height * (0.18f - k * 0.03f);
                            float bw = width * 0.032f;
                            for (int ky = 0; ky < height; ky++)
                            {
                                float taperT = 1f - Mathf.Abs(ky - by) / bh;
                                if (taperT > 0 && Mathf.Abs(x - bx) < bw * taperT)
                                    tex.SetPixel(x, ky, k == 0 ?
                                        new Color(0.20f, 0.85f, 1.00f) :
                                        new Color(0.10f, 0.55f, 0.78f));
                            }
                        }
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
                    bool inRoof = PointInTriangle(p, a, b, c);
                    bool inBody = (x >= w * 0.22f && x <= w * 0.78f && y >= h * 0.15f && y <= h * 0.5f);
                    bool inChimney = (x >= w * 0.65f && x <= w * 0.75f && y >= h * 0.5f && y <= h * 0.8f);
                    
                    if (inRoof || inBody || inChimney)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    
                    // Stencil cutouts
                    if (x >= w * 0.43f && x <= w * 0.57f && y >= h * 0.15f && y <= h * 0.35f)
                    {
                        tex.SetPixel(x, y, clear); // Doorway cutout
                    }
                    if (Vector2.Distance(p, new Vector2(w * 0.5f, h * 0.6f)) <= w * 0.07f)
                    {
                        tex.SetPixel(x, y, clear); // Attic window cutout
                    }
                }
            }
        }
        else if (name == "Shop")
        {
            Color roofRed = new Color(0.92f, 0.25f, 0.25f);
            Color roofWhite = Color.white;
            Color wallColor = new Color(0.85f, 0.85f, 0.9f);
            Color baseGold = new Color(0.95f, 0.72f, 0.15f);
            Color doorColor = new Color(0.2f, 0.2f, 0.25f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // 1. Counter base
                    if (x >= w * 0.15f && x <= w * 0.85f && y >= h * 0.15f && y <= h * 0.25f)
                    {
                        tex.SetPixel(x, y, baseGold);
                    }
                    // 2. Pillars/Walls
                    else if (((x >= w * 0.15f && x <= w * 0.25f) || (x >= w * 0.75f && x <= w * 0.85f)) && y >= h * 0.25f && y <= h * 0.70f)
                    {
                        tex.SetPixel(x, y, wallColor);
                    }
                    // 3. Shop window display area background
                    else if (x >= w * 0.25f && x <= w * 0.75f && y >= h * 0.25f && y <= h * 0.70f)
                    {
                        // Door
                        if (x >= w * 0.43f && x <= w * 0.57f && y >= h * 0.25f && y <= h * 0.58f)
                        {
                            tex.SetPixel(x, y, doorColor);
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color(0.12f, 0.45f, 0.75f, 0.3f));
                        }
                    }
                    // 4. Canopy (trapezoid with stripes)
                    else if (y >= h * 0.70f && y <= h * 0.90f)
                    {
                        float t = (float)(y - h * 0.70f) / (h * 0.20f);
                        float minX = Mathf.Lerp(w * 0.10f, w * 0.18f, t);
                        float maxX = Mathf.Lerp(w * 0.90f, w * 0.82f, t);
                        if (x >= minX && x <= maxX)
                        {
                            int stripeIndex = (int)((x - minX) / 14f);
                            tex.SetPixel(x, y, (stripeIndex % 2 == 0) ? roofRed : roofWhite);
                        }
                    }
                }
            }
        }
        else if (name == "Heroes")
        {
            Color gold = new Color(0.95f, 0.72f, 0.15f);
            Color blue = new Color(0.08f, 0.38f, 0.75f);
            Color starColor = Color.white;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y >= h * 0.15f && y <= h * 0.85f)
                    {
                        float dx = Mathf.Abs(x - w * 0.5f);
                        float pct = (y - h * 0.15f) / (h * 0.70f);
                        float maxWidth = w * 0.36f * Mathf.Pow(pct, 0.45f);

                        if (dx <= maxWidth)
                        {
                            float borderThickness = 6f;
                            bool onBorder = (dx > maxWidth - borderThickness) || (y < h * 0.15f + borderThickness) || (y > h * 0.85f - borderThickness);
                            
                            if (onBorder)
                            {
                                tex.SetPixel(x, y, gold);
                            }
                            else
                            {
                                Vector2 p = new Vector2(x, y);
                                if (PointInStar(p, new Vector2(w * 0.5f, h * 0.52f), 14f, 6f))
                                {
                                    tex.SetPixel(x, y, starColor);
                                }
                                else
                                {
                                    tex.SetPixel(x, y, blue);
                                }
                            }
                        }
                    }
                }
            }
        }
        else if (name == "Missions")
        {
            Color board = new Color(0.18f, 0.22f, 0.32f);
            Color paper = Color.white;
            Color clipHold = new Color(0.95f, 0.72f, 0.15f);
            Color checkColor = new Color(0.15f, 0.68f, 0.35f);
            Color lineCol = new Color(0.75f, 0.78f, 0.82f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    bool inBoard = (x >= 28 && x <= w - 28 && y >= 16 && y <= h - 16);
                    bool inPaper = (x >= 35 && x <= w - 35 && y >= 22 && y <= h - 26);
                    bool inClip = (x >= w/2 - 16 && x <= w/2 + 16 && y >= h - 22 && y <= h - 12);
                    bool inClipHole = (x >= w/2 - 6 && x <= w/2 + 6 && y >= h - 18 && y <= h - 14);

                    if (inClip && !inClipHole)
                    {
                        tex.SetPixel(x, y, clipHold);
                    }
                    else if (inPaper)
                    {
                        if (IsInCheckmark(p, new Vector2(48f, 70f)))
                        {
                            tex.SetPixel(x, y, checkColor);
                        }
                        else if (x >= 62 && x <= 90 && y >= 68 && y <= 72)
                        {
                            tex.SetPixel(x, y, lineCol);
                        }
                        else if (IsInCheckmark(p, new Vector2(48f, 45f)))
                        {
                            tex.SetPixel(x, y, checkColor);
                        }
                        else if (x >= 62 && x <= 90 && y >= 43 && y <= 47)
                        {
                            tex.SetPixel(x, y, lineCol);
                        }
                        else
                        {
                            tex.SetPixel(x, y, paper);
                        }
                    }
                    else if (inBoard)
                    {
                        tex.SetPixel(x, y, board);
                    }
                }
            }
        }
        else if (name == "Themes")
        {
            Color planetColorOuter = new Color(0.12f, 0.65f, 0.72f);
            Color planetColorInner = new Color(0.48f, 0.2f, 0.75f);
            Color ringColor = new Color(0.95f, 0.72f, 0.15f);
            
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float r = 26f;
            
            float cos = Mathf.Cos(-25f * Mathf.Deg2Rad);
            float sin = Mathf.Sin(-25f * Mathf.Deg2Rad);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float distSq = dx * dx + dy * dy;
                    
                    float rx = dx * cos - dy * sin;
                    float ry = dx * sin + dy * cos;
                    
                    float a = 44f;
                    float b = 11f;
                    float val = (rx * rx) / (a * a) + (ry * ry) / (b * b);
                    bool isRing = (val >= 0.76f && val <= 1.24f);
                    bool isPlanet = distSq <= r * r;
                    
                    if (isRing)
                    {
                        if (isPlanet && ry < 0)
                        {
                            float t = Mathf.Clamp01(Mathf.Sqrt(distSq) / r);
                            Color col = Color.Lerp(planetColorInner, planetColorOuter, t);
                            tex.SetPixel(x, y, col);
                        }
                        else
                        {
                            tex.SetPixel(x, y, ringColor);
                        }
                    }
                    else if (isPlanet)
                    {
                        float t = Mathf.Clamp01(Mathf.Sqrt(distSq) / r);
                        Color col = Color.Lerp(planetColorInner, planetColorOuter, t);
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

    private static bool PointInStar(Vector2 p, Vector2 center, float outerRadius, float innerRadius)
    {
        Vector2 d = p - center;
        float dist = d.magnitude;
        if (dist > outerRadius) return false;
        if (dist < innerRadius) return true;
        
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        float sector = (angle + 18f) % 72f - 36f;
        float rad = sector * Mathf.Deg2Rad;
        
        float maxR = Mathf.Lerp(outerRadius, innerRadius, Mathf.Abs(rad) / (36f * Mathf.Deg2Rad));
        return dist <= maxR;
    }

    private static bool PointInLineSegment(Vector2 p, Vector2 start, Vector2 end, float thickness)
    {
        Vector2 line = end - start;
        float len = line.magnitude;
        if (len == 0f) return Vector2.Distance(p, start) <= thickness;
        
        Vector2 dir = line / len;
        float projection = Vector2.Dot(p - start, dir);
        if (projection < 0) return Vector2.Distance(p, start) <= thickness;
        if (projection > len) return Vector2.Distance(p, end) <= thickness;
        
        Vector2 closestPoint = start + dir * projection;
        return Vector2.Distance(p, closestPoint) <= thickness;
    }

    private static bool IsInCheckmark(Vector2 p, Vector2 center)
    {
        Vector2 s1 = new Vector2(center.x - 6f, center.y + 1f);
        Vector2 s2 = new Vector2(center.x, center.y - 5f);
        Vector2 s3 = new Vector2(center.x + 9f, center.y + 6f);
        
        return PointInLineSegment(p, s1, s2, 2.5f) || PointInLineSegment(p, s2, s3, 2.5f);
    }
}
