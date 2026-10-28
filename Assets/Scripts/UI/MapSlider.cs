using UnityEngine;
using UnityEngine.UI;
//===============================================================================-
// ステージの進行度を見せるスライダー。  鍋坂明穂
//===============================================================================-

// スタート地点から、ゴールまでのどれくらい進んだかを、スライダーの塗りで表す。
// 既定ではゴールが左端なので、右端から左へオレンジ色に染まっていく。
// ゴールの画像は、スライダーの左端に子オブジェクト(Image)として置いておく。
// ゴールなしのエンドレスのステージでは、スライダーを隠す。
public class MapSlider : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("進行度を見せるスライダー。空なら同じオブジェクトから探す")]
    [SerializeField] private Slider slider;
    [Tooltip("進行度を測るプレイヤー。空ならシーンから自動で探す")]
    [SerializeField] private PlayerCombat player;
    [Tooltip("ゴールの位置を教えてもらうステージ生成。空ならシーンから自動で探す")]
    [SerializeField] private StageGenerator stage;

    [Header("見た目")]
    [Tooltip("ONなら、ゴールがスライダーの左端、スタートが右端になり、右から左へ染まる。OFFなら左から右へ染まる")]
    [SerializeField] private bool goalOnLeft = true;
    [Tooltip("進んだぶんの色。スライダーの Fill のImageにも適用する")]
    [SerializeField] private Color fillColor = new Color(1f, 0.55f, 0.1f);
    [Tooltip("ONなら、スライダーの背景と塗りを、角の丸くない四角形にする")]
    [SerializeField] private bool squareShape = true;

    [Header("ゴールのアイコン(ゴール側の端に出す)")]
    [Tooltip("ゴールの画像。空なら出さない(自分で子オブジェクトに「GoalIcon」という名前のImageを置いてもよい)")]
    [SerializeField] private Sprite goalIconSprite;
    [Tooltip("アイコンの大きさ(UIの単位)")]
    [SerializeField] private Vector2 goalIconSize = new Vector2(48f, 48f);
    [Tooltip("ゴール側の端の中央から見た、アイコンの位置のずれ。Xがプラスで中へ、マイナスで外へ(ゴールが左端のとき)")]
    [SerializeField] private Vector2 goalIconOffset = Vector2.zero;

    [Tooltip("ONなら、進行度をなめらかに追いかける。OFFならそのまま")]
    [SerializeField] private bool smooth = true;
    [Tooltip("なめらかに追いかける速さ。大きいほど素早い")]
    [Min(0f)][SerializeField] private float smoothRate = 10f;

    [Header("ゴールの位置")]
    [Tooltip("ゴールの中心より、この距離(ワールドの単位)手前を「着いた位置」にする。ゴールに触れた時点で止まる分を引いて、塗りが端まで届くようにする。ゴールに着いても背景が残るなら大きく、着く前に満タンになるなら小さくする")]
    [Min(0f)][SerializeField] private float goalReachMargin = 2f;

    [Header("確認用 (実行中の値)")]
    [Range(0f, 1f)][SerializeField] private float progress;

    private float startX;
    private float goalX;
    private bool ready;   // スタートとゴールの位置が決まったか
    private bool hidden;  // エンドレスなどで進行度が出せないとき

    private void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (slider == null) slider = GetComponentInChildren<Slider>();
        if (player == null) player = FindAnyObjectByType<PlayerCombat>();
        if (stage == null) stage = FindAnyObjectByType<StageGenerator>();

        if (slider == null)
        {
            Debug.LogWarning("MapSlider: Sliderが見つかりません。", this);
            enabled = false;
            return;
        }

        slider.interactable = false; // 操作はできない(表示専用)
        // 操作できない状態の色(半透明)に変わらないようにする。ハンドルが透けて見えるのを防ぐ
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = goalOnLeft ? Slider.Direction.RightToLeft : Slider.Direction.LeftToRight;
        slider.value = 0f;

        SetupLook();
        SetupGoalIcon();
    }

    // 塗りの色と、四角い形にする
    private void SetupLook()
    {
        FitToBackground();

        // 色は、アルファが0だと見えないので1にする
        Color color = fillColor;
        color.a = 1f;

        if (slider.fillRect != null)
        {
            var fill = slider.fillRect.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = color;
                if (squareShape) MakeSquare(fill);
            }
        }

        // 背景(Slider の子の「Background」)
        if (squareShape)
        {
            var bg = slider.transform.Find("Background");
            if (bg != null) MakeSquare(bg.GetComponent<Image>());
        }

        // ハンドルも透けないようにする
        if (slider.handleRect != null)
        {
            var handle = slider.handleRect.GetComponent<Image>();
            if (handle != null)
            {
                Color c = handle.color;
                c.a = 1f;
                handle.color = c;
            }
        }
    }

    // 標準のスライダーは、塗りの領域が背景より少し内側にあり、塗りも左右に少しはみ出す作りになっている。
    // そのせいで、開始時に塗りが少し見えたり、ゴールで背景の隙間が残る。
    // 塗りもハンドルの領域も、背景と同じ位置・大きさにそろえて、塗りが背景と同じ太さで、ぴったり進行度の幅になるようにする
    private void FitToBackground()
    {
        if (slider.fillRect != null)
        {
            if (slider.fillRect.parent is RectTransform fillArea) MatchBackground(fillArea);

            // 横の位置と幅は Slider が進行度に合わせて決める。縦と余白だけそろえる
            var fill = slider.fillRect;
            fill.anchorMin = new Vector2(fill.anchorMin.x, 0f);
            fill.anchorMax = new Vector2(fill.anchorMax.x, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        if (slider.handleRect != null && slider.handleRect.parent is RectTransform handleArea)
            StretchHandleArea(handleArea);
    }

    // ハンドルの領域は、横だけ背景の幅にそろえる。縦はスライダーいっぱいにして、ハンドルの絵がつぶれないようにする
    private void StretchHandleArea(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // rt を背景(Background)と同じ位置・大きさにする。背景が見つからなければ親いっぱいに広げる
    private void MatchBackground(RectTransform rt)
    {
        var bg = slider.transform.Find("Background") as RectTransform;
        if (bg != null)
        {
            rt.anchorMin = bg.anchorMin;
            rt.anchorMax = bg.anchorMax;
            rt.offsetMin = bg.offsetMin;
            rt.offsetMax = bg.offsetMax;
        }
        else
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    // スプライトを外すと、角の丸くない単色の四角になる
    private static void MakeSquare(Image image)
    {
        if (image == null) return;
        image.sprite = null;
        image.type = Image.Type.Simple;
    }

    // スライダーのゴール側の端に、ゴールの画像を置く
    private void SetupGoalIcon()
    {
        var existing = slider.transform.Find("GoalIcon");
        Image icon = existing != null ? existing.GetComponent<Image>() : null;

        if (icon == null)
        {
            if (goalIconSprite == null) return;
            var go = new GameObject("GoalIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(slider.transform, false);
            icon = go.GetComponent<Image>();
        }

        if (goalIconSprite != null) icon.sprite = goalIconSprite;
        icon.raycastTarget = false;
        icon.preserveAspect = true;

        // ゴール側の端の中央に付ける(ゴールが左なら左端、右なら右端)
        var rt = icon.rectTransform;
        float x = goalOnLeft ? 0f : 1f;
        rt.anchorMin = rt.anchorMax = new Vector2(x, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = goalIconSize;
        rt.anchoredPosition = new Vector2(goalOnLeft ? goalIconOffset.x : -goalIconOffset.x, goalIconOffset.y);
        rt.SetAsLastSibling(); // バーやハンドルより手前に出す
    }

    private void Start()
    {
        if (player != null) startX = player.transform.position.x;
    }

    private void Update()
    {
        if (player == null || stage == null) return;

        // ゴールの位置は、ステージ生成が終わってから聞く
        if (!ready)
        {
            if (!stage.TryGetGoalX(out goalX))
            {
                // エンドレスなど、ゴールがないステージでは出さない
                if (stage.IsEndless && !hidden)
                {
                    hidden = true;
                    slider.gameObject.SetActive(false);
                }
                return;
            }
            ready = true;
        }

        float range = goalX - goalReachMargin - startX;
        float target = range > 0.01f ? Mathf.Clamp01((player.transform.position.x - startX) / range) : 0f;

        progress = smooth
            ? Mathf.Lerp(progress, target, 1f - Mathf.Exp(-smoothRate * Time.unscaledDeltaTime))
            : target;
        slider.value = progress;
    }
}