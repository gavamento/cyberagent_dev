//===============================================================================-
//攻撃が当たって前へダッシュしたときにプレイヤーの残像を出すスクリプト    秋田蓮音
//===============================================================================-
using System.Collections.Generic;
using UnityEngine;

// Player に付ける。ダッシュ中に見た目のスプライトをコピーして置き、フェードさせる
// 残像はワールドに置くだけなので、カメラが進むと自然に後ろへ流れていく
[RequireComponent(typeof(PlayerCombat))]
public class PlayerAfterImage : MonoBehaviour
{
    [Tooltip("コピーする見た目。空ならAnimatorのオブジェクトか子から自動で探す")]
    [SerializeField] private SpriteRenderer source;
    [Tooltip("残像のマテリアル。加算合成 (PlayerEffectAdditive) にすると光って見える。空なら見た目と同じ")]
    [SerializeField] private Material material;
    [SerializeField] private Color color = new Color(0.3f, 0.8f, 1f, 1f);
    [Tooltip("出た瞬間の濃さ")]
    [SerializeField, Range(0f, 1f)] private float startAlpha = 0.6f;
    [Tooltip("消えるまでの時間(秒)")]
    [SerializeField, Min(0.01f)] private float fadeTime = 0.25f;
    [Tooltip("ダッシュが始まってから残像を出し続ける時間(秒)")]
    [SerializeField, Min(0f)] private float spawnDuration = 0.1f;
    [Tooltip("残像を出す間隔(秒)")]
    [SerializeField, Min(0.005f)] private float spawnInterval = 0.025f;
    [Tooltip("見た目の描画順からずらす量。マイナスで後ろに描く")]
    [SerializeField] private int sortingOffset = -3;

    private class Image
    {
        public SpriteRenderer renderer;
        public float age;
    }

    private PlayerCombat combat;
    private Transform container;
    private readonly List<Image> actives = new List<Image>();
    private readonly Stack<Image> pool = new Stack<Image>();
    private float spawnRemain;
    private float spawnTimer;

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();

        if (source == null)
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator != null) source = animator.GetComponent<SpriteRenderer>();
        }
        if (source == null) source = GetComponentInChildren<SpriteRenderer>();
        if (source == null)
        {
            Debug.LogWarning("PlayerAfterImage: コピーする SpriteRenderer が見つかりません", this);
            enabled = false;
            return;
        }

        // プレイヤーと一緒に動かないよう、子にはしない
        container = new GameObject("PlayerAfterImages").transform;
    }

    private void OnEnable()
    {
        if (combat != null) combat.OnForwardDash += HandleForwardDash;
    }

    private void OnDisable()
    {
        if (combat != null) combat.OnForwardDash -= HandleForwardDash;
        spawnRemain = 0f;
        for (int i = actives.Count - 1; i >= 0; i--) Release(i);
    }

    private void OnDestroy()
    {
        if (container != null) Destroy(container.gameObject);
    }

    private void HandleForwardDash(bool isCharge)
    {
        spawnRemain = spawnDuration;
        spawnTimer = 0f; // 最初の1枚はすぐ出す
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (spawnRemain > 0f)
        {
            spawnRemain -= dt;
            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                Spawn();
                spawnTimer = spawnInterval;
            }
        }

        for (int i = actives.Count - 1; i >= 0; i--)
        {
            var image = actives[i];
            image.age += dt;
            float remain = 1f - image.age / fadeTime;
            if (remain <= 0f)
            {
                Release(i);
                continue;
            }

            Color c = color;
            c.a = startAlpha * remain * remain;
            image.renderer.color = c;
        }
    }

    private void Spawn()
    {
        if (source.sprite == null) return;

        var image = pool.Count > 0 ? pool.Pop() : CreateImage();
        var sr = image.renderer;
        var t = sr.transform;
        t.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        t.localScale = source.transform.lossyScale;

        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.sharedMaterial = material != null ? material : source.sharedMaterial;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder + sortingOffset;
        Color c = color;
        c.a = startAlpha;
        sr.color = c;

        image.age = 0f;
        sr.gameObject.SetActive(true);
        actives.Add(image);
    }

    private Image CreateImage()
    {
        var go = new GameObject("AfterImage", typeof(SpriteRenderer));
        go.transform.SetParent(container, false);
        return new Image { renderer = go.GetComponent<SpriteRenderer>() };
    }

    private void Release(int index)
    {
        var image = actives[index];
        actives.RemoveAt(index);
        if (image.renderer != null) image.renderer.gameObject.SetActive(false);
        pool.Push(image);
    }
}
