using System;
using UnityEngine;

public static class GameSettings
{
    private const string MusicKey = "Setting_MusicEnabled";
    private const string SoundKey = "Setting_SoundEnabled";
    private const string ControlKey = "Setting_ControlEnabled";

    public static event Action OnSettingsChanged;

    public static bool MusicEnabled
    {
        get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        set => SetBool(MusicKey, value);
    }

    public static bool SoundEnabled
    {
        get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
        set => SetBool(SoundKey, value);
    }

    public static bool ControlEnabled
    {
        get => PlayerPrefs.GetInt(ControlKey, 0) == 1;
        set => SetBool(ControlKey, value);
    }

    private static void SetBool(string key, bool value)
    {
        int nextValue = value ? 1 : 0;
        if (PlayerPrefs.GetInt(key, 1) == nextValue)
            return;

        PlayerPrefs.SetInt(key, nextValue);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }
}
