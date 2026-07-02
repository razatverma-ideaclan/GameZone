using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dynamically adjusts the CanvasScaler match factor based on the current screen aspect ratio.
/// Prevents vertical clipping on wide/landscape screens (desktop) and horizontal clipping on narrow/portrait screens (mobile).
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class CanvasScalerMatch : MonoBehaviour
{
    private CanvasScaler scaler;

    void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        UpdateMatchMode();
    }

#if UNITY_EDITOR
    void Update()
    {
        // Keep it responsive during editor resizing/preview testing
        UpdateMatchMode();
    }
#endif

    private void UpdateMatchMode()
    {
        if (scaler == null) return;

        float aspect = (float)Screen.width / Screen.height;
        // Standard portrait phone aspect ratio is 9:16 (~0.5625).
        // If the screen is wider than portrait (like desktop landscape), match Height (1f) so UI scales down to fit vertically.
        // If the screen is portrait, match Width (0f) to fit horizontally.
        scaler.matchWidthOrHeight = (aspect > 0.6f) ? 1f : 0f;
    }
}
