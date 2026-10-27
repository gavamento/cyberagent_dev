using UnityEngine;
using UnityEngine.UI;

public class SliderScript : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private float sliderValue;   // 目標の値(ここへ向かって動く)
    [SerializeField] private float sliderMin;
    [SerializeField] private float sliderMax;

    [Tooltip("スライダーが目標の値へ追いつく速さ(1秒あたりの増加量)。大きいほど一気に増える。0以下で即反映")]
    [SerializeField] private float fillSpeed = 60f;

    [Header("フィーバー演出")]
    [SerializeField] private GameObject feverEffect;

    private Color normalColor;

    // 最初の1フレームより前に、必ず空(最小値)の状態にしておく
    void Awake()
    {
        if (slider == null)
        {
            Debug.LogError("SliderScript: Slider が未設定です。インスペクターでSliderをドラッグしてください。", this);
            enabled = false;
            return;
        }
        // 最大値が最小値以下だと、いくら加算しても増えないので0〜100にする
        if (sliderMax <= sliderMin)
        {
            Debug.LogWarning($"SliderScript: Slider Max({sliderMax}) が Slider Min({sliderMin}) 以下です。0〜100として動かします。インスペクターで直してください。", this);
            sliderMin = 0f;
            sliderMax = 100f;
        }
        slider.minValue = sliderMin;
        slider.maxValue = sliderMax;
        sliderValue = sliderMin;
        slider.value = sliderMin;
        RemoveFillGap();
        RefreshFill();

        normalColor = slider.fillRect.GetComponent<Image>().color;
        feverEffect.SetActive(false);
    }

    // 標準のSliderは塗りの置き場(Fill Area)の端に余白があり、最大でも隙間ができる。余白を0にして枠いっぱいまで塗る
    private void RemoveFillGap()
    {
        if (slider.fillRect == null) return;

        if (slider.fillRect.parent is RectTransform fillArea)
        {
            fillArea.offsetMin = Vector2.zero;
            fillArea.offsetMax = Vector2.zero;
        }
        slider.fillRect.offsetMin = Vector2.zero;
        slider.fillRect.offsetMax = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        // スコアのカウントアップと同じように、目標の値までじわっと増える(減るときも同じ速さ)
        if (fillSpeed <= 0f)
            slider.value = sliderValue;
        else
            slider.value = Mathf.MoveTowards(slider.value, sliderValue, fillSpeed * Time.deltaTime);

        PlayerCombat combat = FindAnyObjectByType<PlayerCombat>();
        if (combat != null && combat.IsFever)
        {
            float hue = Mathf.Repeat(Time.time * 0.5f, 1.0f);
            slider.fillRect.GetComponent<Image>().color = Color.HSVToRGB(hue, 1.0f, 1.0f);
            feverEffect.SetActive(true);
        }
        else 
        {
            slider.fillRect.GetComponent<Image>().color = normalColor;
            feverEffect.SetActive(false);
        }


            RefreshFill();
    }

    // 最小値のときは、塗りの部分を消して真っ黒にする(Sliderは0でも塗りが少し残るため)
    private void RefreshFill()
    {
        if (slider.fillRect == null) return;
        bool show = slider.value > sliderMin + 0.0001f;
        if (slider.fillRect.gameObject.activeSelf != show)
            slider.fillRect.gameObject.SetActive(show);
    }

    public void SetSliderValue(float value)
    {
        sliderValue = Mathf.Clamp(value, sliderMin, sliderMax);
    }
}