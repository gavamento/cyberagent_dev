//===============================================================================-
//攻撃が当たって前へダッシュしたときのカメラ演出    秋田蓮音
//===============================================================================-
using UnityEngine;

// Main Camera に付ける。ダッシュから少し遅れてカメラを前へ押し出し、背景を一気に流す
// AutoScrollCamera の後、ParallaxBackground の前に位置を足すため実行順を指定している
// 位置のずらし方は HitStopRunner と同じく、Update で前フレームの分を引き、LateUpdate で足す
[DefaultExecutionOrder(50)]
public class DashCameraEffect : MonoBehaviour
{
    [Tooltip("ダッシュを知らせてくるプレイヤー")]
    [SerializeField] private PlayerCombat player;
    [Tooltip("一気に流す背景。空なら背景は流さない")]
    [SerializeField] private ParallaxBackground background;
    [Tooltip("ため攻撃のときに演出の強さに掛ける倍率")]
    [SerializeField, Min(0f)] private float chargeMultiplier = 1.5f;

    [Header("追従の遅れ")]
    [Tooltip("ダッシュから何秒遅れてカメラが追いかけるか")]
    [SerializeField, Min(0f)] private float surgeDelay = 0.04f;
    [Tooltip("カメラが前へ押し出される距離(ユニット)。0で押し出さない")]
    [SerializeField, Min(0f)] private float surgeDistance = 0.8f;
    [Tooltip("押し出しきるまでの時間(秒)")]
    [SerializeField, Min(0f)] private float surgeTime = 0.12f;
    [Tooltip("元の位置へ戻るまでの時間(秒)。短すぎると戻るときにカメラが後ろへ下がって見える")]
    [SerializeField, Min(0.01f)] private float surgeReturnTime = 0.6f;

    [Header("背景")]
    [Tooltip("ダッシュ1回で背景を追加で流す距離(ユニット)")]
    [SerializeField, Min(0f)] private float backgroundBoost = 3f;

    private readonly Pulse surge = new Pulse();
    private Vector3 appliedOffset;

    private void OnEnable()
    {
        if (player == null)
        {
            Debug.LogWarning("DashCameraEffect: player が未設定です", this);
            return;
        }
        player.OnForwardDash += HandleForwardDash;
    }

    private void OnDisable()
    {
        if (player != null) player.OnForwardDash -= HandleForwardDash;
        RemoveOffset();
        surge.Reset();
    }

    private void HandleForwardDash(bool isCharge)
    {
        float power = isCharge ? chargeMultiplier : 1f;
        surge.Begin(surgeDistance * power, surgeDelay, surgeTime, surgeReturnTime);
        if (background != null) background.Boost(backgroundBoost * power);
    }

    private void Update()
    {
        // 前フレームの分を戻してから、AutoScrollCamera に位置を計算させる
        RemoveOffset();
    }

    private void LateUpdate()
    {
        surge.Tick(Time.deltaTime);

        appliedOffset = new Vector3(surge.Value, 0f, 0f);
        transform.position += appliedOffset;
    }

    private void RemoveOffset()
    {
        transform.position -= appliedOffset;
        appliedOffset = Vector3.zero;
    }

    // 待ち → 急に peak まで上がる → なめらかに 0 へ戻る値
    // 戻りきる前に次が来たら、今の値から上がり直す
    private class Pulse
    {
        private float from;
        private float peak;
        private float delay;
        private float rise;
        private float fall;
        private float time;
        private bool active;

        public float Value { get; private set; }

        public void Begin(float strength, float delay, float rise, float fall)
        {
            from = Value;
            peak = Mathf.Max(strength, Value);
            this.delay = delay;
            this.rise = Mathf.Max(rise, 0.0001f);
            this.fall = fall;
            time = 0f;
            active = true;
        }

        public void Reset()
        {
            Value = 0f;
            active = false;
        }

        public void Tick(float dt)
        {
            if (!active) return;
            time += dt;

            float t = time - delay;
            if (t < 0f)
            {
                Value = from;
            }
            else if (t < rise)
            {
                float u = 1f - t / rise;
                Value = Mathf.Lerp(from, peak, 1f - u * u); // 出だしが速い
            }
            else if (t < rise + fall)
            {
                Value = peak * (1f - Mathf.SmoothStep(0f, 1f, (t - rise) / fall));
            }
            else
            {
                Reset();
            }
        }
    }
}
