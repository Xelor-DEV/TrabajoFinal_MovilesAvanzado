using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class AudioManager : NonPersistentSingleton<AudioManager>
{
    // Audio Sources
    [Header("Music Audio Source")]
    [SerializeField] private AudioSource musicAudioSource;

    // Audio Clips
    [Header("Music Clips")]
    [SerializeField] private AudioClip[] musicClips;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip[] sfxClips;

    // Audio Configuration
    [Header("Audio Configuration")]
    [SerializeField] private AudioConfig audioConfig;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private int defaultMusicIndex = 0;

    // SFX Prefab
    [Header("SFX Settings")]
    [SerializeField] private GameObject sfxPrefab;

    // Fade Settings
    [Header("Fade Settings")]
    [Range(0.1f, 5.0f)]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private bool isFading = false;

    // Pause Settings
    [Header("Pause Settings")]
    [SerializeField] private bool isPaused = false;
    [SerializeField] private bool pauseSfxOnGamePause = true;

    [Header("Events")]
    [SerializeField] private UnityEvent OnStart;

    private AudioSource[] sfxSources;
    private float prePauseMusicVolume;
    private float prePauseMusicTime;

    public AudioConfig AudioConfig => audioConfig;
    public bool IsPaused
    {
        get
        {
            return isPaused;
        }
        set
        {
            isPaused = value;
        }
    }

    public AudioClip[] SfxClips
    {
        get
        {
            return sfxClips;
        }
    }

    private void Awake()
    {
        InitializeSfxSources();
    }

    private void Start()
    {
        if (playMusicOnStart == true && musicClips.Length > 0)
        {
            PlayMusic(defaultMusicIndex);
        }

        OnStart?.Invoke();
    }

    private void InitializeSfxSources()
    {
        sfxSources = new AudioSource[sfxClips.Length];
        for (int i = 0; i < sfxClips.Length; i++)
        {
            GameObject sfxInstance = Instantiate(sfxPrefab, transform);
            sfxSources[i] = sfxInstance.GetComponent<AudioSource>();
        }
    }

    // Music Playback Methods
    public void PlayMusic(int index)
    {
        if (isFading || index < 0 || index >= musicClips.Length) return;

        musicAudioSource.Stop();
        musicAudioSource.clip = musicClips[index];
        musicAudioSource.Play();
    }

    public void PlayMusicWithTransition(int index)
    {
        if (isFading || index < 0 || index >= musicClips.Length) return;

        StartCoroutine(FadeOutMusicAndPlayNew(index));
    }

    public void StopMusic()
    {
        if (musicAudioSource.isPlaying && !isFading)
        {
            musicAudioSource.Stop();
        }
    }

    public void StopMusicWithFade()
    {
        if (musicAudioSource.isPlaying && !isFading)
        {
            StartCoroutine(FadeOutMusic());
        }
    }

    private IEnumerator FadeOutMusicAndPlayNew(int newMusicIndex)
    {
        isFading = true;
        float currentVolume = audioConfig.GetMusicVolumeLinear();

        // Fade out
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / fadeDuration;
            audioConfig.SetVolumeOfMusic(Mathf.Lerp(currentVolume, 0.001f, normalizedTime));
            yield return null;
        }

        // Cambio de música
        musicAudioSource.Stop();
        musicAudioSource.clip = musicClips[newMusicIndex];
        musicAudioSource.Play();

        // Fade in
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / fadeDuration;
            audioConfig.SetVolumeOfMusic(Mathf.Lerp(0.001f, currentVolume, normalizedTime));
            yield return null;
        }

        isFading = false;
    }

    private IEnumerator FadeOutMusic()
    {
        isFading = true;
        float originalVolume = audioConfig.GetMusicVolumeLinear();
        float currentTime = 0;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            float newVolume = Mathf.Lerp(originalVolume, 0.001f, currentTime / fadeDuration);
            audioConfig.SetVolumeOfMusic(newVolume);
            yield return null;
        }

        musicAudioSource.Stop();
        audioConfig.SetVolumeOfMusic(originalVolume);
        isFading = false;
    }

    // SFX Playback Methods
    public void PlaySfx(int index)
    {
        if (index < 0 || index >= sfxClips.Length || isPaused) return;

        sfxSources[index].PlayOneShot(sfxClips[index]);
    }

    public AudioSource GetAudioSourceByIndex(int index)
    {
        if (index < 0 || index >= sfxSources.Length)
        {
            Debug.LogWarning("Index out of bounds for SFX sources.");
            return null;
        }

        return sfxSources[index];
    }

    public void StopSfx(int index)
    {
        if (index < 0 || index >= sfxSources.Length) return;
        sfxSources[index].Stop();
    }

    public void StopSfxWithFade(int index)
    {
        if (index < 0 || index >= sfxSources.Length) return;
        StartCoroutine(FadeOutSfx(index));
    }

    private IEnumerator FadeOutSfx(int index)
    {
        AudioSource source = sfxSources[index];
        if (!source.isPlaying) yield break;

        float originalVolume = source.volume;
        float currentTime = 0;

        while (currentTime < fadeDuration && source.isPlaying)
        {
            currentTime += Time.deltaTime;
            source.volume = Mathf.Lerp(originalVolume, 0, currentTime / fadeDuration);
            yield return null;
        }

        source.Stop();
        source.volume = originalVolume;
    }

    // Pause System
    public void PauseAllAudio()
    {
        if (isPaused) return;

        isPaused = true;
        prePauseMusicTime = musicAudioSource.time;
        prePauseMusicVolume = audioConfig.GetMusicVolumeLinear();

        StartCoroutine(FadeAndPauseMusic());

        if (pauseSfxOnGamePause)
        {
            PauseAllSfx();
        }
    }

    public void UnpauseAllAudio(bool withFade = true)
    {
        if (!isPaused) return;

        isPaused = false;
        if (withFade)
        {
            StartCoroutine(FadeInAndUnpauseMusic());
        }
        else
        {
            musicAudioSource.Play();
            musicAudioSource.time = prePauseMusicTime;
            audioConfig.MusicVolume = prePauseMusicVolume;
        }

        if (pauseSfxOnGamePause)
        {
            UnpauseAllSfx();
        }
    }

    // SFX Pause Methods
    public void PauseAllSfx()
    {
        foreach (AudioSource source in sfxSources)
        {
            if (source.isPlaying)
            {
                source.Pause();
            }
        }
    }

    public void UnpauseAllSfx()
    {
        foreach (AudioSource source in sfxSources)
        {
            source.UnPause();
        }
    }

    // Music Pause Methods
    public void PauseMusic()
    {
        if (!musicAudioSource.isPlaying || isPaused) return;

        prePauseMusicTime = musicAudioSource.time;
        prePauseMusicVolume = audioConfig.GetMusicVolumeLinear();
        StartCoroutine(FadeAndPauseMusic());
    }

    public void UnpauseMusic(bool withFade = true)
    {
        if (musicAudioSource.isPlaying || isPaused) return;

        if (withFade)
        {
            StartCoroutine(FadeInAndUnpauseMusic());
        }
        else
        {
            musicAudioSource.Play();
            musicAudioSource.time = prePauseMusicTime;
            audioConfig.MusicVolume = prePauseMusicVolume;
        }
    }

    // Fade Coroutines
    private IEnumerator FadeAndPauseMusic()
    {
        float currentVolume = audioConfig.GetMusicVolumeLinear();

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / fadeDuration;
            audioConfig.SetVolumeOfMusic(Mathf.Lerp(currentVolume, 0.001f, normalizedTime));
            yield return null;
        }

        musicAudioSource.Pause();
    }

    private IEnumerator FadeInAndUnpauseMusic()
    {
        musicAudioSource.UnPause();
        musicAudioSource.time = prePauseMusicTime;
        audioConfig.MusicVolume = 0.001f;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / fadeDuration;
            audioConfig.SetVolumeOfMusic(Mathf.Lerp(0.001f, prePauseMusicVolume, normalizedTime));
            yield return null;
        }
    }
}
