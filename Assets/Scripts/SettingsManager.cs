using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer mainMixer;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("UI Panels")]
    public GameObject settingsPanel;

    [Header("Gameplay")]
    public Slider sensitivitySlider;
    public float defaultSensitivity = 2f;

    [Header("Graphics")]
    public TMPro.TMP_Dropdown resolutionDropdown;
    Resolution[] resolutions;

    private void Start()
    {
        LoadSettings();
        SetupResolutionDropdown();
    }

    public void SetMasterVolume(float volume)
    {
        if (mainMixer != null) mainMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetMusicVolume(float volume)
    {
        if (mainMixer != null) mainMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (mainMixer != null) mainMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }



    public void SetSensitivity(float sens)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sens);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + (int)resolutions[i].refreshRateRatio.value + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && 
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetResolution(int resIndex)
    {
        Resolution res = resolutions[resIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    private void LoadSettings()
    {
        // Audio
        float mVol = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        if (masterVolumeSlider != null) masterVolumeSlider.value = mVol;
        SetMasterVolume(mVol);

        float muVol = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        if (musicVolumeSlider != null) musicVolumeSlider.value = muVol;
        SetMusicVolume(muVol);

        float sVol = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sVol;
        SetSFXVolume(sVol);

        // Sensitivity
        float savedSens = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
        if (sensitivitySlider != null) sensitivitySlider.value = savedSens;
        SetSensitivity(savedSens);
    }

    public void SaveAndExit()
    {
        PlayerPrefs.Save();
        CloseSettings();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
}
