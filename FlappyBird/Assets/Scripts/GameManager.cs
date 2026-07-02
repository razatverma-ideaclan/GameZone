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
        UpdateScoreUI();

        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

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
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        if (pipeSpawner != null) pipeSpawner.StopSpawning();
        if (musicSource != null) musicSource.Stop();

        // Update + persist the best score before showing the panel, so it's
        // always accurate the instant the run ends.
        bool isNewBest = score > bestScore;
        if (isNewBest)
        {
            bestScore = score;
            PlayerPrefs.SetInt(HighScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        SetText(gameOverScoreText, score.ToString());
        SetText(gameOverBestText, bestScore.ToString());

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    private void SetText(GameObject go, string value)
    {
        if (go == null) return;
        UnityEngine.UI.Text text = go.GetComponent<UnityEngine.UI.Text>();
        if (text != null) text.text = value;
    }

    // Hooked up to the Game Over panel's "MENU" button — fully resets via a
    // scene reload and lands back on the Start screen.
    public void RestartGame()
    {
        PlayClickSound();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
}
