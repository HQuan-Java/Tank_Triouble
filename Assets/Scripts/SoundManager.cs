using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SoundType
{
    TankMoving,
    TankFire,
    TankHit,
    TankExplode,
    ItemPickup,
}
[Serializable]
public class Sound
{
    [field: SerializeField]
    public SoundType Type { get; private set; }
    [field: SerializeField]
    public AudioClip Clip { get; private set; }
    [field: SerializeField]
    [field: Range(0f, 1f)]
    public float Volume { get; private set; } = 1f;
}

public class SoundManager : MonoBehaviour
{
    [SerializeField] private List<Sound> sounds;
    [SerializeField] private AudioSource[] musicSources;
    private static SoundManager instance;
    public static SoundManager Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<SoundManager>();
            return instance;
        }
    }

    public void PlaySound(SoundType type, AudioSource source)
    {
        if (!GameSettings.SoundEnabled)
            return;

        var sound = sounds.FirstOrDefault(s => s.Type == type);
        if (sound != null && source != null)
        {
            source.PlayOneShot(sound.Clip, sound.Volume);
        }
    }

    public void PlaySoundAtPosition(SoundType type, Vector3 position)
    {
        if (!GameSettings.SoundEnabled)
            return;

        var sound = sounds.FirstOrDefault(s => s.Type == type);
        if (sound != null && sound.Clip != null)
        {
            AudioSource.PlayClipAtPoint(sound.Clip, position, sound.Volume);
        }
    }

    public Sound GetSound(SoundType type)
    {
        return sounds.FirstOrDefault(s => s.Type == type);
    }

    public AudioClip GetClip(SoundType type)
    {
        return GetSound(type)?.Clip;
    }

    private void Awake()
    {
        ApplyMusicMute();
    }

    private void OnEnable()
    {
        GameSettings.OnSettingsChanged += ApplyMusicMute;
    }

    private void OnDisable()
    {
        GameSettings.OnSettingsChanged -= ApplyMusicMute;
    }

    private void ApplyMusicMute()
    {
        if (musicSources == null)
            return;

        bool muteMusic = !GameSettings.MusicEnabled;
        foreach (var source in musicSources)
        {
            if (source != null)
                source.mute = muteMusic;
        }
    }
}