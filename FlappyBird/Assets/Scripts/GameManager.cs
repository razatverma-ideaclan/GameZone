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
    public UnityEngine.UI.Image playIconImage;
    public Sprite playSprite;
    public Sprite homeSprite;

    [Header("Navigation Buttons")]
    public UnityEngine.UI.Button shopButton;
    public UnityEngine.UI.Button heroesButton;
    public UnityEngine.UI.Button missionsButton;
    public UnityEngine.UI.Button themesButton;

    private bool isViewingHeroes = false;

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

                bool onThemePanel = false;
                if (themeSelectorPanel != null && themeSelectorPanel.activeSelf)
                {
                    RectTransform themeRt = themeSelectorPanel.GetComponent<RectTransform>();
                    if (themeRt != null)
                    {
                        onThemePanel = RectTransformUtility.RectangleContainsScreenPoint(themeRt, clickPos, null);
                    }
                }

                if (!onBottomBar && !onThemePanel && !isViewingHeroes && !justEnteredStartState)
                {
                    if (themeSelectorPanel != null && themeSelectorPanel.activeSelf)
                    {
                        themeSelectorPanel.SetActive(false);
                        SetFocusedButton(null);
                    }
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
        isViewingHeroes = false;
        justEnteredStartState = true;
        startStateTime = Time.time; // Record start screen entry time
        UpdateScoreUI();

        if (startPanel != null) startPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (heroesPanel != null) heroesPanel.SetActive(false);
        if (themeSelectorPanel != null) themeSelectorPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreText != null) scoreText.SetActive(false);

        // Reset play button icon to Play arrow
        if (playIconImage != null && playSprite != null) playIconImage.sprite = playSprite;

        // Update high score on start screen
        bestScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        SetText(startHighScoreText, "BEST: " + bestScore.ToString());

        // Weather reset on start
        WeatherController weather = FindObjectOfType<WeatherController>();
        if (weather != null) weather.ResetWeather();

        // Freeze bird and pipes until the player starts.
        if (bird != null)
        {
            bird.SetActive(true);
            bird.transform.localScale = new Vector3(2.0f, 2.0f, 1f); // Large preview scale
            BirdController birdController = bird.GetComponent<BirdController>();
            if (birdController != null) birdController.ResetBird();
        }

        if (pipeSpawner != null) pipeSpawner.StopSpawning();

        // Remove any pipes left over from a previous run.
        ClearExistingPipes();

        // Start flushing click queue to allow play trigger
        StartCoroutine(ClearTransitionFlag());
        SetFocusedButton(null);
    }

    // Hook this up to the Start button's OnClick() in the Inspector (tapping
    // anywhere on the Start screen also calls this, via Update() above).
    public void StartGame()
    {
        if (CurrentState != GameState.Start) return;

        PlayClickSound();

        if (isViewingHeroes)
        {
            // Center button acts as HOME button! Return to lobby state
            isViewingHeroes = false;
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (heroesPanel != null) heroesPanel.SetActive(false);
            if (bird != null)
            {
                bird.SetActive(true);
                bird.transform.localScale = new Vector3(2.0f, 2.0f, 1f);
                BirdController bc = bird.GetComponent<BirdController>();
                if (bc != null) bc.UpdateSkin(); // Refresh visual preview
            }
            if (playIconImage != null && playSprite != null) playIconImage.sprite = playSprite;
            SetFocusedButton(null);
            ShowToast("RETURNED TO LOBBY");
            return;
        }

        // Lobby state: start the gameplay!
        CurrentState = GameState.Playing;
        SetFocusedButton(null);
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
        SetFocusedButton(shopButton);
        ShowToast("SHOP COMING SOON!");
    }

    public void OnHeroesClicked()
    {
        PlayClickSound();
        if (CurrentState != GameState.Start) return;

        isViewingHeroes = true;
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (heroesPanel != null) heroesPanel.SetActive(true);
        if (themeSelectorPanel != null) themeSelectorPanel.SetActive(false); // Hide world select panel
        if (bird != null) bird.SetActive(false); // Hide bird under character grid screen
        
        // Swap play button icon to Home
        if (playIconImage != null && homeSprite != null) playIconImage.sprite = homeSprite;

        SetFocusedButton(heroesButton);
        RefreshHeroesPanel();
        ShowToast("OPENED HERO SELECTION");
    }

    public void SelectHero(int skinIndex)
    {
        PlayClickSound();
        if (bird != null)
        {
            BirdController bc = bird.GetComponent<BirdController>();
            if (bc != null)
            {
                bc.SetSkin(skinIndex);
                ShowToast("HERO SELECTED!");
            }
        }
        RefreshHeroesPanel();
    }

    public void RefreshHeroesPanel()
    {
        if (heroesPanel == null) return;

        ThemeData currentTheme = ThemeManager.Instance != null ? ThemeManager.Instance.GetCurrentTheme() : null;
        string themeNameLower = currentTheme != null ? currentTheme.themeName.ToLower() : "classic";

        string[] skinNames = new string[3];
        Sprite[] skinSprites = new Sprite[3];

        if (themeNameLower == "classic")
        {
            skinNames = new string[] { "YELLOW HERO", "BLUE HERO", "RED HERO" };
            if (bird != null)
            {
                BirdController bc = bird.GetComponent<BirdController>();
                if (bc != null && bc.skins != null && bc.skins.Length >= 3)
                {
                    skinSprites[0] = bc.skins[0].flapSprites[1];
                    skinSprites[1] = bc.skins[1].flapSprites[1];
                    skinSprites[2] = bc.skins[2].flapSprites[1];
                }
            }
        }
        else
        {
            if (themeNameLower == "space") skinNames = new string[] { "ROCKET SPEEDER", "COSMIC UFO", "COMM SATELLITE" };
            else if (themeNameLower == "football") skinNames = new string[] { "SOCCER BALL", "BASKETBALL", "TENNIS BALL" };
            else if (themeNameLower == "dragon") skinNames = new string[] { "RED DRAKE", "EMERALD DRAGON", "GOLD WYVERN" };
            else if (themeNameLower == "fish") skinNames = new string[] { "GOLDFISH", "BULL SHARK", "PINK JELLYFISH" };
            else if (themeNameLower == "bee") skinNames = new string[] { "HONEY BEE", "LADYBUG", "BUTTERFLY" };
            else if (themeNameLower == "ninja") skinNames = new string[] { "SHADOW NINJA", "CRIMSON NINJA", "SILVER SHINOBI" };
            else skinNames = new string[] { "HERO A", "HERO B", "HERO C" };

            if (currentTheme != null && currentTheme.playerSprites != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    int sprIdx = Mathf.Clamp(i, 0, currentTheme.playerSprites.Length - 1);
                    skinSprites[i] = currentTheme.playerSprites[sprIdx];
                }
            }
        }

        // Header Text update
        Transform headerTextTrans = heroesPanel.transform.Find("HeaderBar/Text");
        if (headerTextTrans != null)
        {
            UnityEngine.UI.Text headerText = headerTextTrans.GetComponent<UnityEngine.UI.Text>();
            if (headerText != null)
            {
                string nameDisplay = currentTheme != null ? currentTheme.themeName.ToUpper() : "CLASSIC";
                int currentSel = bird != null ? bird.GetComponent<BirdController>().currentSkinIndex : 0;
                headerText.text = nameDisplay + " HEROES (" + (currentSel + 1) + "/3)";
            }
        }

        // Apply skin name, preview image, and checkmark indicator to each card
        for (int i = 0; i < 3; i++)
        {
            Transform cardTrans = heroesPanel.transform.Find("Grid/Card" + i);
            if (cardTrans == null) continue;

            // Name
            Transform nameTrans = cardTrans.Find("NameText");
            if (nameTrans != null)
            {
                UnityEngine.UI.Text txt = nameTrans.GetComponent<UnityEngine.UI.Text>();
                if (txt != null) txt.text = skinNames[i];
            }

            // Preview Image
            Transform imgTrans = cardTrans.Find("PreviewImage");
            if (imgTrans != null)
            {
                UnityEngine.UI.Image img = imgTrans.GetComponent<UnityEngine.UI.Image>();
                if (img != null && skinSprites[i] != null)
                {
                    img.sprite = skinSprites[i];
                    img.color = Color.white;
                }
            }

            // Checkmark
            Transform checkTrans = cardTrans.Find("Checkmark");
            if (checkTrans != null)
            {
                int currentSel = bird != null ? bird.GetComponent<BirdController>().currentSkinIndex : 0;
                checkTrans.gameObject.SetActive(i == currentSel);
            }
        }
    }

    public void OnMissionsClicked()
    {
        PlayClickSound();
        SetFocusedButton(missionsButton);
        string[] missions = {
            "MISSION: MAKE 90 LOOPS (0/90)",
            "MISSION: FLY 300 METERS (12/300)",
            "MISSION: PASS 20 GATES (0/20)",
            "MISSION: SCORE 50 POINTS (0/50)"
        };
        ShowToast(missions[Random.Range(0, missions.Length)]);
    }

    public void OnThemesClicked()
    {
        PlayClickSound();
        if (themeSelectorPanel != null)
        {
            if (isViewingHeroes)
            {
                isViewingHeroes = false;
                if (heroesPanel != null) heroesPanel.SetActive(false);
                if (lobbyPanel != null) lobbyPanel.SetActive(true);
                if (bird != null)
                {
                    bird.SetActive(true);
                    bird.transform.localScale = new Vector3(2.0f, 2.0f, 1f);
                    BirdController bc = bird.GetComponent<BirdController>();
                    if (bc != null) bc.UpdateSkin();
                }
                if (playIconImage != null && playSprite != null) playIconImage.sprite = playSprite;
            }

            bool nextState = !themeSelectorPanel.activeSelf;
            themeSelectorPanel.SetActive(nextState);
            SetFocusedButton(nextState ? themesButton : null);
            ShowToast(nextState ? "OPENED WORLD SELECT" : "CLOSED WORLD SELECT");
        }
    }

    private void SetFocusedButton(UnityEngine.UI.Button focusedBtn)
    {
        UnityEngine.UI.Button[] navButtons = { shopButton, heroesButton, missionsButton, themesButton };
        foreach (var btn in navButtons)
        {
            if (btn == null) continue;
            if (btn == focusedBtn)
            {
                btn.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
                btn.image.color = Color.white;
            }
            else
            {
                btn.transform.localScale = Vector3.one;
                btn.image.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
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
