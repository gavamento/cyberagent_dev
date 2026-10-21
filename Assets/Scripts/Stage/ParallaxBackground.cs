using System.Collections.Generic;
using UnityEngine;

// カメラの移動量に係数を掛けて背景を動かすパララックス。横方向は無限にループする
// Sprite が未指定の層は色付きの仮背景を自動生成する
[DefaultExecutionOrder(100)]
public class ParallaxBackground : MonoBehaviour
{
    public enum PlaceholderShape
    {
        Flat,
        Mountains,
        Hills,
    }

    [System.Serializable]
    public class Layer
    {
        public string name = "Layer";
        [Tooltip("背景画像。空なら仮背景を使う")]
        public Sprite sprite;
        [Tooltip("仮背景の色。画像を入れた場合は乗算される (白でそのまま)")]
        public Color color = Color.white;
        [Tooltip("カメラへの追従率。1 で画面に固定 (遠景)、0 で地形と同じ動き (近景)")]
        public Vector2 followRate = new Vector2(0.5f, 0.5f);
        [Tooltip("開始時のカメラ中心から見た層の高さ。仮背景は下端、画像はそのピボットの位置になる")]
        public float yOffset = -4f;
        [Tooltip("画像の拡大率")]
        [Min(0.01f)] public float scale = 1f;
        [Tooltip("描画順。地形 (0) より小さくする")]
        public int sortingOrder = -10;
        [Header("仮背景")]
        public PlaceholderShape shape = PlaceholderShape.Hills;
        [Tooltip("仮背景 1 枚の幅 (ユニット)")]
        [Min(1f)] public float placeholderWidth = 32f;
        [Tooltip("仮背景の高さ (ユニット)")]
        [Min(0.1f)] public float placeholderHeight = 6f;
        [Tooltip("仮背景の起伏の細かさ")]
        [Min(1)] public int placeholderWaves = 3;
    }

    [Tooltip("追従するカメラ。未指定なら Main Camera")]
    [SerializeField] Camera targetCamera;
    [SerializeField] Layer[] layers =
    {
        new Layer
        {
            name = "Sky", color = new Color(0.45f, 0.7f, 0.95f), followRate = new Vector2(1f, 1f),
            yOffset = -9f, sortingOrder = -30, shape = PlaceholderShape.Flat,
            placeholderHeight = 18f,
        },
        new Layer
        {
            name = "FarMountains", color = new Color(0.45f, 0.55f, 0.75f), followRate = new Vector2(0.8f, 0.9f),
            yOffset = -5f, sortingOrder = -20, shape = PlaceholderShape.Mountains,
            placeholderHeight = 9f, placeholderWaves = 4,
        },
        new Layer
        {
            name = "NearHills", color = new Color(0.3f, 0.6f, 0.4f), followRate = new Vector2(0.5f, 0.7f),
            yOffset = -5f, sortingOrder = -10, shape = PlaceholderShape.Hills,
            placeholderHeight = 6f, placeholderWaves = 3,
        },
    };

    class Runtime
    {
        public Layer layer;
        public Transform root;
        public List<Transform> tiles = new List<Transform>();
        public float tileWidth;
    }

    readonly List<Runtime> runtimes = new List<Runtime>();
    readonly List<Object> generated = new List<Object>();
    Vector3 cameraStart;
    bool dirty;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null)
        {
            Debug.LogError("パララックスの追従先カメラが見つかりません。", this);
            enabled = false;
            return;
        }
        cameraStart = targetCamera.transform.position;
        Build();
    }

    // 再生中に Inspector で変えた値を反映する
    void OnValidate()
    {
        if (Application.isPlaying) dirty = true;
    }

    void OnDestroy() => Clear();

    void LateUpdate()
    {
        if (targetCamera == null) return;
        if (dirty)
        {
            dirty = false;
            Build();
        }

        Vector3 cam = targetCamera.transform.position;
        Vector3 moved = cam - cameraStart;
        float halfView = targetCamera.orthographicSize * targetCamera.aspect;

        foreach (var rt in runtimes)
        {
            // 層全体の位置 = 開始位置 + カメラ移動量 × 追従率
            float layerX = cameraStart.x + moved.x * rt.layer.followRate.x;
            float layerY = cameraStart.y + rt.layer.yOffset + moved.y * rt.layer.followRate.y;
            rt.root.position = new Vector3(layerX, layerY, 0f);

            // 画面左端を覆う位置から並べ直してループさせる
            float w = rt.tileWidth;
            float first = layerX + Mathf.Floor((cam.x - halfView - layerX) / w) * w + w * 0.5f;
            for (int i = 0; i < rt.tiles.Count; i++)
                rt.tiles[i].position = new Vector3(first + w * i, layerY, 0f);
        }
    }

    void Build()
    {
        Clear();
        if (layers == null) return;
        float viewWidth = targetCamera.orthographicSize * targetCamera.aspect * 2f;

        foreach (var layer in layers)
        {
            if (layer == null) continue;
            var sprite = layer.sprite != null ? layer.sprite : CreatePlaceholder(layer);

            var root = new GameObject(layer.name).transform;
            root.SetParent(transform, false);
            generated.Add(root.gameObject);

            var rt = new Runtime { layer = layer, root = root };
            rt.tileWidth = Mathf.Max(0.01f, sprite.bounds.size.x * layer.scale);
            int count = Mathf.CeilToInt(viewWidth / rt.tileWidth) + 2;
            for (int i = 0; i < count; i++)
            {
                var tile = new GameObject($"{layer.name}_{i}", typeof(SpriteRenderer));
                tile.transform.SetParent(root, false);
                tile.transform.localScale = Vector3.one * layer.scale;
                var sr = tile.GetComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = layer.color;
                sr.sortingOrder = layer.sortingOrder;
                rt.tiles.Add(tile.transform);
            }
            runtimes.Add(rt);
        }
    }

    void Clear()
    {
        foreach (var obj in generated)
            if (obj != null) Destroy(obj);
        generated.Clear();
        runtimes.Clear();
    }

    // 白いシルエットの仮スプライトを作る。色は SpriteRenderer で付ける
    Sprite CreatePlaceholder(Layer layer)
    {
        const int texWidth = 512;
        int texHeight = Mathf.Clamp(Mathf.RoundToInt(texWidth * layer.placeholderHeight / layer.placeholderWidth), 4, 1024);
        var tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var pixels = new Color32[texWidth * texHeight];
        var clear = new Color32(255, 255, 255, 0);
        var solid = new Color32(255, 255, 255, 255);
        for (int x = 0; x < texWidth; x++)
        {
            float top = SurfaceHeight(layer, x / (float)texWidth) * texHeight;
            for (int y = 0; y < texHeight; y++)
                pixels[y * texWidth + x] = y < top ? solid : clear;
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        // 下端中央を基準にする
        var sprite = Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0f),
            texWidth / layer.placeholderWidth);
        sprite.name = layer.name + "_Placeholder";
        generated.Add(tex);
        generated.Add(sprite);
        return sprite;
    }

    // 0〜1 の横位置に対する稜線の高さ (0〜1)。整数周期の波なので左右がつながる
    static float SurfaceHeight(Layer layer, float u)
    {
        float k = Mathf.PI * 2f * layer.placeholderWaves;
        switch (layer.shape)
        {
            case PlaceholderShape.Mountains:
            {
                // 尖った峰 + 細かい起伏
                float peak = 1f - Mathf.Abs(Mathf.Sin(k * u * 0.5f));
                float detail = Mathf.Sin(k * u * 3f) * 0.05f;
                return Mathf.Clamp01(0.35f + peak * 0.55f + detail);
            }
            case PlaceholderShape.Hills:
            {
                float wave = Mathf.Sin(k * u) * 0.5f + Mathf.Sin(k * u * 2f + 1.3f) * 0.2f;
                return Mathf.Clamp01(0.6f + wave * 0.3f);
            }
            default:
                return 1f;
        }
    }
}
