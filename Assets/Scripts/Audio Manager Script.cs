using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManagerScript : MonoBehaviour
{
    public static AudioManagerScript Instance;

    [Header("Audio Configurations")]
    public AudioMixer audioMixer;

    [Header("Audio Sources")]
    public AudioSource musicSource;

    [Header("SFX Pooling System")]
    [SerializeField] private int sfxPoolSize = 20;
    private AudioSource[] sfxPool;

    [Header("Asteroid Sounds")]
    public AudioClip explosionSound;
    public AudioClip hitSound;

    [Header("Player Sounds")]
    public AudioClip laserSound;
    public AudioClip reloadSound;
    public AudioClip respawnSound;
    public AudioClip thrustSound;
    public AudioClip playerDeathSound;
    private Coroutine musicFadeCoroutine;

    [Header("Background Music")]
    public AudioClip mainMenuMusic;
    public AudioClip battleMusic;

    private static float savedMainMenuMusicTime = 0f;
    private static float savedBattleMusicTime = 0f;
    private float thrustVolumeModifier = 1.0f;

    [Header("UI Sounds")]
    public AudioClip buttonClick;
    public AudioClip scorePointSound;

    [Header("Global Settings")]
    [Range(0f, 1f)] public float globalSfxVolume = 0.8f;
    [Range(0f, 1f)] public float globalMusicVolume = 0.5f;
    public bool dynamicMusicSpeedScaling = false;

    private AsteroidSpawnerScript cachedSpawner;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
            InitializePool();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(transform.root.gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        SaveCurrentTrackTime(SceneManager.GetActiveScene().name);
    }

    private void SaveCurrentTrackTime(string currentSceneName)
    {
        if (musicSource == null || !musicSource.isPlaying) return;

        if ((currentSceneName == "Main Menu" || currentSceneName == "Tutorial") && musicSource.clip == mainMenuMusic)
        {
            savedMainMenuMusicTime = musicSource.time;
        }
        else if (currentSceneName == "Level" && musicSource.clip == battleMusic)
        {
            savedBattleMusicTime = musicSource.time;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string activeScene = scene.name;
        cachedSpawner = FindFirstObjectByType<AsteroidSpawnerScript>();

        if (musicSource == null) return;

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        if (activeScene == "Level")
        {
            if (musicSource.clip == mainMenuMusic && musicSource.isPlaying)
            {
                savedMainMenuMusicTime = musicSource.time;
            }

            musicSource.Stop();
            musicSource.clip = battleMusic;
            musicSource.volume = globalMusicVolume * thrustVolumeModifier;

            if (battleMusic != null)
            {
                if (savedBattleMusicTime > 0f && savedBattleMusicTime < battleMusic.length)
                {
                    musicSource.time = savedBattleMusicTime;
                }
                musicSource.Play();
            }
        }
        else if (activeScene == "Main Menu" || activeScene == "Tutorial")
        {
            if (musicSource.clip == mainMenuMusic && musicSource.isPlaying)
            {
                return;
            }

            if (musicSource.clip == battleMusic && musicSource.isPlaying)
            {
                savedBattleMusicTime = musicSource.time;
            }

            musicSource.Stop();
            musicSource.clip = mainMenuMusic;
            musicSource.volume = globalMusicVolume * thrustVolumeModifier;

            if (mainMenuMusic != null)
            {
                if (savedMainMenuMusicTime > 0f && savedMainMenuMusicTime < mainMenuMusic.length)
                {
                    musicSource.time = savedMainMenuMusicTime;
                }
                musicSource.Play();
            }
        }
    }

    void Update()
    {
        if (musicSource != null)
        {
            if (musicFadeCoroutine == null)
            {
                musicSource.volume = globalMusicVolume * thrustVolumeModifier;
            }

            if (dynamicMusicSpeedScaling && musicSource.isPlaying)
            {
                if (cachedSpawner == null)
                {
                    cachedSpawner = FindFirstObjectByType<AsteroidSpawnerScript>();
                }

                if (cachedSpawner != null)
                {
                    float targetPitch = Mathf.Clamp(cachedSpawner.GetSpeedMultiplier(), 1.0f, 1.5f);
                    musicSource.pitch = Mathf.MoveTowards(musicSource.pitch, targetPitch, Time.deltaTime * 0.1f);
                }
            }
        }
    }

    public void SetThrustVolume(bool isThrusting)
    {
        thrustVolumeModifier = isThrusting ? 1.1f : 0.5f;
    }

    public void ResetAllTimelineMemory()
    {
        savedBattleMusicTime = 0f;
        savedMainMenuMusicTime = 0f;
    }

    private void InitializePool()
    {
        sfxPool = new AudioSource[sfxPoolSize];

        UnityEngine.Audio.AudioMixerGroup[] sfxGroups = audioMixer != null ? audioMixer.FindMatchingGroups("SFXVol") : null;
        UnityEngine.Audio.AudioMixerGroup targetGroup = (sfxGroups != null && sfxGroups.Length > 0) ? sfxGroups[0] : null;

        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject poolObj = new GameObject($"SFX_Pool_Source_{i}");
            poolObj.transform.SetParent(transform);

            AudioSource source = poolObj.AddComponent<AudioSource>();
            source.ignoreListenerPause = true;
            if (targetGroup != null) source.outputAudioMixerGroup = targetGroup;
            sfxPool[i] = source;
        }
    }

    private AudioSource GetAvailableAudioSource()
    {
        AudioSource oldestSource = sfxPool[0];
        float maxTime = 0f;

        for (int i = 0; i < sfxPool.Length; i++)
        {
            if (sfxPool[i] == null) continue;

            // Found a genuinely free source
            if (!sfxPool[i].isPlaying) return sfxPool[i];

            // Track which source has progressed furthest into its track
            if (sfxPool[i].time > maxTime)
            {
                maxTime = sfxPool[i].time;
                oldestSource = sfxPool[i];
            }
        }

        // Pool is full, interrupt the oldest playing sound cleanly
        oldestSource.Stop();
        return oldestSource;
    }

    public void TransitionToMusic(AudioClip newTrack, float fadeDuration = 1.0f)
    {
        if (musicSource == null) return;
        if (musicSource.clip == newTrack && musicSource.isPlaying) return;

        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(CrossfadeMusicRoutine(newTrack, fadeDuration));
    }

    private IEnumerator CrossfadeMusicRoutine(AudioClip newClip, float duration)
    {
        float startVolume = musicSource.volume;

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }
        musicSource.volume = 0f;

        musicSource.clip = newClip;
        if (newClip != null)
        {
            musicSource.Play();

            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, globalMusicVolume * thrustVolumeModifier, t / duration);
                yield return null;
            }
            musicSource.volume = globalMusicVolume * thrustVolumeModifier;
        }
        else
        {
            musicSource.Stop();
        }

        musicFadeCoroutine = null;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableAudioSource();
        if (source != null)
        {
            source.pitch = 1.0f;
            source.clip = clip;
            source.volume = globalSfxVolume;
            source.Play();
        }
    }

    public void PlaySFXWithPitch(AudioClip clip, float minPitch = 0.85f, float maxPitch = 1.15f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableAudioSource();
        if (source != null)
        {
            source.clip = clip;
            source.volume = globalSfxVolume;
            source.pitch = Random.Range(minPitch, maxPitch);
            source.Play();
        }
    }

    public void PlayPlayerDeath() => PlaySFX(playerDeathSound);
    public void PlayPlayerRespawn() => PlaySFX(respawnSound);
    public void PlayPlayerReload() => PlaySFX(reloadSound);
    public void PlayAsteroidHit() => PlaySFXWithPitch(hitSound, 0.9f, 1.1f);
    public void PlayAsteroidExplosion() => PlaySFXWithPitch(explosionSound, 0.8f, 1.2f);
    public void PlayLaserFire() => PlaySFXWithPitch(laserSound, 0.95f, 1.05f);
    public void PlayScorePointSound() => PlaySFXWithPitch(scorePointSound, 0.95f, 1.05f);
}
