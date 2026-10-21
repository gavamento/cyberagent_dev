using System;
using UnityEngine;
using UnityEngine.UI;

// 音量設定のポップアップ
public class SettingsPopup : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private Text masterValueText;
    [SerializeField] private Text bgmValueText;
    [SerializeField] private Text seValueText;
    [SerializeField] private Button closeButton;

    private Action onClosed;

    void Awake()
    {
        masterSlider.onValueChanged.AddListener(value => { VolumeSettings.Master = value; UpdateLabels(); });
        bgmSlider.onValueChanged.AddListener(value => { VolumeSettings.Bgm = value; UpdateLabels(); });
        seSlider.onValueChanged.AddListener(value => { VolumeSettings.Se = value; UpdateLabels(); });
        closeButton.onClick.AddListener(Close);
    }

    public void Open(Action closed)
    {
        onClosed = closed;
        gameObject.SetActive(true);
        masterSlider.SetValueWithoutNotify(VolumeSettings.Master);
        bgmSlider.SetValueWithoutNotify(VolumeSettings.Bgm);
        seSlider.SetValueWithoutNotify(VolumeSettings.Se);
        UpdateLabels();
    }

    public void Close()
    {
        VolumeSettings.Save();
        gameObject.SetActive(false);
        var closed = onClosed;
        onClosed = null;
        closed?.Invoke();
    }

    void UpdateLabels()
    {
        SetLabel(masterValueText, VolumeSettings.Master);
        SetLabel(bgmValueText, VolumeSettings.Bgm);
        SetLabel(seValueText, VolumeSettings.Se);
    }

    static void SetLabel(Text label, float value)
    {
        if (label != null) label.text = Mathf.RoundToInt(value * 100f).ToString();
    }
}
