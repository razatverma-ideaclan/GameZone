using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click scene builder for the Subway Surfer style prototype.
/// Run from the menu: Tools > Subway Surfer > Build Scene.
/// Creates the Player (capsule), Ground, three obstacle prefabs (barrier,
/// low, high), ObstacleSpawner, UI Canvas, and GameManager, wires all
/// references, and saves it as "SampleScene". Safe to re-run.
/// </summary>
public static class SubwaySurferSceneBuilder
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string SceneFolder = "Assets/Scenes";
    private const string ScenePath = SceneFolder + "/SampleScene.unity";
    private const float LaneDistance = 3f;

    [MenuItem("Tools/Subway Surfer/Build Scene")]
    public static void BuildScene()
    {
        EnsureFolders();
        EnsureTags();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildLight();
        GameObject player = BuildPlayer();
        BuildCamera(player);
        BuildGround();
        GameObject[] obstaclePrefabs = BuildObstaclePrefabs();
        GameObject spawnerGO = BuildObstacleSpawner(player, obstaclePrefabs);
        BuildEventSystem();
        Canvas canvas = BuildCanvas();
        GameObject scoreTextGO = BuildScoreText(canvas.transform);
        GameObject startPanel = BuildStartPanel(canvas.transform);
        GameObject gameOverPanel = BuildGameOverPanel(canvas.transform, out Button restartButton);
        GameObject gameManagerGO = BuildGameManager(player, spawnerGO, scoreTextGO, startPanel, gameOverPanel);

        GameManager gm = gameManagerGO.GetComponent<GameManager>();
        UnityEventTools.AddPersistentListener(restartButton.onClick, gm.RestartGame);

        if (!Directory.Exists(SceneFolder)) Directory.CreateDirectory(SceneFolder);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        EditorUtility.DisplayDialog("Subway Surfer", "Scene built and saved to " + ScenePath + ".\nPress Play to test.", "OK");
    }

    // ---------- Folders / Tags ----------

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    private static void EnsureTags()
    {
        AddTag("Obstacle");
    }

    private static void AddTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    // ---------- Scene objects ----------

    private static void BuildLight()
    {
        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    private static GameObject BuildPlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0, 1f, 0);

        // Remove the auto-added CapsuleCollider; CharacterController replaces it.
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());

        Renderer renderer = player.GetComponent<Renderer>();
        renderer.sharedMaterial = MakeColorMaterial(new Color(0.2f, 0.5f, 0.9f));

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.center = new Vector3(0, 1f, 0);
        controller.radius = 0.4f;

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.laneDistance = LaneDistance;

        return player;
    }

    private static void BuildCamera(GameObject player)
    {
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; // avoid default skybox masking rendering issues
        cam.backgroundColor = new Color(0.53f, 0.81f, 0.92f);
        camGO.AddComponent<AudioListener>();

        // Parent to the player so it automatically follows lane changes / jumps.
        camGO.transform.SetParent(player.transform, false);
        camGO.transform.localPosition = new Vector3(0, 4.5f, -8f);
        camGO.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
    }

    private static void BuildGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        // Start well before z=0 so the player never spawns right at the mesh edge.
        ground.transform.position = new Vector3(0, 0, 240);
        ground.transform.localScale = new Vector3(3f, 1f, 50f); // wide enough for 3 lanes, very long
        ground.GetComponent<Renderer>().sharedMaterial = MakeColorMaterial(new Color(0.35f, 0.35f, 0.35f));
    }

    private static GameObject[] BuildObstaclePrefabs()
    {
        GameObject barrier = CreateObstacle("BarrierObstacle", new Vector3(2.6f, 3f, 1f), new Vector3(0, 1.5f, 0), new Color(0.8f, 0.15f, 0.15f));
        GameObject low = CreateObstacle("LowObstacle", new Vector3(2.6f, 1f, 1f), new Vector3(0, 0.5f, 0), new Color(0.9f, 0.5f, 0.1f));
        GameObject high = CreateObstacle("HighObstacle", new Vector3(2.6f, 1.5f, 1f), new Vector3(0, 1.9f, 0), new Color(0.7f, 0.2f, 0.8f));

        if (!Directory.Exists(PrefabFolder)) Directory.CreateDirectory(PrefabFolder);

        GameObject barrierPrefab = PrefabUtility.SaveAsPrefabAsset(barrier, $"{PrefabFolder}/BarrierObstacle.prefab");
        GameObject lowPrefab = PrefabUtility.SaveAsPrefabAsset(low, $"{PrefabFolder}/LowObstacle.prefab");
        GameObject highPrefab = PrefabUtility.SaveAsPrefabAsset(high, $"{PrefabFolder}/HighObstacle.prefab");

        Object.DestroyImmediate(barrier);
        Object.DestroyImmediate(low);
        Object.DestroyImmediate(high);

        return new[] { barrierPrefab, lowPrefab, highPrefab };
    }

    private static GameObject CreateObstacle(string name, Vector3 scale, Vector3 localPosition, Color color)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.tag = "Obstacle";
        obstacle.transform.localScale = scale;
        obstacle.transform.position = localPosition;
        obstacle.GetComponent<Renderer>().sharedMaterial = MakeColorMaterial(color);

        BoxCollider col = obstacle.GetComponent<BoxCollider>();
        col.isTrigger = true;

        return obstacle;
    }

    private static Material MakeColorMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private static GameObject BuildObstacleSpawner(GameObject player, GameObject[] obstaclePrefabs)
    {
        GameObject spawnerGO = new GameObject("ObstacleSpawner");
        ObstacleSpawner spawner = spawnerGO.AddComponent<ObstacleSpawner>();
        spawner.player = player.transform;
        spawner.obstaclePrefabs = obstaclePrefabs;
        spawner.laneDistance = LaneDistance;
        spawner.spawnLookahead = 40f;
        spawner.spawnGap = 14f;
        spawner.destroyBehindDistance = 10f;
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
        CreateLabel("StartText", panel.transform, "Tap to Run", 64);
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
        label.GetComponent<Text>().color = Color.black;

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

    private static GameObject BuildGameManager(GameObject player, GameObject spawnerGO, GameObject scoreTextGO, GameObject startPanel, GameObject gameOverPanel)
    {
        GameObject go = new GameObject("GameManager");
        GameManager gm = go.AddComponent<GameManager>();
        gm.player = player.GetComponent<PlayerController>();
        gm.obstacleSpawner = spawnerGO.GetComponent<ObstacleSpawner>();
        gm.scoreText = scoreTextGO;
        gm.startPanel = startPanel;
        gm.gameOverPanel = gameOverPanel;
        return go;
    }
}
