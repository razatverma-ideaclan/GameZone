using UnityEngine;

/// <summary>
/// Detects taps and swipes on or near the ball and converts them into kick input.
/// Works with mobile touch AND mouse clicks (Unity Editor testing).
///
/// This script is intentionally kept as a thin "input translator": it only reads
/// input and calls BallController.Kick(direction, force). Later, a
/// FootDetectionInputManager can replace this script (or run alongside it) and
/// call the exact same BallController.Kick() method - no other code needs to change.
/// </summary>
public class KickInputManager : MonoBehaviour
{
    [Header("References")]
    public BallController ballController;

    [Header("Input Settings")]
    [Tooltip("Max distance (world units) from the ball a tap/swipe can start to count")]
    public float inputDetectionRadius = 1.5f;
    [Tooltip("Minimum swipe distance (pixels) to be treated as a swipe instead of a simple tap")]
    public float minSwipeDistancePixels = 40f;
    [Tooltip("Scales swipe speed into horizontal kick direction strength")]
    public float swipeSensitivity = 0.02f;

    private Camera mainCamera;
    private Vector2 touchStartPos;
    private bool isTracking = false;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    // ---------- Mouse (Editor testing) ----------

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            isTracking = IsNearBall(touchStartPos);
        }
        else if (Input.GetMouseButtonUp(0) && isTracking)
        {
            Vector2 endPos = Input.mousePosition;
            ProcessInput(touchStartPos, endPos);
            isTracking = false;
        }
    }

    // ---------- Touch (Mobile) ----------

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            touchStartPos = touch.position;
            isTracking = IsNearBall(touchStartPos);
        }
        else if (touch.phase == TouchPhase.Ended && isTracking)
        {
            ProcessInput(touchStartPos, touch.position);
            isTracking = false;
        }
    }

    // ---------- Shared logic ----------

    private bool IsNearBall(Vector2 screenPos)
    {
        if (ballController == null || mainCamera == null) return false;

        Vector3 ballScreenPos = mainCamera.WorldToScreenPoint(ballController.transform.position);
        float screenDistance = Vector2.Distance(screenPos, ballScreenPos);

        // Convert the world-space detection radius into an approximate screen-space distance
        float worldToScreenScale = mainCamera.WorldToScreenPoint(ballController.transform.position + Vector3.right * inputDetectionRadius).x - ballScreenPos.x;
        worldToScreenScale = Mathf.Abs(worldToScreenScale);

        return screenDistance <= Mathf.Max(worldToScreenScale, 80f); // 80px minimum tap area for comfort
    }

    private void ProcessInput(Vector2 startPos, Vector2 endPos)
    {
        Vector2 delta = endPos - startPos;
        float distance = delta.magnitude;

        if (distance < minSwipeDistancePixels)
        {
            // Treated as a simple tap: kick straight up
            ballController.Kick(Vector2.zero, 1f);
        }
        else
        {
            // Treated as a swipe: direction influences horizontal kick, and a
            // faster/longer swipe gives slightly more force (capped for game feel)
            float horizontalDirection = Mathf.Clamp(delta.x * swipeSensitivity, -1f, 1f);
            float forceMultiplier = Mathf.Clamp(1f + (distance * 0.001f), 1f, 1.5f);
            ballController.Kick(new Vector2(horizontalDirection, 0f), forceMultiplier);
        }
    }
}
