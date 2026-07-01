using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the runner: constant forward movement (speeding up over time),
/// left/right lane switching, jump, and slide. Dies on touching an obstacle.
/// Attach to the "Player" GameObject (needs a CharacterController).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Lanes")]
    [Tooltip("Distance in world units between adjacent lanes.")]
    public float laneDistance = 3f;

    [Header("Forward Movement")]
    public float forwardSpeed = 8f;
    public float speedIncreaseRate = 0.15f;
    public float laneChangeSpeed = 12f;

    [Header("Jump / Slide")]
    public float jumpForce = 9f;
    public float gravity = -25f;
    public float slideDuration = 0.7f;

    private CharacterController controller;
    private Vector3 startPosition;
    private int currentLane = 1; // 0 = left, 1 = middle, 2 = right
    private float startForwardSpeed;
    private float verticalVelocity;
    private bool isSliding;
    private bool controlEnabled = false;
    private bool isDead = false;

    private float defaultHeight;
    private Vector3 defaultCenter;

    private Vector2 touchStartPos;
    private bool trackingTouch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        startPosition = transform.position;
        startForwardSpeed = forwardSpeed;
        defaultHeight = controller.height;
        defaultCenter = controller.center;
    }

    void Update()
    {
        if (!controlEnabled || isDead) return;

        HandleKeyboardInput();
        HandleTouchInput();

        forwardSpeed += speedIncreaseRate * Time.deltaTime;

        // Move sideways toward the current lane's X position.
        float targetX = (currentLane - 1) * laneDistance;
        float moveX = Mathf.MoveTowards(0f, targetX - transform.position.x, laneChangeSpeed * Time.deltaTime);

        // Gravity / jump.
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f; // small downward force keeps the controller grounded
        }
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = new Vector3(moveX, verticalVelocity * Time.deltaTime, forwardSpeed * Time.deltaTime);
        controller.Move(move);
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) ChangeLane(1);
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) Jump();
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Slide();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            touchStartPos = touch.position;
            trackingTouch = true;
        }
        else if (touch.phase == TouchPhase.Ended && trackingTouch)
        {
            trackingTouch = false;
            Vector2 delta = touch.position - touchStartPos;
            const float swipeThreshold = 50f;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                if (delta.x > swipeThreshold) ChangeLane(1);
                else if (delta.x < -swipeThreshold) ChangeLane(-1);
            }
            else
            {
                if (delta.y > swipeThreshold) Jump();
                else if (delta.y < -swipeThreshold) Slide();
            }
        }
    }

    private void ChangeLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, 0, 2);
    }

    private void Jump()
    {
        if (!controller.isGrounded || isSliding) return;
        verticalVelocity = jumpForce;
    }

    private void Slide()
    {
        if (isSliding || !controller.isGrounded) return;
        StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        controller.height = defaultHeight * 0.5f;
        controller.center = new Vector3(defaultCenter.x, defaultCenter.y * 0.5f, defaultCenter.z);

        yield return new WaitForSeconds(slideDuration);

        controller.height = defaultHeight;
        controller.center = defaultCenter;
        isSliding = false;
    }

    /// <summary>Called by GameManager when the Start screen begins the run.</summary>
    public void EnableControl()
    {
        isDead = false;
        controlEnabled = true;
    }

    /// <summary>Called by GameManager to reset the player to the starting lane/position.</summary>
    public void ResetPlayer()
    {
        isDead = false;
        controlEnabled = false;
        isSliding = false;
        currentLane = 1;
        forwardSpeed = startForwardSpeed;
        verticalVelocity = 0f;
        controller.height = defaultHeight;
        controller.center = defaultCenter;

        controller.enabled = false; // must disable before teleporting a CharacterController
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        controller.enabled = true;
    }

    /// <summary>Current distance traveled along Z, used by GameManager as the score.</summary>
    public float DistanceTraveled => transform.position.z - startPosition.z;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        controlEnabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}
