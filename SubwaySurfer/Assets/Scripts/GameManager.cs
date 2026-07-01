using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game state controller. Handles Start / Playing / GameOver states,
/// distance-based score tracking, and UI panel switching.
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
    public GameObject scoreText; // assign the in-game score label (Text or TMP)

    [Header("References")]
    public PlayerController player;
    public ObstacleSpawner obstacleSpawner;

    private int score = 0;

    void Awake()
    {
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
        if (CurrentState == GameState.Start)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || TouchStarted())
            {
                StartGame();
            }
        }
        else if (CurrentState == GameState.Playing)
        {
            score = Mathf.FloorToInt(player.DistanceTraveled);
            UpdateScoreUI();
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

        if (player != null) player.ResetPlayer();
        if (obstacleSpawner != null)
        {
            obstacleSpawner.StopSpawning();
            obstacleSpawner.ClearObstacles();
        }
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (player != null) player.EnableControl();
        if (obstacleSpawner != null) obstacleSpawner.StartSpawning();
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        if (obstacleSpawner != null) obstacleSpawner.StopSpawning();
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

        UnityEngine.UI.Text text = scoreText.GetComponent<UnityEngine.UI.Text>();
        if (text != null)
        {
            text.text = score.ToString();
        }
    }
}
