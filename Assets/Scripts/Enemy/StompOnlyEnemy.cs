using UnityEngine;

namespace CyberAgentDev.ObjectPrototypeLearning
{
    // 上から踏まないと倒せない敵。ハンマーは弾き、横から触れたPlayerにはダメージを与える。
    // 接触ダメージ・スコア・破壊演出は同じGameObjectのDestructibleObject2Dに任せる。
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DestructibleObject2D))]
    public sealed class StompOnlyEnemy : MonoBehaviour, IAttackAcceptRule
    {
        [Tooltip("踏んだPlayerを跳ね上げる速さ(ユニット/秒)")]
        [Min(0f)][SerializeField] float bounceVelocity = 12f;
        [Tooltip("Playerの足が、自分の上面よりこれだけ下にめり込んでいても踏んだ扱いにする(ユニット)")]
        [Min(0f)][SerializeField] float stompTolerance = 0.15f;
        [SerializeField] string playerTag = DestructibleObject2D.DefaultPlayerTag;

        [Header("効果音")]
        [SerializeField] AudioClip stompSe;

        DestructibleObject2D destructible;
        Collider2D bodyCollider;

        void Awake()
        {
            destructible = GetComponent<DestructibleObject2D>();
            bodyCollider = GetComponent<Collider2D>();
        }

        // ハンマーはどんな攻撃でも弾かれる
        public bool? CanReceiveAttack(HitAttackBox attackBox, bool? isChargeAttack) => false;

        // 攻撃中に横から触れてもダメージを受ける
        public bool BlocksContactDamage(PlayerCombat player) => false;

        void OnTriggerEnter2D(Collider2D other) => TryStomp(other);
        void OnTriggerStay2D(Collider2D other) => TryStomp(other);

        void TryStomp(Collider2D other)
        {
            if (!isActiveAndEnabled || destructible.IsBroken || other.isTrigger) return;
            var body = other.attachedRigidbody;
            if (body == null || !body.CompareTag(playerTag)) return;

            // 上昇中は踏みつけにしない
            float vy = body.linearVelocity.y;            if (vy > 0.1f) return;

            // 1回前の物理更新での足の位置が、自分の上面より上にあれば上から来た
            float previousFeet = other.bounds.min.y - vy * Time.fixedDeltaTime;
            if (previousFeet < bodyCollider.bounds.max.y - stompTolerance) return;

            body.linearVelocity = new Vector2(body.linearVelocity.x, bounceVelocity);
            AudioManager.PlaySE(stompSe);
            var hitStop = GetComponent<HitStop>();
            if (hitStop != null) hitStop.Play(false);
            destructible.BreakBy(body.GetComponent<PlayerCombat>());
        }
    }
}
