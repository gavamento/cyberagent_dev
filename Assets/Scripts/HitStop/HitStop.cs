//===============================================================================-
//攻撃が当たったときのヒットストップ(停止中はカメラを揺らす)    秋田蓮音
//===============================================================================-
using UnityEngine;

// 敵・オブジェクトに付けるだけで動く。HitAttackBox が当たったときに Play を呼ぶ。
// 敵が Destroy されると以降の接触通知が飛ばされるので、敵が壊れる前の HitAttackBox から呼ぶ。
// Collider と同じ GameObject に付けること。
public class HitStop : MonoBehaviour
{
    [Header("停止時間 (実時間の秒)")]
    [Min(0f)][SerializeField] private float normalDuration = 0.06f;
    [Min(0f)][SerializeField] private float chargeDuration = 0.12f;
    [Tooltip("停止中の時間の速さ。0で完全に止まる")]
    [Range(0f, 1f)][SerializeField] private float stopTimeScale = 0f;

    [Header("カメラの揺れ (ワールド座標の幅)")]
    [Min(0f)][SerializeField] private float normalShake = 0.1f;
    [Min(0f)][SerializeField] private float chargeShake = 0.25f;

    private int lastPlayedFrame = -1;

    // 敵が壊れる前に HitAttackBox から呼ばれる
    public void Play(bool isChargeAttack)
    {
        if (!isActiveAndEnabled) return;
        // HitAttackBox と自分の接触検知の両方から来ても1回にする
        if (lastPlayedFrame == Time.frameCount) return;
        lastPlayedFrame = Time.frameCount;

        Debug.Log($"ヒットストップ発動: {name} ({(isChargeAttack ? "ため攻撃" : "通常攻撃")})", this);
        HitStopRunner.Request(
            isChargeAttack ? chargeDuration : normalDuration,
            stopTimeScale,
            isChargeAttack ? chargeShake : normalShake);
    }

    // 敵側の接触が先に来た場合の保険
    private void OnTriggerEnter2D(Collider2D other)
    {
        var attackBox = other.GetComponentInParent<HitAttackBox>();
        if (attackBox == null || !attackBox.isActiveAndEnabled) return;
        Play(attackBox.IsChargeAttack);
    }
}
