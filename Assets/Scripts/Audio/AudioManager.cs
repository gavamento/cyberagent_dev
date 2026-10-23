using System.Collections.Generic;
using UnityEngine;

// BGM / SE を鳴らす。初めて呼ばれたときに自動で作られ、シーンをまたいで残る
public class AudioManager : MonoBehaviour
{
    // 同じ SE がこの間隔より短く重なったら鳴らさない(音割れ防止)
    const float SameSeInterval = 0.05f;

    static AudioManager instance;

    AudioSource bgmSource;
    AudioSource seSource;
    float bgmScale = 1f;
    readonly Dictionary<AudioClip, float> lastSeTime = new Dictionary<AudioClip, float>();

    static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject(nameof(AudioManager));
                instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // Domain Reload を切っていても前回の参照を持ち越さない
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic() => instance = null;

    void Awake()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        seSource = gameObject.AddComponent<AudioSource>();
        seSource.playOnAwake = false;
    }

    void OnEnable() => VolumeSettings.OnChanged += ApplyBgmVolume;
    void OnDisable() => VolumeSettings.OnChanged -= ApplyBgmVolume;

    void ApplyBgmVolume() => bgmSource.volume = bgmScale * VolumeSettings.Bgm;

    // SE を 1 回鳴らす。clip が空なら何もしない
    public static void PlaySE(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        var self = Instance;
        float now = Time.unscaledTime;
        if (self.lastSeTime.TryGetValue(clip, out float last) && now - last < SameSeInterval) return;
        self.lastSeTime[clip] = now;
        self.seSource.PlayOneShot(clip, volumeScale * VolumeSettings.Se);
    }

    // BGM を流す。同じ曲が流れていれば頭から鳴らし直さない
    public static void PlayBGM(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        var self = Instance;
        self.bgmScale = volumeScale;
        self.ApplyBgmVolume();
        if (self.bgmSource.clip == clip && self.bgmSource.isPlaying) return;
        self.bgmSource.clip = clip;
        self.bgmSource.Play();
    }

    public static void StopBGM()
    {
        if (instance == null) return;
        instance.bgmSource.Stop();
        instance.bgmSource.clip = null;
    }
}
