using UnityEngine;

namespace CyberAgentDev.ObjectPrototypeLearning
{
    // 見切り剣士。近づいたPlayerに構えて斬りかかる。構え中は自動でスローになる。
    // 斬る瞬間にPlayerが空中にいるか、金色の輪の間に攻撃ボタンを押すと斬り返して倒せる。
    // ハンマーを直接当てても弾かれ、地上で斬られるとダメージ。
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DestructibleObject2D), typeof(EnemyGroundMover))]
    public sealed class BladeEnemy : MonoBehaviour, IAttackAcceptRule
    {
        enum State { Walk, Windup, Recover, Retreat }

        [Header("移動(ユニット/秒。マイナスで左へ)")]
        [SerializeField] float walkSpeed = -1f;
        [SerializeField] float windupSpeed = -0.46f;
        [SerializeField] float retreatSpeed = -1.7f;

        [Header("構える条件")]
        [Tooltip("ONなら、Playerの攻撃ヒットボックスの範囲(横)に入ったときも構える。速さから求めた構える距離(下の構え)のほうが遠ければ、そちらで先に構える")]
        [SerializeField] bool alertByHitboxRange = true;
        [Tooltip("ヒットボックスの範囲を、前後にこの距離だけ広げて見る")]
        [Min(0f)][SerializeField] float hitboxRangeMargin = 0f;

        [Header("構え")]
        [Tooltip("斬る瞬間のPlayerとの距離の目安。構える距離はこれに、構えている間にPlayerが近づく分を足して決める")]
        [Min(0f)][SerializeField] float strikeDistance = 3.1f;
        [Tooltip("構えてから斬るまでの時間(スロー込みのゲーム内の秒)")]
        [Min(0.05f)][SerializeField] float windupTime = 0.4f;
        [Tooltip("斬る直前のこの時間は輪が金色になり、攻撃ボタンで斬り返せる")]
        [Min(0f)][SerializeField] float perfectWindow = 0.26f;
        [Tooltip("斬った後、退いていくまでの時間")]
        [Min(0f)][SerializeField] float recoverTime = 0.28f;
        [Tooltip("構え中の時間の速さ")]
        [Range(0.05f, 1f)][SerializeField] float slowTimeScale = 0.3f;

        [Header("斬撃(自分から見たPlayerの位置)")]
        [Tooltip("斬撃が届く、自分より前(左)の距離")]
        [Min(0f)][SerializeField] float strikeReachAhead = 3.5f;
        [Tooltip("斬撃が届く、自分より後ろ(右)の距離")]
        [Min(0f)][SerializeField] float strikeReachBehind = 0.9f;
        [Tooltip("攻撃ボタンでの斬り返しが届く距離")]
        [Min(0f)][SerializeField] float parryReachAhead = 6f;
        [Min(0f)][SerializeField] float parryReachBehind = 0.4f;
        [Tooltip("Playerの足元からこの距離以内に地面がなければ空中とみなす")]
        [Min(0f)][SerializeField] float airHeight = 0.5f;
        [Min(1)][SerializeField] int strikeDamage = 1;

        [Header("ジャンプでの斬り返し(跳んでから斬られるまでの時間)")]
        [Tooltip("この範囲なら「極・見切り」、それより早く跳んでいたら「見切り」")]
        [Min(0f)][SerializeField] float perfectJumpMin = 0.02f;
        [Min(0f)][SerializeField] float perfectJumpMax = 0.24f;

        [Header("スコア(DestructibleObject2Dの点数表は0にしておく)")]
        [SerializeField] int perfectPoints = 1200;
        [SerializeField] float perfectFever = 18f;
        [SerializeField] int earlyPoints = 500;
        [SerializeField] float earlyFever = 6f;

        [Header("輪")]
        [SerializeField] LineRenderer ring;
        [SerializeField] Vector2 ringCenter = new Vector2(0f, 1f);
        [Min(0f)][SerializeField] float ringMaxRadius = 1.4f;
        [Min(0f)][SerializeField] float ringMinRadius = 0.5f;
        [Min(8)][SerializeField] int ringSegments = 40;
        [SerializeField] Color ringColor = new Color(1f, 0.235f, 0.353f);
        [SerializeField] Color perfectRingColor = new Color(1f, 0.76f, 0.1f);

        [Header("上に積んだもの(見切りに失敗したら崩れる)")]
        [Tooltip("剣士の上に積んであるオブジェクト(子オブジェクト)。空なら何も起きない")]
        [SerializeField] Transform stackedObject;
        [Tooltip("崩れるときの飛び出す勢い。Xは左右にランダム、Yは上向き")]
        [SerializeField] Vector2 collapseKick = new Vector2(2f, 4f);
        [Tooltip("崩れるときの回転の速さ(度/秒)。±この範囲でランダム")]
        [SerializeField] float collapseSpin = 360f;
        [Min(0f)][SerializeField] float collapseGravity = 3f;
        [Tooltip("崩れてから消えるまでの時間(秒)")]
        [Min(0.1f)][SerializeField] float collapseLifetime = 1.2f;

        [Header("効果音")]
        [SerializeField] AudioClip alertSe;
        [SerializeField] AudioClip slashSe;
        [SerializeField] AudioClip counterSe;

        // 剣士は大量に生成されるので、見つけたPlayerを全員で使い回す
        static PlayerCombat cachedPlayer;

        readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
        DestructibleObject2D destructible;
        Collider2D bodyCollider;
        EnemyGroundMover mover;
        PlayerCombat player;
        PlayerHealth playerHealth;
        Collider2D playerCollider;
        State state;
        float playerSpeed;
        float lastPlayerX;
        bool hasLastPlayerX;
        float timer;
        float airborneSince = -1f; // 構え中にPlayerが空中に出た時刻。地上なら-1
        bool collapsed;            // 積んだものがもう崩れたか

        bool InPerfectWindow => state == State.Windup && timer <= perfectWindow;

        void Awake()
        {
            destructible = GetComponent<DestructibleObject2D>();
            bodyCollider = GetComponentInChildren<Collider2D>();
            mover = GetComponent<EnemyGroundMover>();
            if (ring != null)
            {
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = ringSegments;
                ring.enabled = false;
            }
        }

        void OnEnable()
        {
            state = State.Walk;
            mover.Speed = walkSpeed;
            if (player == null)
            {
                if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerCombat>();
                player = cachedPlayer;
            }
            if (player == null) return;
            playerHealth = player.GetComponent<PlayerHealth>();
            playerCollider = player.GetComponent<Collider2D>();
            hasLastPlayerX = false;
            player.OnAttackStarted += HandleAttackStarted;
        }

        void OnDisable()
        {
            SlowMotion.Release(this);
            if (player != null) player.OnAttackStarted -= HandleAttackStarted;
            if (ring != null) ring.enabled = false;
        }

        // ハンマーを直接当てても弾かれる
        public bool? CanReceiveAttack(HitAttackBox attackBox, bool? isChargeAttack) => false;

        // 接触ダメージは使わず、斬撃でだけダメージを与える
        public bool BlocksContactDamage(PlayerCombat player) => true;

        void FixedUpdate()
        {
            if (destructible.IsBroken) return;
            float dt = Time.fixedDeltaTime;
            switch (state)
            {
                case State.Walk:
                    if (PlayerAlive() && !player.IsFever)
                    {
                        TrackPlayerSpeed(dt);
                        if (ShouldAlert()) BeginWindup();
                    }
                    break;
                case State.Windup:
                    if (!PlayerAlive()) { EndWindup(State.Retreat); break; }
                    // 跳んだ時刻を覚えておき、斬った瞬間に早すぎたかを判定する
                    if (!PlayerAirborne()) airborneSince = -1f;
                    else if (airborneSince < 0f) airborneSince = Time.time;
                    timer -= dt;
                    if (timer <= 0f) Strike();
                    break;
                case State.Recover:
                    timer -= dt;
                    if (timer <= 0f) EndRecover();
                    break;
            }
        }

        void Update() => UpdateRing();

        void BeginWindup()
        {
            state = State.Windup;
            timer = windupTime;
            mover.Speed = windupSpeed;
            // 構える前から跳んでいたら、早跳びとして扱う
            airborneSince = PlayerAirborne() ? Time.time - windupTime : -1f;
            SlowMotion.Request(this, slowTimeScale);
            AudioManager.PlaySE(alertSe);
            Debug.Log("剣士が構えた", this);
        }

        void EndWindup(State next)
        {
            SlowMotion.Release(this);
            state = next;
            timer = recoverTime;
            mover.Speed = next == State.Retreat ? retreatSpeed : 0f;
        }

        // 斬った後。斬撃が届かなかった・無敵で効かなかったときに、Playerがまだ前にいれば構え直す。通り過ぎていたら退く
        void EndRecover()
        {
            if (PlayerAlive() && PlayerOffset() > 0f)
            {
                state = State.Walk;
                mover.Speed = walkSpeed;
                hasLastPlayerX = false; // 止まっていた間の移動を速さに入れない
                return;
            }
            state = State.Retreat;
            mover.Speed = retreatSpeed;
        }

        void Strike()
        {
            EndWindup(State.Recover);
            AudioManager.PlaySE(slashSe);
            if (!PlayerAlive()) return;
            if (player.IsFever)
            {
                destructible.FeverBreak(player);
                return;
            }

            float dx = PlayerOffset();
            if (dx < -strikeReachBehind || dx > strikeReachAhead)
            {
                CollapseStack(); // 届かない位置でも、見切れていないので崩れる
                return;
            }

            if (PlayerAirborne())
            {
                float sinceJump = Time.time - airborneSince;
                bool perfect = airborneSince >= 0f && sinceJump >= perfectJumpMin && sinceJump <= perfectJumpMax;
                Counter(perfect, "ジャンプ");
                return;
            }

            // 見切れなかった。積んだものが崩れる
            CollapseStack();

            // 地上で受けたらダメージ。斬った剣士もそのまま消える
            if (playerHealth != null && !playerHealth.IsInvincible)
            {
                Debug.Log("斬られた!", this);
                playerHealth.TakeDamage(strikeDamage);
                destructible.Break();
            }
        }

        // 積んであるものを切り離して、回転しながら落とす。剣士が消えても残るよう、親から外す
        void CollapseStack()
        {
            if (collapsed || stackedObject == null) return;
            collapsed = true;

            var t = stackedObject;
            t.SetParent(null, true);
            foreach (var col in t.GetComponentsInChildren<Collider2D>()) col.enabled = false; // Playerや他の判定に触れない

            var body = t.GetComponent<Rigidbody2D>();
            if (body == null) body = t.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = collapseGravity;
            body.linearVelocity = new Vector2(Random.Range(-collapseKick.x, collapseKick.x),
                                              Random.Range(collapseKick.y * 0.5f, collapseKick.y));
            body.angularVelocity = Random.Range(-collapseSpin, collapseSpin);

            Destroy(t.gameObject, collapseLifetime);
            Debug.Log("積んだものが崩れた", this);
        }

        // 攻撃ボタンでの斬り返し。金色の輪の間だけ成功し、Playerは前へ踏み込む
        void HandleAttackStarted(bool isChargeAttack)
        {
            if (state != State.Windup || destructible.IsBroken || !PlayerAlive()) return;
            float dx = PlayerOffset();
            if (dx < -parryReachBehind || dx > parryReachAhead) return;
            if (!InPerfectWindow)
            {
                Debug.Log("早い!", this);
                return;
            }
            player.NotifyHit();
            Counter(true, "攻撃");
        }

        void Counter(bool perfect, string by)
        {
            Debug.Log($"{(perfect ? "極・見切り!!" : "見切り!")} ({by})", this);
            EndWindup(State.Recover);
            player.AddScore(perfect ? perfectPoints : earlyPoints);
            player.AddFever(perfect ? perfectFever : earlyFever);
            AudioManager.PlaySE(counterSe);
            var hitStop = GetComponent<HitStop>();
            if (hitStop != null) hitStop.Play(perfect);
            destructible.BreakBy(player);
        }

        void UpdateRing()
        {
            if (ring == null) return;
            bool show = state == State.Windup && !destructible.IsBroken;
            ring.enabled = show;
            if (!show) return;

            float radius = Mathf.Lerp(ringMinRadius, ringMaxRadius, Mathf.Clamp01(timer / windupTime));
            Color color = InPerfectWindow ? perfectRingColor : ringColor;
            ring.startColor = color;
            ring.endColor = color;
            if (ring.positionCount != ringSegments) ring.positionCount = ringSegments;
            for (int i = 0; i < ringSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / ringSegments;
                ring.SetPosition(i, ringCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        bool PlayerAlive() =>
            player != null && player.isActiveAndEnabled && (playerHealth == null || !playerHealth.IsDead);

        // 構えるか。速さから求めた距離に入るか、Playerの攻撃ヒットボックスの範囲に自分の体が入ったら構える。
        // ヒットボックスの範囲だけで構えると、速いときは構えている間に追い越されて斬撃が届かない
        bool ShouldAlert()
        {
            float dx = PlayerOffset();
            if (dx <= 0f) return false; // 自分がPlayerより前(右)にいるときだけ
            if (dx < AlertDistance()) return true;
            return alertByHitboxRange && InPlayerHitboxRange();
        }

        // Playerの攻撃ヒットボックスの範囲(横)に自分の体が入っているか。範囲が分からなければfalse
        bool InPlayerHitboxRange()
        {
            if (!player.TryGetAttackXRange(out float minX, out float maxX)) return false;

            float myMin, myMax;
            if (bodyCollider != null)
            {
                var b = bodyCollider.bounds;
                myMin = b.min.x;
                myMax = b.max.x;
            }
            else myMin = myMax = transform.position.x;

            return myMax >= minX - hitboxRangeMargin && myMin <= maxX + hitboxRangeMargin;
        }

        // 構える距離。速く走っているPlayerほど手前から構え、斬る瞬間にちょうど届く距離へ入るようにする
        float AlertDistance() => strikeDistance + Mathf.Max(0f, playerSpeed - windupSpeed) * windupTime;

        // Playerは前進の限界で位置を押さえられるので、速度ではなく実際の移動量から速さを測る
        void TrackPlayerSpeed(float dt)
        {
            float x = player.transform.position.x;
            if (hasLastPlayerX && dt > 0f)
                playerSpeed = Mathf.Lerp(playerSpeed, (x - lastPlayerX) / dt, 0.2f);
            lastPlayerX = x;
            hasLastPlayerX = true;
        }

        // 自分から見たPlayerの距離。Playerが左にいればプラス
        float PlayerOffset() => transform.position.x - player.transform.position.x;

        // Playerの足元からairHeight以内に地面(動かない当たり判定)がなければ空中
        bool PlayerAirborne()
        {
            if (playerCollider == null) return player.transform.position.y > transform.position.y + airHeight;
            var bounds = playerCollider.bounds;
            var filter = new ContactFilter2D { useTriggers = false };
            int count = Physics2D.Raycast(new Vector2(bounds.center.x, bounds.min.y + 0.05f), Vector2.down, filter, groundHits, airHeight + 0.05f);
            for (int i = 0; i < count; i++)
            {
                var body = groundHits[i].collider.attachedRigidbody;
                if (body == null || body.bodyType == RigidbodyType2D.Static) return false;
            }
            return true;
        }
    }
}