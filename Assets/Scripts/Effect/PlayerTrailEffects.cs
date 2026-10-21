//===============================================================================-
//プレイヤーの後方グローとトレイルを出すスクリプト    秋田蓮音
//===============================================================================-
using UnityEngine;

// Player に付ける。グローとトレイルは実行時に子オブジェクトとして作る
// 通常時は控えめに、攻撃(ダッシュ)中は強く出す
[RequireComponent(typeof(PlayerCombat))]
public class PlayerTrailEffects : MonoBehaviour
{
    [Header("共通")]
    [Tooltip("加算合成のマテリアル (PlayerEffectAdditive)")]
    [SerializeField] private Material additiveMaterial;
    [Tooltip("通常⇔ダッシュの切り替えの速さ。大きいほど素早く切り替わる")]
    [SerializeField, Min(0f)] private float blendSpeed = 12f;

    [Header("後方グロー")]
    [SerializeField] private Color glowColor = new Color(0.3f, 0.8f, 1f, 1f);
    [Tooltip("グローの付け根の位置(ローカル座標)。ここから左へ伸びる")]
    [SerializeField] private Vector2 glowOffset = new Vector2(-0.2f, 0f);
    [Tooltip("通常時のグローの長さと太さ(ローカル座標)")]
    [SerializeField] private Vector2 glowSize = new Vector2(1.2f, 0.8f);
    [Tooltip("通常時のグローの濃さ")]
    [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.35f;
    [Tooltip("ダッシュ中のグローの長さの倍率")]
    [SerializeField, Min(0f)] private float dashGlowLengthScale = 2f;
    [Tooltip("ダッシュ中のグローの濃さ")]
    [SerializeField, Range(0f, 1f)] private float dashGlowAlpha = 0.9f;
    [Tooltip("ゆらめきの強さ。0でゆらめかない")]
    [SerializeField, Range(0f, 0.5f)] private float glowFlicker = 0.08f;

    [Header("トレイル")]
    [SerializeField] private Color trailColor = new Color(0.3f, 0.8f, 1f, 1f);
    [Tooltip("トレイルの出る位置(ローカル座標)")]
    [SerializeField] private Vector2 trailOffset = Vector2.zero;
    [Tooltip("通常時の太さ")]
    [SerializeField, Min(0f)] private float trailWidth = 0.15f;
    [Tooltip("通常時の濃さ")]
    [SerializeField, Range(0f, 1f)] private float trailAlpha = 0.25f;
    [Tooltip("通常時に軌跡が残る時間(秒)")]
    [SerializeField, Min(0.01f)] private float trailTime = 0.25f;
    [Tooltip("ダッシュ中の太さ")]
    [SerializeField, Min(0f)] private float dashTrailWidth = 0.45f;
    [Tooltip("ダッシュ中の濃さ")]
    [SerializeField, Range(0f, 1f)] private float dashTrailAlpha = 0.85f;
    [Tooltip("ダッシュ中に軌跡が残る時間(秒)")]
    [SerializeField, Min(0.01f)] private float dashTrailTime = 0.35f;

    [Header("トレイルの流れ")]
    [Tooltip("オンなら画面基準で軌跡を後ろへ流す。オフなら普通のワールド基準")]
    [SerializeField] private bool flowOnScreen = true;
    [Tooltip("画面上で軌跡が後ろへ流れる速さ(ユニット/秒)")]
    [SerializeField, Min(0f)] private float flowSpeed = 8f;

    private PlayerCombat combat;
    private SpriteRenderer glowRenderer;
    private Transform glowTransform;
    private TrailRenderer trail;
    private Material trailMaterial;
    private Texture2D glowTexture;
    private Texture2D trailTexture;
    private Sprite glowSprite;

    private Transform cam;
    private Vector3 lastCamPos;
    private Vector3[] trailPoints = new Vector3[64];
    private float dashBlend; // 0=通常、1=ダッシュ

    private const int GlowTexWidth = 64;
    private const int GlowTexHeight = 32;

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();

        if (additiveMaterial == null)
        {
            Debug.LogWarning("PlayerTrailEffects: additiveMaterial が未設定です", this);
            enabled = false;
            return;
        }

        // プレイヤーの絵より後ろに描くため、先に描画順を調べておく
        GetBackOrder(out int layerId, out int order);

        CreateGlow(layerId, order - 1);
        CreateTrail(layerId, order - 2);
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
            lastCamPos = cam.position;
        }
    }

    private void OnEnable()
    {
        if (trail != null) trail.Clear();
        if (cam != null) lastCamPos = cam.position;
    }

    private void OnDestroy()
    {
        if (trailMaterial != null) Destroy(trailMaterial);
        if (glowSprite != null) Destroy(glowSprite);
        if (glowTexture != null) Destroy(glowTexture);
        if (trailTexture != null) Destroy(trailTexture);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        float target = combat.IsAttacking ? 1f : 0f;
        dashBlend = Mathf.Lerp(dashBlend, target, 1f - Mathf.Exp(-blendSpeed * dt));

        UpdateGlow();
        UpdateTrail();
        FlowTrail(dt);
    }

    private void UpdateGlow()
    {
        float flicker = 1f + (Mathf.PerlinNoise(Time.time * 12f, 0f) - 0.5f) * 2f * glowFlicker;

        float length = glowSize.x * Mathf.Lerp(1f, dashGlowLengthScale, dashBlend) * flicker;
        float thick = glowSize.y * flicker;

        // スプライトは 1 x 0.5 ユニットなので、その分を割って合わせる
        glowTransform.localScale = new Vector3(length, thick * 2f, 1f);

        Color c = glowColor;
        c.a = Mathf.Lerp(glowAlpha, dashGlowAlpha, dashBlend);
        glowRenderer.color = c;
    }

    private void UpdateTrail()
    {
        trail.time = Mathf.Lerp(trailTime, dashTrailTime, dashBlend);
        trail.widthMultiplier = Mathf.Lerp(trailWidth, dashTrailWidth, dashBlend);

        Color head = trailColor;
        head.a = Mathf.Lerp(trailAlpha, dashTrailAlpha, dashBlend);
        Color tail = head;
        tail.a = 0f;
        trail.startColor = head;
        trail.endColor = tail;
    }

    // 軌跡の点をカメラに合わせて動かしてから左へずらし、画面上で後ろへ流れて見せる
    private void FlowTrail(float dt)
    {
        if (cam == null) return;

        Vector3 camDelta = cam.position - lastCamPos;
        lastCamPos = cam.position;

        if (!flowOnScreen) return;

        int count = trail.positionCount;
        if (count == 0) return;

        if (trailPoints.Length < count)
            trailPoints = new Vector3[Mathf.NextPowerOfTwo(count)];

        count = trail.GetPositions(trailPoints);

        Vector3 shift = new Vector3(camDelta.x - flowSpeed * dt, camDelta.y, 0f);
        for (int i = 0; i < count; i++)
            trail.SetPosition(i, trailPoints[i] + shift);
    }

    // プレイヤーのスプライトのうち、一番後ろの描画順を返す
    private void GetBackOrder(out int layerId, out int order)
    {
        layerId = 0;
        order = 0;

        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) return;

        layerId = renderers[0].sortingLayerID;
        order = renderers[0].sortingOrder;
        foreach (var r in renderers)
            order = Mathf.Min(order, r.sortingOrder);
    }

    private void CreateGlow(int layerId, int order)
    {
        glowTexture = BuildGlowTexture();
        // 右端を基点にして左へ伸ばす
        glowSprite = Sprite.Create(glowTexture, new Rect(0, 0, GlowTexWidth, GlowTexHeight),
            new Vector2(1f, 0.5f), GlowTexWidth);

        var go = new GameObject("BackGlow");
        glowTransform = go.transform;
        glowTransform.SetParent(transform, false);
        glowTransform.localPosition = glowOffset;

        glowRenderer = go.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = glowSprite;
        glowRenderer.sharedMaterial = additiveMaterial;
        glowRenderer.sortingLayerID = layerId;
        glowRenderer.sortingOrder = order;
    }

    private void CreateTrail(int layerId, int order)
    {
        trailTexture = BuildTrailTexture();
        trailMaterial = new Material(additiveMaterial) { mainTexture = trailTexture };

        var go = new GameObject("Trail");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = trailOffset;

        trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = trailMaterial;
        trail.textureMode = LineTextureMode.Stretch;
        trail.alignment = LineAlignment.View;
        trail.minVertexDistance = 0.05f;
        trail.numCapVertices = 2;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sortingLayerID = layerId;
        trail.sortingOrder = order;
    }

    // 付け根(右)が濃く、先端(左)へ細く薄くなる光
    private static Texture2D BuildGlowTexture()
    {
        var tex = new Texture2D(GlowTexWidth, GlowTexHeight, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var pixels = new Color32[GlowTexWidth * GlowTexHeight];
        for (int y = 0; y < GlowTexHeight; y++)
        {
            float v = (y + 0.5f) / GlowTexHeight * 2f - 1f; // -1〜1
            for (int x = 0; x < GlowTexWidth; x++)
            {
                float u = (x + 0.5f) / GlowTexWidth; // 0=先端、1=付け根
                float halfHeight = Mathf.Lerp(0.25f, 1f, u);
                float across = Mathf.Clamp01(1f - (v / halfHeight) * (v / halfHeight));
                float along = Mathf.SmoothStep(0f, 1f, u) * Mathf.SmoothStep(0f, 1f, (1f - u) * 8f);
                float a = along * across * across;
                pixels[y * GlowTexWidth + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    // 幅方向に中心が濃いぼかし。縦(v)がトレイルの幅方向になる
    private static Texture2D BuildTrailTexture()
    {
        const int h = 32;
        var tex = new Texture2D(2, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var pixels = new Color32[2 * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h * 2f - 1f;
            float a = Mathf.Clamp01(1f - v * v);
            byte b = (byte)(a * a * 255f);
            pixels[y * 2] = pixels[y * 2 + 1] = new Color32(255, 255, 255, b);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
