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

    [Header("Animation")]
    public float animationSpeed = 10f;

    [System.Serializable]
    public struct BirdSkin
    {
        [Tooltip("Sprites for this skin's flap animation (Wing Up, Wing Mid, Wing Down).")]
        public Sprite[] flapSprites;
    }

    [Header("Selectable Skins")]
    public BirdSkin[] skins;
    public int currentSkinIndex = 0;


    [HideInInspector]
    public Sprite[] flapSprites; // fallback for backwards compatibility

    private Sprite[] themeOverrideSprites;

    public void OverrideSprites(Sprite[] sprites)
    {
        themeOverrideSprites = sprites;
    }

    private SpriteRenderer spriteRenderer;
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
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;

        // Auto-add an AudioSource so sounds work without extra Inspector setup.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Randomizes the bob cycle slightly so a restarted bird doesn't look robotic.
        idleTimeOffset = Random.Range(0f, 10f);

        // Load skin selection
        currentSkinIndex = PlayerPrefs.GetInt("FlappyBird_SelectedSkin", 0);
        if (skins != null && currentSkinIndex >= skins.Length) currentSkinIndex = 0;
        UpdateSkin();
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
            AnimateWings(animationSpeed * 0.5f);
            return;
        }

        if (!controlEnabled) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || TouchStarted())
        {
            Flap();
        }

        UpdateTilt();
        AnimateWings(animationSpeed);
    }

    private void AnimateWings(float speed)
    {
        if (spriteRenderer == null) return;

        Sprite[] activeSprites = themeOverrideSprites;
        if (activeSprites == null || activeSprites.Length == 0)
        {
            if (skins != null && skins.Length > 0 && currentSkinIndex < skins.Length)
            {
                activeSprites = skins[currentSkinIndex].flapSprites;
            }
            else
            {
                activeSprites = flapSprites;
            }
        }

        if (activeSprites == null || activeSprites.Length == 0) return;
        int index = (int)(Time.time * speed) % activeSprites.Length;
        spriteRenderer.sprite = activeSprites[index];
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
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        UpdateSkin();
    }

    void OnMouseDown()
    {
        // Cycle bird skins when clicked directly on the start screen
        // Only allow skin cycling if we don't have a theme override active (or if the theme has its own multiple skins)
        if (isIdle && !controlEnabled && themeOverrideSprites == null)
        {
            NextSkin();
        }
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
        SpawnBlastEffect();

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
        if (!alreadyDead)
        {
            SpawnBlastEffect();
        }

        if (!alreadyDead && GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    private void SpawnBlastEffect()
    {
        GameObject blast = new GameObject("PixelBlast");
        blast.transform.position = transform.position;

        ParticleSystem ps = blast.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Stop default auto-play before editing parameters

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.4f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
        main.gravityModifier = 0.35f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0f), new Color(1f, 0.9f, 0.1f));
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.1f), 0f), new GradientColorKey(new Color(0.95f, 0.15f, 0f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var psRenderer = blast.GetComponent<ParticleSystemRenderer>();
        psRenderer.sortingOrder = 40; // in front of bird (30)
        psRenderer.material = new Material(Shader.Find("Sprites/Default"));

        ps.Play();
    }

    public void NextSkin()
    {
        if (skins == null || skins.Length == 0) return;
        currentSkinIndex = (currentSkinIndex + 1) % skins.Length;
        PlayerPrefs.SetInt("FlappyBird_SelectedSkin", currentSkinIndex);
        PlayerPrefs.Save();
        UpdateSkin();
        PlayFlapOrClickSound();
    }

    public void PrevSkin()
    {
        if (skins == null || skins.Length == 0) return;
        currentSkinIndex = (currentSkinIndex - 1 + skins.Length) % skins.Length;
        PlayerPrefs.SetInt("FlappyBird_SelectedSkin", currentSkinIndex);
        PlayerPrefs.Save();
        UpdateSkin();
        PlayFlapOrClickSound();
    }

    public void SetSkin(int index)
    {
        if (skins == null || skins.Length == 0) return;
        currentSkinIndex = Mathf.Clamp(index, 0, skins.Length - 1);
        PlayerPrefs.SetInt("FlappyBird_SelectedSkin", currentSkinIndex);
        PlayerPrefs.Save();
        UpdateSkin();
        if (gameObject.activeInHierarchy)
        {
            PlayFlapOrClickSound();
        }
    }

    public void UpdateSkin()
    {
        if (themeOverrideSprites != null && themeOverrideSprites.Length > 0 && spriteRenderer != null)
        {
            int midIndex = themeOverrideSprites.Length > 1 ? 1 : 0;
            spriteRenderer.sprite = themeOverrideSprites[midIndex];
            return;
        }

        if (skins == null || skins.Length == 0 || currentSkinIndex >= skins.Length || spriteRenderer == null) return;
        Sprite[] activeSprites = skins[currentSkinIndex].flapSprites;
        if (activeSprites != null && activeSprites.Length > 1)
        {
            spriteRenderer.sprite = activeSprites[1]; // default to mid-flap
        }
    }

    private void PlayFlapOrClickSound()
    {
        if (audioSource != null && flapSound != null)
        {
            audioSource.PlayOneShot(flapSound);
        }
    }
}
