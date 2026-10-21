using UnityEngine;

// AudioSource に BGM / SE の音量設定を掛ける
[RequireComponent(typeof(AudioSource))]
public class VolumeTarget : MonoBehaviour
{
    [SerializeField] private VolumeCategory category = VolumeCategory.Se;

    private AudioSource source;
    private float baseVolume;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        baseVolume = source.volume;
    }

    void OnEnable()
    {
        VolumeSettings.OnChanged += Apply;
        Apply();
    }

    void OnDisable()
    {
        VolumeSettings.OnChanged -= Apply;
    }

    void Apply()
    {
        source.volume = baseVolume * VolumeSettings.Get(category);
    }
}
