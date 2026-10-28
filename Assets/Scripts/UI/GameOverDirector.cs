//===============================================================================-
//プレイヤー死亡時の演出(カメラ停止→文字表示→フェードアウト→リザルトへ)    秋田蓮音
//===============================================================================-
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// シーンに置くだけで動く。表示用の Canvas は死亡時に自分で作る
// ヒットストップ中でも止まらないよう、時間は timeScale の影響を受けないものを使う
public class GameOverDirector : MonoBehaviour
{
    [Tooltip("未指定ならシーンから探す")]
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("未指定ならシーンから探す")]
    [SerializeField] private AutoScrollCamera scrollCamera;
    [Tooltip("リザルトへ渡すスコアの持ち主。未指定ならシーンから探す")]
    [SerializeField] private PlayerCombat playerCombat;

    [Header("文字")]
    [SerializeField] private string message = "YOU DIED";
    [Tooltip("未指定なら組み込みフォントを使う")]
    [SerializeField] private Font font;
    [SerializeField] private int fontSize = 160;
    [SerializeField] private Color textColor = Color.white;

    [Header("時間(秒)")]
    [Tooltip("文字が浮かび上がるまでの時間")]
    [SerializeField, Min(0f)] private float messageFadeInTime = 0.5f;
    [Tooltip("文字を出してからフェードアウトを始めるまでの時間")]
    [SerializeField, Min(0f)] private float waitTime = 2f;
    [Tooltip("画面が塗りつぶされるまでの時間")]
    [SerializeField, Min(0f)] private float fadeOutTime = 1f;

    [Header("フェードアウト")]
    [SerializeField] private Color fadeColor = Color.black;

    [Header("遷移先")]
    [SerializeField] private string resultSceneName = "Result";

    private bool started;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (scrollCamera == null)
            scrollCamera = FindAnyObjectByType<AutoScrollCamera>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<PlayerCombat>();

        if (playerHealth == null)
        {
            Debug.LogWarning("GameOverDirector: PlayerHealth が見つかりません", this);
            return;
        }

        playerHealth.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (started) return;
        started = true;
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        StopCamera();

        BuildUI(out Text text, out Image fade);

        // 文字を浮かび上がらせる
        yield return FadeAlpha(text, textColor, messageFadeInTime);

        yield return new WaitForSecondsRealtime(waitTime);

        // 画面全体を塗りつぶす
        yield return FadeAlpha(fade, fadeColor, fadeOutTime);

        GameResult.SetScore(playerCombat != null ? playerCombat.Score : 0, playerCombat != null ? playerCombat.Coins : 0);
        SceneController.ChangeScene(resultSceneName);
    }

    private void StopCamera()
    {
        if (scrollCamera == null) return;

        scrollCamera.enabled = false;
        var progression = scrollCamera.GetComponent<ScrollSpeedProgression>();
        if (progression != null) progression.enabled = false;
    }

    // alpha 0 から目標色の alpha まで上げる
    private IEnumerator FadeAlpha(Graphic graphic, Color target, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            Color c = target;
            c.a = target.a * Mathf.Clamp01(t / duration);
            graphic.color = c;
            yield return null;
        }
        graphic.color = target;
    }

    private void BuildUI(out Text text, out Image fade)
    {
        var canvasGo = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler));
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
