//===============================================================================-
//プレイヤーのHPをハートで表示するスクリプト    秋田蓮音
//===============================================================================-
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Canvas 内の空の GameObject に付ける。スライダーの真下に HP の数だけハートを左から並べる
// HP が減ったら右端のハートがひびの入った破片に割れて飛び散る
public class PlayerHpUI : MonoBehaviour
{
    [Tooltip("未指定ならシーンから探す")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("配置")]
    [Tooltip("この UI の真下に並べる。未指定ならシーンの SliderScript を探す")]
    [SerializeField] private RectTransform placeBelow;
    [Tooltip("placeBelow の左下からのずれ")]
    [SerializeField] private Vector2 placeOffset = new Vector2(0f, -8f);

    [Header("ハート")]
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Vector2 heartSize = new Vector2(64f, 56f);
    [SerializeField] private float spacing = 8f;

    [Header("割れ方")]
    [Tooltip("中心から伸びるひびの本数。破片はこの 2 倍")]
    [SerializeField, Range(3, 12)] private int crackCount = 7;
    [Tooltip("ひびのギザギザの強さ(ハートの幅に対する割合)")]
    [SerializeField, Range(0f, 0.2f)] private float jaggedness = 0.08f;

    [Header("割れる演出")]
    [Tooltip("被弾した瞬間に膨らんで震える時間(秒)")]
    [SerializeField] private float hitDuration = 0.12f;
    [SerializeField] private float hitScale = 1.3f;
    [SerializeField] private float shakeStrength = 6f;
    [Tooltip("ひびが入って破片の間にすき間が開く時間(秒)")]
    [SerializeField] private float crackDuration = 0.1f;
    [SerializeField] private float crackGap = 4f;
    [Tooltip("飛び散って消えるまでの時間(秒)")]
    [SerializeField] private float scatterDuration = 0.7f;
    [SerializeField] private Vector2 scatterSpeed = new Vector2(150f, 400f);
    [SerializeField] private float upwardBoost = 150f;
    [SerializeField] private float gravity = 1500f;
    [SerializeField] private float maxAngularSpeed = 540f;

    private class Shard
    {
        public RectTransform rect;
        public Vector2 direction; // ひびの中心から破片へ向かう向き
        public Vector2 velocity;
        public float angularSpeed;
    }

    private Image[] hearts;

    private void Start()
    {
        if (placeBelow == null)
        {
            var slider = FindAnyObjectByType<SliderScript>();
            if (slider != null) placeBelow = slider.transform as RectTransform;
        }

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHpUI: PlayerHealth が見つかりません", this);
            return;
        }

        CreateHearts(playerHealth.MaxHp);
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].gameObject.SetActive(i < playerHealth.CurrentHp);

        playerHealth.OnHpChanged += HandleHpChanged;
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHpChanged -= HandleHpChanged;
    }

    // Canvas の拡大率が決まる前は位置を計算できないので、毎フレーム合わせる
    private void LateUpdate()
    {
        PlaceBelowTarget();
    }

    // 自分の左上を placeBelow の左下に合わせる
    private void PlaceBelowTarget()
    {
        if (placeBelow == null) return;
        if (Mathf.Abs(transform.lossyScale.x) < 1e-6f || Mathf.Abs(transform.lossyScale.y) < 1e-6f) return;

        var rect = (RectTransform)transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);

        var corners = new Vector3[4];
        placeBelow.GetWorldCorners(corners); // [0] が左下
        rect.position = corners[0];
        rect.anchoredPosition += placeOffset;
    }

    // 破片も同じ親に置くので、LayoutGroup は使わず位置を自分で決める
    private void CreateHearts(int count)
    {
        hearts = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Heart{i}", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = heartSize;
            rect.anchoredPosition = HeartCenter(i);

            var image = go.GetComponent<Image>();
            image.sprite = heartSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            hearts[i] = image;
        }
    }

    private Vector2 HeartCenter(int index)
    {
        return new Vector2(index * (heartSize.x + spacing) + heartSize.x * 0.5f, -heartSize.y * 0.5f);
    }

    // preserveAspect で実際に描かれるハートの大きさ
    private Vector2 DrawSize()
    {
        if (heartSprite == null) return heartSize;
        float aspect = heartSprite.rect.width / heartSprite.rect.height;
        return heartSize.x / heartSize.y > aspect
            ? new Vector2(heartSize.y * aspect, heartSize.y)
            : new Vector2(heartSize.x, heartSize.x / aspect);
    }

    private void HandleHpChanged(int hp, int maxHp)
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            bool alive = i < hp;
            bool shown = hearts[i].gameObject.activeSelf;
            if (shown && !alive)
            {
                hearts[i].gameObject.SetActive(false);
                StartCoroutine(BreakRoutine(i));
            }
            else if (!shown && alive)
            {
                hearts[i].gameObject.SetActive(true);
            }
        }
    }

    // ハートは先に隠し、代わりの破片を動かす(回復されても演出と干渉しない)
    private IEnumerator BreakRoutine(int index)
    {
        var root = new GameObject("HeartBreak", typeof(RectTransform), typeof(CanvasGroup));
        root.layer = gameObject.layer;
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(transform, false);
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.sizeDelta = heartSize;
        Vector2 center = HeartCenter(index);
        rootRect.anchoredPosition = center;
        var group = root.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        var shards = CreateShards(rootRect);

        // 膨らみながら震える
        float time = 0f;
        while (time < hitDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / hitDuration);
            rootRect.localScale = Vector3.one * Mathf.Lerp(hitScale, 1f, t * t);
            rootRect.anchoredPosition = center + Random.insideUnitCircle * shakeStrength;
            yield return null;
        }
        rootRect.localScale = Vector3.one;
        rootRect.anchoredPosition = center;

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
    }

    // ひびの形は CrackPattern で作る
    private List<Shard> CreateShards(RectTransform parent)
    {
        Vector2 size = DrawSize();
        Vector2 impact = new Vector2(Random.Range(-0.12f, 0.12f) * size.x, Random.Range(-0.05f, 0.15f) * size.y);

        var shards = new List<Shard>();
        foreach (var poly in CrackPattern.Build(size, crackCount, jaggedness, impact))
            shards.Add(CreateShard(parent, poly, size, impact));
        return shards;
    }

    private Shard CreateShard(RectTransform parent, List<Vector2> poly, Vector2 size, Vector2 impact)
    {
        Vector2 centroid = Vector2.zero;
        foreach (var p in poly) centroid += p;
        centroid /= poly.Count;
        for (int i = 0; i < poly.Count; i++) poly[i] -= centroid;

        var go = new GameObject("HeartShard", typeof(RectTransform), typeof(CanvasRenderer), typeof(UIShard));
        go.layer = gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = centroid;
        var graphic = go.GetComponent<UIShard>();
        graphic.Setup(heartSprite, poly, size, -centroid);

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
}
