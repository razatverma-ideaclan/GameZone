using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns and animates theme-specific background elements (drifting meteors and random planets in Space,
/// fluffy cartoon outline clouds and walking Goombas in Mario/Classic, and swaying crowd banners in Football) at runtime.
/// </summary>
public class BackgroundAnimator : MonoBehaviour
{
    private string activeTheme = "";
    private List<GameObject> animatedElements = new List<GameObject>();
    
    private Sprite meteorSprite;
    private Sprite cloudSprite;
    private Sprite flagSprite;
    private Sprite goombaSprite;

    private Sprite planetRedSprite;
    private Sprite planetBlueSprite;
    private Sprite planetYellowSprite;

    private float planetSpawnTimer = 0f;
    private float nextPlanetSpawnTime = 8f;

    private float minX = -12f;
    private float maxX = 12f;

    void Start()
    {
        GenerateSprites();
        UpdateAnimationsForTheme();
    }

    void Update()
    {
        // Detect theme change at runtime
        if (ThemeManager.Instance != null)
        {
            var currentTheme = ThemeManager.Instance.GetCurrentTheme();
            if (currentTheme != null && currentTheme.themeName != activeTheme)
            {
                UpdateAnimationsForTheme();
            }
        }

        // Handle periodic planet spawning in Space theme
        if (activeTheme.ToLower() == "space")
        {
            planetSpawnTimer += Time.deltaTime;
            if (planetSpawnTimer >= nextPlanetSpawnTime)
            {
                planetSpawnTimer = 0f;
                nextPlanetSpawnTime = Random.Range(10f, 18f);

                // Prevent too many planets from cluttering the background
                int planetCount = 0;
                foreach (var go in animatedElements)
                {
                    if (go != null && go.name.StartsWith("SpacePlanet")) planetCount++;
                }

                if (planetCount < 3)
                {
                    SpawnRandomPlanet(false);
                }
            }
        }

        // Animate existing elements
        for (int i = animatedElements.Count - 1; i >= 0; i--)
        {
            GameObject go = animatedElements[i];
            if (go == null)
            {
                animatedElements.RemoveAt(i);
                continue;
            }

            DriftData data = go.GetComponent<DriftData>();
            if (data != null)
            {
                go.transform.Translate(Vector3.left * data.speed * Time.deltaTime, Space.World);
                go.transform.Rotate(Vector3.forward * data.rotSpeed * Time.deltaTime);

                // Wrap elements around the screen bounds
                if (go.transform.position.x < minX)
                {
                    if (go.name.StartsWith("SpacePlanet"))
                    {
                        go.transform.position = new Vector3(maxX, Random.Range(1f, 5f), go.transform.position.z);
                    }
                    else if (go.name.StartsWith("MarioGoomba"))
                    {
                        go.transform.position = new Vector3(maxX, -5.3f, go.transform.position.z);
                    }
                    else
                    {
                        go.transform.position = new Vector3(maxX, Random.Range(data.minY, data.maxY), go.transform.position.z);
                    }
                }
            }
        }
    }

    private void SpawnRandomPlanet(bool initial)
    {
        GameObject planet = new GameObject("SpacePlanet");
        planet.transform.SetParent(transform, false);

        SpriteRenderer sr = planet.AddComponent<SpriteRenderer>();
        int choice = Random.Range(0, 3);
        if (choice == 0) sr.sprite = planetRedSprite;
        else if (choice == 1) sr.sprite = planetBlueSprite;
        else sr.sprite = planetYellowSprite;

        sr.sortingOrder = -9; // Behind meteors (-8), in front of stars/space sky (-100)

        float scale = Random.Range(0.6f, 1.2f);
        planet.transform.localScale = new Vector3(scale, scale, 1f);
        
        float startX = initial ? Random.Range(-5f, 5f) : maxX;
        planet.transform.position = new Vector3(startX, Random.Range(1f, 5f), 1.5f);

        DriftData drift = planet.AddComponent<DriftData>();
        drift.speed = Random.Range(0.2f, 0.5f); // Drifts slowly
        drift.rotSpeed = Random.Range(-4f, 4f);
        drift.minY = 1f;
        drift.maxY = 5f;

        animatedElements.Add(planet);
    }

    private void UpdateAnimationsForTheme()
    {
        // Clear any old elements
        foreach (var go in animatedElements)
        {
            if (go != null) Destroy(go);
        }
        animatedElements.Clear();

        if (ThemeManager.Instance == null) return;
        var theme = ThemeManager.Instance.GetCurrentTheme();
        if (theme == null) return;

        activeTheme = theme.themeName;
        string tName = activeTheme.ToLower();

        if (tName == "space")
        {
            // Spawn 5 drifting and rotating space meteors
            for (int i = 0; i < 5; i++)
            {
                GameObject met = new GameObject("SpaceMeteor");
                met.transform.SetParent(transform, false);
                
                SpriteRenderer sr = met.AddComponent<SpriteRenderer>();
                sr.sprite = meteorSprite;
                sr.sortingOrder = -8; // behind players and pipes, but in front of space sky

                float scale = Random.Range(0.6f, 1.5f);
                met.transform.localScale = new Vector3(scale, scale, 1f);
                met.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(-2f, 5f), 1f);

                DriftData drift = met.AddComponent<DriftData>();
                drift.speed = Random.Range(0.8f, 2.5f);
                drift.rotSpeed = Random.Range(-25f, 25f);
                drift.minY = -2f;
                drift.maxY = 5f;

                animatedElements.Add(met);
            }

            // Spawn 1 initial planet visible immediately
            planetSpawnTimer = 0f;
            nextPlanetSpawnTime = Random.Range(10f, 18f);
            SpawnRandomPlanet(true);
        }
        else if (tName == "mario")
        {
            // Spawn 3 drifting clouds
            for (int i = 0; i < 3; i++)
            {
                GameObject cloud = new GameObject("BackgroundCloud");
                cloud.transform.SetParent(transform, false);

                SpriteRenderer sr = cloud.AddComponent<SpriteRenderer>();
                sr.sprite = cloudSprite;
                sr.sortingOrder = -9;

                float scale = Random.Range(1.2f, 2.5f);
                cloud.transform.localScale = new Vector3(scale, scale, 1f);
                cloud.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(2f, 6f), 2f);

                DriftData drift = cloud.AddComponent<DriftData>();
                drift.speed = Random.Range(0.2f, 0.6f);
                drift.rotSpeed = 0f;
                drift.minY = 2f;
                drift.maxY = 6f;

                animatedElements.Add(cloud);
            }

            // Spawn two walking Goombas on the grass floor!
            for (int i = 0; i < 2; i++)
            {
                GameObject goomba = new GameObject("MarioGoomba");
                goomba.transform.SetParent(transform, false);
                SpriteRenderer gSr = goomba.AddComponent<SpriteRenderer>();
                gSr.sprite = goombaSprite;
                gSr.sortingOrder = 12; // Render in front of ground grass (11) to show Goomba face and feet!

                goomba.transform.localScale = new Vector3(1f, 1f, 1f);
                float startX = (i == 0) ? Random.Range(minX, 0f) : Random.Range(0f, maxX);
                goomba.transform.position = new Vector3(startX, -5.3f, 1.2f);

                DriftData gDrift = goomba.AddComponent<DriftData>();
                gDrift.speed = 0.6f + i * 0.4f; // walk slowly along the floor
                gDrift.rotSpeed = 0f;
                gDrift.minY = -5.3f;
                gDrift.maxY = -5.3f;

                animatedElements.Add(goomba);
            }
        }
        else if (tName == "classic")
        {
            // Do NOT spawn dynamic clouds in classic theme (as they are baked into the backdrop texture)
        }
        else if (tName == "football")
        {
            // Spawn 4 swaying spectator flags
            for (int i = 0; i < 4; i++)
            {
                GameObject flag = new GameObject("FootballFlag");
                flag.transform.SetParent(transform, false);

                SpriteRenderer sr = flag.AddComponent<SpriteRenderer>();
                sr.sprite = flagSprite;
                sr.sortingOrder = -8;

                float scale = Random.Range(0.8f, 1.2f);
                flag.transform.localScale = new Vector3(scale, scale, 1f);
                flag.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(-3.5f, -2.5f), 1.5f);

                DriftData drift = flag.AddComponent<DriftData>();
                drift.speed = 0f; // static position, swaying rotation only
                drift.rotSpeed = 0f;
                flag.AddComponent<SwayComponent>();

                animatedElements.Add(flag);
            }
        }
    }

    private void GenerateSprites()
    {
        Color clear = Color.clear;

        // 1. Meteor Sprite (bumpy asteroid with crater cutouts)
        Texture2D metTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color metColor = new Color(0.45f, 0.42f, 0.44f);
        Color darkHole = new Color(0.25f, 0.22f, 0.24f);
        Vector2 center = new Vector2(32, 32);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d = Vector2.Distance(p, center);
                float noise = Mathf.Sin(x * 0.2f) * Mathf.Cos(y * 0.2f) * 3f;
                if (d <= 24f + noise)
                {
                    float cr1 = Vector2.Distance(p, new Vector2(22, 36));
                    float cr2 = Vector2.Distance(p, new Vector2(40, 24));
                    if (cr1 <= 6f || cr2 <= 5f)
                    {
                        metTex.SetPixel(x, y, darkHole);
                    }
                    else
                    {
                        metTex.SetPixel(x, y, metColor);
                    }
                }
                else
                {
                    metTex.SetPixel(x, y, clear);
                }
            }
        }
        metTex.Apply();
        meteorSprite = Sprite.Create(metTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);

        // 2. Cloud Sprite (cartoon fluffy background cloud with black outline and blue shading)
        Texture2D cTex = new Texture2D(128, 64, TextureFormat.RGBA32, false);
        bool[,] mask = new bool[128, 64];
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                Vector2 p = new Vector2(x, y);
                bool inside = false;
                if (Vector2.Distance(p, new Vector2(38, 26)) <= 20f ||
                    Vector2.Distance(p, new Vector2(64, 34)) <= 24f ||
                    Vector2.Distance(p, new Vector2(90, 26)) <= 20f ||
                    (x >= 38 && x <= 90 && y >= 10 && y <= 26))
                {
                    inside = true;
                }
                mask[x, y] = inside;
            }
        }

        Color outlineColor = Color.black;
        Color blueShading = new Color(0.35f, 0.65f, 1f); // light blue for bottom shading
        Color whiteBody = Color.white;

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                if (mask[x, y])
                {
                    bool isEdge = (x == 0 || x == 127 || y == 0 || y == 63 ||
                                   !mask[x - 1, y] || !mask[x + 1, y] ||
                                   !mask[x, y - 1] || !mask[x, y + 1]);
                    
                    if (isEdge)
                    {
                        cTex.SetPixel(x, y, outlineColor);
                    }
                    else
                    {
                        bool isShading = false;
                        if (y < 20 && y > 10)
                        {
                            float wave = Mathf.Sin(x * 0.2f);
                            if (wave > 0.4f && wave < 0.8f)
                            {
                                isShading = true;
                            }
                        }

                        cTex.SetPixel(x, y, isShading ? blueShading : whiteBody);
                    }
                }
                else
                {
                    cTex.SetPixel(x, y, clear);
                }
            }
        }
        cTex.Apply();
        cloudSprite = Sprite.Create(cTex, new Rect(0, 0, 128, 64), new Vector2(0.5f, 0.5f), 100f);

        // 3. Flag Sprite (pole and swaying banner)
        Texture2D fTex = new Texture2D(32, 64, TextureFormat.RGBA32, false);
        Color poleColor = new Color(0.6f, 0.6f, 0.6f);
        Color bannerColor = new Color(0.9f, 0.15f, 0.15f);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                Color col = clear;
                if (x >= 2 && x <= 4 && y >= 4)
                {
                    col = poleColor;
                }
                else if (x > 4 && x < 28 && y >= 36 && y <= 56)
                {
                    col = bannerColor;
                }
                fTex.SetPixel(x, y, col);
            }
        }
        fTex.Apply();
        flagSprite = Sprite.Create(fTex, new Rect(0, 0, 32, 64), new Vector2(0.5f, 0.5f), 100f);

        // 4. Planet Red Sprite (Mars type: rust/red circle with orange swirls)
        Texture2D rPlanetTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        Vector2 planetCenter = new Vector2(64, 64);
        Color planetRed = new Color(0.85f, 0.35f, 0.2f);
        Color planetOrange = new Color(0.95f, 0.6f, 0.15f);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                Vector2 p = new Vector2(x, y);
                float dist = Vector2.Distance(p, planetCenter);
                if (dist <= 40f)
                {
                    float swirl = Mathf.Sin(x * 0.15f + y * 0.08f);
                    rPlanetTex.SetPixel(x, y, swirl > 0.3f ? planetOrange : planetRed);
                }
                else
                {
                    rPlanetTex.SetPixel(x, y, clear);
                }
            }
        }
        rPlanetTex.Apply();
        planetRedSprite = Sprite.Create(rPlanetTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);

        // 5. Planet Blue Sprite (Neptune type: dark blue circle with light cyan swirls)
        Texture2D bPlanetTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        Color planetBlue = new Color(0.1f, 0.3f, 0.75f);
        Color planetCyan = new Color(0.3f, 0.75f, 0.95f);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                Vector2 p = new Vector2(x, y);
                float dist = Vector2.Distance(p, planetCenter);
                if (dist <= 36f)
                {
                    float swirl = Mathf.Cos(x * 0.1f - y * 0.15f);
                    bPlanetTex.SetPixel(x, y, swirl > 0.2f ? planetCyan : planetBlue);
                }
                else
                {
                    bPlanetTex.SetPixel(x, y, clear);
                }
            }
        }
        bPlanetTex.Apply();
        planetBlueSprite = Sprite.Create(bPlanetTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);

        // 6. Planet Yellow Sprite (Saturn type: yellow circle with beautiful rings)
        Texture2D yPlanetTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        Color planetYellow = new Color(0.92f, 0.8f, 0.35f);
        Color planetRing = new Color(0.98f, 0.92f, 0.65f, 0.75f);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                Vector2 p = new Vector2(x, y);
                float dist = Vector2.Distance(p, planetCenter);
                Color pixelColor = clear;

                float ringDistX = (x - 64) * 0.6f + (y - 64) * 0.8f;
                float ringDistY = (y - 64) * 0.6f - (x - 64) * 0.8f;
                float ringRadiusSq = ringDistX * ringDistX + (ringDistY * 3f) * (ringDistY * 3f);

                if (ringRadiusSq >= 42f * 42f && ringRadiusSq <= 60f * 60f)
                {
                    pixelColor = planetRing;
                }

                if (dist <= 26f)
                {
                    pixelColor = planetYellow;
                }

                yPlanetTex.SetPixel(x, y, pixelColor);
            }
        }
        yPlanetTex.Apply();
        planetYellowSprite = Sprite.Create(yPlanetTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);

        // 7. Goomba Sprite (Mario mushroom enemy)
        Texture2D gTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color goombaBrown = new Color(0.65f, 0.35f, 0.15f);
        Color skinColor = new Color(0.98f, 0.82f, 0.72f);
        Color feetColor = new Color(0.2f, 0.2f, 0.2f);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                Color col = clear;
                if (Vector2.Distance(new Vector2(x, y), new Vector2(32, 40)) <= 22f && y >= 28)
                {
                    col = goombaBrown;
                }
                else if (x >= 20 && x <= 44 && y >= 14 && y < 28)
                {
                    col = skinColor;
                    if ((x == 26 || x == 38) && y >= 20 && y <= 24)
                    {
                        col = Color.black;
                    }
                }
                else if (((x >= 14 && x <= 26) || (x >= 38 && x <= 50)) && y >= 4 && y < 14)
                {
                    col = feetColor;
                }
                gTex.SetPixel(x, y, col);
            }
        }
        gTex.Apply();
        goombaSprite = Sprite.Create(gTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
    }
}

/// <summary>
/// Container holding horizontal drift velocities and screen vertical limits.
/// </summary>
public class DriftData : MonoBehaviour
{
    public float speed;
    public float rotSpeed;
    public float minY;
    public float maxY;
}

/// <summary>
/// Adds organic rotational sway movement to simulate wind or flags.
/// </summary>
public class SwayComponent : MonoBehaviour
{
    private float swaySpeed;
    private float swayAmount;
    private float startRot;

    void Start()
    {
        swaySpeed = Random.Range(2f, 4f);
        swayAmount = Random.Range(8f, 15f);
        startRot = transform.localEulerAngles.z;
    }

    void Update()
    {
        float angle = startRot + Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
