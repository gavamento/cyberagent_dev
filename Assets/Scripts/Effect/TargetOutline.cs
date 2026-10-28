//===============================================================================-
//攻撃が届く敵の輪郭を点滅させるスクリプト    秋田蓮音
//===============================================================================-
using System.Collections.Generic;
using UnityEngine;

// HitboxTargetHighlighter が実行時に敵へ付ける。プレハブに置く必要はない
// 同じスプライトを8方向に少しずらした単色コピーを後ろに置いて、輪郭に見せる
public class TargetOutline : MonoBehaviour
{
    private static readonly Vector2[] Directions =
    {
        new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        new Vector2(0.7071f, 0.7071f), new Vector2(-0.7071f, 0.7071f),
        new Vector2(0.7071f, -0.7071f), new Vector2(-0.7071f, -0.7071f),
    };

    // 元のスプライトと、その輪郭用コピー
    private class Entry
    {
        public SpriteRenderer source;
        public SpriteRenderer[] copies;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private Material material;
    private Color color;
    private float width;
    private float blinkSpeed;
    private float minAlpha;
    private float maxAlpha;
    private bool highlighted;
    private bool built;

    public void Setup(Material outlineMaterial, Color outlineColor, float outlineWidth,
                      float blinkPerSecond, float alphaMin, float alphaMax)
    {
        material = outlineMaterial;
        color = outlineColor;
        width = outlineWidth;
        blinkSpeed = blinkPerSecond;
        minAlpha = alphaMin;
        maxAlpha = alphaMax;
    }

    // 輪郭の色を変える(判定の色)。点滅は毎フレーム色を塗り直すので、次のフレームから反映される
    public void SetColor(Color outlineColor)
    {
        color = outlineColor;
    }

    public void SetHighlighted(bool on)
    {
        if (highlighted == on) return;
        highlighted = on;
        if (on && !built) Build();
        if (!on) Apply(0f);
    }

    // 最初に強調されたときだけコピーを作る
    private void Build()
    {
        built = true;
        foreach (var source in GetComponentsInChildren<SpriteRenderer>(true))
        {
            var entry = new Entry { source = source, copies = new SpriteRenderer[Directions.Length] };
            for (int i = 0; i < Directions.Length; i++)
            {
                // 元の子に置く。EffectManager が GetComponentInChildren で拾うのは先に見つかる元のスプライトになる
                var go = new GameObject("Outline", typeof(SpriteRenderer));
                go.transform.SetParent(source.transform, false);

                var sr = go.GetComponent<SpriteRenderer>();
                sr.sharedMaterial = material;
                sr.sortingLayerID = source.sortingLayerID;
                sr.sortingOrder = source.sortingOrder - 1; // 元のすぐ後ろ
                sr.enabled = false;
                entry.copies[i] = sr;
            }
            entries.Add(entry);
        }
    }

    // アニメーションや向きの変化に合わせるため、毎フレーム元から写す
    private void LateUpdate()
    {
        if (!highlighted) return;

        // ヒットストップ中も点滅が止まらないよう実時間で
        float wave = Mathf.PingPong(Time.unscaledTime * blinkSpeed * 2f, 1f);
        Apply(Mathf.Lerp(minAlpha, maxAlpha, wave));
    }

    // alpha が0なら非表示
    private void Apply(float alpha)
    {
        Color c = color;
        c.a = color.a * alpha;

        foreach (var entry in entries)
        {
            var source = entry.source;
            bool show = alpha > 0f && source != null && source.enabled && source.sprite != null
                        && source.gameObject.activeInHierarchy;

            // ワールドでの太さをローカルへ直す(親が拡大されていても太さを揃える)
            Vector3 lossy = source != null ? source.transform.lossyScale : Vector3.one;
            float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : Mathf.Abs(lossy.x);
            float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : Mathf.Abs(lossy.y);

            for (int i = 0; i < entry.copies.Length; i++)
            {
                var sr = entry.copies[i];
                if (sr == null) continue;
                sr.enabled = show;
                if (!show) continue;

                sr.sprite = source.sprite;
                sr.flipX = source.flipX;
                sr.flipY = source.flipY;
                sr.color = c;
                Vector2 d = Directions[i] * width;
                sr.transform.localPosition = new Vector3(d.x / sx, d.y / sy, 0f);
            }
        }
    }
}
