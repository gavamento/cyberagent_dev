/*
* ファイル名　EffectManager.cs
* タイトル　　エフェクトマネージャー
* 作成者　　　久保木幹太
* 作成日　　　10月21日
* 更新日　　　10月21日
*/

using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

public static class EffectManager
{
    // VFXGraphのプロパティ名
    private static readonly int SpriteTextureID = Shader.PropertyToID("SpriteTexture");
    private static readonly int ParticleSizeID = Shader.PropertyToID("ParticleSize");

    // パレットの色数
    private const int PaletteSize = 256;
    // これよりα値が高けりゃパレットに使う
    private const float AlphaThreshold = 0.5f;

    // 一度作ったパレットを使いまわすためのキャッシュ
    private static readonly Dictionary<Sprite, Texture2D> paletteCache = new Dictionary<Sprite, Texture2D>();

    /// <summary>
    /// スプライトの不透明ピクセルだけを集めたパレット画像を取得(初回のみ)
    /// </summary>
    /// <param name="sprite">使用するスプライト</param>
    private static Texture2D GetPalette(Sprite sprite)
    {
        // パレットがもう作られてたらそっちを使う
        if (paletteCache.TryGetValue(sprite, out Texture2D cached) && cached != null)
        {
            return cached;
        }

        Texture2D source = sprite.texture;
        
        // スプライト側の Read/Write にチェックが入ってないとピクセルが読めないから画像をそのまま返す
        if (!source.isReadable)
        {
            Debug.LogWarning($"{source.name} のRead/Writeが無効だよ。Import Settings で有効にしてね！");
            return source;
        }

        // スプライトの範囲だけを読み取り
        Rect r = sprite.textureRect;
        Color[] pixels = source.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);

        // 不透明なピクセルだけを集める
        List<Color> opaque = new List<Color>();
        foreach (Color c in pixels)
        {
            // α値が高いかどうか
            if (c.a > AlphaThreshold) opaque.Add(c);
        }

        // ランダムに抜き出してパレットを作る
        Color[] colors = new Color[PaletteSize];
        for (int i = 0; i < PaletteSize; i++)
        {
            // 不透明なピクセルが1つもなかったら白で埋める
            Color c = opaque.Count > 0 ? opaque[Random.Range(0, opaque.Count)] : Color.white;
            c.a = 1.0f; // 半透明のピクセルも不透明にする
            colors[i] = c;
        }

        // パレット用のテクスチャを作成
        Texture2D palette = new Texture2D(PaletteSize, 1, TextureFormat.RGBA32, false);
        palette.filterMode = FilterMode.Point;      // 隣の色と混ざらないようにする
        palette.wrapMode = TextureWrapMode.Clamp;   // UVが範囲外になっても端の色を使う
        palette.SetPixels(colors);
        palette.Apply();                            // 書き込んだ色をGPU側に反映

        // 次のためにキャッシュを保存
        paletteCache[sprite] = palette;
        return palette;
    }

    /// <summary>
    /// エフェクトを発生させる(スプライト指定 or スプライト指定なし)
    /// </summary>
    /// <param name="effectPrefab">生成するVFXのプレハブ</param>
    /// <param name="baseTransform">基準の座標</param>
    /// <param name="positionOffset">オフセット</param>
    /// <param name="particleSize">パーティクルのサイズ</param>
    /// <param name="targetSprite">使用するスプライト</param>
    public static void PlayEffect(
        GameObject effectPrefab, 
        Transform baseTransform,
        Vector3 positionOffset = default,
        float? particleSize = null,
        Sprite targetSprite = null)
    {
        if (effectPrefab == null || baseTransform == null) return;

        // 生成座標の計算
        Vector3 spawnPosition = baseTransform.position + positionOffset;

        // エフェクトの生成
        GameObject vfxObj = Object.Instantiate(effectPrefab, spawnPosition, Quaternion.identity);
        VisualEffect vfx = vfxObj.GetComponent<VisualEffect>();

        if (vfx == null) return;

        // サイズ指定があるならVFXに渡す
        if (particleSize.HasValue && vfx.HasFloat(ParticleSizeID))
        {
            vfx.SetFloat(ParticleSizeID, particleSize.Value);
        }

        // スプライトが指定されてたら、不透明な色だけを集めたテクスチャをVFXのプロパティに渡す
        // テクスチャのランダムな位置から色を読み取って、パーティクルにそれっぽい色を付けるため(VFXの方で処理してる)
        if (targetSprite != null && vfx.HasTexture(SpriteTextureID))
        {
            vfx.SetTexture(SpriteTextureID, GetPalette(targetSprite));
        }
    }

    /// <summary>
    /// エフェクトを発生させる(スプライト自動読み込み(自分or子オブジェクトのみ))
    /// </summary>
    /// <param name="effectPrefab">生成するVFXのプレハブ</param>
    /// <param name="target">基準の座標</param>
    /// <param name="positionOffset">オフセット</param>
    /// <param name="particleSize">パーティクルの大きさ</param>
    public static void PlayEffectWithOwnerSprite(
        GameObject effectPrefab,
        Transform target,
        Vector3 positionOffset = default,
        float? particleSize = null
        )
    {
        if (target == null) return;

        // 自分か子オブジェクトのSpriteRendererを探す
        SpriteRenderer sr = target.GetComponentInChildren<SpriteRenderer>();
        Sprite sprite = sr != null ? sr.sprite : null;

        // エフェクト生成
        PlayEffect(effectPrefab, target, positionOffset, particleSize, sprite);
    }
}