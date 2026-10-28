//===============================================================================-
//拾ったコインの枚数を画面右上に出す    秋田蓮音
//===============================================================================-
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// シーンに置くだけで動く。表示用の Canvas は自分で作る
public class CoinCounterUI : MonoBehaviour
{
    [Tooltip("枚数の持ち主。未指定ならシーンから探す")]
    [SerializeField] private PlayerCombat playerCombat;

    [Header("文字")]
    [SerializeField] private string prefix = "COIN x ";
    [Tooltip("未指定なら組み込みフォントを使う")]
    [SerializeField] private Font font;
    [SerializeField] private int fontSize = 70;
    [SerializeField] private Color textColor = new Color(1f, 0.85f, 0.2f, 1f);
    [Tooltip("画面右上の角からの距離")]
    [SerializeField] private Vector2 margin = new Vector2(40f, 20f);

    [Header("取ったときの演出")]
    [SerializeField, Min(0.01f)] private float popDuration = 0.2f;
    [SerializeField] private float popScale = 1.4f;

    private Text text;
    private Coroutine popRoutine;

    private void Start()
    {
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<PlayerCombat>();

        BuildUI();
        SetCount(playerCombat != null ? playerCombat.Coins : 0);

        if (playerCombat == null)
        {
            Debug.LogWarning("CoinCounterUI: PlayerCombat が見つかりません", this);
            return;
        }
        playerCombat.OnCoinsChanged += HandleCoinsChanged;
    }

    private void OnDestroy()
    {
        if (playerCombat != null) playerCombat.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int coins)
    {
        SetCount(coins);
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopRoutine(text.rectTransform));
    }

    private void SetCount(int coins)
    {
        text.text = prefix + coins;
    }

    // 膨らんだ状態から元の大きさへ戻す
    private IEnumerator PopRoutine(RectTransform rect)
    {
        float time = 0f;
        while (time < popDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / popDuration);
            rect.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, 1f - (1f - t) * (1f - t));
            yield return null;
        }
        rect.localScale = Vector3.one;
        popRoutine = null;
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("CoinCounterCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 右上の角に右寄せで置く。膨らむ中心は文字の右端
        var textGo = new GameObject("CoinCount", typeof(RectTransform), typeof(Text));
        textGo.layer = gameObject.layer;
        var rect = (RectTransform)textGo.transform;
        rect.SetParent(canvasGo.transform, false);
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(600f, fontSize * 1.3f);
        rect.anchoredPosition = -margin;

        text = textGo.GetComponent<Text>();
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAnchor.UpperRight;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
    }
}
