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
    [Tooltip("Maximum speed the ball can ever reach, so it can't rocket off screen")]
    public float maxSpeed = 14f;
    [Tooltip("How far past the top edge of the screen the ball can go before bouncing back down")]
    public float topBoundsPadding = 0.3f;
    [Tooltip("Strength of the bounce when the ball hits the top of the screen")]
    public float topBounceStrength = 0.6f;

    [Header("Start Position")]
    public Vector3 startPosition = new Vector3(0f, -2f, 0f);
    [Tooltip("Automatic upward bounce given to the ball right when it (re)spawns, so the player has time to react")]
    public float startBounceForce = 6f;
    [Tooltip("Seconds after spawning during which the ball cannot trigger Game Over, giving the player time to react")]
    public float startGracePeriod = 1.2f;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private bool isGameOverTriggered = false;
    private float graceTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        // Sit still on the Start Menu instead of immediately falling under gravity -
        // physics only turns on once GameManager.StartGame() calls ResetBall().
        PlaceIdle();
    }

    private void Update()
    {
        // Don't run gameplay physics/bounds checks until a game is actually in progress
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (graceTimer > 0f) graceTimer -= Time.deltaTime;

        ClampSpeed();
        CheckSideBounds();
        CheckTopBounds();
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
        ClampSpeed();

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
        rb.simulated = true; // turn physics back on for actual gameplay
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        graceTimer = startGracePeriod;

        // Give the ball an automatic little bounce upward so it doesn't just
        // sit there or immediately fall - gives the player a moment to react.
        rb.AddForce(new Vector2(0f, startBounceForce), ForceMode2D.Impulse);
    }

    /// <summary>
    /// Places the ball at rest with physics disabled - used on the Start Menu
    /// (and Game Over screen) so it doesn't fall or drift on its own.
    /// </summary>
    public void PlaceIdle()
    {
        isGameOverTriggered = false;
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
    }

    private void ClampSpeed()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private void CheckSideBounds()
    {
        if (mainCamera == null) return;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

        // Clamp immediately at the edge (0-1 viewport range) instead of letting it drift past first
        if (viewportPos.x < 0f)
        {
            Vector3 edge = mainCamera.ViewportToWorldPoint(new Vector3(0f, viewportPos.y, transform.position.z - mainCamera.transform.position.z));
            transform.position = new Vector3(edge.x + sideBoundsPadding, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(Mathf.Abs(rb.linearVelocity.x) * sideBounceStrength, rb.linearVelocity.y);
        }
        else if (viewportPos.x > 1f)
        {
            Vector3 edge = mainCamera.ViewportToWorldPoint(new Vector3(1f, viewportPos.y, transform.position.z - mainCamera.transform.position.z));
            transform.position = new Vector3(edge.x - sideBoundsPadding, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(-Mathf.Abs(rb.linearVelocity.x) * sideBounceStrength, rb.linearVelocity.y);
        }
    }

    private void CheckTopBounds()
    {
        if (mainCamera == null) return;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

        if (viewportPos.y > 1f)
        {
            Vector3 edge = mainCamera.ViewportToWorldPoint(new Vector3(viewportPos.x, 1f, transform.position.z - mainCamera.transform.position.z));
            transform.position = new Vector3(transform.position.x, edge.y - topBoundsPadding, transform.position.z);
            // Bounce back down, same as the side walls do
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -Mathf.Abs(rb.linearVelocity.y) * topBounceStrength);
        }
    }

    private void CheckBottomBoundary()
    {
        if (isGameOverTriggered) return;
        if (graceTimer > 0f) return;

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
