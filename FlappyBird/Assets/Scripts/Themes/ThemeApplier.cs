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

    private void Start()
    {
        InitializeFallbacks();
        ApplySelectedTheme();
    }

    /// <summary>
    /// Discovers and caches all the baseline scene assets so they can be
    /// used as defaults if a theme has unassigned/missing assets.
    /// </summary>
    public void InitializeFallbacks()
    {
        if (initialized) return;

        // Player / Bird Controller fallbacks
        BirdController bird = FindObjectOfType<BirdController>();
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
            themeNameLabel.text = theme.themeName.ToUpper();
        }

        // 2. Override Player Sprites
        BirdController bird = FindObjectOfType<BirdController>();
        if (bird != null)
        {
            if (theme.themeName.ToLower() == "classic")
            {
                if (originalClassicSkins != null) bird.skins = originalClassicSkins;
                bird.OverrideSprites(null);
            }
            else
            {
                BirdController.BirdSkin[] overrideSkins = new BirdController.BirdSkin[3];
                for (int s = 0; s < 3; s++)
                {
                    overrideSkins[s] = new BirdController.BirdSkin();
                    Sprite spr = null;
                    if (theme.playerSprites != null && theme.playerSprites.Length > 0)
                    {
                        int sprIdx = Mathf.Clamp(s, 0, theme.playerSprites.Length - 1);
                        spr = theme.playerSprites[sprIdx];
                    }
                    else
                    {
                        spr = originalPlayerSprites[1];
                    }
                    overrideSkins[s].flapSprites = new Sprite[] { spr, spr, spr };
                }
                bird.skins = overrideSkins;
                bird.OverrideSprites(null);
            }
            
            // Re-apply skin graphics and index logic
            int savedSkin = PlayerPrefs.GetInt("FlappyBird_SelectedSkin", 0);
            bird.currentSkinIndex = Mathf.Clamp(savedSkin, 0, bird.skins.Length - 1);
            bird.UpdateSkin();
            
            // Audio Overrides
            bird.flapSound = theme.flapSound != null ? theme.flapSound : originalFlapSound;
            bird.hitSound = theme.hitSound != null ? theme.hitSound : originalHitSound;
            bird.fallSound = theme.hitSound != null ? theme.hitSound : originalFallSound; // land sound fallback
        }

        // 3. Override Background SpScrolling background components
        foreach (var sr in FindObjectsOfType<SpriteRenderer>())
        {
            if (sr.gameObject.name.StartsWith("Background"))
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
            // If theme is not Classic (index 0), use the custom color and hide the weather gradients
            if (theme.themeName.ToLower() != "classic")
            {
                Camera.main.backgroundColor = theme.themeColor;
                if (skyBg != null) skyBg.SetActive(false);
            }
            else
            {
                Camera.main.backgroundColor = originalCameraColor;
                if (skyBg != null) skyBg.SetActive(true);
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
