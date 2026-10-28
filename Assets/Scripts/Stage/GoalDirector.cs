//===============================================================================-
//ゴール時の演出(カメラ・プレイヤー停止→GOAL表示→フェードアウト→リザルトへ)    秋田蓮音
//===============================================================================-
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Goal プレハブに付ける。同じ GameObject の Is Trigger の Collider2D にプレイヤーが触れたら始まる
// 表示用の Canvas はゴール時に自分で作る。時間は timeScale の影響を受けないものを使う
public class GoalDirector : MonoBehaviour
{
    [Tooltip("未指定ならシーンから探す")]
    [SerializeField] private AutoScrollCamera scrollCamera;

    [Header("文字")]
    [SerializeField] private string message = "GOAL";
    [Tooltip("未指定なら組み込みフォントを使う")]
    [SerializeField] private Font font;
    [SerializeField] private int fontSize = 200;
    [SerializeField] private Color textColor = new Color(1f, 0.85f, 0.2f, 1f);
    [Tooltip("出始めの大きさの倍率。ここから等倍へ縮む")]
    [SerializeField, Min(1f)] private float popScale = 1.6f;

    [Header("時間(秒)")]
    [Tooltip("文字が出きるまでの時間")]
    [SerializeField, Min(0f)] private float messageInTime = 0.4f;
    [Tooltip("文字を出してからフェードアウトを始めるまでの時間")]
    [SerializeField, Min(0f)] private float waitTime = 2f;
    [Tooltip("画面が塗りつぶされるまでの時間")]
    [SerializeField, Min(0f)] private float fadeOutTime = 1f;

    [Header("フェードアウト")]
    [SerializeField] private Color fadeColor = Color.black;

    [Header("遷移先")]
    [SerializeField] private string resultSceneName = "Result";

    private bool started;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (started) return;

        var player = other.GetComponentInParent<PlayerCombat>();
        if (player == null) return;

        var health = player.GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;

        started = true;
        StartCoroutine(GoalRoutine(player));
    }

    private IEnumerator GoalRoutine(PlayerCombat player)
    {
        StopCamera();
        StopPlayer(player);

        BuildUI(out Text text, out Image fade);

        // 大きく出して等倍へ縮めながら浮かび上がらせる
        var rt = text.rectTransform;
        float t = 0f;
        while (t < messageInTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / messageInTime);
            float eased = 1f - (1f - k) * (1f - k);
            rt.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, eased);
            text.color = new Color(textColor.r, textColor.g, textColor.b, textColor.a * k);
            yield return null;
        }
        rt.localScale = Vector3.one;
        text.color = textColor;

        yield return new WaitForSecondsRealtime(waitTime);

        // 画面全体を塗りつぶす
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            Color c = fadeColor;
            c.a = fadeColor.a * Mathf.Clamp01(t / fadeOutTime);
            fade.color = c;
            yield return null;
        }
        fade.color = fadeColor;

        GameResult.SetScore(player != null ? player.Score : 0, player != null ? player.Coins : 0);
        SceneController.ChangeScene(resultSceneName);
    }

    private void StopCamera()
    {
        if (scrollCamera == null) scrollCamera = FindAnyObjectByType<AutoScrollCamera>();
        if (scrollCamera == null) return;

        scrollCamera.enabled = false;
        var progression = scrollCamera.GetComponent<ScrollSpeedProgression>();
        if (progression != null) progression.enabled = false;
    }

    // 操作・移動・被ダメージを止める。画面外での死亡も起きないよう PlayerHealth ごと止める
    private void StopPlayer(PlayerCombat player)
    {
        var input = player.GetComponent<PlayerInput>();
        if (input != null) input.enabled = false;

        var highlighter = player.GetComponent<HitboxTargetHighlighter>();
        if (highlighter != null) highlighter.enabled = false;

        player.enabled = false;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.enabled = false;
            // 無敵中の点滅で消えたまま止まらないよう、表示を戻す
            foreach (var r in player.GetComponentsInChildren<SpriteRenderer>())
                r.enabled = true;
        }
    }

    private void BuildUI(out Text text, out Image fade)
    {
        var canvasGo = new GameObject("GoalCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 他の UI より手前

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 文字(フェードより奥に置き、最後は一緒に塗りつぶされる)
        var textGo = new GameObject("Message", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(canvasGo.transform, false);
        Stretch(textGo.GetComponent<RectTransform>());

        text = textGo.GetComponent<Text>();
        text.text = message;
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.color = new Color(textColor.r, textColor.g, textColor.b, 0f);

        // 全画面のフェード
        var fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fadeGo.transform.SetParent(canvasGo.transform, false);
        Stretch(fadeGo.GetComponent<RectTransform>());

        fade = fadeGo.GetComponent<Image>();
        fade.raycastTarget = false;
        fade.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
