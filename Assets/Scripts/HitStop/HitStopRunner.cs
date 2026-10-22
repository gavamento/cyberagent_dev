//===============================================================================-
//ヒットストップの時間停止とカメラの揺れを管理する常駐オブジェクト    秋田蓮音
//===============================================================================-
using UnityEngine;

// HitStop から自動で生成される。シーンに置く必要はない。
// 敵が当たった直後に Destroy されても止まったままにならないよう、処理は敵とは別のこのオブジェクトで行う。
// 他のスクリプトより後に動かして、カメラの位置が決まってから揺れを足す。
[DefaultExecutionOrder(10000)]
public class HitStopRunner : MonoBehaviour
{
    private static HitStopRunner instance;

    // 処理落ちした1フレームで停止時間が一気に減らないよう、1フレームに進める時間の上限
    private const float MaxFrameStep = 1f / 30f;

    private bool stopping;
    private float savedTimeScale = 1f;
    private float stopTimeScale;
    private float startTime;
    private float elapsed;
    private float totalDuration;
    private float shakeStrength;
    private int startFrame;

    private Transform shakenCamera;
    private Vector3 appliedOffset;

    public static void Request(float duration, float timeScale, float shake)
    {
        if (duration <= 0f) return;
        if (instance == null)
        {
            var go = new GameObject(nameof(HitStopRunner));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HitStopRunner>();
        }
        instance.Begin(duration, timeScale, shake);
    }

    private void Begin(float duration, float timeScale, float shake)
    {
        if (!stopping)
        {
            stopping = true;
            savedTimeScale = Time.timeScale;
            stopTimeScale = timeScale;
            startTime = Time.realtimeSinceStartup;
            startFrame = Time.frameCount;
            elapsed = 0f;
            totalDuration = duration;
            shakeStrength = shake;
        }
        else
        {
            // 停止中に重なったら重ねがけせず、長い・強いほうを採用する
            totalDuration = Mathf.Max(totalDuration, elapsed + duration);
            stopTimeScale = Mathf.Min(stopTimeScale, timeScale);
            shakeStrength = Mathf.Max(shakeStrength, shake);
        }
        Time.timeScale = stopTimeScale;
    }

    private void Update()
    {
        // 前フレームの揺れを戻してから、カメラのスクリプトに位置を計算させる
        RemoveOffset();
        if (!stopping) return;

        // 当たったフレームは途中から止まるので数えず、次のフレームから数える
        if (Time.frameCount > startFrame)
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep);
        if (elapsed >= totalDuration) End();
    }

    private void LateUpdate()
    {
        if (!stopping || shakeStrength <= 0f) return;
        var cam = Camera.main;
        if (cam == null) return;

        // 終わりに向けて揺れを弱める
        float remain = Mathf.Clamp01(1f - elapsed / Mathf.Max(totalDuration, 0.0001f));
        shakenCamera = cam.transform;
        appliedOffset = (Vector3)(Random.insideUnitCircle * shakeStrength * remain);
        shakenCamera.position += appliedOffset;
    }

    private void RemoveOffset()
    {
        if (shakenCamera != null) shakenCamera.position -= appliedOffset;
        shakenCamera = null;
        appliedOffset = Vector3.zero;
    }

    private void End()
    {
        stopping = false;
        Time.timeScale = savedTimeScale;
        Debug.Log($"ヒットストップ終了: {Time.realtimeSinceStartup - startTime:F3}秒 (設定 {totalDuration:F3}秒) / {Time.frameCount - startFrame}フレーム停止 (timeScale {stopTimeScale} → {savedTimeScale})");
    }

    private void OnDestroy()
    {
        RemoveOffset();
        if (stopping) End();
    }
}
