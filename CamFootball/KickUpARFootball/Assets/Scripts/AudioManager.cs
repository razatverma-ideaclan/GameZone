using UnityEngine;

/// <summary>
/// Singleton audio manager. Assign AudioClips in the Inspector once you have
/// real sound files - the game works fine with these left empty for now.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Effects")]
    public AudioClip kickSound;
    public AudioClip scoreSound;
    public AudioClip gameOverSound;
    public AudioClip buttonClickSound;

    [Header("Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.3f;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume;
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void PlayKickSound()
    {
        PlaySfx(kickSound);
    }

    public void PlayScoreSound()
    {
        PlaySfx(scoreSound);
    }

    public void PlayGameOverSound()
    {
        PlaySfx(gameOverSound);
    }

    public void PlayButtonClick()
    {
        PlaySfx(buttonClickSound);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
