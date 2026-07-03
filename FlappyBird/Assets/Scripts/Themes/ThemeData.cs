using UnityEngine;

/// <summary>
/// Holds visual, coloring, and audio configuration assets for a single visual theme.
/// </summary>
[CreateAssetMenu(fileName = "NewTheme", menuName = "Flappy Bird/Theme Data")]
public class ThemeData : ScriptableObject
{
    [Header("General Settings")]
    public string themeName;
    [Tooltip("Camera clear color and background highlight colors for this theme.")]
    public Color themeColor = Color.white;

    [Header("Player Visuals")]
    [Tooltip("Wing flapping animation frames for the player. Set 1 sprite for a static frame.")]
    public Sprite[] playerSprites;

    [Header("Obstacle Visuals")]
    [Tooltip("Body of the top obstacle (e.g. upper goal post, upper asteroid pipe).")]
    public Sprite obstacleTopSprite;
    [Tooltip("Body of the bottom obstacle.")]
    public Sprite obstacleBottomSprite;
    [Tooltip("Cap/mouth of the top obstacle. Optional.")]
    public Sprite obstacleTopCapSprite;
    [Tooltip("Cap/mouth of the bottom obstacle. Optional.")]
    public Sprite obstacleBottomCapSprite;

    [Header("Environment Visuals")]
    [Tooltip("Background backdrop sprite (fits to screen size).")]
    public Sprite backgroundSprite;
    [Tooltip("Scrolling ground main dirt segment sprite.")]
    public Sprite groundDirtSprite;
    [Tooltip("Scrolling ground decorative grass/top surface sprite.")]
    public Sprite groundGrassSprite;

    [Header("Themed Audio (Optional)")]
    public AudioClip flapSound;
    public AudioClip scoreSound;
    public AudioClip hitSound;
    public AudioClip backgroundMusic;
}
