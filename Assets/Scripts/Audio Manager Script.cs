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

    private static float savedBattleMusicTime = 0f;
    private float thrustVolumeModifier = 1.0f;

    [Header("UI Sounds")]
    public AudioClip buttonHover;
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
        }
        else
        {
            Destroy(transform.root.gameObject);
            return;
        }
    }

    void Start()
    {
        string activeScene = SceneManager.GetActiveScene().name;

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.volume = globalMusicVolume;
            musicSource.playOnAwake = false;

            if (activeScene == "Level")
            {
                if (musicSource.isPlaying && musicSource.clip != battleMusic)
                {
                    musicSource.Stop();
                }

                musicSource.clip = battleMusic;

                if (battleMusic != null)
                {
                    if (savedBattleMusicTime > 0f && savedBattleMusicTime < battleMusic.length)
                    {
                        musicSource.time = savedBattleMusicTime;
                    }
                }

                musicSource.Play();
            }
            else if ((activeScene == "Main Menu" || activeScene == "Tutorial") && mainMenuMusic != null)
            {
                if (musicSource.clip == mainMenuMusic && musicSource.isPlaying)
                {
                    return;
                }

                musicSource.Stop();
                musicSource.clip = mainMenuMusic;
                musicSource.Play();
            }
        }
        cachedSpawner = FindFirstObjectByType<AsteroidSpawnerScript>();
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

    void OnDestroy()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        if (activeScene == "Level" && musicSource != null && musicSource.clip == battleMusic && musicSource.isPlaying)
        {
            savedBattleMusicTime = musicSource.time;
        }
    }

    public void ResetBattleTimelineMemory()
    {
        savedBattleMusicTime = 0f;
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
        for (int i = 0; i < sfxPool.Length; i++)
        {
            if (sfxPool[i] != null && !sfxPool[i].isPlaying) return sfxPool[i];
        }
        return sfxPool[0];
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
