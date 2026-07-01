using UnityEngine;

/// <summary>
/// Controls the bird: gravity fall + flap on input, and death on collision.
/// Attach to the "Bird" GameObject (needs Rigidbody2D + a Collider2D).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BirdController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Upward impulse applied on each flap.")]
    public float flapForce = 5f;

    [Tooltip("Extra gravity multiplier on top of Rigidbody2D's gravity scale.")]
    public float gravityScale = 1.5f;

    [Header("Rotation (optional visual flair)")]
    public bool tiltWithVelocity = true;
    public float maxTiltAngle = 30f;
    public float tiltSpeed = 5f;

    private Rigidbody2D rb;
    private Vector3 startPosition;
    private bool controlEnabled = false;
    private bool isDead = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    void Start()
    {
        rb.gravityScale = gravityScale;
        // Bird stays still until GameManager enables control.
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        if (!controlEnabled || isDead) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || TouchStarted())
        {
            Flap();
        }

        if (tiltWithVelocity)
        {
            float targetAngle = Mathf.Clamp(rb.linearVelocity.y * 6f, -maxTiltAngle, maxTiltAngle);
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, tiltSpeed * Time.deltaTime);
        }
    }

    private bool TouchStarted()
    {
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }

    private void Flap()
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(Vector2.up * flapForce, ForceMode2D.Impulse);
    }

    /// <summary>Called by GameManager when the Start screen begins the run.</summary>
    public void EnableControl()
    {
        isDead = false;
        controlEnabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>Called by GameManager to reset the bird to its starting pose.</summary>
    public void ResetBird()
    {
        isDead = false;
        controlEnabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Die();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ground can be set up as a trigger instead of a solid collider if preferred.
        if (other.CompareTag("Ground") || other.CompareTag("Pipe"))
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        controlEnabled = false;
        rb.bodyType = RigidbodyType2D.Static; // freeze bird in place on death

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}
