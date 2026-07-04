using UnityEngine;

/// <summary>
/// Smoothly transitions the background sky colors over time based on the player's score.
/// Cross-fades the alpha values of four sky gradient layers (Day, Sunset, Night, Dawn).
/// </summary>
public class WeatherController : MonoBehaviour
{
    public SpriteRenderer skyDay;
    public SpriteRenderer skySunset;
    public SpriteRenderer skyNight;
    public SpriteRenderer skyDawn;

    [Tooltip("How fast the weather transitions (alpha change speed per second).")]
    public float transitionSpeed = 0.5f;

    private float targetDay = 1f;
    private float targetSunset = 0f;
    private float targetNight = 0f;
    private float targetDawn = 0f;

    private float currentDay = 1f;
    private float currentSunset = 0f;
    private float currentNight = 0f;
    private float currentDawn = 0f;

    void Start()
    {
        // Initialize everything
        currentDay = targetDay;
        currentSunset = targetSunset;
        currentNight = targetNight;
        currentDawn = targetDawn;

        SetAlpha(skyDay, currentDay);
        SetAlpha(skySunset, currentSunset);
        SetAlpha(skyNight, currentNight);
        SetAlpha(skyDawn, currentDawn);
    }

    void Update()
    {
        // Smoothly interpolate alphas toward targets
        currentDay = Mathf.MoveTowards(currentDay, targetDay, transitionSpeed * Time.deltaTime);
        currentSunset = Mathf.MoveTowards(currentSunset, targetSunset, transitionSpeed * Time.deltaTime);
        currentNight = Mathf.MoveTowards(currentNight, targetNight, transitionSpeed * Time.deltaTime);
        currentDawn = Mathf.MoveTowards(currentDawn, targetDawn, transitionSpeed * Time.deltaTime);

        SetAlpha(skyDay, currentDay);
        SetAlpha(skySunset, currentSunset);
        SetAlpha(skyNight, currentNight);
        SetAlpha(skyDawn, currentDawn);

        if (ThemeManager.Instance != null && ThemeManager.Instance.GetCurrentTheme() != null && ThemeManager.Instance.GetCurrentTheme().themeName.ToLower() == "mario")
        {
            Color dayCol = Color.white;
            Color sunsetCol = new Color(0.95f, 0.55f, 0.3f); // sunset orange
            Color nightCol = new Color(0.0f, 0.65f, 0.75f);  // teal cyan (underground level style)
            Color dawnCol = new Color(0.8f, 0.5f, 0.9f);    // dawn purple

            Color blendedColor = dayCol * currentDay + sunsetCol * currentSunset + nightCol * currentNight + dawnCol * currentDawn;

            SpriteRenderer[] srs = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            foreach (var sr in srs)
            {
                if (sr == null) continue;
                string srName = sr.gameObject.name;
                // Exclude characters from environmental weather tinting
                if (srName.Contains("Player") || srName.Contains("Bird") || srName.Contains("Goomba")) continue;

                if (srName.Contains("Ground") || srName.Contains("Obstacle") || srName.Contains("Pipe") || srName.Contains("Background") || srName.Contains("Grass"))
                {
                    sr.color = blendedColor;
                }
            }
        }
        else
        {
            // Reset color tints to default white for other themes
            SpriteRenderer[] srs = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            foreach (var sr in srs)
            {
                if (sr == null) continue;
                string srName = sr.gameObject.name;
                if (srName.Contains("Ground") || srName.Contains("Obstacle") || srName.Contains("Pipe") || srName.Contains("Background") || srName.Contains("Grass"))
                {
                    sr.color = Color.white;
                }
            }
        }
    }

    /// <summary>
    /// Changes the target weather according to the score.
    /// Blends slowly and continuously: Day -> Sunset -> Night -> Dawn.
    /// </summary>
    public void SetWeatherByScore(int score)
    {
        // Cycles continuously over 40 points (10 points per weather state)
        float phase = (score % 40) / 10f; // 0.0 to 4.0

        if (phase < 1f) // Day to Sunset
        {
            targetDay = 1f - phase;
            targetSunset = phase;
            targetNight = 0f;
            targetDawn = 0f;
        }
        else if (phase < 2f) // Sunset to Night
        {
            float t = phase - 1f;
            targetDay = 0f;
            targetSunset = 1f - t;
            targetNight = t;
            targetDawn = 0f;
        }
        else if (phase < 3f) // Night to Dawn
        {
            float t = phase - 2f;
            targetDay = 0f;
            targetSunset = 0f;
            targetNight = 1f - t;
            targetDawn = t;
        }
        else // Dawn to Day
        {
            float t = phase - 3f;
            targetDay = t;
            targetSunset = 0f;
            targetNight = 0f;
            targetDawn = 1f - t;
        }
    }

    /// <summary>
    /// Instantly resets weather back to Day state (for game restarts).
    /// </summary>
    public void ResetWeather()
    {
        targetDay = 1f; targetSunset = 0f; targetNight = 0f; targetDawn = 0f;
        currentDay = 1f; currentSunset = 0f; currentNight = 0f; currentDawn = 0f;

        SetAlpha(skyDay, 1f);
        SetAlpha(skySunset, 0f);
        SetAlpha(skyNight, 0f);
        SetAlpha(skyDawn, 0f);
    }

    private void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
