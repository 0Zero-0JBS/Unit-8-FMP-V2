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

    private void ConfigureSettingsDpadRows()
    {
        if (musicSlider == null || sfxSlider == null || settingsBackButton == null)
            return;

        Navigation backNav = new Navigation { mode = Navigation.Mode.Explicit };
        backNav.selectOnUp = null;               
        backNav.selectOnDown = musicSlider;     
        backNav.selectOnLeft = null;
        backNav.selectOnRight = null;
        settingsBackButton.navigation = backNav;

        Navigation musicNav = new Navigation { mode = Navigation.Mode.Explicit };
        musicNav.selectOnUp = settingsBackButton; 
        musicNav.selectOnDown = sfxSlider;         
        musicNav.selectOnLeft = null;
        musicNav.selectOnRight = null;
        musicSlider.navigation = musicNav;

        Navigation sfxNav = new Navigation { mode = Navigation.Mode.Explicit };
        sfxNav.selectOnUp = musicSlider;        
        sfxNav.selectOnDown = null;               
        sfxNav.selectOnLeft = null;
        sfxNav.selectOnRight = null;
        sfxSlider.navigation = sfxNav;
    }

    private void SetupControllerNavigation(Selectable element)
    {
        if (element == null) return;

        EventTrigger trigger = element.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = element.gameObject.AddComponent<EventTrigger>();

        // Mouse hover sync
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener((data) => {
            float mouseX = Input.GetAxisRaw("Mouse X");
            float mouseY = Input.GetAxisRaw("Mouse Y");

            if (EventSystem.current != null && (Mathf.Abs(mouseX) > 0.05f || Mathf.Abs(mouseY) > 0.05f))
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(element.gameObject);
            }
        });
        trigger.triggers.Add(pointerEnter);
    }

    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVol", volume);

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
