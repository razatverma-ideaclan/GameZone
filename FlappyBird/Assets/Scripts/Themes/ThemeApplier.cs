using UnityEngine;

/// <summary>
/// Controls applying visual, background color, and audio updates to all
/// game elements in the current scene based on the selected theme.
/// </summary>
public class ThemeApplier : MonoBehaviour
{
    public static ThemeApplier Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("Optional Text UI component to show the name of the current theme.")]
    public UnityEngine.UI.Text themeNameLabel;

    // Cached original assets to act as fallback defaults
    private Sprite[] originalPlayerSprites;
    private Sprite originalObstacleTopSprite;
    private Sprite originalObstacleBottomSprite;
    private Sprite originalObstacleTopCapSprite;
    private Sprite originalObstacleBottomCapSprite;
    private Sprite originalBackgroundSprite;
    private Sprite originalGroundDirtSprite;
    private Sprite originalGroundGrassSprite;

    private AudioClip originalFlapSound;
    private AudioClip originalHitSound;
    private AudioClip originalFallSound;
    private AudioClip originalScoreSound;
    private AudioClip originalMusic;

    private Color originalCameraColor;
    private BirdController.BirdSkin[] originalClassicSkins;
    private bool initialized = false;

    private void Awake()
    {
        Instance = this;
    }

    private const string SelectedHeroKey = "FlappyBird_SelectedHeroGlobal";

    private void Start()
    {
        InitializeFallbacks();
        ApplySelectedTheme();
        ApplySelectedHero();
    }

    /// <summary>
    /// Discovers and caches all the baseline scene assets so they can be
    /// used as defaults if a theme has unassigned/missing assets.
    /// </summary>
    public void InitializeFallbacks()
    {
        if (initialized) return;

        // Player / Bird Controller fallbacks (include inactive — bird is hidden while
        // browsing non-Lobby menu screens, but its data should still be readable)
        BirdController bird = FindFirstObjectByType<BirdController>(FindObjectsInactive.Include);
        if (bird != null)
        {
            originalClassicSkins = bird.skins;
            if (bird.skins != null && bird.skins.Length > 0)
            {
                originalPlayerSprites = bird.skins[0].flapSprites;
            }
            else
            {
                originalPlayerSprites = bird.flapSprites;
            }
            originalFlapSound = bird.flapSound;
            originalHitSound = bird.hitSound;
            originalFallSound = bird.fallSound;
        }

        // Pipe / Obstacle prefab fallbacks
        PipeSpawner spawner = FindObjectOfType<PipeSpawner>();
        if (spawner != null && spawner.pipePairPrefab != null)
            {
            Transform top = spawner.pipePairPrefab.transform.Find("PipeTop");
            if (top != null) originalObstacleTopSprite = top.GetComponent<SpriteRenderer>().sprite;

            Transform bottom = spawner.pipePairPrefab.transform.Find("PipeBottom");
            if (bottom != null) originalObstacleBottomSprite = bottom.GetComponent<SpriteRenderer>().sprite;

            Transform capTop = spawner.pipePairPrefab.transform.Find("CapTop");
            if (capTop != null) originalObstacleTopCapSprite = capTop.GetComponent<SpriteRenderer>().sprite;

            Transform capBottom = spawner.pipePairPrefab.transform.Find("CapBottom");
            if (capBottom != null) originalObstacleBottomCapSprite = capBottom.GetComponent<SpriteRenderer>().sprite;
        }

        // Background / sky fallbacks
        GameObject bgGO = GameObject.Find("Background");
        if (bgGO != null)
        {
            originalBackgroundSprite = bgGO.GetComponent<SpriteRenderer>().sprite;
        }

        // Ground/Grass fallbacks
        GameObject groundTile = GameObject.Find("GroundTileA");
        if (groundTile != null)
        {
            originalGroundDirtSprite = groundTile.GetComponent<SpriteRenderer>().sprite;
            Transform grass = groundTile.transform.Find("Grass");
            if (grass != null) originalGroundGrassSprite = grass.GetComponent<SpriteRenderer>().sprite;
        }

        // GameManager / Audio fallbacks
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            originalScoreSound = gm.scoreSound;
            originalMusic = gm.backgroundMusic;
        }

        // Main Camera background color fallback
        if (Camera.main != null)
        {
            originalCameraColor = Camera.main.backgroundColor;
        }

        initialized = true;
    }

    /// <summary>
    /// Reads the currently selected theme from the manager and applies it.
    /// </summary>
    public void ApplySelectedTheme()
    {
        if (ThemeManager.Instance == null) return;
        ThemeData theme = ThemeManager.Instance.GetCurrentTheme();
        if (theme != null)
        {
            ApplyTheme(theme);
        }
    }

    /// <summary>
    /// Implements visual, background color, and audio overrides for a specific ThemeData.
    /// </summary>
    public void ApplyTheme(ThemeData theme)
    {
        InitializeFallbacks();

        if (theme == null) return;

        // 1. Update theme name on UI
        if (themeNameLabel != null)
        {
            themeNameLabel.text = "WORLD: " + theme.themeName.ToUpper();
        }

        // 2. Environment audio (per-world flap/hit/land sounds). The bird's visual sprite is
        // fully independent of world selection now — see ApplyHeroSprite() / ApplySelectedHero().
        // Include inactive: picking a World also happens while the bird is hidden (Worlds screen).
        BirdController bird = FindFirstObjectByType<BirdController>(FindObjectsInactive.Include);
        if (bird != null)
        {
            bird.flapSound = theme.flapSound != null ? theme.flapSound : originalFlapSound;
            bird.hitSound = theme.hitSound != null ? theme.hitSound : originalHitSound;
            bird.fallSound = theme.hitSound != null ? theme.hitSound : originalFallSound; // land sound fallback
        }

        // 3. Override Background Scrolling background components — applied immediately,
        // the menu reflects whichever world is currently selected.
        foreach (var sr in FindObjectsOfType<SpriteRenderer>())
        {
            string name = sr.gameObject.name;
            if (name == "Background" || name == "Background2")
            {
                sr.sprite = theme.backgroundSprite != null ? theme.backgroundSprite : originalBackgroundSprite;
            }
        }

        // 4. Override Ground Dirt and Grass
        foreach (var go in GameObject.FindGameObjectsWithTag("Ground"))
        {
            SpriteRenderer dirtSr = go.GetComponent<SpriteRenderer>();
            if (dirtSr != null)
            {
                dirtSr.sprite = theme.groundDirtSprite != null ? theme.groundDirtSprite : originalGroundDirtSprite;
            }

            Transform grass = go.transform.Find("Grass");
            if (grass != null)
            {
                SpriteRenderer grassSr = grass.GetComponent<SpriteRenderer>();
                if (grassSr != null)
                {
                    grassSr.sprite = theme.groundGrassSprite != null ? theme.groundGrassSprite : originalGroundGrassSprite;
                }
            }
        }

        // 5. Override Camera Background color and Sky gradients
        GameObject skyBg = GameObject.Find("SkyBackground");
        if (Camera.main != null)
        {
            // Keep SkyBackground active for all themes to allow score-based weather cycles!
            if (skyBg != null) skyBg.SetActive(true);

            if (theme.themeName.ToLower() != "classic")
            {
                Camera.main.backgroundColor = theme.themeColor;

                // Tint weather sky gradient layers to match theme color tones
                WeatherController weather = FindFirstObjectByType<WeatherController>();
                if (weather != null)
                {
                    Color themeCol = theme.themeColor;
                    if (weather.skyDay != null) 
                        weather.skyDay.color = new Color(themeCol.r, themeCol.g, themeCol.b, weather.skyDay.color.a);
                    
                    if (weather.skySunset != null) 
                        weather.skySunset.color = new Color(Mathf.Clamp01(themeCol.r * 1.3f), Mathf.Clamp01(themeCol.g * 0.7f), Mathf.Clamp01(themeCol.b * 0.4f), weather.skySunset.color.a);
                    
                    if (weather.skyNight != null) 
                        weather.skyNight.color = new Color(Mathf.Clamp01(themeCol.r * 0.2f), Mathf.Clamp01(themeCol.g * 0.2f), Mathf.Clamp01(themeCol.b * 0.4f), weather.skyNight.color.a);
                    
                    if (weather.skyDawn != null) 
                        weather.skyDawn.color = new Color(Mathf.Clamp01(themeCol.r * 0.8f), Mathf.Clamp01(themeCol.g * 0.6f), Mathf.Clamp01(themeCol.b * 0.9f), weather.skyDawn.color.a);
                }
            }
            else
            {
                Camera.main.backgroundColor = originalCameraColor;
                WeatherController weather = FindFirstObjectByType<WeatherController>();
                if (weather != null)
                {
                    if (weather.skyDay != null) weather.skyDay.color = new Color(1f, 1f, 1f, weather.skyDay.color.a);
                    if (weather.skySunset != null) weather.skySunset.color = new Color(1f, 1f, 1f, weather.skySunset.color.a);
                    if (weather.skyNight != null) weather.skyNight.color = new Color(1f, 1f, 1f, weather.skyNight.color.a);
                    if (weather.skyDawn != null) weather.skyDawn.color = new Color(1f, 1f, 1f, weather.skyDawn.color.a);
                }
            }
        }

        // 6. Override Audio on GameManager
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.scoreSound = theme.scoreSound != null ? theme.scoreSound : originalScoreSound;
            
            // Background Music override
            AudioClip newMusic = theme.backgroundMusic != null ? theme.backgroundMusic : originalMusic;
            if (gm.backgroundMusic != newMusic)
            {
                gm.backgroundMusic = newMusic;
                // If game has already started and music is playing, swap it dynamically
                AudioSource musicSource = gm.GetComponent<AudioSource>();
                if (musicSource == null) musicSource = gm.gameObject.GetComponentInChildren<AudioSource>();
                
                if (musicSource != null && musicSource.isPlaying)
                {
                    musicSource.Stop();
                    musicSource.clip = newMusic;
                    if (newMusic != null) musicSource.Play();
                }
            }
        }

        // 7. Update any active pipes/obstacles already spawned in the scene
        foreach (var applier in FindObjectsOfType<ThemeObstacleApplier>())
        {
            applier.ApplyCurrentTheme();
        }

        // 8. Dynamic Bottom Bar Custom Styling matching the selected theme
        GameObject bottomBar = GameObject.Find("BottomNavBar");
        if (bottomBar != null)
        {
            MenuThemeConfig themeConfig = FindFirstObjectByType<MenuThemeConfig>();
            if (themeConfig != null)
            {
                themeConfig.ApplyTheme(theme);
                return;
            }

            Color themeColor = theme.themeColor;
            
            // Background tint (blend with theme color but very dark for contrast)
            UnityEngine.UI.Image barImg = bottomBar.GetComponent<UnityEngine.UI.Image>();
            if (barImg != null)
            {
                Color barColor = Color.Lerp(themeColor, Color.black, 0.90f);
                barColor.a = 0.95f;
                barImg.color = barColor;
            }

            // Outline glow (bright themed outline border)
            UnityEngine.UI.Outline barOutline = bottomBar.GetComponent<UnityEngine.UI.Outline>();
            if (barOutline != null)
            {
                Color outlineColor = Color.Lerp(themeColor, Color.white, 0.40f);
                outlineColor.a = 0.35f;
                barOutline.effectColor = outlineColor;
            }

            // Active indicator pill tint
            Transform indTrans = bottomBar.transform.Find("ActiveIndicator");
            if (indTrans != null)
            {
                UnityEngine.UI.Image indImg = indTrans.GetComponent<UnityEngine.UI.Image>();
                if (indImg != null)
                {
                    Color indColor = themeColor;
                    indColor.a = 0.22f; // semi-transparent glow
                    indImg.color = indColor;
                }
            }

            // Center play/home button tint
            Transform playTrans = bottomBar.transform.Find("PlayButton");
            if (playTrans != null)
            {
                UnityEngine.UI.Image playImg = playTrans.GetComponent<UnityEngine.UI.Image>();
                if (playImg != null)
                {
                    Color playBtnColor = themeColor;
                    // Saturate and brighten it so it pops!
                    float h, s, v;
                    Color.RGBToHSV(playBtnColor, out h, out s, out v);
                    s = Mathf.Max(s, 0.78f);
                    v = Mathf.Max(v, 0.90f);
                    playImg.color = Color.HSVToRGB(h, s, v);
                }
            }
        }
    }

    /// <summary>
    /// Reads the persisted global hero choice (0-20, spanning all 7 worlds x 3 skins) and
    /// applies it to the bird — fully independent of which world is currently selected.
    /// </summary>
    public void ApplySelectedHero()
    {
        InitializeFallbacks();
        // Default to Space world (1) / Cosmic UFO skin (1) => global index 4, for first-time players.
        int globalIndex = PlayerPrefs.GetInt(SelectedHeroKey, 4);
        ApplyHeroSprite(globalIndex);
    }

    /// <summary>
    /// Forces the bird to display one specific hero (world*3 + skin) regardless of the
    /// currently active world/environment theme. Writes directly into BirdController.skins
    /// (as a single always-index-0 entry) — the same skins[]/currentSkinIndex mechanism the
    /// old per-theme skin picker used, rather than the separate OverrideSprites path.
    /// </summary>
    public void ApplyHeroSprite(int globalIndex)
    {
        // Include inactive: the bird is deliberately deactivated while browsing the Heroes
        // screen (so it doesn't show behind the cards), and FindObjectOfType() without this
        // flag silently skips inactive GameObjects — which was why picks made while on the
        // Heroes screen intermittently failed to reach the bird at all.
        BirdController bird = FindFirstObjectByType<BirdController>(FindObjectsInactive.Include);
        if (bird == null) return;

        int worldIndex = globalIndex / 3;
        int skinIndex = globalIndex % 3;

        Sprite[] frames = null;
        if (worldIndex == 0)
        {
            if (originalClassicSkins != null && skinIndex < originalClassicSkins.Length)
            {
                frames = originalClassicSkins[skinIndex].flapSprites;
            }
        }
        else if (ThemeManager.Instance != null && ThemeManager.Instance.themes != null && worldIndex < ThemeManager.Instance.themes.Length)
        {
            ThemeData heroWorld = ThemeManager.Instance.themes[worldIndex];
            if (heroWorld.playerSprites != null && skinIndex < heroWorld.playerSprites.Length)
            {
                Sprite s = heroWorld.playerSprites[skinIndex];
                frames = new Sprite[] { s, s, s };
            }
        }

        if (frames == null) return; // couldn't resolve this hero — leave whatever is currently showing

        bird.skins = new BirdController.BirdSkin[] { new BirdController.BirdSkin { flapSprites = frames } };
        bird.currentSkinIndex = 0;
        bird.OverrideSprites(null); // clear any stale override so skins[0] always wins
        bird.UpdateSkin();
    }

    /// <summary>
    /// Helper method called by obstacles to format themselves on spawn.
    /// </summary>
    public void ApplyThemeToObstacle(GameObject obstacle)
    {
        if (ThemeManager.Instance == null) return;
        ThemeData theme = ThemeManager.Instance.GetCurrentTheme();
        if (theme == null) return;

        // Apply to PipeBottom
        Transform bottom = obstacle.transform.Find("PipeBottom");
        if (bottom != null)
        {
            Sprite s = theme.obstacleBottomSprite != null ? theme.obstacleBottomSprite : originalObstacleBottomSprite;
            bottom.GetComponent<SpriteRenderer>().sprite = s;
        }

        // Apply to PipeTop
        Transform top = obstacle.transform.Find("PipeTop");
        if (top != null)
        {
            Sprite s = theme.obstacleTopSprite != null ? theme.obstacleTopSprite : originalObstacleTopSprite;
            top.GetComponent<SpriteRenderer>().sprite = s;
        }

        // Apply to CapBottom
        Transform capBottom = obstacle.transform.Find("CapBottom");
        if (capBottom != null)
        {
            if (theme.themeName.ToLower() != "classic" && theme.obstacleBottomCapSprite == null)
            {
                capBottom.gameObject.SetActive(false); // Hide cap lip for custom obstacles if no cap sprite is set
            }
            else
            {
                capBottom.gameObject.SetActive(true);
                Sprite s = theme.obstacleBottomCapSprite != null ? theme.obstacleBottomCapSprite : originalObstacleBottomCapSprite;
                capBottom.GetComponent<SpriteRenderer>().sprite = s;
            }
        }

        // Apply to CapTop
        Transform capTop = obstacle.transform.Find("CapTop");
        if (capTop != null)
        {
            if (theme.themeName.ToLower() != "classic" && theme.obstacleTopCapSprite == null)
            {
                capTop.gameObject.SetActive(false);
            }
            else
            {
                capTop.gameObject.SetActive(true);
                Sprite s = theme.obstacleTopCapSprite != null ? theme.obstacleTopCapSprite : originalObstacleTopCapSprite;
                capTop.GetComponent<SpriteRenderer>().sprite = s;
            }
        }
    }
}
