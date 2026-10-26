//===============================================================================-
//画面全体をガラスのように割りながら次のシーンへ切り替える    秋田蓮音
//===============================================================================-
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ScreenShatterTransition.Load("シーン名") で呼ぶ。シーンに置く必要はない
// 今の画面を撮って破片にし、破片の後ろで次のシーンを読み込む
public class ScreenShatterTransition : MonoBehaviour
{
    // 長さは 1080 の高さを基準にした値。実際の画面の高さに合わせて伸縮する
    private const int CrackCount = 7;
    private const float Jaggedness = 0.04f;
    private const float CrackGap = 14f;
    private const float CrackDuration = 0.25f;
    private const float ScatterDuration = 1.1f;
    private const float Gravity = 2600f;
    private const float ScatterSpeedMin = 150f;
    private const float ScatterSpeedMax = 600f;
    private const float UpwardBoost = 350f;
    private const float MaxAngularSpeed = 240f;
    private const float ShakeStrength = 16f;
    private const float ShakeDuration = 0.25f;

    private class Shard
    {
        public RectTransform rect;
        public Vector2 direction;
        public Vector2 velocity;
        public float angularSpeed;
    }

    private static ScreenShatterTransition running;

    // 読み込めないシーン名なら何もせず false を返す
    public static bool Load(string sceneName)
    {
        if (running != null) return true;
        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"ScreenShatterTransition: シーン \"{sceneName}\" が Build Settings にありません");
            return false;
        }

        var go = new GameObject("ScreenShatterTransition");
        DontDestroyOnLoad(go);
        running = go.AddComponent<ScreenShatterTransition>();
        running.StartCoroutine(running.TransitionRoutine(sceneName));
        return true;
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        // 描き終わった画面を撮る
        yield return new WaitForEndOfFrame();
        Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
        var sprite = Sprite.Create(capture, new Rect(0f, 0f, capture.width, capture.height), new Vector2(0.5f, 0.5f));

        // 1 単位 = 1 ピクセルの Canvas にして、撮った画像とぴったり重ねる
        var canvasGo = new GameObject("ShatterCanvas", typeof(Canvas), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 他の UI より手前
        var group = canvasGo.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var rootGo = new GameObject("Shards", typeof(RectTransform));
        var root = (RectTransform)rootGo.transform;
        root.SetParent(canvasGo.transform, false);

        var size = new Vector2(capture.width, capture.height);
        float scale = size.y / 1080f;
        var shards = CreateShards(root, sprite, size, scale);

        // 破片が画面を覆っている間に切り替える
        SceneManager.LoadScene(sceneName);

        // 揺れながらひびのすき間が開く
        var basePositions = new Vector2[shards.Count];
        for (int i = 0; i < shards.Count; i++) basePositions[i] = shards[i].rect.anchoredPosition;
        float time = 0f;
        while (time < CrackDuration)
        {
            time += Time.unscaledDeltaTime;
            float gap = CrackGap * scale * Mathf.SmoothStep(0f, 1f, time / CrackDuration);
            for (int i = 0; i < shards.Count; i++)
                shards[i].rect.anchoredPosition = basePositions[i] + shards[i].direction * gap;

            float power = Mathf.Clamp01(1f - time / ShakeDuration);
            root.anchoredPosition = Random.insideUnitCircle * ShakeStrength * scale * power;
            yield return null;
        }
        root.anchoredPosition = Vector2.zero;

        // 落ちながら消える
        time = 0f;
        while (time < ScatterDuration)
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;
            foreach (var shard in shards)
            {
                shard.velocity.y -= Gravity * scale * dt;
                shard.rect.anchoredPosition += shard.velocity * dt;
                shard.rect.Rotate(0f, 0f, shard.angularSpeed * dt);
            }
            group.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, time / ScatterDuration));
            yield return null;
        }

        Destroy(sprite);
        Destroy(capture);
        running = null;
        Destroy(gameObject);
    }

    private List<Shard> CreateShards(RectTransform parent, Sprite sprite, Vector2 size, float scale)
    {
        Vector2 impact = new Vector2(Random.Range(-0.1f, 0.1f) * size.x, Random.Range(-0.1f, 0.1f) * size.y);

        var shards = new List<Shard>();
        foreach (var poly in CrackPattern.Build(size, CrackCount, Jaggedness, impact))
            shards.Add(CreateShard(parent, sprite, poly, size, impact, scale));
        return shards;
    }

    private Shard CreateShard(RectTransform parent, Sprite sprite, List<Vector2> poly, Vector2 size, Vector2 impact, float scale)
    {
        Vector2 centroid = Vector2.zero;
        foreach (var p in poly) centroid += p;
        centroid /= poly.Count;
        for (int i = 0; i < poly.Count; i++) poly[i] -= centroid;

        var go = new GameObject("ScreenShard", typeof(RectTransform), typeof(CanvasRenderer), typeof(UIShard));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = centroid;
        go.GetComponent<UIShard>().Setup(sprite, poly, size, -centroid);

        Vector2 direction = centroid - impact;
        direction = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
        return new Shard
        {
            rect = rect,
            direction = direction,
            velocity = (direction * Random.Range(ScatterSpeedMin, ScatterSpeedMax) + Vector2.up * UpwardBoost) * scale,
            angularSpeed = Random.Range(-MaxAngularSpeed, MaxAngularSpeed),
        };
    }
}
