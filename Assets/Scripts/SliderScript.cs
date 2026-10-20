using UnityEngine;
using UnityEngine.UI;

public class SliderScript : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private float sliderValue;
    [SerializeField] private float sliderMin;
    [SerializeField] private float sliderMax;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slider.minValue = sliderMin;
        slider.maxValue = sliderMax;
    }

    // Update is called once per frame
    void Update()
    {
        slider.value = sliderValue;
    }

    public void SetSliderValue(int value)
    {
        sliderValue = value;
    }
}
