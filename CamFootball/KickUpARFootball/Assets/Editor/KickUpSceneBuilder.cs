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
        EnsureSpriteFolder();

        Sprite ballSprite = GetBallSprite();
        Sprite rectSprite = GetOrCreateShapeSprite(SpriteFolder + "/GoalRect.png", false);

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
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasGO;
        if (canvas == null)
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
            canvasGO = canvas.gameObject;
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

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

        GameObject camBgWorldGO = GameObject.Find("CameraBackgroundWorld");
        if (camBgWorldGO == null) camBgWorldGO = new GameObject("CameraBackgroundWorld");

        var camBgRenderer = GetOrAdd<SpriteRenderer>(camBgWorldGO);
        camBgRenderer.sprite = rectSprite;
        camBgRenderer.color = Color.white;
        camBgRenderer.sortingOrder = -100;

        camBgWorldGO.transform.position = new Vector3(0f, 0f, 5f);
        camBgWorldGO.transform.localScale = new Vector3(30f, 30f, 1f);

        RawImage cameraBgImage = null;

        // ---------------- Start Panel ----------------
        GameObject startPanel = FindOrCreatePanel(canvasGO.transform, "StartPanel");
        Text titleText = FindOrCreateText(startPanel.transform, "TitleText", "KickUp AR Football", 64, new Vector2(0, 400));
        Text instructionText = FindOrCreateText(startPanel.transform, "InstructionText", "Tap or swipe the ball to kick it up", 28, new Vector2(0, -500));
        Button infiniteBtn = FindOrCreateButton(startPanel.transform, "InfiniteModeButton", "Infinite Kick", new Vector2(0, 60));
        Button basketBtn = FindOrCreateButton(startPanel.transform, "BasketModeButton", "Basket Challenge", new Vector2(0, -80));

        // ---------------- Gameplay Panel ----------------
        GameObject gameplayPanel = FindOrCreatePanel(canvasGO.transform, "GameplayPanel");
        Text scoreText = FindOrCreateText(gameplayPanel.transform, "ScoreText", "Score: 0", 40, new Vector2(-300, 800), TextAnchor.UpperLeft);
        Text bestScoreText = FindOrCreateText(gameplayPanel.transform, "BestScoreText", "Best: 0", 28, new Vector2(-300, 730), TextAnchor.UpperLeft);
        Text timerText = FindOrCreateText(gameplayPanel.transform, "TimerText", "Time: 60", 32, new Vector2(300, 800), TextAnchor.UpperRight);
        gameplayPanel.SetActive(false);

        // ---------------- Game Over Panel ----------------
        GameObject gameOverPanel = FindOrCreatePanel(canvasGO.transform, "GameOverPanel");
        Text gameOverText = FindOrCreateText(gameOverPanel.transform, "GameOverText", "Game Over", 56, new Vector2(0, 300));
        Text finalScoreText = FindOrCreateText(gameOverPanel.transform, "FinalScoreText", "Score: 0", 36, new Vector2(0, 150));
        Text bestScoreGameOverText = FindOrCreateText(gameOverPanel.transform, "BestScoreGameOverText", "Best: 0", 32, new Vector2(0, 90));
        Button restartBtn = FindOrCreateButton(gameOverPanel.transform, "RestartButton", "Restart", new Vector2(0, -60));
        Button mainMenuBtn = FindOrCreateButton(gameOverPanel.transform, "MainMenuButton", "Main Menu", new Vector2(0, -180));
        gameOverPanel.SetActive(false);

        // ---------------- Managers ----------------
        GameObject managers = GameObject.Find("Managers");
        if (managers == null) managers = new GameObject("Managers");

        var gameManager = GetOrAdd<GameManager>(managers);
        var uiManager = GetOrAdd<UIManager>(managers);
        GetOrAdd<AudioManager>(managers);
        var camBgManager = GetOrAdd<CameraBackgroundManager>(managers);
        var kickInput = GetOrAdd<KickInputManager>(managers);

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

        kickInput.ballController = ballController;
        camBgManager.backgroundRenderer = camBgRenderer;

        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(kickInput);
        EditorUtility.SetDirty(camBgManager);

        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log("KickUp AR Football: scene built successfully! Press Play to test (click/drag the ball with your mouse).");
        EditorUtility.DisplayDialog("KickUp AR Football", "Scene built successfully!\n\nPress Play and click/drag on the ball to test kicking.", "OK");
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

    private static GameObject FindOrCreatePanel(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        StretchFull(panel.GetComponent<RectTransform>());
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
        text.alignment = alignment;
        text.color = Color.white;

        return text;
    }

    private static Button FindOrCreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<Button>();

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(420, 110);
        rt.anchoredPosition = anchoredPos;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 0.9f);

        Button button = go.AddComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        StretchFull(textGO.GetComponent<RectTransform>());
        Text text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return button;
    }
}
