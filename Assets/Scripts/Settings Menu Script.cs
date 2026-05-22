using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SettingsMenuScript : MonoBehaviour
{
    public AudioMixer audioMixer;

    [Header("Volume Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Navigation Links")]
    public Button settingsBackButton;

    void Start()
    {
        // 1. Automatically wire up the D-pad paths and audio focus triggers at runtime
        ConfigureSettingsDpadRows();
        SetupControllerNavigation(musicSlider);
        SetupControllerNavigation(sfxSlider);

        // Load Saved Data
        float savedMusic = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVol", 0.75f);

        // Apply to UI
        if (musicSlider) musicSlider.value = savedMusic;
        if (sfxSlider) sfxSlider.value = savedSFX;

        // Initial Mixer Update
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);
    }

    // 2. Programmatically builds your explicit D-pad navigation grid layout
    private void ConfigureSettingsDpadRows()
    {
        if (musicSlider == null || sfxSlider == null || settingsBackButton == null)
            return;

        Navigation musicNav = new Navigation { mode = Navigation.Mode.Explicit };
        musicNav.selectOnDown = sfxSlider;
        musicSlider.navigation = musicNav;

        Navigation sfxNav = new Navigation { mode = Navigation.Mode.Explicit };
        sfxNav.selectOnUp = musicSlider;
        sfxNav.selectOnDown = settingsBackButton;
        sfxSlider.navigation = sfxNav;

        Navigation backNav = new Navigation { mode = Navigation.Mode.Explicit };
        backNav.selectOnUp = sfxSlider;
        settingsBackButton.navigation = backNav;
    }

    // 3. Dynamically injects mouse-to-controller sync hooks and acoustic audio triggers
    private void SetupControllerNavigation(Selectable element)
    {
        if (element == null) return;

        EventTrigger trigger = element.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = element.gameObject.AddComponent<EventTrigger>();

        // Mouse hover sync
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener((data) => {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(element.gameObject);
        });
        trigger.triggers.Add(pointerEnter);

        // Controller focus highlight sound
        EventTrigger.Entry selectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        selectEntry.callback.AddListener((data) => {
            if (AudioManagerScript.Instance != null && AudioManagerScript.Instance.buttonHover != null)
                AudioManagerScript.Instance.PlaySFX(AudioManagerScript.Instance.buttonHover);
        });
        trigger.triggers.Add(selectEntry);
    }

    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVol", volume);

        // IMPROVEMENT: Synchronise manager baseline with UI changes
        if (AudioManagerScript.Instance != null)
        {
            AudioManagerScript.Instance.globalMusicVolume = volume;
            if (AudioManagerScript.Instance.musicSource != null)
            {
                AudioManagerScript.Instance.musicSource.volume = volume;
            }
        }

        if (audioMixer != null)
        {
            float vol = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
            // FIX: Targets "MusicVol" parameter instead of overwriting Master
            audioMixer.SetFloat("MusicVol", vol);
        }
        else
        {
            Debug.LogWarning("SettingsMenuScript: AudioMixer is unassigned in the inspector slot!");
        }
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("SFXVol", volume);

        // IMPROVEMENT: Instantly updates global volume values inside active pooled sound channels
        if (AudioManagerScript.Instance != null)
        {
            AudioManagerScript.Instance.globalSfxVolume = volume;
        }

        if (audioMixer != null)
        {
            float vol = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
            audioMixer.SetFloat("SFXVol", vol);
        }
    }
}
