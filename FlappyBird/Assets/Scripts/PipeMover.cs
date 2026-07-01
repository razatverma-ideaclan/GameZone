using UnityEngine;

/// <summary>
/// Moves a pipe pair (or ground segment) from right to left at a constant speed,
/// and destroys it once it goes off-screen to the left.
/// Attach this to the root "PipePair" prefab/GameObject that PipeSpawner instantiates.
/// </summary>
public class PipeMover : MonoBehaviour
{
    public float speed = 3f;

    [Tooltip("World X position at which this object destroys itself (should be left of the camera view).")]
    public float destroyXPosition = -12f;

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x < destroyXPosition)
        {
            Destroy(gameObject);
        }
    }
}
