//===============================================================================-
//四角を割れたガラスのような多角形に切り分ける    秋田蓮音
//===============================================================================-
using System.Collections.Generic;
using UnityEngine;

// 衝撃点から放射状のひびと、それをつなぐ横のひびで切り分ける
// 各ひびは途中に折れ点を入れてギザギザにし、隣り合う破片で同じ線を共有する
public static class CrackPattern
{
    // 中心 0・大きさ size の四角を切り分けた多角形を返す。破片は crackCount の 2 倍
    public static List<List<Vector2>> Build(Vector2 size, int crackCount, float jaggedness, Vector2 impact)
    {
        Vector2 half = size * 0.5f;
        float jag = size.x * jaggedness;

        int n = crackCount;
        float step = Mathf.PI * 2f / n;
        float start = Random.Range(0f, Mathf.PI * 2f);
        var angles = new float[n];
        var inner = new Vector2[n]; // 衝撃点と折れ点の間
        var bend = new Vector2[n];  // 横のひびとの交点
        var outer = new Vector2[n]; // 折れ点と端の間
        var end = new Vector2[n];   // 外枠との交点
        for (int i = 0; i < n; i++)
        {
            angles[i] = start + step * (i + Random.Range(-0.3f, 0.3f));
            var dir = new Vector2(Mathf.Cos(angles[i]), Mathf.Sin(angles[i]));
            var side = new Vector2(-dir.y, dir.x);
            float length = DistanceToEdge(impact, dir, half);
            float bendAt = length * Random.Range(0.35f, 0.6f);

            end[i] = impact + dir * length;
            bend[i] = impact + dir * bendAt + side * Random.Range(-jag, jag);
            inner[i] = Vector2.Lerp(impact, bend[i], 0.5f) + side * Random.Range(-jag, jag) * 0.5f;
            outer[i] = Vector2.Lerp(bend[i], end[i], 0.5f) + side * Random.Range(-jag, jag);
        }

        var polys = new List<List<Vector2>>();
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            var mid = Vector2.Lerp(bend[i], bend[j], 0.5f) + Random.insideUnitCircle * jag * 0.5f;

            polys.Add(new List<Vector2> { impact, inner[i], bend[i], mid, bend[j], inner[j] });

            var outerPoly = new List<Vector2> { bend[i], outer[i], end[i] };
            outerPoly.AddRange(CornersBetween(impact, angles[i], angles[j], half));
            outerPoly.Add(end[j]);
            outerPoly.Add(outer[j]);
            outerPoly.Add(bend[j]);
            outerPoly.Add(mid);
            polys.Add(outerPoly);
        }
        return polys;
    }

    // from から dir の向きに進んで、中心 0・半径 half の四角の端に当たるまでの距離
    private static float DistanceToEdge(Vector2 from, Vector2 dir, Vector2 half)
    {
        float tx = Mathf.Abs(dir.x) > 1e-5f ? ((dir.x > 0f ? half.x : -half.x) - from.x) / dir.x : float.MaxValue;
        float ty = Mathf.Abs(dir.y) > 1e-5f ? ((dir.y > 0f ? half.y : -half.y) - from.y) / dir.y : float.MaxValue;
        return Mathf.Min(tx, ty);
    }

    // from から見て angleA から angleB までの間(反時計回り)にある四角の角を、角度順に返す
    private static List<Vector2> CornersBetween(Vector2 from, float angleA, float angleB, Vector2 half)
    {
        float span = Mathf.Repeat(angleB - angleA, Mathf.PI * 2f);
        var found = new List<(float, Vector2)>();
        foreach (var corner in new[] { new Vector2(half.x, half.y), new Vector2(-half.x, half.y), new Vector2(-half.x, -half.y), new Vector2(half.x, -half.y) })
        {
            Vector2 d = corner - from;
            float offset = Mathf.Repeat(Mathf.Atan2(d.y, d.x) - angleA, Mathf.PI * 2f);
            if (offset > 0f && offset < span) found.Add((offset, corner));
        }
        found.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        var result = new List<Vector2>();
        foreach (var f in found) result.Add(f.Item2);
        return result;
    }
}
