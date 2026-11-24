
using UnityEngine;
using UnityEngine.UI;

public class SettingsAudioUI : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;

    public Toggle musicToggle;
    public Toggle sfxToggle;

    void OnEnable()
    {
        if (AudioManager.Instance == null)
            return;

        musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);

        musicToggle.SetIsOnWithoutNotify(AudioManager.Instance.MusicEnabled);
        sfxToggle.SetIsOnWithoutNotify(AudioManager.Instance.SFXEnabled);

        musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);

        musicToggle.onValueChanged.AddListener(AudioManager.Instance.ToggleMusic);
        sfxToggle.onValueChanged.AddListener(AudioManager.Instance.ToggleSFX);
    }

    void OnDisable()
    {
        if (AudioManager.Instance == null)
            return;

        musicSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetMusicVolume);
        sfxSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetSFXVolume);

        musicToggle.onValueChanged.RemoveListener(AudioManager.Instance.ToggleMusic);
        sfxToggle.onValueChanged.RemoveListener(AudioManager.Instance.ToggleSFX);
    }
}
