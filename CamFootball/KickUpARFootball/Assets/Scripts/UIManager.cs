using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows/hides the three UI panels (Start, Gameplay, Game Over) and updates
/// all on-screen text. Button OnClick() events in the Inspector should call
/// the public methods below.
///
/// Uses Unity's built-in UI Text component (no TextMeshPro required) to keep
/// setup simple. If you prefer TextMeshPro, swap "Text" for "TMPro.TMP_Text"
/// and add "using TMPro;" at the top.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject gameplayPanel;
    public GameObject gameOverPanel;

    [Header("Start Panel")]
    public Text titleText;
    public Text instructionText;
    public Button infiniteModeButton;
    public Button basketModeButton;

    [Header("Gameplay Panel")]
    public Text scoreText;
    public Text bestScoreText;
    public Text timerText;

    [Header("Game Over Panel")]
    public Text gameOverText;
    public Text finalScoreText;
    public Text bestScoreGameOverText;
    public Button restartButton;
    public Button mainMenuButton;

    private void Start()
    {
        // Wire up buttons in code as a convenience; you can also do this
        // manually in the Inspector via each Button's OnClick() list.
        if (infiniteModeButton != null) infiniteModeButton.onClick.AddListener(OnClickStartInfinite);
        if (basketModeButton != null) basketModeButton.onClick.AddListener(OnClickStartBasket);
        if (restartButton != null) restartButton.onClick.AddListener(OnClickRestart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnClickMainMenu);

        if (timerText != null)
        {
            timerText.gameObject.SetActive(false); // hidden until Basket Mode starts
        }
    }

    // ---------- Panel visibility ----------

    public void ShowStartScreen()
    {
        SetPanel(startPanel, true);
        SetPanel(gameplayPanel, false);
        SetPanel(gameOverPanel, false);
    }

    public void ShowGameplayUI()
    {
        SetPanel(startPanel, false);
        SetPanel(gameplayPanel, true);
        SetPanel(gameOverPanel, false);

        bool isBasketMode = GameManager.Instance != null &&
                             GameManager.Instance.CurrentMode == GameManager.GameMode.Basket;

        if (timerText != null) timerText.gameObject.SetActive(isBasketMode);
        if (bestScoreText != null) bestScoreText.gameObject.SetActive(!isBasketMode);
    }

    public void ShowGameOverScreen(int finalScore, int bestScore)
    {
        SetPanel(startPanel, false);
        SetPanel(gameplayPanel, false);
        SetPanel(gameOverPanel, true);

        if (finalScoreText != null) finalScoreText.text = $"Score: {finalScore}";
        if (bestScoreGameOverText != null) bestScoreGameOverText.text = $"Best: {bestScore}";
    }

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    // ---------- Text updates ----------

    public void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }

    public void UpdateBestScore(int bestScore)
    {
        if (bestScoreText != null) bestScoreText.text = $"Best: {bestScore}";
    }

    public void UpdateTimer(float secondsRemaining)
    {
        if (timerText != null) timerText.text = $"Time: {Mathf.CeilToInt(secondsRemaining)}";
    }

    // ---------- Button handlers ----------

    public void OnClickStartInfinite()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.StartGame(GameManager.GameMode.Infinite);
    }

    public void OnClickStartBasket()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.StartGame(GameManager.GameMode.Basket);
    }

    public void OnClickRestart()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.RestartGame();
    }

    public void OnClickMainMenu()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.GoToMainMenu();
    }
}
