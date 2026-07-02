using UnityEngine;

/// <summary>
/// Central game state and score manager.
/// Other scripts call into this instead of talking to each other directly.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, GameOver }
    public enum GameMode { Infinite, Basket }

    [Header("Current State (read-only at runtime)")]
    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public GameMode CurrentMode { get; private set; } = GameMode.Infinite;

    [Header("Score")]
    public int CurrentScore { get; private set; } = 0;
    public int BestScoreInfinite { get; private set; } = 0;
    public int BestScoreBasket { get; private set; } = 0;

    [Header("Scene References")]
    public UIManager uiManager;
    public BallController ballController;
    public BasketGoalController basketGoalController;

    private const string BEST_SCORE_INFINITE_KEY = "BestScore_Infinite";
    private const string BEST_SCORE_BASKET_KEY = "BestScore_Basket";

    private void Awake()
    {
        // Simple singleton so any script can reach GameManager.Instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadBestScores();
    }

    private void Start()
    {
        // Begin on the main menu
        GoToMainMenu();
    }

    // ---------- Game flow ----------

    public void StartGame(GameMode mode)
    {
        CurrentMode = mode;
        CurrentScore = 0;
        CurrentState = GameState.Playing;

        if (ballController != null)
        {
            ballController.ResetBall();
        }

        if (mode == GameMode.Basket && basketGoalController != null)
        {
            basketGoalController.ResetTimer();
            basketGoalController.gameObject.SetActive(true);
        }
        else if (basketGoalController != null)
        {
            basketGoalController.gameObject.SetActive(false);
        }

        if (uiManager != null)
        {
            uiManager.ShowGameplayUI();
            uiManager.UpdateScore(CurrentScore);
            uiManager.UpdateBestScore(GetBestScoreForCurrentMode());
        }

        AudioManager.Instance?.PlayButtonClick();
    }

    public void RestartGame()
    {
        StartGame(CurrentMode);
    }

    public void GoToMainMenu()
    {
        CurrentState = GameState.MainMenu;
        CurrentScore = 0;

        if (uiManager != null)
        {
            uiManager.ShowStartScreen();
        }

        AudioManager.Instance?.PlayButtonClick();
    }

    public void AddScore(int amount = 1)
    {
        if (CurrentState != GameState.Playing) return;

        CurrentScore += amount;

        if (uiManager != null)
        {
            uiManager.UpdateScore(CurrentScore);
        }

        AudioManager.Instance?.PlayScoreSound();
        SaveBestScoreIfNeeded();
    }

    public void GameOver()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.GameOver;
        SaveBestScoreIfNeeded();

        if (uiManager != null)
        {
            uiManager.ShowGameOverScreen(CurrentScore, GetBestScoreForCurrentMode());
        }

        AudioManager.Instance?.PlayGameOverSound();
    }

    // ---------- Best score persistence ----------

    private int GetBestScoreForCurrentMode()
    {
        return CurrentMode == GameMode.Infinite ? BestScoreInfinite : BestScoreBasket;
    }

    private void SaveBestScoreIfNeeded()
    {
        if (CurrentMode == GameMode.Infinite)
        {
            if (CurrentScore > BestScoreInfinite)
            {
                BestScoreInfinite = CurrentScore;
                PlayerPrefs.SetInt(BEST_SCORE_INFINITE_KEY, BestScoreInfinite);
                PlayerPrefs.Save();
            }
        }
        else
        {
            if (CurrentScore > BestScoreBasket)
            {
                BestScoreBasket = CurrentScore;
                PlayerPrefs.SetInt(BEST_SCORE_BASKET_KEY, BestScoreBasket);
                PlayerPrefs.Save();
            }
        }
    }

    private void LoadBestScores()
    {
        BestScoreInfinite = PlayerPrefs.GetInt(BEST_SCORE_INFINITE_KEY, 0);
        BestScoreBasket = PlayerPrefs.GetInt(BEST_SCORE_BASKET_KEY, 0);
    }
}
