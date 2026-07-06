using UnityEngine;

/// <summary>
/// Singleton manager that controls theme loading, selection persistence,
/// and supplies the active ThemeData to the gameplay scene.
/// </summary>
public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    [Header("Available Themes")]
    [Tooltip("Assign all ThemeData assets (Classic, Space, Football, etc.) in the order they should appear.")]
    public ThemeData[] themes;

    private const string ThemePrefsKey = "FlappyBird_SelectedThemeIndex";
    private int selectedThemeIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadThemeSelection();
    }

    /// <summary>
    /// Loads the selected theme index from PlayerPrefs, ensuring bounds check.
    /// </summary>
    public void LoadThemeSelection()
    {
        // Default to Space (index 1) for first-time players who haven't picked a world yet.
        selectedThemeIndex = PlayerPrefs.GetInt(ThemePrefsKey, 1);
        if (themes != null && (selectedThemeIndex < 0 || selectedThemeIndex >= themes.Length))
        {
            selectedThemeIndex = 0;
        }
    }

    /// <summary>
    /// Retrieves the currently selected theme data. Falls back to classic (index 0) if invalid.
    /// </summary>
    public ThemeData GetCurrentTheme()
    {
        if (themes == null || themes.Length == 0) return null;
        if (selectedThemeIndex < 0 || selectedThemeIndex >= themes.Length) selectedThemeIndex = 0;
        return themes[selectedThemeIndex];
    }

    /// <summary>
    /// Updates the selected theme index, saves to PlayerPrefs, and triggers visual updates.
    /// </summary>
    public void SelectTheme(int index)
    {
        if (themes == null || index < 0 || index >= themes.Length) return;

        selectedThemeIndex = index;
        PlayerPrefs.SetInt(ThemePrefsKey, selectedThemeIndex);
        PlayerPrefs.Save();

        // Immediately apply the theme to any visual elements in the current scene
        ThemeApplier applier = FindObjectOfType<ThemeApplier>();
        if (applier != null)
        {
            applier.ApplyTheme(GetCurrentTheme());
        }
    }

    /// <summary>
    /// Gets the current selected theme index.
    /// </summary>
    public int GetSelectedThemeIndex()
    {
        return selectedThemeIndex;
    }
}
