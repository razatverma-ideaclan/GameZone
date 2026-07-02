using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// One-click scene builder for KickUp AR Football.
/// Run via menu: KickUp AR Football > Build Full Scene (Auto Setup)
/// Creates the Camera, Ball, BasketGoal, full Canvas UI (Start/Gameplay/GameOver panels),
/// and the Managers object, then wires every Inspector reference automatically.
/// Safe to run multiple times - it reuses existing objects instead of duplicating them.
/// </summary>
public static class KickUpSceneBuilder
{
    private const string SpriteFolder = "Assets/Sprites";

    [MenuItem("KickUp AR Football/Build Full Scene (Auto Setup)")]
    public static void BuildScene()
    {
        CleanupPreviousUI();
        EnsureSpriteFolder();

        Sprite ballSprite = GetBallSprite();
        Sprite rectSprite = GetOrCreateShapeSprite(SpriteFolder + "/GoalRect.png", false);

        // Polished UI sprites: rounded 9-sliced buttons and a soft card-style panel
        // background, so buttons scale cleanly at any size and text stays readable
        // over the live camera feed.
        Sprite buttonPrimarySprite = GetOrCreateUISprite(SpriteFolder + "/ButtonPrimary.png", new Vector4(30, 30, 30, 30));
        Sprite buttonSecondarySprite = GetOrCreateUISprite(SpriteFolder + "/ButtonSecondary.png", new Vector4(30, 30, 30, 30));
        Sprite panelSprite = GetOrCreateUISprite(SpriteFolder + "/PanelBackground.png", new Vector4(50, 50, 50, 50));

        // ---------------- Camera ----------------
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
            camGO.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.6f, 0.2f);

        // Remove a leftover FlareLayer component (left over from Unity's default 3D
        // camera setup) - with a Directional Light + default Sun flare still in the
        // scene, this renders a bright "sun" disc on screen even in this 2D game.
        Component flareLayer = cam.gameObject.GetComponent("FlareLayer");
        if (flareLayer != null) Object.DestroyImmediate(flareLayer);

        // This is a 2D camera-passthrough game with no lighting needs - remove any
        // leftover Directional Light from Unity's default scene template, which is
        // otherwise the actual source of that "sun" appearance (its lens flare and/or
        // its yellow sun gizmo in the Scene view).
        Light[] leftoverLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in leftoverLights)
        {
            Object.DestroyImmediate(light.gameObject);
        }

        // ---------------- Ball ----------------
        if (!HasTag("Ball")) CreateTag("Ball");

        GameObject ballGO = GameObject.Find("Ball");
        if (ballGO == null) ballGO = new GameObject("Ball");
        ballGO.tag = "Ball";

        var ballSr = GetOrAdd<SpriteRenderer>(ballGO);
        ballSr.sprite = ballSprite;
        ballSr.color = Color.white;

        var ballRb = GetOrAdd<Rigidbody2D>(ballGO);
        ballRb.gravityScale = 1.5f;
        ballRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        GetOrAdd<CircleCollider2D>(ballGO);
        var ballController = GetOrAdd<BallController>(ballGO);

        ballGO.transform.position = new Vector3(0f, -2f, 0f);
        ballGO.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        // ---------------- Basket Goal ----------------
        GameObject basketGO = GameObject.Find("BasketGoal");
        if (basketGO == null) basketGO = new GameObject("BasketGoal");

        var basketSr = GetOrAdd<SpriteRenderer>(basketGO);
        basketSr.sprite = rectSprite;
        basketSr.color = new Color(1f, 0.6f, 0f);

        var basketCollider = GetOrAdd<BoxCollider2D>(basketGO);
        basketCollider.isTrigger = true;

        var basketController = GetOrAdd<BasketGoalController>(basketGO);
        basketController.ballController = ballController;

        basketGO.transform.position = new Vector3(0f, 3.5f, 0f);
        basketGO.transform.localScale = new Vector3(2f, 0.6f, 1f);

        // ---------------- Canvas & EventSystem ----------------
        // IMPORTANT: search by exact NAME, not FindFirstObjectByType<Canvas>().
        // This scene has TWO canvases (this main UI one, and "BackgroundCanvas" for
        // the camera feed) - FindFirstObjectByType<Canvas>() is ambiguous once more
        // than one Canvas exists and can silently return the wrong one on a rebuild,
        // which would put buttons on the wrong canvas and break clicks.
        GameObject canvasGO = GameObject.Find("Canvas");
        Canvas canvas;
        if (canvasGO == null)
        {
            canvasGO = new GameObject("Canvas", typeof(RectTransform));
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas = canvasGO.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasGO.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Defensively ensure the raycaster/scaler are present even if this
            // Canvas object already existed from an earlier run.
            if (canvasGO.GetComponent<GraphicRaycaster>() == null)
            {
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            if (canvasGO.GetComponent<CanvasScaler>() == null)
            {
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        // Defensively fix the EventSystem regardless of whether one already existed.
        // A common cause of "clicks do nothing" is an EventSystem left with the NEW
        // Input System's UI module (InputSystemUIInputModule) while this project's
        // Active Input Handling is set to the OLD Input Manager - that combination
        // silently drops all clicks with no errors. We remove that module if present
        // and make sure the classic StandaloneInputModule is attached instead.
        EventSystem existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
        GameObject eventSystemGO;
        if (existingEventSystem == null)
        {
            eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
        }
        else
        {
            eventSystemGO = existingEventSystem.gameObject;
        }

        Component newInputSystemModule = eventSystemGO.GetComponent("InputSystemUIInputModule");
        if (newInputSystemModule != null)
        {
            Object.DestroyImmediate(newInputSystemModule);
        }

        if (eventSystemGO.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }

        EditorUtility.SetDirty(eventSystemGO);

        // IMPORTANT: the camera background must NOT live inside the Canvas.
        // A Screen Space - Overlay Canvas always draws on top of everything else in the
        // scene, so a full-screen camera feed inside it would cover the Ball. Instead we
        // place the camera feed as a world-space sprite positioned further from the
        // camera (higher Z) than the Ball, so normal 2D sorting draws it behind gameplay.

        // Remove any old Canvas-based camera background from a previous version of this script.
        Transform oldCamBg = canvasGO.transform.Find("CameraBackground");
        if (oldCamBg != null)
        {
            Object.DestroyImmediate(oldCamBg.gameObject);
        }

        // Clean up the old Quad-based attempt if it exists (superseded by the approach below).
        GameObject oldQuad = GameObject.Find("CameraBackgroundWorld");
        if (oldQuad != null) Object.DestroyImmediate(oldQuad);

        // Use a RawImage (proven to correctly display the live webcam feed) but put it
        // on its OWN Canvas using Screen Space - Camera mode with a large Plane Distance.
        // That places this canvas further from the camera than the Ball in depth, so the
        // Ball naturally renders in front of it - while the main UI Canvas (buttons/text)
        // stays in Screen Space - Overlay mode, always drawn on top of everything.
        GameObject bgCanvasGO = GameObject.Find("BackgroundCanvas");
        Canvas bgCanvas;
        if (bgCanvasGO == null)
        {
            bgCanvasGO = new GameObject("BackgroundCanvas", typeof(RectTransform));
            bgCanvas = bgCanvasGO.AddComponent<Canvas>();
            bgCanvasGO.AddComponent<GraphicRaycaster>();
        }
        else
        {
            bgCanvas = bgCanvasGO.GetComponent<Canvas>();
        }

        bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        bgCanvas.worldCamera = cam;
        bgCanvas.planeDistance = 20f; // farther from camera than the Ball (Ball sits near z=0, camera at z=-10)

        Transform camBgT = bgCanvasGO.transform.Find("CameraBackground");
        RawImage cameraBgImage;
        if (camBgT == null)
        {
            GameObject camBgGO = new GameObject("CameraBackground", typeof(RectTransform));
            camBgGO.transform.SetParent(bgCanvasGO.transform, false);
            StretchFull(camBgGO.GetComponent<RectTransform>());
            cameraBgImage = camBgGO.AddComponent<RawImage>();
        }
        else
        {
            cameraBgImage = camBgT.GetComponent<RawImage>();
        }
        // Purely visual - must never intercept touches meant for buttons
        cameraBgImage.raycastTarget = false;

        // ---------------- Start Panel ----------------
        GameObject startPanel = FindOrCreatePanel(canvasGO.transform, "StartPanel", panelSprite, new Vector2(850, 1000));
        Text titleText = FindOrCreateText(startPanel.transform, "TitleText", "KickUp AR Football", 64, new Vector2(0, 400));
        Text instructionText = FindOrCreateText(startPanel.transform, "InstructionText", "Tap or swipe the ball to kick it up", 28, new Vector2(0, -500));
        Button infiniteBtn = FindOrCreateButton(startPanel.transform, "InfiniteModeButton", "Infinite Kick", new Vector2(0, 60), buttonPrimarySprite);
        Button basketBtn = FindOrCreateButton(startPanel.transform, "BasketModeButton", "Basket Challenge", new Vector2(0, -80), buttonSecondarySprite);

        // ---------------- Gameplay Panel ----------------
        GameObject gameplayPanel = FindOrCreatePanel(canvasGO.transform, "GameplayPanel");
        // (intentionally no background sprite here - this panel overlays live gameplay/camera)
        Text scoreText = FindOrCreateText(gameplayPanel.transform, "ScoreText", "Score: 0", 40, new Vector2(-300, 800), TextAnchor.UpperLeft);
        Text bestScoreText = FindOrCreateText(gameplayPanel.transform, "BestScoreText", "Best: 0", 28, new Vector2(-300, 730), TextAnchor.UpperLeft);
        Text timerText = FindOrCreateText(gameplayPanel.transform, "TimerText", "Time: 60", 32, new Vector2(300, 800), TextAnchor.UpperRight);
        Button exitToMenuBtn = FindOrCreateButton(gameplayPanel.transform, "ExitToMenuButton", "Menu", new Vector2(-300, 630), buttonSecondarySprite);
        ShrinkButton(exitToMenuBtn, new Vector2(200, 80), 24);
        Text motionDebugText = FindOrCreateText(gameplayPanel.transform, "MotionDebugText", "Motion: --", 22, new Vector2(0, -860), TextAnchor.LowerCenter);
        gameplayPanel.SetActive(false);

        // ---------------- Game Over Panel ----------------
        GameObject gameOverPanel = FindOrCreatePanel(canvasGO.transform, "GameOverPanel", panelSprite, new Vector2(850, 900));
        Text gameOverText = FindOrCreateText(gameOverPanel.transform, "GameOverText", "Game Over", 56, new Vector2(0, 300));
        Text finalScoreText = FindOrCreateText(gameOverPanel.transform, "FinalScoreText", "Score: 0", 36, new Vector2(0, 150));
        Text bestScoreGameOverText = FindOrCreateText(gameOverPanel.transform, "BestScoreGameOverText", "Best: 0", 32, new Vector2(0, 90));
        Button restartBtn = FindOrCreateButton(gameOverPanel.transform, "RestartButton", "Restart", new Vector2(0, -60), buttonPrimarySprite);
        Button mainMenuBtn = FindOrCreateButton(gameOverPanel.transform, "MainMenuButton", "Main Menu", new Vector2(0, -180), buttonSecondarySprite);
        gameOverPanel.SetActive(false);

        // ---------------- Permission Panel ----------------
        // Shown automatically by UIManager whenever CameraBackgroundManager reports
        // that camera permission was denied, with a button to open OS Settings and
        // a button to retry (e.g. after the user has just granted it).
        GameObject permissionPanel = FindOrCreatePanel(canvasGO.transform, "PermissionPanel", panelSprite, new Vector2(850, 700));
        Text permissionText = FindOrCreateText(permissionPanel.transform, "PermissionText", "Camera access is needed to play.\nPlease allow camera permission to continue.", 30, new Vector2(0, 120));
        Button openSettingsBtn = FindOrCreateButton(permissionPanel.transform, "OpenSettingsButton", "Open Settings", new Vector2(0, -60), buttonPrimarySprite);
        Button retryCameraBtn = FindOrCreateButton(permissionPanel.transform, "RetryButton", "Try Again", new Vector2(0, -190), buttonSecondarySprite);
        permissionPanel.SetActive(false);

        // ---------------- Managers ----------------
        GameObject managers = GameObject.Find("Managers");
        if (managers == null) managers = new GameObject("Managers");

        var gameManager = GetOrAdd<GameManager>(managers);
        var uiManager = GetOrAdd<UIManager>(managers);
        var audioManager = GetOrAdd<AudioManager>(managers);
        WireDefaultAudioClips(audioManager);
        var camBgManager = GetOrAdd<CameraBackgroundManager>(managers);
        var kickInput = GetOrAdd<KickInputManager>(managers);
        var motionKickInput = GetOrAdd<MotionKickInputManager>(managers);

        gameManager.uiManager = uiManager;
        gameManager.ballController = ballController;
        gameManager.basketGoalController = basketController;

        uiManager.startPanel = startPanel;
        uiManager.gameplayPanel = gameplayPanel;
        uiManager.gameOverPanel = gameOverPanel;
        uiManager.titleText = titleText;
        uiManager.instructionText = instructionText;
        uiManager.infiniteModeButton = infiniteBtn;
        uiManager.basketModeButton = basketBtn;
        uiManager.scoreText = scoreText;
        uiManager.bestScoreText = bestScoreText;
        uiManager.timerText = timerText;
        uiManager.gameOverText = gameOverText;
        uiManager.finalScoreText = finalScoreText;
        uiManager.bestScoreGameOverText = bestScoreGameOverText;
        uiManager.restartButton = restartBtn;
        uiManager.mainMenuButton = mainMenuBtn;
        uiManager.exitToMenuButton = exitToMenuBtn;
        uiManager.permissionPanel = permissionPanel;
        uiManager.permissionText = permissionText;
        uiManager.openSettingsButton = openSettingsBtn;
        uiManager.retryCameraButton = retryCameraBtn;
        uiManager.cameraBackgroundManager = camBgManager;

        kickInput.ballController = ballController;
        motionKickInput.ballController = ballController;
        motionKickInput.cameraBackgroundManager = camBgManager;
        motionKickInput.debugText = motionDebugText;

        // Force these tuning values on every rebuild - they're the current best
        // known settings for reliable repeated kicks. Public fields only keep their
        // FIRST-EVER serialized value once a component exists in the scene, so
        // without this, code changes to the defaults above would never actually
        // reach an already-created MotionKickInputManager in your saved scene.
        motionKickInput.sampleResolution = 40;
        motionKickInput.motionThreshold = 0.10f;
        motionKickInput.consecutiveFramesRequired = 1;
        motionKickInput.kickCooldown = 0.35f;
        motionKickInput.checkEveryNFrames = 2;
        motionKickInput.detectionHeightLimit = 0.75f;
        camBgManager.backgroundRawImage = cameraBgImage;

        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(kickInput);
        EditorUtility.SetDirty(camBgManager);
        EditorUtility.SetDirty(motionKickInput);

        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log("KickUp AR Football: scene built successfully! Press Play to test (click/drag the ball with your mouse).");
        EditorUtility.DisplayDialog("KickUp AR Football", "Scene built successfully!\n\nPress Play and click/drag on the ball to test kicking.", "OK");
    }

    /// <summary>
    /// Removes any Canvas or EventSystem objects from a previous run before rebuilding.
    /// This makes the builder fully self-healing: even if an earlier bug left duplicate
    /// or misplaced UI objects behind, running this always produces one clean, correctly
    /// wired UI Canvas + BackgroundCanvas + EventSystem, with nothing left over.
    /// </summary>
    private static void CleanupPreviousUI()
    {
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases)
        {
            Object.DestroyImmediate(c.gameObject);
        }

        EventSystem[] allEventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (EventSystem es in allEventSystems)
        {
            Object.DestroyImmediate(es.gameObject);
        }
    }

    private static void WireDefaultAudioClips(AudioManager audioManager)
    {
        const string audioFolder = "Assets/Audio";

        if (audioManager.kickSound == null)
            audioManager.kickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(audioFolder + "/KickSound.wav");
        if (audioManager.scoreSound == null)
            audioManager.scoreSound = AssetDatabase.LoadAssetAtPath<AudioClip>(audioFolder + "/ScoreSound.wav");
        if (audioManager.gameOverSound == null)
            audioManager.gameOverSound = AssetDatabase.LoadAssetAtPath<AudioClip>(audioFolder + "/GameOverSound.wav");
        if (audioManager.buttonClickSound == null)
            audioManager.buttonClickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(audioFolder + "/ButtonClickSound.wav");

        EditorUtility.SetDirty(audioManager);
    }

    // ---------------- Helpers ----------------

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }

    private static bool HasTag(string tag)
    {
        foreach (var t in UnityEditorInternal.InternalEditorUtility.tags)
            if (t == tag) return true;
        return false;
    }

    private static void CreateTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    private static void EnsureSpriteFolder()
    {
        if (!AssetDatabase.IsValidFolder(SpriteFolder))
            AssetDatabase.CreateFolder("Assets", "Sprites");
    }

    private static Sprite GetBallSprite()
    {
        // Prefer a real downloaded ball image if one was placed in Assets/Sprites (e.g. BallSprite.png)
        string realBallPath = SpriteFolder + "/BallSprite.png";
        Sprite realBall = AssetDatabase.LoadAssetAtPath<Sprite>(realBallPath);
        if (realBall == null && File.Exists(realBallPath))
        {
            AssetDatabase.ImportAsset(realBallPath);
            TextureImporter importer = AssetImporter.GetAtPath(realBallPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            realBall = AssetDatabase.LoadAssetAtPath<Sprite>(realBallPath);
        }
        if (realBall != null) return realBall;

        // Fallback: generated placeholder circle
        return GetOrCreateShapeSprite(SpriteFolder + "/BallCircle.png", true);
    }

    /// <summary>
    /// Loads a PNG as a 9-sliced UI sprite (rounded buttons/panels) with the given
    /// border so it stretches cleanly at any size without warping the rounded corners.
    /// </summary>
    private static Sprite GetOrCreateUISprite(string path, Vector4 border)
    {
        if (!File.Exists(path)) return null;

        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (existing == null || importer == null || importer.spriteBorder != border)
        {
            AssetDatabase.ImportAsset(path);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
            existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        return existing;
    }

    /// <summary>
    /// Adds (or updates) a dark outline behind a UI Text so it stays readable when
    /// placed over a bright, busy live camera feed.
    /// </summary>
    /// <summary>
    /// Resizes an already-created button (and shrinks its label font to match),
    /// for small secondary buttons like the in-gameplay "Menu" exit button.
    /// </summary>
    private static void ShrinkButton(Button button, Vector2 size, int fontSize)
    {
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        Text label = button.GetComponentInChildren<Text>();
        if (label != null) label.fontSize = fontSize;
    }

    private static void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        Outline outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private static Sprite GetOrCreateShapeSprite(string path, bool circle)
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool fill = circle ? Vector2.Distance(new Vector2(x, y), center) <= radius : true;
                pixels[y * size + x] = fill ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(path, png);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject FindOrCreatePanel(Transform parent, string name, Sprite backgroundSprite = null, Vector2? backgroundSize = null)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        StretchFull(panel.GetComponent<RectTransform>());

        // Optional centered "card" background (used for Start / Game Over panels,
        // never for the in-gameplay HUD panel, which must stay see-through over the camera).
        if (backgroundSprite != null)
        {
            GameObject bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(panel.transform, false);
            bgGO.transform.SetAsFirstSibling();

            RectTransform bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = backgroundSize ?? new Vector2(850, 1200);

            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.sprite = backgroundSprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;
        }

        return panel;
    }

    private static Text FindOrCreateText(Transform parent, string name, string defaultText, int fontSize, Vector2 anchoredPos, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<Text>();

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900, 120);
        rt.anchoredPosition = anchoredPos;

        Text text = go.AddComponent<Text>();
        text.text = defaultText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = Color.white;

        AddOutline(go, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));

        return text;
    }

    private static Button FindOrCreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Sprite sprite = null)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<Button>();

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(420, 110);
        rt.anchoredPosition = anchoredPos;

        Image img = go.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.2f, 0.5f, 0.9f);
        }

        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        StretchFull(textGO.GetComponent<RectTransform>());
        Text text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        AddOutline(textGO, new Color(0f, 0f, 0f, 0.6f), new Vector2(1.5f, -1.5f));

        return button;
    }
}
