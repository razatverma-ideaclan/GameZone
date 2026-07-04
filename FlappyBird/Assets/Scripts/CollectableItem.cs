using UnityEngine;

public class CollectableItem : MonoBehaviour
{
    public enum ItemType { Coin, Magnet, Boost, Double }

    public ItemType itemType = ItemType.Coin;
    public int coinValue = 1;

    [Tooltip("Base leftward speed, matching PipeMover's default so items and pipes scroll in sync.")]
    public float speed = 3f;
    [Tooltip("World X position at which this item destroys itself (should be left of the camera view).")]
    public float destroyXPosition = -12f;

    private Transform bird;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // No longer parented to a PipePair, so this item must scroll itself — same formula as PipeMover.
        if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            float currentSpeed = speed;
            if (GameManager.Instance != null) currentSpeed *= GameManager.Instance.GetCurrentSpeedMultiplier();
            transform.position += Vector3.left * currentSpeed * Time.deltaTime;

            if (transform.position.x < destroyXPosition)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (itemType != ItemType.Coin || GameManager.Instance == null) return;

        float magnetRadius = GameManager.Instance.GetMagnetRadius();
        if (magnetRadius <= 0f) return;

        if (bird == null)
        {
            GameObject birdGO = GameManager.Instance.bird;
            if (birdGO == null) return;
            bird = birdGO.transform;
        }

        float dist = Vector3.Distance(transform.position, bird.position);
        if (dist <= magnetRadius)
        {
            float pullSpeed = Mathf.Lerp(3f, 14f, 1f - Mathf.Clamp01(dist / magnetRadius));
            transform.position = Vector3.MoveTowards(transform.position, bird.position, pullSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Bird")) return;
        if (GameManager.Instance == null) return;

        switch (itemType)
        {
            case ItemType.Coin:
                GameManager.Instance.AddCoins(coinValue);
                break;
            case ItemType.Magnet:
                GameManager.Instance.ActivateMagnet();
                break;
            case ItemType.Boost:
                GameManager.Instance.ActivateBoost();
                break;
            case ItemType.Double:
                GameManager.Instance.ActivateDouble();
                break;
        }

        SpawnPickupSpark();
        Destroy(gameObject);
    }

    private void SpawnPickupSpark()
    {
        GameObject spark = new GameObject("PickupSpark");
        spark.transform.position = transform.position;

        ParticleSystem ps = spark.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = 0.3f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.gravityModifier = 0f;
        main.startColor = sr != null ? (Color)sr.color : Color.white;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var psRenderer = spark.GetComponent<ParticleSystemRenderer>();
        psRenderer.sortingOrder = 40;
        psRenderer.material = new Material(Shader.Find("Sprites/Default"));

        ps.Play();
    }
}
