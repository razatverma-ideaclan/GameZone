using UnityEngine;

/// <summary>
/// Stretches this sprite to always fully cover the Main Camera's current
/// view, so there are no gaps or letterboxing on any screen size or aspect
/// ratio. Attach to the Background GameObject (needs a Sprite Renderer).
/// Recalculates every frame in LateUpdate() — after CameraFitWidth's
/// Update() has already resized the camera for that frame — so it stays
/// correct even if the aspect ratio changes at runtime (Editor Game view
/// dropdown, desktop window resize, Android split-screen, etc.).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitToCamera : MonoBehaviour
{
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || sr == null || sr.sprite == null) return;

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size; // world units at scale (1,1,1)
        if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

        float scaleX = (camHalfWidth * 2f) / spriteSize.x;
        float scaleY = (camHalfHeight * 2f) / spriteSize.y;

        // Use the larger of the two so the background always fully covers the
        // screen without ever stretching/distorting the art — like CSS
        // "background-size: cover." Any excess simply extends past the
        // camera's edges, which is invisible to the player.
        float uniformScale = Mathf.Max(scaleX, scaleY) * 1.02f; // tiny safety margin so no hairline gap ever shows at the edges

        transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);
    }
}
