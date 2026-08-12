using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Exposed Mixer Parameter Names")]
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string musicParam = "MusicVolume";
    [SerializeField] private string sfxParam = "SFXVolume";

    [Header("UI Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        InitializeSlider(masterSlider, masterParam, SetMasterVolume);
        InitializeSlider(musicSlider, musicParam, SetMusicVolume);
        InitializeSlider(sfxSlider, sfxParam, SetSFXVolume);
    }

    private void InitializeSlider(Slider slider, string paramName, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        if (slider == null) return;

        slider.minValue = 0.0001f;
        slider.maxValue = 1f;

        float savedValue = PlayerPrefs.GetFloat(paramName, 0.75f);
        slider.value = savedValue;
        SetMixerVolume(paramName, savedValue);

        slider.onValueChanged.AddListener(onValueChanged);
    }

    public void SetMasterVolume(float value) => SetMixerVolume(masterParam, value);
    public void SetMusicVolume(float value) => SetMixerVolume(musicParam, value);
    public void SetSFXVolume(float value) => SetMixerVolume(sfxParam, value);

    private void SetMixerVolume(string paramName, float sliderValue)
    {
        // Convert 0.0001 to 1 linear slider value to logarithmic -80dB to 0dB
        float dbValue = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        if (mainMixer != null)
        {
            mainMixer.SetFloat(paramName, dbValue);
        }
        PlayerPrefs.SetFloat(paramName, sliderValue);
    }
}