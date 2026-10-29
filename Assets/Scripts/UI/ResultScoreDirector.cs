//===============================================================================-
//リザルトのスコア演出(一の位から壁が割れて数字が出る→コインの内訳→クリックで割れてタイトルへ)    秋田蓮音
//===============================================================================-
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Result シーンに置くだけで動く。表示用の Canvas は自分で作る
// 時間は timeScale の影響を受けないものを使う
[RequireComponent(typeof(UIShatterEffect))]
public class ResultScoreDirector : MonoBehaviour
{
    [Header("スコア")]
    [Tooltip("0 埋めする桁数。スコアがこれより長ければ桁を増やす")]
    [SerializeField, Min(1)] private int digitCount = 6;
    [Tooltip("ゲームを通さず Result を直接再生したときに出すスコア")]
    [SerializeField, Min(0)] private int previewScore = 1230;

    [Header("コインボーナス")]
    [Tooltip("取ったコインの枚数から足すボーナス。SCORE はボーナス込みの合計を出す")]
    [SerializeField] private CoinBonus coinBonus = new CoinBonus();
    [Tooltip("ゲームを通さず Result を直接再生したときのコインの枚数")]
    [SerializeField, Min(0)] private int previewCoins = 12;
    [Tooltip("内訳の書式。{0} が枚数、{1} がボーナス")]
    [SerializeField] private string bonusFormat = "COIN x{0}   BONUS +{1:#,0}";
    [SerializeField] private int bonusFontSize = 56;
    [SerializeField] private float bonusGap = 30f;

    [Header("文字")]
    [Tooltip("未指定なら組み込みフォントを使う")]
    [SerializeField] private Font font;
    [SerializeField] private int digitFontSize = 150;
    [SerializeField] private Color digitColor = Color.white;
    [SerializeField] private string label = "SCORE";
    [SerializeField] private int labelFontSize = 80;
    [SerializeField] private float labelGap = 30f;
    [SerializeField] private string hint = "CLICK TO TITLE";
    [SerializeField] private int hintFontSize = 48;
    [SerializeField] private float hintGap = 60f;

    [Header("壁")]
    [Tooltip("未指定なら単色の板にする")]
    [SerializeField] private Sprite wallSprite;
    [SerializeField] private Color wallColor = new Color(0.45f, 0.4f, 0.38f);
    [SerializeField] private Vector2 wallSize = new Vector2(130f, 180f);
    [SerializeField] private float spacing = 12f;

    [Header("割れ方")]
    [Tooltip("中心から伸びるひびの本数。破片はこの 2 倍")]
    [SerializeField, Range(3, 12)] private int crackCount = 6;
    [Tooltip("ひびのギザギザの強さ(壁の幅に対する割合)")]
    [SerializeField, Range(0f, 0.2f)] private float jaggedness = 0.08f;

    [Header("時間(秒)")]
    [Tooltip("シーンが始まってから最初の壁が割れるまで")]
    [SerializeField, Min(0f)] private float startDelay = 0.8f;
    [Tooltip("ある桁が割れ始めてから次の桁が割れ始めるまで")]
    [SerializeField, Min(0f)] private float digitInterval = 0.35f;
    [Tooltip("割れる前に壁が震える時間")]
    [SerializeField, Min(0f)] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeStrength = 6f;
    [Tooltip("ひびが入って破片の間にすき間が開く時間")]
    [SerializeField, Min(0f)] private float crackDuration = 0.08f;
    [SerializeField] private float crackGap = 6f;
    [Tooltip("飛び散って消えるまでの時間")]
    [SerializeField, Min(0.01f)] private float scatterDuration = 0.8f;
    [Tooltip("数字が現れたときに膨らんで戻るまでの時間")]
    [SerializeField, Min(0.01f)] private float popDuration = 0.2f;
    [SerializeField] private float popScale = 1.4f;

    [Header("破片の飛び方")]
    [SerializeField] private Vector2 scatterSpeed = new Vector2(250f, 700f);
    [SerializeField] private float upwardBoost = 300f;
    [SerializeField] private float gravity = 2500f;
    [SerializeField] private float maxAngularSpeed = 540f;

    [Header("遷移先")]
    [SerializeField] private string titleSceneName = "Title";

    [Header("サウンド")]
    [SerializeField] private AudioClip breakSound;
    [SerializeField] private AudioClip breakEndSound;
    [SerializeField] private AudioClip BGM;

    private class Shard
    {
        public RectTransform rect;
        public Vector2 direction; // ひびの中心から破片へ向かう向き
        public Vector2 velocity;
        public float angularSpeed;
    }

    private RectTransform resultRoot; // 最後にまとめて割る範囲
    private Text[] digits;
    private Image[] walls;
    private Vector2[] slotCenters;
    private Text hintText;
    private Text bonusText; // コインが 0 枚なら null
    private Sprite shardSprite;
    private int runningBreaks;

    private void Start()
    {
        int baseScore = Mathf.Max(0, GameResult.HasScore ? GameResult.LastScore : previewScore);
        int coins = Mathf.Max(0, GameResult.HasScore ? GameResult.LastCoins : previewCoins);
        int bonus = coinBonus.Calculate(coins);
        int score = (int)System.Math.Min((long)baseScore + bonus, int.MaxValue);

        string text = score.ToString();
        text = text.PadLeft(Mathf.Max(digitCount, text.Length), '0');
        string bonusLine = coins > 0
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, bonusFormat, coins, bonus)
            : null;

        shardSprite = wallSprite != null ? wallSprite : CreatePlainSprite();
        BuildUI(text, bonusLine);
        StartCoroutine(ResultRoutine());
    }

    private IEnumerator ResultRoutine()
    {
        yield return new WaitForSecondsRealtime(startDelay);

        // 一の位(右端)から順に割る
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            StartCoroutine(BreakRoutine(i));
            AudioManager.PlaySE(breakSound);
            if (i > 0) yield return new WaitForSecondsRealtime(digitInterval);
        }
        while (runningBreaks > 0) yield return null;

        // 全桁が出てからコインの内訳を出す
        if (bonusText != null)
        {
            bonusText.gameObject.SetActive(true);
            AudioManager.PlayBGM(BGM);
            yield return PopRoutine((RectTransform)bonusText.transform);
        }

        // 演出中のクリックは無視し、終わってからのクリックだけ受け付ける
        hintText.gameObject.SetActive(true);
        yield return null;
        while (!Input.GetMouseButtonUp(0))
        {
            Color c = hintText.color;
            c.a = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * 4f));
            hintText.color = c;
            yield return null;
        }

        hintText.gameObject.SetActive(false);
        AudioManager.PlaySE(breakEndSound);
        AudioManager.StopBGM();
        GetComponent<UIShatterEffect>().Shatter(resultRoot, () => SceneController.ChangeScene(titleSceneName));
    }

    // 壁は先に隠し、代わりの破片を動かす
    private IEnumerator BreakRoutine(int index)
    {
        runningBreaks++;
        var wall = (RectTransform)walls[index].transform;
        Vector2 center = slotCenters[index];

        // 震える
        float time = 0f;
        while (time < shakeDuration)
        {
            time += Time.unscaledDeltaTime;
            wall.anchoredPosition = center + Random.insideUnitCircle * shakeStrength;
            yield return null;
        }
        wall.anchoredPosition = center;
        wall.gameObject.SetActive(false);

        var root = new GameObject("WallBreak", typeof(RectTransform), typeof(CanvasGroup));
        root.layer = gameObject.layer;
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(resultRoot, false);
        rootRect.sizeDelta = wallSize;
        rootRect.anchoredPosition = center;
        var group = root.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        var shards = CreateShards(rootRect);
        digits[index].gameObject.SetActive(true);
        StartCoroutine(PopRoutine((RectTransform)digits[index].transform));

        // ひびのすき間が開く
        var basePositions = new Vector2[shards.Count];
        for (int i = 0; i < shards.Count; i++) basePositions[i] = shards[i].rect.anchoredPosition;
        time = 0f;
        while (time < crackDuration)
        {
            time += Time.unscaledDeltaTime;
            float gap = crackGap * Mathf.SmoothStep(0f, 1f, time / crackDuration);
            for (int i = 0; i < shards.Count; i++)
                shards[i].rect.anchoredPosition = basePositions[i] + shards[i].direction * gap;
            yield return null;
        }

        // 飛び散りながら落ちて消える
        time = 0f;
        while (time < scatterDuration)
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;
            foreach (var shard in shards)
            {
                shard.velocity.y -= gravity * dt;
                shard.rect.anchoredPosition += shard.velocity * dt;
                shard.rect.Rotate(0f, 0f, shard.angularSpeed * dt);
            }
            group.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, time / scatterDuration));
            yield return null;
        }

        Destroy(root);
        runningBreaks--;
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
    }

    private List<Shard> CreateShards(RectTransform parent)
    {
        Vector2 impact = new Vector2(Random.Range(-0.15f, 0.15f) * wallSize.x, Random.Range(-0.15f, 0.15f) * wallSize.y);

        var shards = new List<Shard>();
        foreach (var poly in CrackPattern.Build(wallSize, crackCount, jaggedness, impact))
            shards.Add(CreateShard(parent, poly, impact));
        return shards;
    }

    private Shard CreateShard(RectTransform parent, List<Vector2> poly, Vector2 impact)
    {
        Vector2 centroid = Vector2.zero;
        foreach (var p in poly) centroid += p;
        centroid /= poly.Count;
        for (int i = 0; i < poly.Count; i++) poly[i] -= centroid;

        var go = new GameObject("WallShard", typeof(RectTransform), typeof(CanvasRenderer), typeof(UIShard));
        go.layer = gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = wallSize;
        rect.anchoredPosition = centroid;
        var graphic = go.GetComponent<UIShard>();
        graphic.color = wallColor;
        graphic.Setup(shardSprite, poly, wallSize, -centroid);

        Vector2 direction = centroid - impact;
        direction = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
        return new Shard
        {
            rect = rect,
            direction = direction,
            velocity = direction * Random.Range(scatterSpeed.x, scatterSpeed.y) + Vector2.up * upwardBoost,
            angularSpeed = Random.Range(-maxAngularSpeed, maxAngularSpeed),
        };
    }

    // 壁スプライトが無いときの白い板(色は wallColor で付ける)
    private static Sprite CreatePlainSprite()
    {
        var tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    // 破片も同じ親に置くので、LayoutGroup は使わず位置を自分で決める
    private void BuildUI(string scoreText, string bonusLine)
    {
        var canvasGo = new GameObject("ResultCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        int count = scoreText.Length;
        float rowWidth = count * wallSize.x + (count - 1) * spacing;
        float labelHeight = labelFontSize * 1.2f;
        float bonusHeight = bonusLine != null ? bonusFontSize * 1.2f : 0f;
        float bonusBlock = bonusLine != null ? bonusGap + bonusHeight : 0f;

        // ラベル・数字の行・コインの内訳をまとめた範囲。中心を画面中央に置く
        var rootGo = new GameObject("Result", typeof(RectTransform));
        rootGo.layer = gameObject.layer;
        resultRoot = (RectTransform)rootGo.transform;
        resultRoot.SetParent(canvasGo.transform, false);
        resultRoot.sizeDelta = new Vector2(rowWidth, labelHeight + labelGap + wallSize.y + bonusBlock);
        float rowY = -resultRoot.sizeDelta.y * 0.5f + bonusBlock + wallSize.y * 0.5f;

        var labelText = CreateText("Label", resultRoot, label, labelFontSize, new Vector2(rowWidth, labelHeight));
        ((RectTransform)labelText.transform).anchoredPosition = new Vector2(0f, resultRoot.sizeDelta.y * 0.5f - labelHeight * 0.5f);

        digits = new Text[count];
        walls = new Image[count];
        slotCenters = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            slotCenters[i] = new Vector2(-rowWidth * 0.5f + wallSize.x * 0.5f + i * (wallSize.x + spacing), rowY);

            // 数字は壁が割れるまで隠しておく
            digits[i] = CreateText($"Digit{i}", resultRoot, scoreText[i].ToString(), digitFontSize, wallSize);
            ((RectTransform)digits[i].transform).anchoredPosition = slotCenters[i];
            digits[i].gameObject.SetActive(false);
        }
        // 壁は数字より手前に描く
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Wall{i}", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(resultRoot, false);
            rect.sizeDelta = wallSize;
            rect.anchoredPosition = slotCenters[i];

            walls[i] = go.GetComponent<Image>();
            walls[i].sprite = shardSprite;
            walls[i].color = wallColor;
            walls[i].raycastTarget = false;
        }

        // コインの内訳は数字の行の下。最後は一緒に割る
        if (bonusLine != null)
        {
            bonusText = CreateText("CoinBonus", resultRoot, bonusLine, bonusFontSize, new Vector2(rowWidth, bonusHeight));
            ((RectTransform)bonusText.transform).anchoredPosition = new Vector2(0f, -resultRoot.sizeDelta.y * 0.5f + bonusHeight * 0.5f);
            bonusText.gameObject.SetActive(false);
        }

        // 案内は割らないので resultRoot の外に置く
        hintText = CreateText("Hint", (RectTransform)canvasGo.transform, hint, hintFontSize, new Vector2(1600f, hintFontSize * 1.5f));
        ((RectTransform)hintText.transform).anchoredPosition = new Vector2(0f, -resultRoot.sizeDelta.y * 0.5f - hintGap);
        hintText.gameObject.SetActive(false);
    }

    private Text CreateText(string name, RectTransform parent, string value, int size, Vector2 rectSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.layer = gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = rectSize;

        var text = go.GetComponent<Text>();
        text.text = value;
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = digitColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
