using UnityEngine;

/// <summary>
/// Attached to the spawned PipePair obstacles. Immediately applies the selected
/// theme's sprites to top/bottom pipes and caps on Start.
/// </summary>
public class ThemeObstacleApplier : MonoBehaviour
{
    private void Start()
    {
        ApplyCurrentTheme();
    }

    public void ApplyCurrentTheme()
    {
        if (ThemeApplier.Instance != null)
        {
            ThemeApplier.Instance.ApplyThemeToObstacle(gameObject);
        }
    }
}
