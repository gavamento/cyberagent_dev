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

        [Header("踏まれたときの演出")]
        [Tooltip("踏まれた瞬間に出す、割れた1枚の画像。空なら何も出さない")]
        [SerializeField] Sprite brokenSprite;
        [Tooltip("割れた画像を、そのままの濃さで出しておく時間(秒)")]
        [Min(0f)][SerializeField] float brokenHoldTime = 0.08f;
        [Tooltip("そのあと、フェードして消えるまでの時間(秒)")]
        [Min(0.01f)][SerializeField] float brokenFadeTime = 0.35f;

        [Header("効果音")]
        [SerializeField] AudioClip stompSe;

        DestructibleObject2D destructible;
        Collider2D bodyCollider;

        void Awake()
        {
            destructible = GetComponent<DestructibleObject2D>();
            bodyCollider = GetComponent<Collider2D>();
        }

        // 踏まれた位置に、割れた画像を残してフェードさせる。
        // 本体は壊れて消えるので、フェードは別のオブジェクトに任せる
        void SpawnBrokenSprite()
        {
            if (brokenSprite == null) return;

            // 見た目のスプライト(自分か子の最初のもの)に位置・大きさ・並び順をそろえる
            var source = GetComponentInChildren<SpriteRenderer>();
            if (source == null) return;

            var go = new GameObject($"{name}_Broken");
            var t = go.transform;
            t.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            t.localScale = source.transform.lossyScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = brokenSprite;
            sr.flipX = source.flipX;
            sr.flipY = source.flipY;
            sr.color = source.color;
            sr.sharedMaterial = source.sharedMaterial;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder;

            go.AddComponent<BrokenFader>().Begin(sr, brokenHoldTime, brokenFadeTime);

            // 元の絵は隠して、割れた画像に差し替える
            foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;
        }

        // 割れた画像を少し出しておいて、フェードして消す
        sealed class BrokenFader : MonoBehaviour
        {
            SpriteRenderer sr;
            float hold;
            float fade;
            float timer;
            Color baseColor;

            public void Begin(SpriteRenderer renderer, float holdTime, float fadeTime)
            {
                sr = renderer;
                hold = holdTime;
                fade = Mathf.Max(0.01f, fadeTime);
                baseColor = sr.color;
            }

            // ヒットストップで時間が止まっている間も、割れた画像が見えるように、ゲーム内の時間で数える
            void Update()
            {
                timer += Time.deltaTime;
                if (timer <= hold) return;

                float p = Mathf.Clamp01((timer - hold) / fade);
                var c = baseColor;
                c.a = baseColor.a * (1f - p);
                sr.color = c;
                if (p >= 1f) Destroy(gameObject);
            }
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
            float vy = body.linearVelocity.y; if (vy > 0.1f) return;

            // 1回前の物理更新での足の位置が、自分の上面より上にあれば上から来た
            float previousFeet = other.bounds.min.y - vy * Time.fixedDeltaTime;
            if (previousFeet < bodyCollider.bounds.max.y - stompTolerance) return;

            body.linearVelocity = new Vector2(body.linearVelocity.x, bounceVelocity);
            AudioManager.PlaySE(stompSe);
            SpawnBrokenSprite();
            var hitStop = GetComponent<HitStop>();
            if (hitStop != null) hitStop.Play(false);
            destructible.BreakBy(body.GetComponent<PlayerCombat>());
        }
    }
}