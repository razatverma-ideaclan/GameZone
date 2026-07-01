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

    [Header("References")]
    public GameObject bird;
    public PipeSpawner pipeSpawner;

    private int score = 0;

    void Awake()
    {
        // Simple singleton so other scripts can call GameManager.Instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (bird != null)
        {
            BirdController birdController = bird.GetComponent<BirdController>();
            if (birdController != null) birdController.EnableControl();
        }

        if (pipeSpawner != null) pipeSpawner.StartSpawning();
    }

    public void AddScore(int amount = 1)
    {
        if (CurrentState != GameState.Playing) return;
        score += amount;
        UpdateScoreUI();
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        if (pipeSpawner != null) pipeSpawner.StopSpawning();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    // Hooked up to the Restart button's OnClick() in the Inspector.
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        GameObject[] pipes = GameObject.FindGameObjectsWithTag("Pipe");
        foreach (GameObject pipe in pipes)
        {
            Destroy(pipe);
        }
    }
}
