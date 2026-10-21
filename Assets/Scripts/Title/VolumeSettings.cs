using System;
using UnityEngine;

public enum VolumeCategory
{
    Bgm,
    Se,
}

// 全体 / BGM / SE の音量を保持し PlayerPrefs に保存する
public static class VolumeSettings
{
    const string MasterKey = "Volume_Master";
    const string BgmKey = "Volume_Bgm";
    const string SeKey = "Volume_Se";

    static float master = 1f;
    static float bgm = 1f;
    static float se = 1f;

    // BGM / SE の値が変わったときの通知
    public static event Action OnChanged;

    public static float Master
    {
        get => master;
        set
        {
            master = Mathf.Clamp01(value);
            AudioListener.volume = master;
            PlayerPrefs.SetFloat(MasterKey, master);
            OnChanged?.Invoke();
        }
    }

    public static float Bgm
    {
        get => bgm;
        set
        {
            bgm = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BgmKey, bgm);
            OnChanged?.Invoke();
        }
    }

    public static float Se
    {
        get => se;
        set
        {
            se = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SeKey, se);
            OnChanged?.Invoke();
        }
    }

    public static float Get(VolumeCategory category) => category == VolumeCategory.Bgm ? bgm : se;

    public static void Save() => PlayerPrefs.Save();

    // どのシーンから起動しても保存値を反映する
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 1f));
        bgm = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, 1f));
        se = Mathf.Clamp01(PlayerPrefs.GetFloat(SeKey, 1f));
        AudioListener.volume = master;
    }
}
