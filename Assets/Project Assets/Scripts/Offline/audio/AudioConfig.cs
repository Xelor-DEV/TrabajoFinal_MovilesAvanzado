using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Configuration/Audio Config")]
public class AudioConfig : ScriptableObject
{
    [Header("Audio Mixer Settings")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Volume Settings")]
    [Range(0.001f, 1f), Tooltip("Controla el volumen de la música (0.001f = silencio, 1 = volumen máximo)")]
    [SerializeField] private float musicVolume = 0.5f;

    [Header("Volume Settings")]
    [Range(0.001f, 1f), Tooltip("Controla el volumen de los efectos de sonido (0.001f = silencio, 1 = volumen máximo)")]
    [SerializeField] private float sfxVolume = 0.5f;

    [Header("Volume Settings")]
    [Range(0.001f, 1f), Tooltip("Controla el volumen general del audio (0.001f = silencio, 1 = volumen máximo)")]
    [SerializeField] private float masterVolume = 0.5f;

    public AudioMixer AudioMixer
    {
        get
        {
            return audioMixer;
        }
    }

    public float MusicVolume
    {
        get
        {
            return musicVolume;
        }
        set
        {
            SetVolumeOfMusic(value);
        }
    }
    public float SfxVolume
    {
        get
        {
            return sfxVolume;
        }
        set
        {
            SetVolumeOfSfx(value);
        }
    }
    public float MasterVolume
    {
        get
        {
            return masterVolume;
        }
        set
        {
            SetVolumeOfMaster(value);
        }
    }

    public void SetVolumeOfMusic(float newVolume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(newVolume) * 20f);
        musicVolume = newVolume;
    }

    public void SetVolumeOfSfx(float newVolume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(newVolume) * 20f);
        sfxVolume = newVolume;
    }

    public void SetVolumeOfMaster(float newVolume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(newVolume) * 20f);
        masterVolume = newVolume;
    }

    public float GetMusicVolumeLinear()
    {
        audioMixer.GetFloat("MusicVolume", out float dbVolume);
        return Mathf.Pow(10f, dbVolume / 20f);
    }

    public float GetSfxVolumeLinear()
    {
        audioMixer.GetFloat("SFXVolume", out float dbVolume);
        return Mathf.Pow(10f, dbVolume / 20f);
    }

    public float GetMasterVolumeLinear()
    {
        audioMixer.GetFloat("MasterVolume", out float dbVolume);
        return Mathf.Pow(10f, dbVolume / 20f);
    }

    public void ApplyToMixer()
    {
        SetVolumeOfMusic(musicVolume);
        SetVolumeOfSfx(sfxVolume);
        SetVolumeOfMaster(masterVolume);
    }
}