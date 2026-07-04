using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attached to the UI Canvas to apply a premium dark neon-blue glassmorphic theme
/// in real-time. Supports full customization and previewing inside the Unity Editor.
/// </summary>
[ExecuteAlways]
public class MenuThemeConfig : MonoBehaviour
{
    [Header("Color Configuration")]
    [Tooltip("Background color of panels and navigation bar.")]
    public Color panelColor = new Color(0.08f, 0.09f, 0.12f, 0.88f); // charcoal dark blue
    
    [Tooltip("Outline border glow color (neon cyan).")]
    public Color glowBorderColor = new Color(0.0f, 0.85f, 1.0f, 0.6f); // neon cyan
    
    [Tooltip("Center Play button color tint.")]
    public Color playButtonColor = new Color(0.0f, 0.35f, 1.0f, 1.0f); // bright blue
    
    [Tooltip("Notification/Pill badge color tint.")]
    public Color badgeColor = new Color(0.8f, 0.0f, 1.0f, 1.0f); // violet
    
    [Tooltip("Dynamic highlight text accent color.")]
    public Color accentTextColor = new Color(0.0f, 1.0f, 0.9f, 1.0f); // bright cyan

    [Header("Sprite Overrides (Optional)")]
    [Tooltip("Custom sprite for panels (rounded glass look).")]
    public Sprite glassPanelOverride;
    
    [Tooltip("Custom sprite for neon border glows.")]
    public Sprite glowOutlineOverride;
    
    [Tooltip("Custom sprite for the play button (raised glossy circle).")]
    public Sprite playButtonOverride;
    
    [Tooltip("Custom sprite for the pill-shaped badges.")]
    public Sprite pillBadgeOverride;

    [Header("Scene Object References")]
    public Image bottomBarBg;
    public Image bottomBarOutline;
    public Image centerPlayButtonImage;
    public Image leaderboardButtonImage;
    public Image verticalClimbButtonImage;
    public Image highScoreBadgeImage;
    public Image activeIndicatorImage;

    private Sprite generatedGlassPanel;
    private Sprite generatedGlowOutline;
    private Sprite generatedPlayButton;
    private Sprite generatedPillBadge;

    private void Awake()
    {
        ApplyTheme();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Triggers visual updates inside the Editor inspector in real-time as colors change
        UnityEditor.EditorApplication.delayCall += () => {
            if (this != null) ApplyTheme();
        };
    }
#endif

    public void ApplyTheme()
    {
        GenerateTexturesIfNeeded();

        Sprite panelSprite = glassPanelOverride != null ? glassPanelOverride : generatedGlassPanel;
        Sprite outlineSprite = glowOutlineOverride != null ? glowOutlineOverride : generatedGlowOutline;
        Sprite playSprite = playButtonOverride != null ? playButtonOverride : generatedPlayButton;
        Sprite badgeSprite = pillBadgeOverride != null ? pillBadgeOverride : generatedPillBadge;

        // 1. Bottom Nav Bar Background & Outline
        if (bottomBarBg != null)
        {
            bottomBarBg.sprite = panelSprite;
            bottomBarBg.color = panelColor;
        }
        if (bottomBarOutline != null)
        {
            bottomBarOutline.sprite = outlineSprite;
            bottomBarOutline.color = glowBorderColor;
        }

        // 2. Center Play Button (Made transparent so the sliding ActiveIndicator circle acts as the background)
        if (centerPlayButtonImage != null)
        {
            centerPlayButtonImage.sprite = null;
            centerPlayButtonImage.color = Color.clear;
            Outline outline = centerPlayButtonImage.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
        }

        // 3. Ranks (Leaderboard) Button
        if (leaderboardButtonImage != null)
        {
            leaderboardButtonImage.sprite = panelSprite;
            leaderboardButtonImage.color = panelColor;
            
            Outline outline = leaderboardButtonImage.GetComponent<Outline>();
            if (outline != null) outline.effectColor = glowBorderColor;
        }

        // 4. Climb (Vertical Mode) Button
        if (verticalClimbButtonImage != null)
        {
            verticalClimbButtonImage.sprite = panelSprite;
            verticalClimbButtonImage.color = panelColor;
            
            Outline outline = verticalClimbButtonImage.GetComponent<Outline>();
            if (outline != null) outline.effectColor = glowBorderColor;
        }

        // 4.5 style all buttons in the bottom bar automatically
        if (bottomBarBg != null)
        {
            foreach (Transform child in bottomBarBg.transform)
            {
                if (child.name.EndsWith("Button"))
                {
                    Image btnImg = child.GetComponent<Image>();
                    if (btnImg != null)
                    {
                        btnImg.sprite = null;
                        btnImg.color = Color.clear;
                        
                        Outline outline = child.GetComponent<Outline>();
                        if (outline != null) outline.enabled = false;
                    }
                }
            }
        }

        // 5. High Score Badge (Made completely transparent as requested, removing backing capsule)
        if (highScoreBadgeImage != null)
        {
            highScoreBadgeImage.sprite = null;
            highScoreBadgeImage.color = Color.clear;
            
            Outline outline = highScoreBadgeImage.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
        }

        // 6. Active Indicator Pill (Frosted white glass highlight)
        if (activeIndicatorImage != null)
        {
            activeIndicatorImage.sprite = playSprite;
            activeIndicatorImage.color = new Color(1f, 1f, 1f, 0.16f);
            
            Outline outline = activeIndicatorImage.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
        }

        // 7. Update text colors inside badges
        UpdateChildTextColors();
    }

    private void UpdateChildTextColors()
    {
        if (leaderboardButtonImage != null)
        {
            Text text = leaderboardButtonImage.GetComponentInChildren<Text>();
            if (text != null) text.color = Color.white;
        }
        if (verticalClimbButtonImage != null)
        {
            Text text = verticalClimbButtonImage.GetComponentInChildren<Text>();
            if (text != null) text.color = Color.white;
        }
        if (highScoreBadgeImage != null)
        {
            Text text = highScoreBadgeImage.GetComponentInChildren<Text>();
            if (text != null) text.color = accentTextColor;
        }
    }

    private void GenerateTexturesIfNeeded()
    {
        if (generatedGlassPanel != null) return;

        // Generate Glass Panel (1024x128 high resolution, with 64px circular cap borders)
        generatedGlassPanel = CreateSprite(1024, 128, (w, h) => GenerateGlassPanelTexture(w, h), new Vector4(64, 12, 64, 12));
        // Generate Glow Outline (1024x128 high resolution, with 64px circular cap borders)
        generatedGlowOutline = CreateSprite(1024, 128, (w, h) => GenerateGlowOutlineTexture(w, h), new Vector4(64, 12, 64, 12));
        // Generate Play Button (Glossy Circle / Active Indicator, high resolution)
        generatedPlayButton = CreateSprite(256, 256, (w, h) => GenerateGlossyCircleTexture(w, h), new Vector4(0, 0, 0, 0));
        // Generate Pill Badge
        generatedPillBadge = CreateSprite(128, 64, (w, h) => GeneratePillTexture(w, h), new Vector4(32, 12, 32, 12));
    }

    private Sprite CreateSprite(int w, int h, System.Func<int, int, Texture2D> generator, Vector4 borders)
    {
        Texture2D tex = generator(w, h);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borders);
    }

    private Texture2D GenerateGlassPanelTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        for (int y = 0; y < h; y++)
        {
            float normY = (float)y / h; // 0 at bottom, 1 at top
            for (int x = 0; x < w; x++)
            {
                float normX = (float)x / w;
                
                // Fully rounded capsule ends: distance to capsule shape
                float radius = h * 0.5f;
                float dist = 0f;
                if (x < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                }
                else if (x > w - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(w - radius, radius));
                }
                else
                {
                    dist = Mathf.Abs(y - radius);
                }
                
                if (dist > radius)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    // Translucent white base color for frosted glass
                    float baseAlpha = 0.28f;
                    
                    // Specular bevel highlight along the top edge
                    float specular = 0f;
                    if (normY > 0.88f)
                    {
                        specular = (normY - 0.88f) / 0.12f * 0.35f;
                    }
                    
                    // Chromatic refraction along the bottom edge (rainbow prism effect)
                    Color chromaticGlow = Color.clear;
                    if (normY < 0.18f)
                    {
                        float intensity = (1f - (normY / 0.18f)) * 0.45f;
                        // Spectrum: red-orange -> yellow -> green -> cyan -> blue -> violet
                        float hue = normX;
                        Color specCol = Color.HSVToRGB(hue, 0.75f, 0.95f);
                        chromaticGlow = new Color(specCol.r, specCol.g, specCol.b, intensity);
                    }
                    
                    Color finalCol = Color.white;
                    finalCol = Color.Lerp(finalCol, Color.white, specular);
                    
                    float finalAlpha = Mathf.Clamp01(baseAlpha + specular);
                    Color pixelCol = new Color(finalCol.r, finalCol.g, finalCol.b, finalAlpha);
                    
                    // Layer the chromatic glow
                    if (chromaticGlow.a > 0f)
                    {
                        pixelCol = Color.Lerp(pixelCol, chromaticGlow, chromaticGlow.a);
                    }
                    
                    tex.SetPixel(x, y, pixelCol);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private Texture2D GenerateGlowOutlineTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            float radius = h * 0.5f;
            for (int x = 0; x < w; x++)
            {
                float dist = 0f;
                if (x < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                }
                else if (x > w - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(w - radius, radius));
                }
                else
                {
                    dist = Mathf.Abs(y - radius);
                }

                // Draw a thin border outline at the edge of the capsule
                float edgeWidth = 1.5f;
                float borderDist = Mathf.Abs(dist - radius);
                if (borderDist <= edgeWidth)
                {
                    float alpha = 1f - (borderDist / edgeWidth);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * 0.65f));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private Texture2D GenerateGlossyCircleTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Vector2 center = new Vector2(w / 2f, h / 2f);
        float radius = w * 0.46f;
        
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                if (dist > radius)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    // Soft white frosted glass highlight for the active indicator
                    float normD = dist / radius;
                    float alpha = Mathf.Lerp(0.55f, 0.25f, normD); // soft center, faded edges
                    float edgeGlow = (normD > 0.88f) ? (normD - 0.88f) / 0.12f * 0.4f : 0f;
                    
                    Color finalCol = Color.white;
                    tex.SetPixel(x, y, new Color(finalCol.r, finalCol.g, finalCol.b, Mathf.Clamp01(alpha + edgeGlow)));
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private Texture2D GeneratePillTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = h / 2f;
        
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = GetDistanceToCorners(x, y, w, h, r);
                if (d > r)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private float GetDistanceToCorners(int x, int y, int w, int h, float r)
    {
        float insideXMin = r;
        float insideXMax = w - r;
        float insideYMin = r;
        float insideYMax = h - r;
        
        if (x >= insideXMin && x <= insideXMax || y >= insideYMin && y <= insideYMax)
        {
            return 0f;
        }
        
        float cornerX = x < insideXMin ? insideXMin : insideXMax;
        float cornerY = y < insideYMin ? insideYMin : insideYMax;
        
        return Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY));
    }
}
