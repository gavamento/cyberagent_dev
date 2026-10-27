//===============================================================================-
//カメラ演出のずれ(揺れ・押し出し)を描画のあいだだけ乗せる常駐オブジェクト    秋田蓮音
//===============================================================================-
using System.Collections.Generic;
using UnityEngine;

// Add から自動で生成される。シーンに置く必要はない。
// 演出のスクリプトは LateUpdate で Add してずれを足し、ここが次のフレームの最初に引いて戻す。
// FixedUpdate・Update の間はずれのない位置になるので、前進限界や画面外の判定が演出に引っぱられない。
[DefaultExecutionOrder(-10000)]
public class CameraOffset : MonoBehaviour
{
    private static CameraOffset instance;

    private readonly List<(Transform target, Vector3 offset)> applied = new List<(Transform, Vector3)>();

    // target を offset だけずらす。次のフレームの最初に自動で戻る
    public static void Add(Transform target, Vector3 offset)
    {
        if (target == null || offset == Vector3.zero) return;
        if (instance == null)
        {
            var go = new GameObject(nameof(CameraOffset));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<CameraOffset>();
        }
        target.position += offset;
        instance.applied.Add((target, offset));
    }

    // 物理の更新がないフレームもあるので、FixedUpdate と Update の両方で戻す
    private void FixedUpdate() => RemoveAll();
    private void Update() => RemoveAll();
    private void OnDestroy() => RemoveAll();

    private void RemoveAll()
    {
        foreach (var (target, offset) in applied)
            if (target != null) target.position -= offset;
        applied.Clear();
    }
}
