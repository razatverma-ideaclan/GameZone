using UnityEngine;

/// <summary>
/// Used only in Basket Mode. Detects when the ball enters the basket/goal
/// trigger area, adds score, resets the ball, and manages the optional timer.
/// Requires a Collider2D with "Is Trigger" checked on the same GameObject.
/// </summary>
public class BasketGoalController : MonoBehaviour
{
    [Header("References")]
    public BallController ballController;

    [Header("Ball Identification")]
    [Tooltip("The tag used on the Ball GameObject, e.g. 'Ball'")]
    public string ballTag = "Ball";

    [Header("Timer")]
    [Tooltip("Enable a countdown timer for Basket Mode")]
    public bool useTimer = true;
    public float timerDurationSeconds = 60f;

    private float timeRemaining;
    private bool timerRunning = false;

    private void OnEnable()
    {
        ResetTimer();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (GameManager.Instance.CurrentMode != GameManager.GameMode.Basket)
            return;

        if (useTimer && timerRunning)
        {
            timeRemaining -= Time.deltaTime;

            if (GameManager.Instance.uiManager != null)
            {
                GameManager.Instance.uiManager.UpdateTimer(Mathf.Max(0f, timeRemaining));
            }

            if (timeRemaining <= 0f)
            {
                timerRunning = false;
                GameManager.Instance.GameOver();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (GameManager.Instance.CurrentMode != GameManager.GameMode.Basket)
            return;

        if (!other.CompareTag(ballTag))
            return;

        GameManager.Instance.AddScore(1);

        if (ballController != null)
        {
            ballController.ResetBall();
        }
    }

    public void ResetTimer()
    {
        timeRemaining = timerDurationSeconds;
        timerRunning = useTimer;

        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.UpdateTimer(timeRemaining);
        }
    }
}
