//===============================================================================-
//プレイヤーが触れると拾えるコイン(スコアと加速)    秋田蓮音
//===============================================================================-
using UnityEngine;

// コインのプレハブに付ける。同じ GameObject の Is Trigger の Collider2D にプレイヤーの体が触れたら拾う
// StageGenerator がランダムに置く。取ると点数が入り、敵を壊したときと同じ flow が増えて加速する
public class Coin : MonoBehaviour
{
    [Tooltip("取ったときに入る点数")]
    [SerializeField, Min(0)] private int points = 100;
    [Tooltip("取ったときに増える flow (敵を1回壊すと ScrollSpeedProgression の flowPerBreak だけ増える)")]
    [SerializeField, Min(0f)] private float flowAmount = 1f;
    [SerializeField] private AudioClip pickupSe;

    private bool collected;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponent<HitAttackBox>() != null) return; // ハンマーでは拾わず、体で触れたときだけ

        var player = other.GetComponentInParent<PlayerCombat>();
        if (player == null) return;

        var health = player.GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;

        collected = true; // 体の Collider が複数あっても1回だけ
        player.AddScore(points);
        player.AddFlow(flowAmount);
        AudioManager.PlaySE(pickupSe);
        Destroy(gameObject);
    }
}
