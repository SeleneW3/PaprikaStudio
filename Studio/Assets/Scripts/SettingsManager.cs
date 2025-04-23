using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Volume Controls")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    
    [Header("Other Settings")]
    [SerializeField] private Toggle fullscreenToggle;
    
    private void Start()
    {
        InitializeSliders();
        AddListeners();
    }
    
    private void InitializeSliders()
    {
        // Initialize music volume slider
        if (musicVolumeSlider != null && SoundManager.Instance != null)
        {
            // Get initial value (could be stored in PlayerPrefs)
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.value = musicVolume;
        }
        
        // Initialize SFX volume slider
        if (sfxVolumeSlider != null && SoundManager.Instance != null)
        {
            // Get initial value (could be stored in PlayerPrefs)
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxVolumeSlider.value = sfxVolume;
        }
        
        // Initialize fullscreen toggle
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
        }
    }
    
    private void AddListeners()
    {
        // Add listener to music volume slider
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        // Add listener to SFX volume slider
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        
        // Add listener to fullscreen toggle
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }
    }
    
    private void OnMusicVolumeChanged(float volume)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicVolume(volume);
            
            // Save the setting
            PlayerPrefs.SetFloat("MusicVolume", volume);
            PlayerPrefs.Save();
            
            // Optional: Play a sound to demonstrate the new volume level
            // SoundManager.Instance.PlaySFX("UIClick");
        }
    }
    
    private void OnSFXVolumeChanged(float volume)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(volume);
            
            // Save the setting
            PlayerPrefs.SetFloat("SFXVolume", volume);
            PlayerPrefs.Save();
            
            // Play a sound to demonstrate the new volume level
            SoundManager.Instance.PlaySFX("UIClick");
        }
    }
    
    private void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        
        // Save the setting
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
} 