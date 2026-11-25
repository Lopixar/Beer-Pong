
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Bounce Sounds")]
    public AudioClip[] bounceClips;

    [Header("Other Sound Effects")]
    public AudioClip scoreClip;
    public AudioClip throwClip;
    public AudioClip winClip; // optional, played when the victory screen shows up
    public AudioClip loseClip; // optional, played when the player runs out of throws

    const string MUSIC_VOLUME = "MusicVolume";
    const string SFX_VOLUME = "SFXVolume";
    const string MUSIC_ENABLED = "MusicEnabled";
    const string SFX_ENABLED = "SFXEnabled";

    int lastBounceIndex = -1;

    public float MusicVolume =>
        PlayerPrefs.GetFloat(MUSIC_VOLUME, 0.5f);

    public float SFXVolume =>
        PlayerPrefs.GetFloat(SFX_VOLUME, 1f);

    public bool MusicEnabled =>
        PlayerPrefs.GetInt(MUSIC_ENABLED, 1) == 1;

    public bool SFXEnabled =>
        PlayerPrefs.GetInt(SFX_ENABLED, 1) == 1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (musicSource != null)
        {
            if (backgroundMusic != null)
                musicSource.clip = backgroundMusic;

            musicSource.loop = true;
            musicSource.volume = MusicVolume;
            musicSource.mute = !MusicEnabled;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = SFXVolume;
            sfxSource.mute = !SFXEnabled;
        }
    }

    void Start()
    {
        if (musicSource != null && musicSource.clip != null
            && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (musicSource != null)
            musicSource.volume = volume;

        PlayerPrefs.SetFloat(MUSIC_VOLUME, volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (sfxSource != null)
            sfxSource.volume = volume;

        PlayerPrefs.SetFloat(SFX_VOLUME, volume);
        PlayerPrefs.Save();
    }

    public void ToggleMusic(bool enabled)
    {
        if (musicSource != null)
            musicSource.mute = !enabled;

        PlayerPrefs.SetInt(MUSIC_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleSFX(bool enabled)
    {
        if (sfxSource != null)
            sfxSource.mute = !enabled;

        PlayerPrefs.SetInt(SFX_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null && !sfxSource.mute)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayBounce()
    {
        if (bounceClips == null || bounceClips.Length == 0)
            return;

        int validCount = 0;

        for (int i = 0; i < bounceClips.Length; i++)
        {
            if (bounceClips[i] != null && i != lastBounceIndex)
                validCount++;
        }

        if (validCount == 0)
            return;

        int selectedPosition = Random.Range(0, validCount);
        int selectedIndex = -1;

        for (int i = 0; i < bounceClips.Length; i++)
        {
            if (bounceClips[i] != null && i != lastBounceIndex)
            {
                if (selectedPosition == 0)
                {
                    selectedIndex = i;
                    break;
                }

                selectedPosition--;
            }
        }

        lastBounceIndex = selectedIndex;
        PlaySFX(bounceClips[selectedIndex]);
    }

    public void PlayScore()
    {
        PlaySFX(scoreClip);
    }

    public void PlayWin()
    {
        PlaySFX(winClip);
    }

    public void PlayLose()
    {
        PlaySFX(loseClip);
    }

    public void PlayThrow()
    {
        if (throwClip != null && sfxSource != null && !sfxSource.mute)
        {
            sfxSource.pitch = Random.Range(0.90f, 1.10f);
            sfxSource.PlayOneShot(throwClip);
            sfxSource.pitch = 1f;
        }
    }
}
