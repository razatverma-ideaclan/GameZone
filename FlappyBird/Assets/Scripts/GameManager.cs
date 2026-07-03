using UnityEngine;
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

    public enum MenuScreen { Lobby, Worlds, Heroes, Shop, Quests }
    public MenuScreen CurrentScreen { get; private set; } = MenuScreen.Lobby;

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject gameOverPanel;
    public GameObject scoreText; // assign the in-game score label (TextMeshProUGUI or Text)
    [Tooltip("Text on the Game Over panel showing this run's score.")]
    public GameObject gameOverScoreText;
    [Tooltip("Text on the Game Over panel showing the all-time best score.")]
    public GameObject gameOverBestText;
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

    private const string HighScoreKey = "FlappyBird_HighScore";
    private int bestScore = 0;

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
    }

    void Start()
    {
        ShowStartState();
    }

    void Update()
    {
        if (CurrentState == GameState.Start)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartGame();
                return;
            }

            if (Input.GetMouseButtonDown(0) || TouchStarted())
            {
                Vector2 clickPos = Input.mousePosition;
                if (Input.touchCount > 0) clickPos = Input.GetTouch(0).position;

                bool onBottomBar = false;
                if (shopButton != null && shopButton.transform.parent != null)
                {
                    RectTransform barRt = shopButton.transform.parent.GetComponent<RectTransform>();
                    if (barRt != null)
                    {
                        onBottomBar = RectTransformUtility.RectangleContainsScreenPoint(barRt, clickPos, null);
                    }
                }

                if (!onBottomBar && CurrentScreen == MenuScreen.Lobby && !justEnteredStartState)
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
        UpdateScoreUI();

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

        UpdateNavVisuals(screen);

        GameObject activePanel;
        switch (screen)
        {
            case MenuScreen.Worlds: activePanel = themeSelectorPanel; break;
            case MenuScreen.Heroes: activePanel = heroesPanel; break;
            case MenuScreen.Shop: activePanel = shopPanel; break;
            case MenuScreen.Quests: activePanel = questsPanel; break;
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

        if (backgroundMusic != null && musicSource != null && !musicSource.isPlaying)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.Play();
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
        if (CurrentState == GameState.Start) return 1f; // Normal speed on start menu
        
        int speedTier = score / 10;
        float multiplier = 1f + speedTier * 0.08f; // 8% speed increase per 10 score points
        return Mathf.Min(multiplier, 1.48f); // Capped at +48% speed for playability
    }

    private Coroutine gameOverCoroutine;

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        if (pipeSpawner != null) pipeSpawner.StopSpawning();
        if (musicSource != null) musicSource.Stop();

        // Trigger camera shake to add feedback on impact
        StartCoroutine(ShakeCamera(0.22f, 0.14f));

        // Hide top-right score counter when Game Over card displays
        if (scoreText != null) scoreText.SetActive(false);

        // Update + persist the best score before showing the panel, so it's
        // always accurate the instant the run ends.
        bool isNewBest = score > bestScore;
        if (isNewBest)
        {
            bestScore = score;
            PlayerPrefs.SetInt(HighScoreKey, bestScore);
            PlayerPrefs.Save();
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

    private static readonly string[] HeroWorldNames = { "Classic", "Space", "Football", "Dragon", "Fish", "Bee", "Ninja" };

    /// <summary>
    /// Hooked up to every card in the all-worlds Heroes roster. globalIndex is (worldIndex*3 + skinIndex),
    /// covering all 7 worlds x 3 skins = 21 heroes. Fully independent of the selected World —
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
            if (headerText != null) headerText.text = "HEROES (" + (currentGlobal + 1) + "/21)";
        }

        // Update the selected-state outline/scale/checkmark on all 21 cards (names/sprites are
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
    }

    private void SetNavButtonActive(UnityEngine.UI.Button btn, bool active, bool isCenter)
    {
        if (btn == null) return;
        float targetScale = active ? 1.15f : (isCenter ? 0.9f : 1f);
        btn.transform.localScale = new Vector3(targetScale, targetScale, 1f);

        if (isCenter)
        {
            // Keeps its yellow branding, but visibly fades when it's not the active tab
            // (Play, on Lobby) instead of always reading as highlighted.
            Color c = btn.image.color;
            btn.image.color = new Color(c.r, c.g, c.b, active ? 1f : 0.55f);
        }
        else
        {
            btn.image.color = active ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.9f);
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
