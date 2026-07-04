using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game state controller. Handles Start / Playing / GameOver states,
/// score tracking, and UI panel switching.
/// Attach this to an empty GameObject named "GameManager".
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Start, Playing, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Start;

    public enum MenuScreen { Lobby, Worlds, Heroes, Shop, Quests, Leaderboard }
    public MenuScreen CurrentScreen { get; private set; } = MenuScreen.Lobby;

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject gameOverPanel;
    public GameObject scoreText; // assign the in-game score label (TextMeshProUGUI or Text)
    [Tooltip("Text on the Game Over panel showing this run's score.")]
    public GameObject gameOverScoreText;
    [Tooltip("Text on the Game Over panel showing the all-time best score.")]
    public GameObject gameOverBestText;
    [Tooltip("Text on the Game Over panel showing coins collected this run.")]
    public GameObject gameOverCoinsText;
    [Tooltip("Text on the Start panel showing the high score.")]
    public GameObject startHighScoreText;

    [Header("Game Over Board Refinements")]
    public UnityEngine.UI.Image medalImage;
    public Sprite bronzeMedal;
    public Sprite silverMedal;
    public Sprite goldMedal;
    public Sprite platinumMedal;
    public Sprite medalPlaceholder;
    public GameObject newBestBadge;
    public RectTransform resultCardTransform;

    [Header("Optimized Start Screen")]
    public GameObject toastPanel;
    public GameObject themeSelectorPanel;
    public GameObject lobbyPanel;
    public GameObject heroesPanel;
    public GameObject shopPanel;
    public GameObject questsPanel;
    public GameObject leaderboardPanel;
    public GameObject screenBackdrop;
    public UnityEngine.UI.Image playIconImage;
    public Sprite playSprite;
    public Sprite homeSprite;

    [Header("Navigation Buttons")]
    public UnityEngine.UI.Button shopButton;
    public UnityEngine.UI.Button heroesButton;
    public UnityEngine.UI.Button missionsButton;
    public UnityEngine.UI.Button themesButton;
    public UnityEngine.UI.Button centerButton;

    [Header("References")]
    public GameObject bird;
    public PipeSpawner pipeSpawner;
    public ItemSpawner itemSpawner;

    private const string HighScoreKey = "FlappyBird_HighScore";
    private int bestScore = 0;

    // --- Coins & Power-ups ---
    private const string TotalCoinsKey = "FlappyBird_TotalCoins";
    private const string MagnetLevelKey = "FlappyBird_MagnetLevel";
    private const string BoostLevelKey = "FlappyBird_BoostLevel";
    private const string DoubleLevelKey = "FlappyBird_DoubleLevel";
    private const float BaseMagnetRadius = 4.0f;
    private const float BoostSpeedMultiplier = 2.2f;

    // --- Starter Powers: buy charges in the Shop, arm one on the Lobby to auto-apply at the start of your next run ---
    public enum StarterPower { None, Magnet, Boost, Double, Hammer }
    private const string StarterMagnetCountKey = "FlappyBird_StarterMagnetCount";
    private const string StarterBoostCountKey = "FlappyBird_StarterBoostCount";
    private const string StarterDoubleCountKey = "FlappyBird_StarterDoubleCount";
    private const string StarterHammerCountKey = "FlappyBird_StarterHammerCount";
    private const string StarterPowersGrantedKey = "FlappyBird_StarterPowersGranted";
    public const int StarterPurchaseCost = 20;
    private const int StarterFreeGrantAmount = 3;

    public int StarterMagnetCount { get; private set; }
    public int StarterBoostCount { get; private set; }
    public int StarterDoubleCount { get; private set; }
    public int StarterHammerCount { get; private set; }
    public StarterPower ArmedStarter { get; private set; } = StarterPower.None;

    [Header("Coins & Power-ups HUD")]
    public GameObject coinsText;
    public GameObject magnetTimerText;
    public GameObject boostTimerText;
    public GameObject doubleTimerText;
    public GameObject hammerChargesWidget;

    [Header("Starter Power Widgets (Lobby icon + count)")]
    public GameObject starterMagnetWidget;
    public GameObject starterBoostWidget;
    public GameObject starterDoubleWidget;
    public GameObject starterHammerWidget;

    public int TotalCoins { get; private set; }
    public int MagnetLevel { get; private set; } = 1;
    public int BoostLevel { get; private set; } = 1;
    public int DoubleLevel { get; private set; } = 1;

    private int sessionCoins = 0;
    private float magnetTimer = 0f;
    private float boostTimer = 0f;
    private float doubleTimer = 0f;
    private int hammerCharges = 0;

    public bool IsBoostActive => boostTimer > 0f;
    public bool IsDoubleActive => doubleTimer > 0f;
    public bool IsInvulnerable() => IsBoostActive;
    public int HammerCharges => hammerCharges;

    public float GetMagnetRadius()
    {
        if (magnetTimer > 0f) return BaseMagnetRadius;
        return 0f;
    }

    [Header("Audio (optional — leave empty to skip)")]
    public AudioClip scoreSound;
    public AudioClip buttonClickSound;
    [Tooltip("Optional low-volume looping music. Leave empty to skip.")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.25f;

    private int score = 0;
    private float startStateTime = 0f;
    private bool justEnteredStartState = false;
    private AudioSource sfxSource;
    private AudioSource musicSource;

    void Awake()
    {
        // Simple singleton so other scripts can call GameManager.Instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Two separate AudioSources: one for one-shot sound effects, one for looping music.
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        // Local on-device persistence — survives app restarts (PlayerPrefs is
        // Unity's standard local key/value storage, backed by the OS on each platform).
        bestScore = PlayerPrefs.GetInt(HighScoreKey, 0);

        TotalCoins = PlayerPrefs.GetInt(TotalCoinsKey, 0);
        MagnetLevel = PlayerPrefs.GetInt(MagnetLevelKey, 1);
        BoostLevel = PlayerPrefs.GetInt(BoostLevelKey, 1);
        DoubleLevel = PlayerPrefs.GetInt(DoubleLevelKey, 1);

        // First launch only: gift 3 free charges of each starter power so new players
        // can try them without needing coins first.
        if (PlayerPrefs.GetInt(StarterPowersGrantedKey, 0) == 0)
        {
            PlayerPrefs.SetInt(StarterMagnetCountKey, StarterFreeGrantAmount);
            PlayerPrefs.SetInt(StarterBoostCountKey, StarterFreeGrantAmount);
            PlayerPrefs.SetInt(StarterDoubleCountKey, StarterFreeGrantAmount);
            PlayerPrefs.SetInt(StarterHammerCountKey, StarterFreeGrantAmount);
            PlayerPrefs.SetInt(StarterPowersGrantedKey, 1);
            PlayerPrefs.Save();
        }
        StarterMagnetCount = PlayerPrefs.GetInt(StarterMagnetCountKey, 0);
        StarterBoostCount = PlayerPrefs.GetInt(StarterBoostCountKey, 0);
        StarterDoubleCount = PlayerPrefs.GetInt(StarterDoubleCountKey, 0);
        StarterHammerCount = PlayerPrefs.GetInt(StarterHammerCountKey, 0);
    }

    void Start()
    {
        ShowStartState();
    }

    void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            TickPowerupTimers();
        }

        if (CurrentState == GameState.Start)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartGame();
                return;
            }

            if (Input.GetMouseButtonDown(0) || TouchStarted())
            {
                // Any tap on a UI element (nav bar, badges, the Leaderboard button, etc.) is
                // handled by that element's own Button.onClick — don't also treat it as tap-to-start.
                bool overUI = EventSystem.current != null && (Input.touchCount > 0
                    ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                    : EventSystem.current.IsPointerOverGameObject());

                if (!overUI && CurrentScreen == MenuScreen.Lobby && !justEnteredStartState)
                {
                    StartGame();
                }
            }
        }
    }

    private bool TouchStarted()
    {
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }

    public void ShowStartState()
    {
        CurrentState = GameState.Start;
        justEnteredStartState = true;
        startStateTime = Time.time; // Record start screen entry time
        score = 0; // each run starts fresh — this was never reset, so score kept accumulating across retries
        UpdateScoreUI();

        sessionCoins = 0;
        magnetTimer = 0f;
        boostTimer = 0f;
        doubleTimer = 0f;
        hammerCharges = 0;
        SetBoostTrail(false);
        if (magnetTimerText != null) magnetTimerText.SetActive(false);
        if (boostTimerText != null) boostTimerText.SetActive(false);
        if (doubleTimerText != null) doubleTimerText.SetActive(false);
        if (hammerChargesWidget != null) hammerChargesWidget.SetActive(false);
        UpdateCoinsUI();
        RefreshStarterWidgets();

        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreText != null) scoreText.SetActive(false);

        // Update high score on start screen
        bestScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        SetText(startHighScoreText, "BEST: " + bestScore.ToString());

        // Weather reset on start
        WeatherController weather = FindObjectOfType<WeatherController>();
        if (weather != null) weather.ResetWeather();

        if (pipeSpawner != null) pipeSpawner.StopSpawning();
        if (itemSpawner != null) itemSpawner.StopSpawning();

        // Remove any pipes left over from a previous run.
        ClearExistingPipes();

        // Always land back on the Lobby screen (resets bird preview, panels, nav visuals).
        SetMenuScreen(MenuScreen.Lobby);

        // Start flushing click queue to allow play trigger
        StartCoroutine(ClearTransitionFlag());
    }

    /// <summary>
    /// Single source of truth for which full-screen menu panel is showing
    /// (Lobby / Worlds / Heroes). Replaces the old scattered SetActive() calls
    /// and boolean flags so Home/back navigation works uniformly from any screen.
    /// </summary>
    public void SetMenuScreen(MenuScreen screen)
    {
        CurrentScreen = screen;
        bool onLobby = screen == MenuScreen.Lobby;

        if (lobbyPanel != null) lobbyPanel.SetActive(onLobby);
        if (themeSelectorPanel != null) themeSelectorPanel.SetActive(screen == MenuScreen.Worlds);
        if (heroesPanel != null) heroesPanel.SetActive(screen == MenuScreen.Heroes);
        if (shopPanel != null) shopPanel.SetActive(screen == MenuScreen.Shop);
        if (questsPanel != null) questsPanel.SetActive(screen == MenuScreen.Quests);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(screen == MenuScreen.Leaderboard);
        if (screenBackdrop != null) screenBackdrop.SetActive(!onLobby);

        // Bird preview only shows on the Lobby screen.
        if (bird != null)
        {
            bird.SetActive(onLobby);
            if (onLobby)
            {
                bird.transform.localScale = new Vector3(2.0f, 2.0f, 1f); // Large preview scale
                BirdController birdController = bird.GetComponent<BirdController>();
                if (birdController != null) birdController.ResetBird();
            }
        }

        // Center nav button doubles as Play (Lobby) / Home (any other screen).
        if (playIconImage != null)
        {
            Sprite iconSprite = onLobby ? playSprite : homeSprite;
            if (iconSprite != null) playIconImage.sprite = iconSprite;
        }

        if (screen == MenuScreen.Heroes) RefreshHeroesPanel();
        if (screen == MenuScreen.Worlds)
        {
            ThemeSelectorUI selector = themeSelectorPanel != null ? themeSelectorPanel.GetComponent<ThemeSelectorUI>() : null;
            if (selector != null) selector.UpdateSelectionUI();
        }
        if (screen == MenuScreen.Leaderboard)
        {
            LeaderboardUI board = leaderboardPanel != null ? leaderboardPanel.GetComponent<LeaderboardUI>() : null;
            if (board != null) board.Refresh();
        }
        if (screen == MenuScreen.Quests)
        {
            PowerupUpgradeUI shop = questsPanel != null ? questsPanel.GetComponent<PowerupUpgradeUI>() : null;
            if (shop != null) shop.Refresh();
        }
        if (screen == MenuScreen.Shop)
        {
            StarterPowerShopUI starterShop = shopPanel != null ? shopPanel.GetComponent<StarterPowerShopUI>() : null;
            if (starterShop != null) starterShop.Refresh();
        }

        UpdateNavVisuals(screen);

        GameObject activePanel;
        switch (screen)
        {
            case MenuScreen.Worlds: activePanel = themeSelectorPanel; break;
            case MenuScreen.Heroes: activePanel = heroesPanel; break;
            case MenuScreen.Shop: activePanel = shopPanel; break;
            case MenuScreen.Quests: activePanel = questsPanel; break;
            case MenuScreen.Leaderboard: activePanel = leaderboardPanel; break;
            default: activePanel = lobbyPanel; break;
        }
        if (screenTransitionCoroutine != null) StopCoroutine(screenTransitionCoroutine);
        if (activePanel != null) screenTransitionCoroutine = StartCoroutine(AnimateScreenIn(activePanel));
    }

    private Coroutine screenTransitionCoroutine;

    /// <summary>Fades/slides a menu screen in on entry. Same ease-out-sine pattern as AnimateGameOverBoard().</summary>
    private System.Collections.IEnumerator AnimateScreenIn(GameObject screenPanel)
    {
        RectTransform rt = screenPanel.GetComponent<RectTransform>();
        CanvasGroup cg = screenPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = screenPanel.AddComponent<CanvasGroup>();

        Vector2 originalPos = rt != null ? rt.anchoredPosition : Vector2.zero;
        Vector2 startPos = originalPos + new Vector2(0, -40f);

        cg.alpha = 0f;
        if (rt != null) rt.anchoredPosition = startPos;

        float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI * 0.5f); // ease out sine
            cg.alpha = t;
            if (rt != null) rt.anchoredPosition = Vector2.Lerp(startPos, originalPos, t);
            yield return null;
        }

        cg.alpha = 1f;
        if (rt != null) rt.anchoredPosition = originalPos;
    }

    // Hook this up to the Start button's OnClick() in the Inspector (tapping
    // anywhere on the Start screen also calls this, via Update() above).
    public void StartGame()
    {
        if (CurrentState != GameState.Start) return;

        PlayClickSound();

        if (CurrentScreen != MenuScreen.Lobby)
        {
            // Center button acts as HOME button on any non-Lobby screen (Worlds, Heroes, ...).
            SetMenuScreen(MenuScreen.Lobby);
            return;
        }

        // Lobby state: start the gameplay!
        CurrentState = GameState.Playing;

        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreText != null) scoreText.SetActive(true);

        if (bird != null)
        {
            bird.transform.localScale = new Vector3(1.2f, 1.2f, 1f); // Standard gameplay scale
            BirdController birdController = bird.GetComponent<BirdController>();
            if (birdController != null) birdController.EnableControl();
        }

        if (pipeSpawner != null) pipeSpawner.StartSpawning();
        if (itemSpawner != null) itemSpawner.StartSpawning();

        if (backgroundMusic != null && musicSource != null && !musicSource.isPlaying)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        if (ArmedStarter != StarterPower.None)
        {
            switch (ArmedStarter)
            {
                case StarterPower.Magnet: ActivateMagnet(); break;
                case StarterPower.Boost: ActivateBoostForDuration(GetPowerupDuration(BoostLevel)); break;
                case StarterPower.Double: ActivateDouble(); break;
                case StarterPower.Hammer: ActivateHammer(); break;
            }
            ArmedStarter = StarterPower.None;
            RefreshStarterWidgets();
        }
    }

    public void AddScore(int amount = 1)
    {
        if (CurrentState != GameState.Playing) return;
        score += amount;
        UpdateScoreUI();
        if (scoreSound != null && sfxSource != null) sfxSource.PlayOneShot(scoreSound);

        // Weather change dynamically during play
        WeatherController weather = FindObjectOfType<WeatherController>();
        if (weather != null) weather.SetWeatherByScore(score);
    }

    public float GetCurrentSpeedMultiplier()
    {
        float themeScale = 1f;
        if (ThemeManager.Instance != null && ThemeManager.Instance.GetCurrentTheme() != null && ThemeManager.Instance.GetCurrentTheme().themeName.ToLower() == "mario")
        {
            themeScale = 0.62f; // Slower speed specifically for Mario theme
        }

        if (CurrentState == GameState.Start) return 1f * themeScale;
        
        int speedTier = score / 10;
        float multiplier = 1f + speedTier * 0.08f; // 8% speed increase per 10 score points
        multiplier = Mathf.Min(multiplier, 1.48f);
        if (IsBoostActive) multiplier *= BoostSpeedMultiplier;
        return multiplier * themeScale;
    }

    // --- Coins & Power-ups ---

    public static float GetPowerupDuration(int level)
    {
        switch (level)
        {
            case 1: return 5f;
            case 2: return 7f;
            case 3: return 9f;
            case 4: return 11f;
            default: return 15f; // level 5 (max)
        }
    }

    public void AddCoins(int amount)
    {
        int actual = IsDoubleActive ? amount * 2 : amount;
        sessionCoins += actual;
        UpdateCoinsUI();
    }

    public void ActivateMagnet()
    {
        magnetTimer = GetPowerupDuration(MagnetLevel);
        UpdateTimerUI(magnetTimerText, magnetTimer);
    }

    public void ActivateBoost()
    {
        ActivateBoostForDuration(GetPowerupDuration(BoostLevel));
    }

    private void ActivateBoostForDuration(float duration)
    {
        boostTimer = duration;
        UpdateTimerUI(boostTimerText, boostTimer);
        SetBoostTrail(true);
    }

    public void ActivateDouble()
    {
        doubleTimer = GetPowerupDuration(DoubleLevel);
        UpdateTimerUI(doubleTimerText, doubleTimer);
    }

    /// <summary>Hammer is charge-based, not a timer — one charge destroys the next pipe touched.</summary>
    public void ActivateHammer()
    {
        hammerCharges++;
        UpdateHammerUI();
    }

    /// <summary>Called from BirdController when the bird touches a pipe — consumes one charge if available.</summary>
    public bool TryConsumeHammerCharge()
    {
        if (hammerCharges <= 0) return false;
        hammerCharges--;
        UpdateHammerUI();
        return true;
    }

    private void UpdateHammerUI()
    {
        if (hammerChargesWidget == null) return;
        hammerChargesWidget.SetActive(hammerCharges > 0);
        if (hammerCharges <= 0) return;

        Transform numberTrans = hammerChargesWidget.transform.Find("Number");
        if (numberTrans == null) return;
        UnityEngine.UI.Text numberText = numberTrans.GetComponent<UnityEngine.UI.Text>();
        if (numberText != null) numberText.text = hammerCharges.ToString();
    }

    private void SetBoostTrail(bool active)
    {
        // Skipping Die() in HandleImpact alone isn't real invulnerability — the bird's Dynamic
        // Rigidbody2D still physically collides with the pipe's solid BoxCollider2D and gets
        // shoved/flung off-screen. Ignoring the layer pair suppresses that contact entirely.
        int birdLayer = LayerMask.NameToLayer("Bird");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (birdLayer >= 0 && obstacleLayer >= 0) Physics2D.IgnoreLayerCollision(birdLayer, obstacleLayer, active);

        if (bird == null) return;
        BirdController birdController = bird.GetComponent<BirdController>();
        if (birdController != null) birdController.SetBoostTrailActive(active);
    }

    private void TickPowerupTimers()
    {
        if (magnetTimer > 0f)
        {
            magnetTimer -= Time.deltaTime;
            if (magnetTimer <= 0f) { magnetTimer = 0f; if (magnetTimerText != null) magnetTimerText.SetActive(false); }
            else UpdateTimerUI(magnetTimerText, magnetTimer);
        }

        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
            {
                boostTimer = 0f;
                if (boostTimerText != null) boostTimerText.SetActive(false);
                SetBoostTrail(false);
            }
            else UpdateTimerUI(boostTimerText, boostTimer);
        }

        if (doubleTimer > 0f)
        {
            doubleTimer -= Time.deltaTime;
            if (doubleTimer <= 0f) { doubleTimer = 0f; if (doubleTimerText != null) doubleTimerText.SetActive(false); }
            else UpdateTimerUI(doubleTimerText, doubleTimer);
        }
    }

    /// <summary>Updates the countdown number on an icon+number power-up indicator (see BuildTimerIndicator).</summary>
    private void UpdateTimerUI(GameObject go, float secondsLeft)
    {
        if (go == null) return;
        if (!go.activeSelf) go.SetActive(true);
        Transform numberTrans = go.transform.Find("Number");
        if (numberTrans == null) return;
        UnityEngine.UI.Text numberText = numberTrans.GetComponent<UnityEngine.UI.Text>();
        if (numberText != null) numberText.text = secondsLeft.ToString("F1");
    }

    /// <summary>Cost to go from the given level to level+1. Levels 1-5, so 4 upgrade steps.</summary>
    public int GetUpgradeCost(int currentLevel)
    {
        switch (currentLevel)
        {
            case 1: return 100;
            case 2: return 250;
            case 3: return 500;
            case 4: return 1000;
            default: return -1; // already max (level 5)
        }
    }

    public bool UpgradeMagnet()
    {
        int cost = GetUpgradeCost(MagnetLevel);
        if (cost < 0 || TotalCoins < cost) return false;
        TotalCoins -= cost;
        MagnetLevel++;
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        PlayerPrefs.SetInt(MagnetLevelKey, MagnetLevel);
        PlayerPrefs.Save();
        return true;
    }

    public bool UpgradeBoost()
    {
        int cost = GetUpgradeCost(BoostLevel);
        if (cost < 0 || TotalCoins < cost) return false;
        TotalCoins -= cost;
        BoostLevel++;
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        PlayerPrefs.SetInt(BoostLevelKey, BoostLevel);
        PlayerPrefs.Save();
        return true;
    }

    public bool UpgradeDouble()
    {
        int cost = GetUpgradeCost(DoubleLevel);
        if (cost < 0 || TotalCoins < cost) return false;
        TotalCoins -= cost;
        DoubleLevel++;
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        PlayerPrefs.SetInt(DoubleLevelKey, DoubleLevel);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Hooked up to each Shop "BUY" button — one purchase adds one charge of that power.</summary>
    public bool BuyStarterCharge(StarterPower type)
    {
        PlayClickSound();
        if (type == StarterPower.None || TotalCoins < StarterPurchaseCost) return false;

        TotalCoins -= StarterPurchaseCost;
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        AdjustStarterCount(type, +1);
        PlayerPrefs.Save();
        UpdateCoinsUI();
        RefreshStarterWidgets();
        return true;
    }

    /// <summary>Int overload so this can be wired via UnityEventTools.AddIntPersistentListener at scene-build time.</summary>
    public void BuyStarterCharge(int typeIndex)
    {
        BuyStarterCharge((StarterPower)typeIndex);
    }

    /// <summary>Hooked up to each Lobby icon — arms/un-arms a starter power for the next run, consuming/refunding a charge.</summary>
    public void ToggleArmedStarter(StarterPower type)
    {
        PlayClickSound();
        if (type == StarterPower.None) return;

        if (ArmedStarter == type)
        {
            AdjustStarterCount(type, +1);
            ArmedStarter = StarterPower.None;
        }
        else
        {
            if (ArmedStarter != StarterPower.None)
            {
                AdjustStarterCount(ArmedStarter, +1);
                ArmedStarter = StarterPower.None;
            }
            if (GetStarterCount(type) > 0)
            {
                AdjustStarterCount(type, -1);
                ArmedStarter = type;
            }
        }
        PlayerPrefs.Save();
        RefreshStarterWidgets();
    }

    /// <summary>Int overload so this can be wired via UnityEventTools.AddIntPersistentListener at scene-build time.</summary>
    public void ToggleArmedStarter(int typeIndex)
    {
        ToggleArmedStarter((StarterPower)typeIndex);
    }

    private int GetStarterCount(StarterPower type)
    {
        switch (type)
        {
            case StarterPower.Magnet: return StarterMagnetCount;
            case StarterPower.Boost: return StarterBoostCount;
            case StarterPower.Double: return StarterDoubleCount;
            case StarterPower.Hammer: return StarterHammerCount;
            default: return 0;
        }
    }

    private void AdjustStarterCount(StarterPower type, int delta)
    {
        switch (type)
        {
            case StarterPower.Magnet: StarterMagnetCount += delta; PlayerPrefs.SetInt(StarterMagnetCountKey, StarterMagnetCount); break;
            case StarterPower.Boost: StarterBoostCount += delta; PlayerPrefs.SetInt(StarterBoostCountKey, StarterBoostCount); break;
            case StarterPower.Double: StarterDoubleCount += delta; PlayerPrefs.SetInt(StarterDoubleCountKey, StarterDoubleCount); break;
            case StarterPower.Hammer: StarterHammerCount += delta; PlayerPrefs.SetInt(StarterHammerCountKey, StarterHammerCount); break;
        }
    }

    /// <summary>Updates the 3 Lobby icon+count widgets — call after any change to counts or the armed selection.</summary>
    public void RefreshStarterWidgets()
    {
        UpdateStarterWidget(starterMagnetWidget, StarterMagnetCount, ArmedStarter == StarterPower.Magnet);
        UpdateStarterWidget(starterBoostWidget, StarterBoostCount, ArmedStarter == StarterPower.Boost);
        UpdateStarterWidget(starterDoubleWidget, StarterDoubleCount, ArmedStarter == StarterPower.Double);
        UpdateStarterWidget(starterHammerWidget, StarterHammerCount, ArmedStarter == StarterPower.Hammer);
    }

    private void UpdateStarterWidget(GameObject widget, int count, bool armed)
    {
        if (widget == null) return;

        Transform countTrans = widget.transform.Find("Count");
        if (countTrans != null)
        {
            UnityEngine.UI.Text countText = countTrans.GetComponent<UnityEngine.UI.Text>();
            if (countText != null) countText.text = "x" + count;
        }

        UnityEngine.UI.Outline outline = widget.GetComponent<UnityEngine.UI.Outline>();
        if (outline != null)
        {
            outline.effectColor = armed ? new Color(0.95f, 0.72f, 0.15f) : new Color(0.35f, 0.35f, 0.4f);
            outline.effectDistance = armed ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
        }
        widget.transform.localScale = armed ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
    }

    private void UpdateCoinsUI()
    {
        SetText(coinsText, (TotalCoins + sessionCoins).ToString());
    }

    private Coroutine gameOverCoroutine;

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        if (pipeSpawner != null) pipeSpawner.StopSpawning();
        if (itemSpawner != null) itemSpawner.StopSpawning();
        if (musicSource != null) musicSource.Stop();

        // Trigger camera shake to add feedback on impact
        StartCoroutine(ShakeCamera(0.22f, 0.14f));

        // Hide top-right score counter when Game Over card displays
        if (scoreText != null) scoreText.SetActive(false);

        // Bank this run's coins into the persistent total.
        if (sessionCoins > 0)
        {
            TotalCoins += sessionCoins;
            PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
            PlayerPrefs.Save();
        }
        SetBoostTrail(false);

        // Update + persist the best score before showing the panel, so it's
        // always accurate the instant the run ends.
        bool isNewBest = score > bestScore;
        if (isNewBest)
        {
            bestScore = score;
            PlayerPrefs.SetInt(HighScoreKey, bestScore);
            PlayerPrefs.Save();

            if (LeaderboardManager.Instance != null) LeaderboardManager.Instance.SubmitScore(bestScore);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverCoroutine != null) StopCoroutine(gameOverCoroutine);
            gameOverCoroutine = StartCoroutine(AnimateGameOverBoard(isNewBest));
        }
    }

    private System.Collections.IEnumerator AnimateGameOverBoard(bool isNewBest)
    {
        Vector2 finalCardPos = new Vector2(0, 50); // layout center
        Vector2 startCardPos = new Vector2(0, -1200); // start off screen bottom
        if (resultCardTransform != null)
        {
            resultCardTransform.anchoredPosition = startCardPos;
        }

        if (medalImage != null)
        {
            if (medalPlaceholder != null)
            {
                medalImage.sprite = medalPlaceholder;
                medalImage.gameObject.SetActive(true);
                medalImage.transform.localScale = Vector3.one;
            }
            else
            {
                medalImage.gameObject.SetActive(false);
            }
        }
        if (newBestBadge != null) newBestBadge.SetActive(false);

        SetText(gameOverScoreText, "0");
        SetText(gameOverBestText, bestScore.ToString());
        SetText(gameOverCoinsText, sessionCoins.ToString());

        // Slide card up
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // ease out sine
            if (resultCardTransform != null)
            {
                resultCardTransform.anchoredPosition = Vector2.Lerp(startCardPos, finalCardPos, t);
            }
            yield return null;
        }
        if (resultCardTransform != null) resultCardTransform.anchoredPosition = finalCardPos;

        // Count up score
        if (score > 0)
        {
            float countDuration = Mathf.Min(0.8f, score * 0.05f);
            float countElapsed = 0f;
            while (countElapsed < countDuration)
            {
                countElapsed += Time.unscaledDeltaTime;
                int currentVal = Mathf.RoundToInt(Mathf.Lerp(0f, score, countElapsed / countDuration));
                SetText(gameOverScoreText, currentVal.ToString());
                yield return null;
            }
        }
        SetText(gameOverScoreText, score.ToString());

        // Pop "NEW" badge
        if (isNewBest && newBestBadge != null)
        {
            newBestBadge.SetActive(true);
            newBestBadge.transform.localScale = Vector3.zero;
            float popElapsed = 0f;
            while (popElapsed < 0.2f)
            {
                popElapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(0f, 1f, popElapsed / 0.2f);
                newBestBadge.transform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            newBestBadge.transform.localScale = Vector3.one;
        }

        // Reveal Medal
        Sprite medalSprite = null;
        if (score >= 40) medalSprite = platinumMedal;
        else if (score >= 30) medalSprite = goldMedal;
        else if (score >= 20) medalSprite = silverMedal;
        else if (score >= 10) medalSprite = bronzeMedal;
 
        if (medalSprite != null && medalImage != null)
        {
            medalImage.sprite = medalSprite;
            medalImage.gameObject.SetActive(true);
            medalImage.transform.localScale = Vector3.zero;
            float popElapsed = 0f;
            while (popElapsed < 0.3f)
            {
                popElapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(0f, 1.2f, popElapsed / 0.3f);
                if (popElapsed > 0.2f)
                {
                    scale = Mathf.Lerp(1.2f, 1.0f, (popElapsed - 0.2f) / 0.1f);
                }
                medalImage.transform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            medalImage.transform.localScale = Vector3.one;
        }
    }

    private void SetText(GameObject go, string value)
    {
        if (go == null) return;
        UnityEngine.UI.Text text = go.GetComponent<UnityEngine.UI.Text>();
        if (text != null) text.text = value;
    }

    // Hooked up to the Game Over panel's "MENU" button — returns straight to the Start screen state.
    public void RestartGame()
    {
        PlayClickSound();
        justEnteredStartState = true;
        ShowStartState();
        StartCoroutine(ClearTransitionFlag());
    }

    private System.Collections.IEnumerator ClearTransitionFlag()
    {
        // Wait until the user has fully released the click/touch
        while (Input.GetMouseButton(0) || Input.touchCount > 0)
        {
            yield return null;
        }
        
        // Wait one extra frame for all EventSystem pointer click queues to flush
        yield return null;
        
        justEnteredStartState = false;
    }

    /// <summary>Hooked up to the Game Over panel's "RETRY" button — jumps straight back into play without a scene reload or an extra tap on Start.</summary>
    public void RetryGame()
    {
        ShowStartState();
        StartGame(); // StartGame() already plays the click sound — avoid double-triggering it here
    }

    /// <summary>Call from any UI Button's OnClick() for a consistent click sound.</summary>
    public void PlayClickSound()
    {
        if (buttonClickSound != null && sfxSource != null) sfxSource.PlayOneShot(buttonClickSound);
    }

    private void UpdateScoreUI()
    {
        if (scoreText == null) return;

        // Uses legacy UI.Text (comes with Unity by default, no extra package needed).
        // If you prefer TextMeshPro, swap the type below to TMPro.TextMeshProUGUI.
        UnityEngine.UI.Text text = scoreText.GetComponent<UnityEngine.UI.Text>();
        if (text != null)
        {
            text.text = score.ToString();
        }
    }

    private void ClearExistingPipes()
    {
        // Destroying only the "Pipe"-tagged children (the top/bottom pipe
        // bodies) left the parent PipePair object behind — including its caps
        // and PipeMover — as an orphaned, still-moving leftover. Finding every
        // PipeMover and destroying its root GameObject removes the whole
        // pipe pair (body + caps + score trigger) in one go.
        PipeMover[] pipes = FindObjectsOfType<PipeMover>();
        foreach (PipeMover pipe in pipes)
        {
            Destroy(pipe.gameObject);
        }
    }

    private System.Collections.IEnumerator ShakeCamera(float duration, float magnitude)
    {
        Transform camTrans = Camera.main != null ? Camera.main.transform : null;
        if (camTrans == null) yield break;

        Vector3 originalPos = camTrans.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            camTrans.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        camTrans.localPosition = originalPos;
    }

    // --- Bottom Navigation Tab Click Handlers ---

    public void OnShopClicked()
    {
        PlayClickSound();
        if (CurrentState != GameState.Start) return;

        SetMenuScreen(CurrentScreen == MenuScreen.Shop ? MenuScreen.Lobby : MenuScreen.Shop);
    }

    public void OnHeroesClicked()
    {
        PlayClickSound();
        if (CurrentState != GameState.Start) return;

        SetMenuScreen(CurrentScreen == MenuScreen.Heroes ? MenuScreen.Lobby : MenuScreen.Heroes);
    }

    private static readonly string[] HeroWorldNames = { "Classic", "Space", "Football", "Dragon", "Fish", "Bee", "Ninja", "Mario", "Mars" };

    /// <summary>
    /// Hooked up to every card in the all-worlds Heroes roster. globalIndex is (worldIndex*3 + skinIndex),
    /// covering all 9 worlds x 3 skins = 27 heroes. Fully independent of the selected World —
    /// picking a hero here never changes which world/environment is active, and vice versa.
    /// </summary>
    private const string SelectedHeroKey = "FlappyBird_SelectedHeroGlobal";

    public void SelectHeroGlobal(int globalIndex)
    {
        PlayClickSound();

        PlayerPrefs.SetInt(SelectedHeroKey, globalIndex);
        PlayerPrefs.Save();

        if (ThemeApplier.Instance != null) ThemeApplier.Instance.ApplyHeroSprite(globalIndex);

        RefreshHeroesPanel();
    }

    public void RefreshHeroesPanel()
    {
        if (heroesPanel == null) return;

        int currentGlobal = PlayerPrefs.GetInt(SelectedHeroKey, 0);
        int currentWorld = currentGlobal / 3;
        int currentSkin = currentGlobal % 3;

        // Header Text update
        Transform headerTextTrans = heroesPanel.transform.Find("HeaderBar/Text");
        if (headerTextTrans != null)
        {
            UnityEngine.UI.Text headerText = headerTextTrans.GetComponent<UnityEngine.UI.Text>();
            if (headerText != null) headerText.text = "HEROES (" + (currentGlobal + 1) + "/27)";
        }

        // Update the selected-state outline/scale/checkmark on all 27 cards (names/sprites are
        // baked in at build time since the roster never changes).
        for (int t = 0; t < HeroWorldNames.Length; t++)
        {
            for (int s = 0; s < 3; s++)
            {
                Transform cardTrans = heroesPanel.transform.Find("ScrollView/Viewport/Content/" + HeroWorldNames[t] + "Card" + s);
                if (cardTrans == null) continue;

                bool selected = t == currentWorld && s == currentSkin;

                Transform checkTrans = cardTrans.Find("Checkmark");
                if (checkTrans != null) checkTrans.gameObject.SetActive(selected);

                UnityEngine.UI.Outline cardOutline = cardTrans.GetComponent<UnityEngine.UI.Outline>();
                if (cardOutline != null)
                {
                    cardOutline.effectColor = selected ? new Color(0.95f, 0.72f, 0.15f) : new Color(0.35f, 0.35f, 0.4f);
                    cardOutline.effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
                }
                cardTrans.localScale = selected ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
            }
        }
    }

    public void OnMissionsClicked()
    {
        PlayClickSound();
        if (CurrentState != GameState.Start) return;

        SetMenuScreen(CurrentScreen == MenuScreen.Quests ? MenuScreen.Lobby : MenuScreen.Quests);
    }

    public void OnThemesClicked()
    {
        PlayClickSound();
        if (CurrentState != GameState.Start) return;

        SetMenuScreen(CurrentScreen == MenuScreen.Worlds ? MenuScreen.Lobby : MenuScreen.Worlds);
    }

    public void OnCenterNavClicked()
    {
        PlayClickSound();
        if (CurrentState != GameState.Start) return;

        if (CurrentScreen != MenuScreen.Lobby)
        {
            SetMenuScreen(MenuScreen.Lobby);
        }
        else
        {
            StartGame();
        }
    }

    public void OnLeaderboardClicked()
    {
        PlayClickSound();
        if (CurrentState != GameState.Start) return;

        SetMenuScreen(CurrentScreen == MenuScreen.Leaderboard ? MenuScreen.Lobby : MenuScreen.Leaderboard);
    }

    /// <summary>
    /// Styles all 5 bottom-bar slots so exactly one reads as active, matching CurrentScreen.
    /// Replaces the old SetFocusedButton, which excluded the center button and left it
    /// permanently looking "active" regardless of the real screen state.
    /// </summary>
    private RectTransform navIndicator;
    private Coroutine navIndicatorCoroutine;

    /// <summary>
    /// Styles all 5 bottom-bar slots so exactly one reads as active, matching CurrentScreen.
    /// Replaces the old SetFocusedButton, which excluded the center button and left it
    /// permanently looking "active" regardless of the real screen state.
    /// </summary>
    private void UpdateNavVisuals(MenuScreen screen)
    {
        SetNavButtonActive(shopButton, screen == MenuScreen.Shop, false);
        SetNavButtonActive(heroesButton, screen == MenuScreen.Heroes, false);
        SetNavButtonActive(missionsButton, screen == MenuScreen.Quests, false);
        SetNavButtonActive(themesButton, screen == MenuScreen.Worlds, false);
        SetNavButtonActive(centerButton, screen == MenuScreen.Lobby, true);

        // Slide the active indicator behind the active button
        UnityEngine.UI.Button targetBtn = null;
        bool isCenter = false;
        switch (screen)
        {
            case MenuScreen.Shop: targetBtn = shopButton; break;
            case MenuScreen.Heroes: targetBtn = heroesButton; break;
            case MenuScreen.Quests: targetBtn = missionsButton; break;
            case MenuScreen.Worlds: targetBtn = themesButton; break;
            case MenuScreen.Lobby: targetBtn = centerButton; isCenter = true; break;
        }

        if (targetBtn != null)
        {
            if (navIndicatorCoroutine != null) StopCoroutine(navIndicatorCoroutine);

            if (navIndicator == null && shopButton != null && shopButton.transform.parent != null)
            {
                Transform t = shopButton.transform.parent.Find("ActiveIndicator");
                if (t != null) navIndicator = t.GetComponent<RectTransform>();
            }

            if (navIndicator != null)
            {
                // Soft translucent capsule highlight behind active icon
                UnityEngine.UI.Image indImg = navIndicator.GetComponent<UnityEngine.UI.Image>();
                if (indImg != null)
                {
                    indImg.color = new Color(1f, 1f, 1f, 0.16f);
                }

                RectTransform targetRt = targetBtn.GetComponent<RectTransform>();
                Vector2 targetPos = targetRt.anchoredPosition;
                Vector2 targetSize = new Vector2(100, 90);

                if (navIndicator.anchoredPosition == Vector2.zero) // Snap on first layout
                {
                    navIndicator.anchoredPosition = targetPos;
                    navIndicator.sizeDelta = targetSize;
                }
                else
                {
                    navIndicatorCoroutine = StartCoroutine(AnimateNavIndicator(targetPos, targetSize));
                }
            }
        }
    }

    private System.Collections.IEnumerator AnimateNavIndicator(Vector2 targetPos, Vector2 targetSize)
    {
        float t = 0f;
        float duration = 0.22f;
        Vector2 startPos = navIndicator.anchoredPosition;
        Vector2 startSize = navIndicator.sizeDelta;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Sin((t / duration) * Mathf.PI * 0.5f); // ease-out-sine
            navIndicator.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);
            navIndicator.sizeDelta = Vector2.Lerp(startSize, targetSize, progress);
            yield return null;
        }

        navIndicator.anchoredPosition = targetPos;
        navIndicator.sizeDelta = targetSize;
    }

    private void SetNavButtonActive(UnityEngine.UI.Button btn, bool active, bool isCenter)
    {
        if (btn == null) return;

        // Find MenuThemeConfig for our custom neon theme variables
        MenuThemeConfig themeConfig = FindFirstObjectByType<MenuThemeConfig>();

        Color activeColor = Color.white;
        if (themeConfig != null)
        {
            activeColor = themeConfig.playButtonColor;
        }

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt != null)
        {
            // Keep button size uniform and flat at Y: 0 (Liquid Glass style)
            rt.sizeDelta = new Vector2(80, 80);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
        }

        // All navigation buttons have transparent backgrounds at all times.
        // The ActiveIndicator (frosted glass capsule) provides the active highlight background behind them.
        btn.image.color = Color.clear;
        btn.image.sprite = null;
        
        Outline outline = btn.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        // 1. Style the child icon (Play, Home, Shop, Heroes, Quests, Worlds)
        Transform iconTrans = btn.transform.Find("Icon");
        if (iconTrans != null)
        {
            RectTransform iconRt = iconTrans.GetComponent<RectTransform>();
            if (iconRt != null)
            {
                iconRt.sizeDelta = active ? new Vector2(96, 96) : new Vector2(58, 58);
            }

            UnityEngine.UI.Image img = iconTrans.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                // Active colorful icon is fully colored (white tint), inactive is faded/desaturated (grey tint)
                img.color = active ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.75f);
            }
        }

        // 2. Style the label text
        Transform labelTrans = btn.transform.Find("Label");
        if (labelTrans != null)
        {
            UnityEngine.UI.Text txt = labelTrans.GetComponent<UnityEngine.UI.Text>();
            if (txt != null)
            {
                txt.color = active ? activeColor : new Color(0.7f, 0.7f, 0.7f, 0.8f);
                txt.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;

                RectTransform labelRt = labelTrans.GetComponent<RectTransform>();
                if (labelRt != null)
                {
                    // Fixed flat Y position below the icon
                    labelRt.anchoredPosition = new Vector2(0, -38f);
                }
            }
        }
    }

    // --- Canvas Toast Notification System ---

    public void ShowToast(string message)
    {
        // Suppress on-screen logs/toasts to maintain clean aesthetic
        return;
    }

    private Coroutine toastCoroutine;

    private System.Collections.IEnumerator AnimateToast()
    {
        CanvasGroup cg = toastPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, t / 0.2f);
                yield return null;
            }
            cg.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(1.8f);

        if (cg != null)
        {
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / 0.2f);
                yield return null;
            }
            cg.alpha = 0f;
        }
        toastPanel.SetActive(false);
    }
}
