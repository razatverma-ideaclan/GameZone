using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A lightweight component to register mouse/touch clicks on 2D World-space GameObjects.
/// Resolves UI positioning issues by letting selectors live directly in world space as children of the bird.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WorldButton : MonoBehaviour
{
    [Tooltip("Callbacks invoked when this world object is clicked or tapped.")]
    public UnityEvent onClick = new UnityEvent();

    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        // Check for click/touch start events
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null) return;

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 clickPos = new Vector2(worldPos.x, worldPos.y);

            if (col != null && col.OverlapPoint(clickPos))
            {
                // Verify the click is not blocked by screen-space UI elements
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                if (onClick != null)
                {
                    onClick.Invoke();
                }
            }
        }
    }
}
