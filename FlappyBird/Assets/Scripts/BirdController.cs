using UnityEngine;

/// <summary>
/// Controls the bird: idle float before the game starts, gravity fall + flap
/// during play, rotation that tilts up on flap / down while falling, and
/// death on collision (which fully freezes and locks out the bird).
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

    [Header("Rotation")]
    [Tooltip("How far up the bird tilts on a flap.")]
    public float maxUpTiltAngle = 25f;
    [Tooltip("How far down the bird tilts while falling.")]
    public float maxDownTiltAngle = 80f;
    [Tooltip("How quickly the bird rotates toward its target tilt.")]
    public float tiltSpeed = 6f;

    [Header("Idle Animation (before Start)")]
    [Tooltip("How high/low the bird bobs while waiting on the Start screen.")]
    public float idleBobHeight = 0.3f;
    [Tooltip("How fast the bird bobs up and down while idle.")]
    public float idleBobSpeed = 2.5f;

    [Header("Audio (optional — leave empty to skip)")]
    public AudioClip flapSound;
    [Tooltip("Plays the instant the bird hits a pipe/wall/ceiling.")]
    public AudioClip hitSound;
    [Tooltip("Plays once the bird actually lands on the ground after falling.")]
    public AudioClip fallSound;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Vector3 startPosition;
    private bool controlEnabled = false;
    private bool isDead = false;
    private bool hasLanded = false;
    private bool isIdle = true;
    private float idleTimeOffset;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;

        // Auto-add an AudioSource so sounds work without extra Inspector setup.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Randomizes the bob cycle slightly so a restarted bird doesn't look robotic.
        idleTimeOffset = Random.Range(0f, 10f);
    }

    void Start()
    {
        rb.gravityScale = gravityScale;
        // Bird stays still (no gravity) until GameManager enables control.
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        if (isDead)
        {
            // Keep tilting nose-down while gravity carries the bird to the floor,
            // so the death fall still looks alive instead of a frozen sprite.
            if (!hasLanded) UpdateTilt();
            return;
        }

        if (isIdle)
        {
            IdleFloat();
            return;
        }

        if (!controlEnabled) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || TouchStarted())
        {
            Flap();
        }

        UpdateTilt();
    }

    private void IdleFloat()
    {
        // Smooth up/down bob using a sine wave — no physics/gravity involved.
        float offsetY = Mathf.Sin((Time.time + idleTimeOffset) * idleBobSpeed) * idleBobHeight;
        transform.position = startPosition + new Vector3(0f, offsetY, 0f);
    }

    private void UpdateTilt()
    {
        float targetAngle = rb.linearVelocity.y >= 0f
            ? Mathf.Clamp(rb.linearVelocity.y * 8f, 0f, maxUpTiltAngle)
            : Mathf.Clamp(rb.linearVelocity.y * 8f, -maxDownTiltAngle, 0f);

        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, tiltSpeed * Time.deltaTime);
    }

    private bool TouchStarted()
    {
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }

    private void Flap()
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(Vector2.up * flapForce, ForceMode2D.Impulse);
        PlaySound(flapSound);
    }

    /// <summary>Called by GameManager when the Start screen begins the run.</summary>
    public void EnableControl()
    {
        isDead = false;
        isIdle = false;
        controlEnabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>Called by GameManager to reset the bird to its starting pose (Start screen / idle).</summary>
    public void ResetBird()
    {
        isDead = false;
        hasLanded = false;
        controlEnabled = false;
        isIdle = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ground/Ceiling/Wall/Pipe can all be set up as triggers instead of solid colliders.
        HandleImpact(other.gameObject);
    }

    private void HandleImpact(GameObject other)
    {
        if (hasLanded) return; // already stopped on the floor — ignore anything further

        if (other.CompareTag("Ground"))
        {
            Land();
        }
        else if (other.CompareTag("Pipe") || other.CompareTag("Wall") || other.CompareTag("Ceiling"))
        {
            Die();
        }
    }

    /// <summary>
    /// Hit a pipe/wall/ceiling. Plays the impact sound and ends the run
    /// immediately (pipes freeze, input locks) but leaves the Rigidbody2D
    /// Dynamic so gravity keeps pulling the bird down to the ground — it
    /// doesn't just freeze mid-air.
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        isIdle = false;
        controlEnabled = false;
        rb.bodyType = RigidbodyType2D.Dynamic; // keep falling under gravity instead of freezing in place

        PlaySound(hitSound);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    /// <summary>
    /// The bird has actually reached the ground — stop it for good and play
    /// the separate landing thud. Handles both "hit a pipe, then fell" and a
    /// direct fall straight into the floor.
    /// </summary>
    private void Land()
    {
        bool alreadyDead = isDead;
        isDead = true;
        hasLanded = true;
        isIdle = false;
        controlEnabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; // fully stopped — no more falling or movement

        PlaySound(fallSound);

        if (!alreadyDead && GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }
}
