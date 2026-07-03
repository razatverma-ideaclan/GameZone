using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the UI theme selection buttons in the Main Menu (Start screen).
/// Updates button visual states and notifies ThemeManager on selection clicks.
/// </summary>
public class ThemeSelectorUI : MonoBehaviour
{
    [Header("Theme Selection Buttons")]
    [Tooltip("The buttons corresponding to each theme in order (Classic = 0, Space = 1, etc.).")]
    public Button[] themeButtons;

    [Header("Selection Colors")]
    public Color normalBorderColor = new Color(0.38f, 0.15f, 0.02f); // dark brown
    public Color selectedBorderColor = new Color(0.95f, 0.72f, 0.15f); // bright gold

    private void Start()
    {
        // Bind click events to each theme button
        for (int i = 0; i < themeButtons.Length; i++)
        {
            int index = i; // local copy for delegate closure
            if (themeButtons[i] != null)
            {
                themeButtons[i].onClick.AddListener(() => OnThemeButtonClicked(index));
            }
        }

        UpdateSelectionUI();
    }

    private void OnThemeButtonClicked(int index)
    {
        if (ThemeManager.Instance != null)
        {
            ThemeManager.Instance.SelectTheme(index);

            // Play the UI click sound if GameManager is loaded
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayClickSound();
            }

            UpdateSelectionUI();
        }
    }

    /// <summary>
    /// Highlights the selected theme button outline and dimensions, resetting other buttons.
    /// </summary>
    public void UpdateSelectionUI()
    {
        if (ThemeManager.Instance == null) return;
        int selectedIndex = ThemeManager.Instance.GetSelectedThemeIndex();

        for (int i = 0; i < themeButtons.Length; i++)
        {
            if (themeButtons[i] == null) continue;

            Outline outline = themeButtons[i].GetComponent<Outline>();
            RectTransform rt = themeButtons[i].GetComponent<RectTransform>();

            if (i == selectedIndex)
            {
                // Selected state: enable gold outline and scale up slightly
                if (outline != null)
                {
                    outline.enabled = true;
                    outline.effectColor = selectedBorderColor;
                    outline.effectDistance = new Vector2(4f, -4f);
                }
                if (rt != null)
                {
                    rt.localScale = new Vector3(1.08f, 1.08f, 1f);
                }
            }
            else
            {
                // Normal state: reset outline and scale
                if (outline != null)
                {
                    // keep border but with normal brown color
                    outline.enabled = true;
                    outline.effectColor = normalBorderColor;
                    outline.effectDistance = new Vector2(2f, -2f);
                }
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                }
            }
        }
    }
}
