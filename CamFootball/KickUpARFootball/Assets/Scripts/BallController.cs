using UnityEngine;

/// <summary>
/// Controls the football's movement: gravity, kick force, swipe influence,
/// screen bounds, and falling-below-screen (game over) detection.
/// Requires a Rigidbody2D and a Collider2D on the same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How strong the default upward kick is")]
    public float kickForce = 12f;
    [Tooltip("How much a swipe's horizontal direction affects the ball")]
    public float horizontalSwipeForce = 4f;
    [Tooltip("Gravity scale applied to the ball's Rigidbody2D")]
    public float gravityScale = 1.5f;

    [Header("Bounds")]
    [Tooltip("How far past the left/right edge of the screen the ball can go before bouncing back")]
    public float sideBoundsPadding = 0.3f;
    [Tooltip("Strength of the bounce when the ball hits a side wall")]
    public float sideBounceStrength = 0.6f;
    [Tooltip("Y position (world space) below which the ball triggers Game Over")]
    public float gameOverBottomY = -6f;

    [Header("Start Position")]
    public Vector3 startPosition = new Vector3(0f, -2f, 0f);

    private Rigidbody2D rb;
    private Camera mainCamera;
    private bool isGameOverTriggered = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        ResetBall();
    }

    private void Update()
    {
        CheckSideBounds();
        CheckBottomBoundary();
    }

    /// <summary>
    /// Applies a kick to the ball. Called by KickInputManager (and, later,
    /// by a foot-detection input manager using the same signature).
    /// </summary>
    /// <param name="direction">Normalized horizontal direction (-1 to 1) from swipe/tap</param>
    /// <param name="forceMultiplier">Optional multiplier on the base kick force</param>
    public void Kick(Vector2 direction, float forceMultiplier = 1f)
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Reset vertical velocity so kicks feel consistent, keep some horizontal carry-over
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, 0f);

        Vector2 kick = new Vector2(direction.x * horizontalSwipeForce, kickForce * forceMultiplier);
        rb.AddForce(kick, ForceMode2D.Impulse);

        AudioManager.Instance?.PlayKickSound();

        if (GameManager.Instance != null && GameManager.Instance.CurrentMode == GameManager.GameMode.Infinite)
        {
            GameManager.Instance.AddScore(1);
        }
    }

    /// <summary>
    /// Resets the ball to its starting position and clears velocity.
    /// Used at game start and after scoring in Basket Mode.
    /// </summary>
    public void ResetBall()
    {
        isGameOverTriggered = false;
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void CheckSideBounds()
    {
        if (mainCamera == null) return;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

        if (viewportPos.x < -0.05f)
        {
            Vector3 edge = mainCamera.ViewportToWorldPoint(new Vector3(0f, viewportPos.y, transform.position.z - mainCamera.transform.position.z));
            transform.position = new Vector3(edge.x + sideBoundsPadding, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(Mathf.Abs(rb.linearVelocity.x) * sideBounceStrength, rb.linearVelocity.y);
        }
        else if (viewportPos.x > 1.05f)
        {
            Vector3 edge = mainCamera.ViewportToWorldPoint(new Vector3(1f, viewportPos.y, transform.position.z - mainCamera.transform.position.z));
            transform.position = new Vector3(edge.x - sideBoundsPadding, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(-Mathf.Abs(rb.linearVelocity.x) * sideBounceStrength, rb.linearVelocity.y);
        }
    }

    private void CheckBottomBoundary()
    {
        if (isGameOverTriggered) return;

        if (transform.position.y < gameOverBottomY)
        {
            isGameOverTriggered = true;

            if (GameManager.Instance != null && GameManager.Instance.CurrentMode == GameManager.GameMode.Infinite)
            {
                GameManager.Instance.GameOver();
            }
            else if (GameManager.Instance != null && GameManager.Instance.CurrentMode == GameManager.GameMode.Basket)
            {
                // In Basket Mode, missing the ball does not end the game immediately;
                // it just resets so the player can keep trying until the timer ends.
                ResetBall();
            }
        }
    }
}
