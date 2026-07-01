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
        EnsureFolders();
        EnsureTags();

        Sprite circleSprite = GetOrCreateSprite("Circle", true);
        Sprite squareSprite = GetOrCreateSprite("Square", false);

        // Start from a fresh empty scene so re-running this is safe.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera();
        GameObject ground = BuildGround(squareSprite);
        GameObject bird = BuildBird(circleSprite);
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

    // ---------- Sprite generation (simple primitives, no external assets) ----------

    private static Sprite GetOrCreateSprite(string name, bool circle)
    {
        string path = $"{SpriteFolder}/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = circle
                    ? Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) <= radius
                    : true;
                tex.SetPixel(x, y, inside ? Color.white : clear);
            }
        }
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size; // 1 texture = 1 world unit at scale 1
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------- Scene objects ----------

    private static void BuildCamera()
    {
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
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
        ground.transform.position = new Vector3(0, -4.5f, 0);
        ground.transform.localScale = new Vector3(24, 1.5f, 1);
        ground.AddComponent<BoxCollider2D>();
        return ground;
    }

    private static GameObject BuildBird(Sprite circleSprite)
    {
        GameObject bird = new GameObject("Bird");
        bird.tag = "Bird";
        SpriteRenderer sr = bird.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = Color.yellow;
        bird.transform.position = new Vector3(-3, 0, 0);
        bird.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        bird.AddComponent<Rigidbody2D>();
        bird.AddComponent<CircleCollider2D>();
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
