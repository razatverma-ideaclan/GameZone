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

        // Force-regenerate sprites every run so a previous broken import can't linger.
        if (AssetDatabase.IsValidFolder(SpriteFolder))
        {
            AssetDatabase.DeleteAsset(SpriteFolder);
        }
        EnsureFolders();

        Sprite squareSprite = GetOrCreateSprite("Square", 128, 128, (w, h) => GenerateSquareTexture(w), 128);
        Sprite birdSprite = GetOrCreateSprite("Bird", 128, 128, (w, h) => GenerateBirdTexture(w), 128);
        const float bgWorldWidth = 20f;
        Sprite backgroundSprite = GetOrCreateSprite("Background", 512, 288, (w, h) => GenerateBackgroundTexture(w, h), 512f / bgWorldWidth);

        // Start from a fresh empty scene so re-running this is safe.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera();
        BuildBackground(backgroundSprite);
        GameObject ground = BuildGround(squareSprite);
        GameObject bird = BuildBird(birdSprite);
        GameObject pipePairPrefab = BuildPipePairPrefab(squareSprite);
        GameObject pipeSpawnerGO = BuildPipeSpawner(pipePairPrefab);
        BuildEventSystem();
        Canvas canvas = BuildCanvas();
        GameObject scoreTextGO = BuildScoreText(canvas.transform);
        GameObject startPanel = BuildStartPanel(canvas.transform);
        GameObject gameOverPanel = BuildGameOverPanel(canvas.transform, out Button restartButton);
        GameObject gameManagerGO = BuildGameManager(bird, pipeSpawnerGO, scoreTextGO, startPanel, gameOverPanel);

        // Wire the restart button now that GameManager exists.
        GameManager gm = gameManagerGO.GetComponent<GameManager>();
        UnityEventTools.AddPersistentListener(restartButton.onClick, gm.RestartGame);

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

    // ---------- Sprite generation (original, procedurally drawn art — no external assets) ----------

    private static Sprite GetOrCreateSprite(string name, int width, int height, System.Func<int, int, Texture2D> generator, float pixelsPerUnit)
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
        importer.filterMode = FilterMode.Bilinear;
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
        Color bodyColor = new Color(1f, 0.82f, 0.2f);
        Color bellyColor = new Color(1f, 0.93f, 0.55f);
        Color wingColor = new Color(0.95f, 0.6f, 0.1f);

        Vector2 bodyCenter = new Vector2(size * 0.44f, size * 0.5f);
        float bodyRadius = size * 0.36f;
        Vector2 bellyCenter = new Vector2(size * 0.4f, size * 0.36f);
        float bellyRadius = size * 0.22f;
        Vector2 wingCenter = new Vector2(size * 0.32f, size * 0.5f);
        float wingRx = size * 0.18f;
        float wingRy = size * 0.13f;
        Vector2 eyeCenter = new Vector2(size * 0.58f, size * 0.66f);
        float eyeRadius = size * 0.13f;
        Vector2 pupilCenter = eyeCenter + new Vector2(size * 0.035f, size * 0.02f);
        float pupilRadius = size * 0.05f;
        Vector2 beakTop = new Vector2(size * 0.72f, size * 0.56f);
        Vector2 beakBottom = new Vector2(size * 0.72f, size * 0.40f);
        Vector2 beakTip = new Vector2(size * 0.92f, size * 0.48f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                Color pixel = clear;

                if (Vector2.Distance(p, bodyCenter) <= bodyRadius)
                {
                    pixel = bodyColor;
                    if (IsInsideEllipse(p, wingCenter, wingRx, wingRy)) pixel = wingColor;
                    if (IsInsideEllipse(p, bellyCenter, bellyRadius, bellyRadius)) pixel = bellyColor;
                }

                if (PointInTriangle(p, beakTop, beakBottom, beakTip)) pixel = new Color(0.9f, 0.25f, 0.1f);
                if (Vector2.Distance(p, eyeCenter) <= eyeRadius) pixel = Color.white;
                if (Vector2.Distance(p, pupilCenter) <= pupilRadius) pixel = Color.black;

                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBackgroundTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color skyTop = new Color(0.30f, 0.60f, 0.85f);
        Color skyBottom = new Color(0.75f, 0.88f, 0.97f);
        Color hillColor = new Color(0.45f, 0.62f, 0.42f);
        Color cloudColor = new Color(1f, 1f, 1f, 0.9f);

        Vector2[] cloudCenters =
        {
            new Vector2(width * 0.18f, height * 0.78f),
            new Vector2(width * 0.55f, height * 0.85f),
            new Vector2(width * 0.82f, height * 0.72f)
        };
        float cloudScale = width * 0.05f;

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

        float hillBaseHeight = height * 0.16f;
        float hillAmplitude = height * 0.04f;
        for (int x = 0; x < width; x++)
        {
            float hillTopY = hillBaseHeight + hillAmplitude * Mathf.Sin(x * 0.05f);
            for (int y = 0; y < hillTopY; y++)
            {
                tex.SetPixel(x, y, hillColor);
            }
        }

        tex.Apply();
        return tex;
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
        GameObject bg = new GameObject("Background");
        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;
        sr.sortingOrder = -100;
        bg.transform.position = new Vector3(0, 0, 0);
    }

    // ---------- Scene objects ----------

    private static void BuildCamera()
    {
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor; // avoid the default skybox masking missing sprites
        cam.backgroundColor = new Color(0.53f, 0.81f, 0.92f); // sky blue
        camGO.transform.position = new Vector3(0, 0, -10);
        camGO.AddComponent<AudioListener>();
    }

    private static GameObject BuildGround(Sprite squareSprite)
    {
        GameObject ground = new GameObject("Ground");
        ground.tag = "Ground";
        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = new Color(0.55f, 0.4f, 0.25f);
        sr.sortingOrder = -1;
        ground.transform.position = new Vector3(0, -4.5f, 0);
        ground.transform.localScale = new Vector3(24, 1.5f, 1);
        ground.AddComponent<BoxCollider2D>();

        // Purely cosmetic grass strip sitting right on the dirt's top edge.
        GameObject grass = new GameObject("Grass");
        grass.transform.SetParent(ground.transform);
        SpriteRenderer grassSr = grass.AddComponent<SpriteRenderer>();
        grassSr.sprite = squareSprite;
        grassSr.color = new Color(0.3f, 0.75f, 0.3f);
        grassSr.sortingOrder = 0;
        grass.transform.localScale = new Vector3(1f, 0.2f, 1f); // 0.3 world units tall (parent scale.y = 1.5)
        grass.transform.localPosition = new Vector3(0, 0.5f, 0); // sits at the dirt's top edge

        return ground;
    }

    private static GameObject BuildBird(Sprite birdSprite)
    {
        GameObject bird = new GameObject("Bird");
        bird.tag = "Bird";
        SpriteRenderer sr = bird.AddComponent<SpriteRenderer>();
        sr.sprite = birdSprite;
        bird.transform.position = new Vector3(-3, 0, 0);
        bird.transform.localScale = new Vector3(0.9f, 0.9f, 1);
        bird.AddComponent<Rigidbody2D>();
        CircleCollider2D col = bird.AddComponent<CircleCollider2D>();
        col.radius = 0.36f;
        bird.AddComponent<BirdController>();
        return bird;
    }

    private static GameObject BuildPipePairPrefab(Sprite squareSprite)
    {
        const float gapHeight = 2.5f;
        const float pipeHeight = 10f;
        const float pipeWidth = 1f;
        float half = gapHeight / 2f;

        GameObject root = new GameObject("PipePair");

        GameObject pipeBottom = new GameObject("PipeBottom");
        pipeBottom.transform.SetParent(root.transform);
        pipeBottom.tag = "Pipe";
        SpriteRenderer bottomSr = pipeBottom.AddComponent<SpriteRenderer>();
        bottomSr.sprite = squareSprite;
        bottomSr.color = new Color(0.2f, 0.7f, 0.2f);
        pipeBottom.transform.localScale = new Vector3(pipeWidth, pipeHeight, 1);
        pipeBottom.transform.localPosition = new Vector3(0, -half - pipeHeight / 2f, 0);
        pipeBottom.AddComponent<BoxCollider2D>();

        GameObject pipeTop = new GameObject("PipeTop");
        pipeTop.transform.SetParent(root.transform);
        pipeTop.tag = "Pipe";
        SpriteRenderer topSr = pipeTop.AddComponent<SpriteRenderer>();
        topSr.sprite = squareSprite;
        topSr.color = new Color(0.2f, 0.7f, 0.2f);
        pipeTop.transform.localScale = new Vector3(pipeWidth, pipeHeight, 1);
        pipeTop.transform.localPosition = new Vector3(0, half + pipeHeight / 2f, 0);
        pipeTop.AddComponent<BoxCollider2D>();

        // Cosmetic caps sitting right at the pipe mouths (classic pipe-lip look).
        Color capColor = new Color(0.12f, 0.5f, 0.12f);
        GameObject capBottom = new GameObject("CapBottom");
        capBottom.transform.SetParent(root.transform);
        SpriteRenderer capBottomSr = capBottom.AddComponent<SpriteRenderer>();
        capBottomSr.sprite = squareSprite;
        capBottomSr.color = capColor;
        capBottom.transform.localScale = new Vector3(pipeWidth * 1.25f, 0.3f, 1f);
        capBottom.transform.localPosition = new Vector3(0, -half, 0);

        GameObject capTop = new GameObject("CapTop");
        capTop.transform.SetParent(root.transform);
        SpriteRenderer capTopSr = capTop.AddComponent<SpriteRenderer>();
        capTopSr.sprite = squareSprite;
        capTopSr.color = capColor;
        capTop.transform.localScale = new Vector3(pipeWidth * 1.25f, 0.3f, 1f);
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
        spawner.spawnInterval = 1.5f;
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
        scaler.referenceResolution = new Vector2(1080, 1920);
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
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -80);
        rt.sizeDelta = new Vector2(300, 120);
        return go;
    }

    private static GameObject BuildStartPanel(Transform canvasTransform)
    {
        GameObject panel = CreatePanel("StartPanel", canvasTransform, new Color(0, 0, 0, 0.35f));
        CreateLabel("StartText", panel.transform, "Tap to Play", 64);
        return panel;
    }

    private static GameObject BuildGameOverPanel(Transform canvasTransform, out Button restartButton)
    {
        GameObject panel = CreatePanel("GameOverPanel", canvasTransform, new Color(0, 0, 0, 0.55f));
        CreateLabel("GameOverText", panel.transform, "Game Over", 72, new Vector2(0, 100));

        GameObject buttonGO = new GameObject("RestartButton");
        buttonGO.transform.SetParent(panel.transform, false);
        Image img = buttonGO.AddComponent<Image>();
        img.color = Color.white;
        RectTransform btnRt = buttonGO.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(300, 100);
        btnRt.anchoredPosition = new Vector2(0, -60);
        restartButton = buttonGO.AddComponent<Button>();

        GameObject label = CreateLabel("Text", buttonGO.transform, "Restart", 40);
        Text labelText = label.GetComponent<Text>();
        labelText.color = Color.black;

        panel.SetActive(false);
        return panel;
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
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600, 150);
        rt.anchoredPosition = offset ?? Vector2.zero;
        return go;
    }

    private static GameObject BuildGameManager(GameObject bird, GameObject pipeSpawnerGO, GameObject scoreTextGO, GameObject startPanel, GameObject gameOverPanel)
    {
        GameObject go = new GameObject("GameManager");
        GameManager gm = go.AddComponent<GameManager>();
        gm.bird = bird;
        gm.pipeSpawner = pipeSpawnerGO.GetComponent<PipeSpawner>();
        gm.scoreText = scoreTextGO;
        gm.startPanel = startPanel;
        gm.gameOverPanel = gameOverPanel;
        return go;
    }
}
