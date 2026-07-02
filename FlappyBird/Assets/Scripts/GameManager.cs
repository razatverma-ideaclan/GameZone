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
        // Any tap/click/spacebar while on the Start screen begins the game.
        if (CurrentState == GameState.Start)
        {
            // Prevent accidental tap leaks immediately after clicking Menu to return
            if (Time.time - startStateTime < 0.25f) return;

            // Ignore clicks/touches that are over UI elements (like bird selector arrows or the start button)
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                if (Input.touchCount > 0)
                {
                    if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                        return;
                }
                else
                {
                    if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                        return;
                }
            }

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || TouchStarted())
            {
                StartGame();
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
        Time.timeScale = 1f;
        score = 0;
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

        // Freeze bird and pipes until the player starts.
        if (bird != null)
        {
            bird.SetActive(true);
            BirdController birdController = bird.GetComponent<BirdController>();
            if (birdController != null) birdController.ResetBird();
        }

        if (pipeSpawner != null) pipeSpawner.StopSpawning();

        // Remove any pipes left over from a previous run.
        ClearExistingPipes();
    }

    // Hook this up to the Start button's OnClick() in the Inspector (tapping
    // anywhere on the Start screen also calls this, via Update() above).
    public void StartGame()
    {
        if (CurrentState != GameState.Start) return;

        PlayClickSound();

        CurrentState = GameState.Playing;
        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreText != null) scoreText.SetActive(true);

        if (bird != null)
        {
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
        ShowStartState();
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
}
