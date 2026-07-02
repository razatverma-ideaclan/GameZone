using UnityEngine;

/// <summary>
/// Scrolls two adjacent ground tiles left in an endless loop (classic Flappy
/// Bird floor scroll). Uses a single accumulated offset taken modulo the tile
/// width, so the two tiles are always perfectly adjacent with no seam or gap,
/// regardless of frame rate. Attach to an empty parent; assign tileA/tileB to
/// the two ground tile Transforms (each the same width).
/// </summary>
public class GroundScroller : MonoBehaviour
{
    public Transform tileA;
    public Transform tileB;
    public float tileWidth = 20f;
    public float scrollSpeed = 3f; // matches the pipe scroll speed so the floor and pipes move together

    private float scrollOffset;

    void Update()
    {
        if (tileA == null || tileB == null) return;

        // Freeze once the run has ended, same as the pipes — but keep scrolling
        // on the Start screen so the floor feels alive while waiting to tap.
        bool frozen = GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.GameOver;
        if (!frozen)
        {
            scrollOffset += scrollSpeed * Time.deltaTime;
            scrollOffset %= tileWidth;
        }

        float baseX = -scrollOffset;
        Vector3 posA = tileA.position;
        Vector3 posB = tileB.position;
        tileA.position = new Vector3(baseX, posA.y, posA.z);
        tileB.position = new Vector3(baseX + tileWidth, posB.y, posB.z);
    }
}
