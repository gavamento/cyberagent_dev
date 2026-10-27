//===============================================================================-
//剣士の見切りなどで使うスロー演出の時間の速さを管理する常駐オブジェクト    秋田蓮音
//===============================================================================-
using System.Collections.Generic;
using UnityEngine;

// Request から自動で生成される。シーンに置く必要はない。
// 依頼主が1つでもいる間はスローへ、いなくなったら通常へ、実時間でなめらかに戻す。
// timeScale はヒットストップと取り合わないよう HitStopRunner.SetBaseTimeScale 経由で変える。
[DefaultExecutionOrder(9000)]
public class SlowMotion : MonoBehaviour
{
    private static SlowMotion instance;

    // 入りは速く、戻りはゆっくり(1秒あたりの補間の強さ)
    private const float EnterRate = 14f;
    private const float ExitRate = 6f;

    private readonly Dictionary<Object, float> requests = new Dictionary<Object, float>();
    private readonly List<Object> removeBuffer = new List<Object>();
    private float current = 1f;
    private float defaultFixedDeltaTime;

    public static bool IsActive => instance != null && instance.current < 0.999f;

    // owner が Release するか Destroy されるまで、timeScale を scale へ近づける
    public static void Request(Object owner, float scale)
    {
        if (owner == null) return;
        if (instance == null)
        {
            var go = new GameObject(nameof(SlowMotion));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SlowMotion>();
        }
        instance.requests[owner] = Mathf.Clamp(scale, 0.05f, 1f);
    }

    public static void Release(Object owner)
    {
        if (instance != null && owner != null) instance.requests.Remove(owner);
    }

    private void Awake()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Update()
    {
        // Destroy された依頼主は外す
        removeBuffer.Clear();
        foreach (var owner in requests.Keys)
            if (owner == null) removeBuffer.Add(owner);
        foreach (var owner in removeBuffer) requests.Remove(owner);

        float target = 1f;
        foreach (float scale in requests.Values) target = Mathf.Min(target, scale);

        // 通常のまま何も依頼がなければ、他の時間操作に触れない
        if (target >= 1f && current >= 1f) return;

        float rate = target < current ? EnterRate : ExitRate;
        current += (target - current) * Mathf.Min(1f, Time.unscaledDeltaTime * rate);
        if (Mathf.Abs(target - current) < 0.005f) current = target;
        Apply(current);
    }

    private void Apply(float scale)
    {
        HitStopRunner.SetBaseTimeScale(scale);
        // スロー中も物理の更新回数を保ち、動きと入力の反応がカクつかないようにする
        Time.fixedDeltaTime = defaultFixedDeltaTime * scale;
    }

    private void OnDestroy()
    {
        if (current < 1f) Apply(1f);
        if (instance == this) instance = null;
    }
}
