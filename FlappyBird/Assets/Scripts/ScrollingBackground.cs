using UnityEngine;

/// <summary>
/// Two copies of the background sprite, both kept scaled to fully "cover" the
/// camera's current view (same cover-fit math as BackgroundFitToCamera), and
/// scrolled slowly left as a parallax layer behind the ground/pipes. The pair
/// is always repositioned as exactly adjacent to the camera, so there's never
/// a seam or gap no matter the aspect ratio or scroll offset.
/// Attach to the first background tile (needs a Sprite Renderer); assign
/// tileB to the second tile's Transform (same sprite, same sorting order).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ScrollingBackground : MonoBehaviour
{
    public Transform tileB;
    [Tooltip("Slow drift speed — noticeably slower than the ground/pipes for a parallax depth feel.")]
    public float scrollSpeed = 0.6f;

    private SpriteRenderer sr;
    private float scrollOffset;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || sr == null || sr.sprite == null || tileB == null) return;

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size; // world units at scale (1,1,1)
        if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

        float scaleX = (camHalfWidth * 2f) / spriteSize.x;
        float scaleY = (camHalfHeight * 2f) / spriteSize.y;
        float uniformScale = Mathf.Max(scaleX, scaleY) * 1.05f; // extra margin — this layer scrolls, so it needs a bit more overlap than a static background

        transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        tileB.localScale = transform.localScale;

        float tileWidth = spriteSize.x * uniformScale;

        // Freeze on Game Over like everything else, but keep drifting on the Start screen.
        bool frozen = GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.GameOver;
        if (!frozen)
        {
            scrollOffset += scrollSpeed * Time.deltaTime;
            scrollOffset %= tileWidth;
        }

        float baseX = cam.transform.position.x - scrollOffset;
        transform.position = new Vector3(baseX, cam.transform.position.y, transform.position.z);
        tileB.position = new Vector3(baseX + tileWidth, cam.transform.position.y, tileB.position.z);
    }
}
