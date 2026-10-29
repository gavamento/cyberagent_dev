/*
* ファイル名　BreakEffectParticle.cs
* タイトル　　撃破エフェクト(Particle System 版)
* 作成者　　　秋田蓮音
* 作成日　　　10月29日
* 更新日　　　10月29日
*/

using System.Collections.Generic;
using UnityEngine;

// BreakEffect.vfx を Particle System で作り直したもの。WebGL でも動く
// 飛び方・重力・空気抵抗・縮み方はプレハブの Particle System 側で設定してある
// ここでは粒の数・大きさ・速さの倍率と、スプライトから拾った色だけを決める
[RequireComponent(typeof(ParticleSystem))]
public class BreakEffectParticle : MonoBehaviour
{
    [Tooltip("一度に出す粒の数")]
    [SerializeField, Min(1)] private int count = 50;
    [Tooltip("粒の大きさ。VFX 版の ParticleSize と同じ")]
    [SerializeField, Min(0.0f)] private float particleSize = 0.2f;

    // 拾っておく色の数
    private const int PaletteSize = 256;
    // この割合より不透明なピクセルだけ色に使う
    private const float AlphaThreshold = 0.5f;
    // これより暗い色は使わない(全部暗いときだけ使う)
    private const float MinBrightness = 0.2f;

    // スプライトごとに拾った色をとっておく
    private static readonly Dictionary<Sprite, Color[]> paletteCache = new Dictionary<Sprite, Color[]>();

    private ParticleSystem ps;
    // プレハブに設定された初速の範囲。倍率はこれに掛ける
    private ParticleSystem.MinMaxCurve baseSpeed;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        baseSpeed = ps.main.startSpeed;
    }

    /// <summary>
    /// 粒をまとめて出す。sprite を渡すとその色の粒になり、null なら白
    /// </summary>
    /// <param name="sprite">色を拾うスプライト</param>
    /// <param name="size">粒の大きさ。null ならインスペクターの値</param>
    /// <param name="speedMultiplier">飛ぶ速さの倍率</param>
    public void Play(Sprite sprite, float? size = null, float speedMultiplier = 1.0f)
    {
        // startSpeedMultiplier は「2 つの値の間」モードだと最大値を上書きしてしまうので、範囲ごと作り直す
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(baseSpeed.constantMin * speedMultiplier, baseSpeed.constantMax * speedMultiplier);

        Color[] palette = GetPalette(sprite);
        var emitParams = new ParticleSystem.EmitParams { startSize = size ?? particleSize };

        // 色を 1 粒ずつ変えたいので 1 個ずつ出す
        for (int i = 0; i < count; i++)
        {
            emitParams.startColor = palette[Random.Range(0, palette.Length)];
            ps.Emit(emitParams, 1);
        }
    }

    // スプライトの不透明なピクセルから色を集める(1 スプライトにつき 1 回)
    private static Color[] GetPalette(Sprite sprite)
    {
        if (sprite == null) return new[] { Color.white };
        if (paletteCache.TryGetValue(sprite, out Color[] cached)) return cached;

        Texture2D source = sprite.texture;
        if (!source.isReadable)
        {
            Debug.LogWarning($"{source.name} の Read/Write が無効なので白い粒にします。Import Settings で有効にしてください");
            return new[] { Color.white };
        }

        Rect r = sprite.textureRect;
        Color[] pixels = source.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);

        var bright = new List<Color>();
        var dark = new List<Color>();
        foreach (Color c in pixels)
        {
            if (c.a <= AlphaThreshold) continue;

            Color opaque = new Color(c.r, c.g, c.b, 1.0f);
            if (c.r * 0.2f + c.g * 0.7f + c.b * 0.07f < MinBrightness) dark.Add(opaque);
            else bright.Add(opaque);
        }

        // 大きい画像でも持つ量が増えないよう、決まった数だけランダムに抜き出す
        List<Color> picked = bright.Count > 0 ? bright : dark;
        Color[] palette = new Color[picked.Count > 0 ? PaletteSize : 1];
        for (int i = 0; i < palette.Length; i++)
        {
            palette[i] = picked.Count > 0 ? picked[Random.Range(0, picked.Count)] : Color.white;
        }
        paletteCache[sprite] = palette;
        return palette;
    }
}
