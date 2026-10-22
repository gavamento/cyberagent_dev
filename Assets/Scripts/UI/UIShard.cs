//===============================================================================-
//スプライトの一部を多角形で切り出して描く UI 破片    秋田蓮音
//===============================================================================-
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

// 割れる演出用。points の形でスプライトを切り抜いて表示する
[RequireComponent(typeof(CanvasRenderer))]
public class UIShard : MaskableGraphic
{
    private Sprite sprite;
    private List<Vector2> points;
    private Vector2 spriteSize;   // 元スプライトを表示したときの大きさ
    private Vector2 spriteCenter; // 元スプライトの中心(自分のローカル座標)

    public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;

    // points は自分のローカル座標。凹んだ形でもよい
    public void Setup(Sprite sprite, List<Vector2> points, Vector2 spriteSize, Vector2 spriteCenter)
    {
        this.sprite = sprite;
        this.points = points;
        this.spriteSize = spriteSize;
        this.spriteCenter = spriteCenter;
        raycastTarget = false;
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (sprite == null || points == null || points.Count < 3) return;

        Vector4 uv = DataUtility.GetOuterUV(sprite);
        foreach (var p in points)
        {
            Vector2 t = (p - spriteCenter) / spriteSize + new Vector2(0.5f, 0.5f);
            var vertex = UIVertex.simpleVert;
            vertex.position = p;
            vertex.color = color;
            vertex.uv0 = new Vector2(Mathf.Lerp(uv.x, uv.z, t.x), Mathf.Lerp(uv.y, uv.w, t.y));
            vh.AddVert(vertex);
        }

        var tris = Triangulate(points);
        for (int i = 0; i + 2 < tris.Count; i += 3)
            vh.AddTriangle(tris[i], tris[i + 1], tris[i + 2]);
    }

    // 耳刈り取り法で三角形に分ける
    private static List<int> Triangulate(List<Vector2> pts)
    {
        var idx = new List<int>();
        for (int i = 0; i < pts.Count; i++) idx.Add(i);
        if (SignedArea(pts) < 0f) idx.Reverse();

        var tris = new List<int>();
        while (idx.Count > 3)
        {
            bool clipped = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int a = idx[(i + idx.Count - 1) % idx.Count], b = idx[i], c = idx[(i + 1) % idx.Count];
                if (Cross(pts[b] - pts[a], pts[c] - pts[b]) <= 1e-5f) continue; // 凹んだ角

                bool blocked = false;
                foreach (int j in idx)
                {
                    if (j == a || j == b || j == c) continue;
                    if (InTriangle(pts[j], pts[a], pts[b], pts[c])) { blocked = true; break; }
                }
                if (blocked) continue;

                tris.Add(a); tris.Add(b); tris.Add(c);
                idx.RemoveAt(i);
                clipped = true;
                break;
            }
            // 形が崩れていて切れないときは扇形で埋める
            if (!clipped)
            {
                for (int i = 1; i + 1 < idx.Count; i++) { tris.Add(idx[0]); tris.Add(idx[i]); tris.Add(idx[i + 1]); }
                return tris;
            }
        }
        tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]);
        return tris;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    private static float SignedArea(List<Vector2> pts)
    {
        float area = 0f;
        for (int i = 0; i < pts.Count; i++)
            area += Cross(pts[i], pts[(i + 1) % pts.Count]);
        return area * 0.5f;
    }

    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        return Cross(b - a, p - a) >= 0f && Cross(c - b, p - b) >= 0f && Cross(a - c, p - c) >= 0f;
    }
}
