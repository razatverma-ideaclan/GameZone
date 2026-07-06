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

    private Sprite marsMeteorSprite;
    private Sprite marsSatelliteSprite;

    private Sprite emberSprite;
    private Sprite bubbleSprite;
    private Sprite pollenSprite;
    private Sprite fogSprite;

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
        else if (tName == "mars")
        {
            // Spawn 4 drifting and rotating Mars meteors
            for (int i = 0; i < 4; i++)
            {
                GameObject met = new GameObject("MarsMeteor");
                met.transform.SetParent(transform, false);
                
                SpriteRenderer sr = met.AddComponent<SpriteRenderer>();
                sr.sprite = marsMeteorSprite;
                sr.sortingOrder = -8; // behind players and pipes

                float scale = Random.Range(0.6f, 1.3f);
                met.transform.localScale = new Vector3(scale, scale, 1f);
                met.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(-2f, 5f), 1f);

                DriftData drift = met.AddComponent<DriftData>();
                drift.speed = Random.Range(0.6f, 1.8f);
                drift.rotSpeed = Random.Range(-20f, 20f);
                drift.minY = -2f;
                drift.maxY = 5f;

                animatedElements.Add(met);
            }

            // Spawn 1 orbiting satellite
            GameObject sat = new GameObject("MarsSatellite");
            sat.transform.SetParent(transform, false);
            SpriteRenderer satSr = sat.AddComponent<SpriteRenderer>();
            satSr.sprite = marsSatelliteSprite;
            satSr.sortingOrder = -9; // behind meteors

            sat.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            sat.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(2f, 5f), 1.2f);

            DriftData satDrift = sat.AddComponent<DriftData>();
            satDrift.speed = 0.3f; // drifts very slowly
            satDrift.rotSpeed = 2f; // very slight rotation
            satDrift.minY = 2f;
            satDrift.maxY = 5f;

            animatedElements.Add(sat);
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
                // Raised so the Goomba's feet land ON TOP of the brick floor (dirtTopY = -5.6)
                // instead of sinking into it — was previously placed too low, burying the legs/body.
                const float goombaGroundY = -4.8f;
                goomba.transform.position = new Vector3(startX, goombaGroundY, 1.2f);

                DriftData gDrift = goomba.AddComponent<DriftData>();
                gDrift.speed = 0.6f + i * 0.4f; // walk slowly along the floor
                gDrift.rotSpeed = 0f;
                gDrift.minY = goombaGroundY;
                gDrift.maxY = goombaGroundY;

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
        else if (tName == "dragon")
        {
            // Spawn 5 drifting embers
            for (int i = 0; i < 5; i++)
            {
                GameObject ember = new GameObject("DragonEmber");
                ember.transform.SetParent(transform, false);

                SpriteRenderer sr = ember.AddComponent<SpriteRenderer>();
                sr.sprite = emberSprite;
                sr.sortingOrder = -8;

                float scale = Random.Range(0.6f, 1.4f);
                ember.transform.localScale = new Vector3(scale, scale, 1f);
                ember.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(-2f, 5f), 1f);

                DriftData drift = ember.AddComponent<DriftData>();
                drift.speed = Random.Range(0.8f, 2.2f);
                drift.rotSpeed = Random.Range(-15f, 15f);
                drift.minY = -2f;
                drift.maxY = 5f;

                animatedElements.Add(ember);
            }
        }
        else if (tName == "fish")
        {
            // Spawn 6 drifting bubbles
            for (int i = 0; i < 6; i++)
            {
                GameObject bubble = new GameObject("FishBubble");
                bubble.transform.SetParent(transform, false);

                SpriteRenderer sr = bubble.AddComponent<SpriteRenderer>();
                sr.sprite = bubbleSprite;
                sr.sortingOrder = -8;

                float scale = Random.Range(0.5f, 1.3f);
                bubble.transform.localScale = new Vector3(scale, scale, 1f);
                bubble.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(-3f, 5f), 1f);

                DriftData drift = bubble.AddComponent<DriftData>();
                drift.speed = Random.Range(0.3f, 1.0f);
                drift.rotSpeed = 0f;
                drift.minY = -3f;
                drift.maxY = 5f;

                animatedElements.Add(bubble);
            }
        }
        else if (tName == "bee")
        {
            // Spawn 8 drifting pollen flecks
            for (int i = 0; i < 8; i++)
            {
                GameObject pollen = new GameObject("BeePollen");
                pollen.transform.SetParent(transform, false);

                SpriteRenderer sr = pollen.AddComponent<SpriteRenderer>();
                sr.sprite = pollenSprite;
                sr.sortingOrder = -8;

                float scale = Random.Range(0.8f, 1.6f);
                pollen.transform.localScale = new Vector3(scale, scale, 1f);
                pollen.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(-3f, 5f), 1f);

                DriftData drift = pollen.AddComponent<DriftData>();
                drift.speed = Random.Range(0.4f, 1.2f);
                drift.rotSpeed = Random.Range(-30f, 30f);
                drift.minY = -3f;
                drift.maxY = 5f;

                animatedElements.Add(pollen);
            }
        }
        else if (tName == "ninja")
        {
            // Spawn 4 drifting fog wisps
            for (int i = 0; i < 4; i++)
            {
                GameObject fog = new GameObject("NinjaFog");
                fog.transform.SetParent(transform, false);

                SpriteRenderer sr = fog.AddComponent<SpriteRenderer>();
                sr.sprite = fogSprite;
                sr.sortingOrder = -8;

                float scale = Random.Range(1.0f, 2.0f);
                fog.transform.localScale = new Vector3(scale, scale, 1f);
                fog.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(-2f, 4f), 1f);

                DriftData drift = fog.AddComponent<DriftData>();
                drift.speed = Random.Range(0.2f, 0.6f);
                drift.rotSpeed = 0f;
                drift.minY = -2f;
                drift.maxY = 4f;

                animatedElements.Add(fog);
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

        // 4. Planet Red Sprite — TEAL PIXEL CRATER MOON (large, from reference)
        //    Big teal/cyan planet with 3 dark round craters and a pixelated surface
        int moonSize = 128;
        Texture2D rPlanetTex = new Texture2D(moonSize, moonSize, TextureFormat.RGBA32, false);
        Vector2 planetCenter = new Vector2(moonSize / 2f, moonSize / 2f);
        float moonRadius = moonSize * 0.42f;
        Color moonBase    = new Color(0.10f, 0.78f, 0.72f); // bright teal
        Color moonMid     = new Color(0.07f, 0.60f, 0.58f); // medium teal
        Color moonDark    = new Color(0.04f, 0.38f, 0.40f); // dark teal shadow
        Color moonShine   = new Color(0.55f, 0.98f, 0.95f); // bright highlight
        Color craterColor = new Color(0.04f, 0.30f, 0.34f); // dark crater fill
        Color craterRim   = new Color(0.06f, 0.42f, 0.48f); // crater inner rim

        // Crater positions (x, y, radius)
        float[,] craters = { { 42f, 70f, 11f }, { 76f, 52f, 8f }, { 60f, 84f, 7f } };

        for (int y = 0; y < moonSize; y++)
        {
            for (int x = 0; x < moonSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), planetCenter);
                if (dist > moonRadius) { rPlanetTex.SetPixel(x, y, clear); continue; }

                // Pixel-art quantised shading: shade based on x position (light left)
                float shade = Mathf.Clamp01((moonRadius - dist) / moonRadius);
                float lightX = (float)x / moonSize;
                Color baseCol = lightX < 0.35f ? moonShine : (lightX < 0.65f ? moonBase : (lightX < 0.85f ? moonMid : moonDark));

                // Horizontal band variation (slight stripes like a pixel planet)
                int bandY = (int)(y * 4f / moonSize);
                if (bandY % 2 == 0) baseCol = Color.Lerp(baseCol, moonMid, 0.15f);

                // Draw craters
                bool inCrater = false;
                for (int c = 0; c < 3; c++)
                {
                    float cd = Vector2.Distance(new Vector2(x, y), new Vector2(craters[c, 0], craters[c, 1]));
                    if (cd <= craters[c, 2]) { baseCol = craterColor; inCrater = true; break; }
                    if (cd <= craters[c, 2] + 2.5f && !inCrater) { baseCol = craterRim; }
                }

                rPlanetTex.SetPixel(x, y, baseCol);
            }
        }
        rPlanetTex.Apply();
        planetRedSprite = Sprite.Create(rPlanetTex, new Rect(0, 0, moonSize, moonSize), new Vector2(0.5f, 0.5f), 100f);

        // 5. Planet Blue Sprite — PINK/PURPLE SATURN (from reference, small with angled ring trail)
        Texture2D bPlanetTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        Vector2 saturnCenter = new Vector2(64, 64);
        float saturnRadius = 28f;
        Color saturnBody  = new Color(0.75f, 0.40f, 0.85f); // pink-purple body
        Color saturnMid   = new Color(0.60f, 0.28f, 0.72f);
        Color saturnShine = new Color(0.92f, 0.70f, 1.00f); // highlight
        Color ringColor   = new Color(0.55f, 0.25f, 0.72f, 0.70f); // purple ring trail
        Color ringFade    = new Color(0.35f, 0.15f, 0.55f, 0.35f);

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), saturnCenter);
                Color px = clear;

                // Angled elliptical ring trail (diagonal lower-left to upper-right)
                float rx = (x - 64) * 0.85f + (y - 64) * 0.52f;
                float ry = (y - 64) * 0.85f - (x - 64) * 0.52f;
                float rDist = Mathf.Sqrt(rx * rx + (ry * 2.8f) * (ry * 2.8f));
                if (rDist >= 34f && rDist <= 54f)
                    px = rDist <= 44f ? ringColor : ringFade;

                // Planet body on top
                if (dist <= saturnRadius)
                {
                    float lx = (float)x / 128f;
                    px = lx < 0.30f ? saturnShine : (lx < 0.65f ? saturnBody : saturnMid);
                }

                bPlanetTex.SetPixel(x, y, px);
            }
        }
        bPlanetTex.Apply();
        planetBlueSprite = Sprite.Create(bPlanetTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);

        // 6. Planet Yellow Sprite — BLUE BLOB PLANET (small pure blue dot from reference)
        Texture2D yPlanetTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Vector2 blobCenter = new Vector2(32, 32);
        Color blobBase  = new Color(0.18f, 0.42f, 0.98f);
        Color blobShine = new Color(0.52f, 0.72f, 1.00f);
        Color blobDark  = new Color(0.08f, 0.18f, 0.62f);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), blobCenter);
                if (dist > 24f) { yPlanetTex.SetPixel(x, y, clear); continue; }
                float lx = (float)x / 64f;
                Color c = lx < 0.30f ? blobShine : (lx < 0.75f ? blobBase : blobDark);
                yPlanetTex.SetPixel(x, y, c);
            }
        }
        yPlanetTex.Apply();
        planetYellowSprite = Sprite.Create(yPlanetTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);

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

        // 8. Mars Meteor Sprite (rusty red/brown rock with darker crater holes)
        Texture2D marsMetTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color marsMetColor = new Color(0.38f, 0.18f, 0.12f); // rusty red-brown
        Color marsDarkHole = new Color(0.20f, 0.08f, 0.05f); // deeper shadow
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d = Vector2.Distance(p, new Vector2(32, 32));
                float noise = Mathf.Sin(x * 0.22f) * Mathf.Cos(y * 0.22f) * 3f;
                if (d <= 23f + noise)
                {
                    float cr1 = Vector2.Distance(p, new Vector2(24, 38));
                    float cr2 = Vector2.Distance(p, new Vector2(38, 22));
                    if (cr1 <= 6f || cr2 <= 5f)
                    {
                        marsMetTex.SetPixel(x, y, marsDarkHole);
                    }
                    else
                    {
                        marsMetTex.SetPixel(x, y, marsMetColor);
                    }
                }
                else
                {
                    marsMetTex.SetPixel(x, y, clear);
                }
            }
        }
        marsMetTex.Apply();
        marsMeteorSprite = Sprite.Create(marsMetTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);

        // 9. Mars Satellite Sprite (blue panels, grey core body, antenna)
        Texture2D satTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                Color col = clear;
                // central body (circle)
                if (Vector2.Distance(new Vector2(x, y), new Vector2(32, 32)) <= 8f)
                {
                    col = new Color(0.70f, 0.70f, 0.75f); // light grey metal
                }
                // solar panels (horizontal bars on left/right)
                else if (y >= 26 && y <= 38 && ((x >= 8 && x <= 22) || (x >= 42 && x <= 56)))
                {
                    col = new Color(0.12f, 0.38f, 0.82f); // solar panel blue
                    // grid lines
                    if (x % 5 == 0 || y % 6 == 0) col = Color.white;
                }
                // antenna stick
                else if (x >= 31 && x <= 33 && y >= 40 && y <= 50)
                {
                    col = new Color(0.50f, 0.50f, 0.55f);
                }
                satTex.SetPixel(x, y, col);
            }
        }
        satTex.Apply();
        marsSatelliteSprite = Sprite.Create(satTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);

        // 10. Ember Sprite (Dragon) — glowing orange-red circle
        Texture2D emberTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Vector2 emberCenter = new Vector2(16, 16);
        Color emberCore = new Color(1f, 0.85f, 0.2f);
        Color emberMid = new Color(0.95f, 0.45f, 0.1f);
        Color emberEdge = new Color(0.75f, 0.15f, 0.05f);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), emberCenter);
                if (dist > 14f) { emberTex.SetPixel(x, y, clear); continue; }
                Color c = dist < 5f ? emberCore : (dist < 10f ? emberMid : emberEdge);
                emberTex.SetPixel(x, y, c);
            }
        }
        emberTex.Apply();
        emberSprite = Sprite.Create(emberTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);

        // 11. Bubble Sprite (Fish) — translucent blue bubble with a bright highlight fleck
        Texture2D bubbleTex = new Texture2D(48, 48, TextureFormat.RGBA32, false);
        Vector2 bubbleCenter = new Vector2(24, 24);
        Color bubbleFill = new Color(0.65f, 0.85f, 1f, 0.35f);
        Color bubbleRim = new Color(0.85f, 0.95f, 1f, 0.75f);
        Color bubbleShine = new Color(1f, 1f, 1f, 0.9f);
        for (int y = 0; y < 48; y++)
        {
            for (int x = 0; x < 48; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), bubbleCenter);
                if (dist > 20f) { bubbleTex.SetPixel(x, y, clear); continue; }
                Color c = dist > 17f ? bubbleRim : bubbleFill;
                if (Vector2.Distance(new Vector2(x, y), new Vector2(17, 31)) <= 4f) c = bubbleShine;
                bubbleTex.SetPixel(x, y, c);
            }
        }
        bubbleTex.Apply();
        bubbleSprite = Sprite.Create(bubbleTex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 100f);

        // 12. Pollen Sprite (Bee) — small soft yellow-white fleck
        Texture2D pollenTex = new Texture2D(20, 20, TextureFormat.RGBA32, false);
        Vector2 pollenCenter = new Vector2(10, 10);
        Color pollenCore = new Color(1f, 0.95f, 0.6f);
        Color pollenEdge = new Color(0.95f, 0.8f, 0.3f, 0.6f);
        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 20; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), pollenCenter);
                if (dist > 8f) { pollenTex.SetPixel(x, y, clear); continue; }
                pollenTex.SetPixel(x, y, dist < 4f ? pollenCore : pollenEdge);
            }
        }
        pollenTex.Apply();
        pollenSprite = Sprite.Create(pollenTex, new Rect(0, 0, 20, 20), new Vector2(0.5f, 0.5f), 100f);

        // 13. Fog Wisp Sprite (Ninja) — soft translucent grey cloud puff
        Texture2D fogTex = new Texture2D(96, 48, TextureFormat.RGBA32, false);
        Color fogColor = new Color(0.55f, 0.55f, 0.6f, 0.45f);
        for (int y = 0; y < 48; y++)
        {
            for (int x = 0; x < 96; x++)
            {
                bool inside = Vector2.Distance(new Vector2(x, y), new Vector2(28, 24)) <= 16f ||
                              Vector2.Distance(new Vector2(x, y), new Vector2(48, 26)) <= 20f ||
                              Vector2.Distance(new Vector2(x, y), new Vector2(70, 24)) <= 16f;
                fogTex.SetPixel(x, y, inside ? fogColor : clear);
            }
        }
        fogTex.Apply();
        fogSprite = Sprite.Create(fogTex, new Rect(0, 0, 96, 48), new Vector2(0.5f, 0.5f), 100f);
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
