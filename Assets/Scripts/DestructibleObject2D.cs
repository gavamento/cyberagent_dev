using System.Collections.Generic;
using UnityEngine;

namespace CyberAgentDev.ObjectPrototypeLearning
{
    // タグごとの、倒したときの点数
    [System.Serializable]
    public struct TagScore
    {
        [Tooltip("Unityのタグ名(Tag Managerに登録したものと完全一致)")]
        public string tagName;

        [Tooltip("このタグのものを倒したときにPlayerへ加算される点数")]
        public int points;

        [Tooltip("このタグのものを倒したときに加算されるフィーバーゲージ(0〜100。100で突進)")]
        public float fever;
    }

    [DisallowMultipleComponent]
    public sealed class DestructibleObject2D : MonoBehaviour
    {
        const float MinimumDamageIntervalSeconds = 0.01f;
        public const string DefaultPlayerTag = "Player";
        public const string DefaultObjectTag = "Object";
        public const string DefaultEnemyTag = "Enemy";
        public const string DefaultIronTag = "Iron";

        // 対象タグは配列で持ち、Inspectorから候補を増減できるようにする。
        [SerializeField] string[] playerTags = { DefaultPlayerTag };
        [SerializeField] string[] objectTags = { DefaultObjectTag, DefaultEnemyTag, DefaultIronTag };
        // 接触でPlayerにダメージを与えるタグ(Objectは与えない)
        [SerializeField] string[] damageTags = { DefaultEnemyTag, DefaultIronTag };
        [SerializeField] bool canDamagePlayer = true;
        [Min(1)][SerializeField] int contactDamage = 1;
        [Min(MinimumDamageIntervalSeconds)][SerializeField] float contactDamageIntervalSeconds = 1f;
        [SerializeField] bool canBreakFromAttack = true;
        // 旧シーンの保存値を引き継ぐためだけに保持し、起動時に専用部品へ移す。
        [HideInInspector][SerializeField] bool chargeOnly;
        [Min(1)][SerializeField] int hitsToBreak = 1;
        [SerializeField] bool isBroken;
        [Min(0)][SerializeField] int currentHitsTaken;

        [Header("倒したときのスコア(このオブジェクトのタグで点数が決まる)")]
        [SerializeField]
        TagScore[] scoreByTag =
        {
            new TagScore { tagName = DefaultObjectTag, points = 100, fever = 4 },
            new TagScore { tagName = DefaultEnemyTag,  points = 200, fever = 4 },
            new TagScore { tagName = DefaultIronTag,   points = 400, fever = 10 },
        };

        [Header("エフェクトの種類")]
        [SerializeField] private GameObject effectPrefab = null;
        [Header("エフェクトの種類")]
        [SerializeField] private GameObject debricPrefab = null;
        [Header("エフェクトの種類")]
        [SerializeField] private GameObject shockPrefab = null;
        [Header("エフェクトのオフセット")]
        [SerializeField] private Vector3 positionOffset;

        [Header("壊れたときの音")]
        [SerializeField] private AudioClip breakSe = null;

        readonly List<Collider2D> contacts = new List<Collider2D>();
        readonly List<Collider2D> attackContacts = new List<Collider2D>();
        readonly HashSet<Transform> handledPlayers = new HashSet<Transform>();
        // 攻撃判定ごとに、最後に受け付けた攻撃の番号(HitAttackBox.AttackId)
        readonly Dictionary<HitAttackBox, int> receivedAttackIds = new Dictionary<HitAttackBox, int>();
        Collider2D objectCollider;
        IAttackAcceptRule[] acceptRules;
        float nextDamageTime;
        PlayerCombat lastAttacker; // 最後に攻撃したPlayer(スコアの加算先)

        // Damage Tags(Enemy・Iron)のものだけが接触ダメージを出す。Objectは設定がONでもダメージなし。
        public bool CanDamagePlayer => canDamagePlayer && MatchesAnyTag(gameObject, damageTags);
        public int ContactDamage => contactDamage;
        public bool ChargeOnly
        {
            get
            {
                var rule = GetComponent<ChargeOnlyEnemyScript>();
                return rule != null ? rule.enabled : chargeOnly;
            }
        }
        public bool IsBroken => isBroken;
        public int RemainingHits => Mathf.Max(0, hitsToBreak - currentHitsTaken);

        void Awake()
        {
            // Enemyの円形など、同じGameObjectにある既存の当たり判定を使う。
            objectCollider = GetComponent<Collider2D>();
            if (objectCollider == null || !objectCollider.isTrigger)
            {
                Debug.LogError("同じGameObjectにIs Triggerを有効にしたCollider2Dを付けてください。", this);
                enabled = false;
                return;
            }
            if (chargeOnly && GetComponent<ChargeOnlyEnemyScript>() == null)
                gameObject.AddComponent<ChargeOnlyEnemyScript>();
            acceptRules = GetComponents<IAttackAcceptRule>();
        }

        // 有効な受け付け条件の部品。なければnull(どの攻撃でも壊れる)
        IAttackAcceptRule ActiveRule()
        {
            if (acceptRules == null) return null;
            foreach (var rule in acceptRules)
                if (rule is Behaviour behaviour && behaviour != null && behaviour.enabled) return rule;
            return null;
        }

        void Reset()
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.isTrigger = true;
        }
        void OnTriggerEnter2D(Collider2D other) => RegisterContact(other);
        void OnTriggerStay2D(Collider2D other) => RegisterContact(other);
        void OnTriggerExit2D(Collider2D other) => contacts.Remove(other);

        void RegisterContact(Collider2D other)
        {
            if (!isActiveAndEnabled || other == null) return;
            // 攻撃Colliderの接触もObjectが受け付け、破壊条件をここから確認する。
            var attackBox = other.GetComponentInParent<HitAttackBox>();
            if (attackBox != null)
            {
                ReceiveAttack(attackBox);
                return;
            }
            if (!contacts.Contains(other)) contacts.Add(other);
        }

        // 無効化した後に、古い接触や攻撃履歴を持ち越さない。
        void OnDisable()
        {
            contacts.Clear();
            attackContacts.Clear();
            handledPlayers.Clear();
            receivedAttackIds.Clear();
            nextDamageTime = 0f;
        }

        void FixedUpdate()
        {
            if (isBroken || !MatchesAnyTag(gameObject, objectTags)) return;
            bool canDamageNow = CanDamagePlayer && Time.time >= nextDamageTime;
            bool damageSent = false;
            handledPlayers.Clear();
            for (int index = contacts.Count - 1; index >= 0 && !isBroken; index--)
            {
                if (index >= contacts.Count) continue;
                var other = contacts[index];
                if (other == null || !other.enabled || !other.gameObject.activeInHierarchy)
                {
                    contacts.RemoveAt(index);
                    continue;
                }
                // 退出通知が来る前でも、既に離れたPlayerへダメージを送らない。
                if (!other.Distance(objectCollider).isOverlapped) continue;
                var player = FindTaggedPlayer(other.transform);
                // 同じPlayerにColliderが複数あっても、同じ更新では1回だけ処理する。
                if (player == null || !handledPlayers.Add(player)) continue;
                if (HandleContact(player, canDamageNow)) damageSent = true;
            }
            if (damageSent) nextDamageTime = Time.time + contactDamageIntervalSeconds;
        }

        bool HandleContact(Transform player, bool canDamageNow)
        {
            var combat = player.GetComponent<PlayerCombat>();
            // フィーバー中のPlayerに触れたら、ダメージなしで一発で壊れる。
            if (combat != null && combat.IsFever)
            {
                FeverBreak(combat);
                return false;
            }
            // Playerが公開する攻撃中の状態を使い、ダッシュ中も接触ダメージを止める。
            // 受け付け条件の部品があれば、攻撃中でも守られるかはその部品が決める(Ironはため攻撃中だけ守られる)。
            var rule = ActiveRule();
            if (combat != null && combat.IsAttacking && (rule == null || rule.BlocksContactDamage(combat))) return false;
            if (!canDamageNow) return false;
            if (contactDamage < 1 || contactDamageIntervalSeconds <= 0f
                || float.IsNaN(contactDamageIntervalSeconds) || float.IsInfinity(contactDamageIntervalSeconds))
            {
                Debug.LogError("接触ダメージと通知間隔は正の値にしてください。", this);
                enabled = false;
                return false;
            }
            var health = player.GetComponent<PlayerHealth>();
            if (health == null || !health.isActiveAndEnabled || health.IsDead) return false;
            // Enemyを含む接触ダメージはここから通知し、HP・死亡はPlayerへ任せる。
            health.TakeDamage(contactDamage);
            return true;
        }

        // 旧OnHit通知とも互換を保つ。耐久の更新は共通処理で一度だけ行う。
        public void OnHit(bool isChargeAttack) => ReceiveAttack(FindAttackBox(), isChargeAttack);

        void ReceiveAttack(HitAttackBox attackBox, bool? isChargeAttack = null)
        {
            if (!isActiveAndEnabled || isBroken || !canBreakFromAttack
                || !MatchesAnyTag(gameObject, objectTags) || attackBox == null
                || !attackBox.isActiveAndEnabled || FindTaggedPlayer(attackBox.transform) == null) return;
            var rule = ActiveRule();
            bool? accepted = rule != null ? rule.CanReceiveAttack(attackBox, isChargeAttack) : true;
            // 情報不足は保留し、後から届く旧通知の確定値も受け付けられるようにする。
            if (!accepted.HasValue) return;
            // Triggerと旧通知の両方が来ても、同じ攻撃を二重に数えない。連打した次の攻撃は番号が変わるので受け付ける。
            if (receivedAttackIds.TryGetValue(attackBox, out int receivedId) && receivedId == attackBox.AttackId) return;
            receivedAttackIds[attackBox] = attackBox.AttackId;
            if (!accepted.Value) return;
            if (hitsToBreak < 1)
            {
                Debug.LogError("攻撃回数は1以上にしてください。", this);
                return;
            }
            // 攻撃したPlayerを覚えておき、壊れたときにスコアを渡す。
            lastAttacker = attackBox.GetComponentInParent<PlayerCombat>();
            if (lastAttacker == null)
                Debug.LogWarning("攻撃したPlayerにPlayerCombatが見つかりません。スコアは加算されません。", this);
            else
                lastAttacker.NotifyHit(); // 攻撃が通ったので、Playerは「当たった」扱い(前へ出る)
            // 壊す前にヒットストップを出す(Destroy後は接触通知が届かないため)
            var hitStop = GetComponent<HitStop>();
            if (hitStop != null) hitStop.Play(attackBox.IsChargeAttack);
            if (currentHitsTaken < hitsToBreak) currentHitsTaken++;
            if (currentHitsTaken >= hitsToBreak) Break();
        }

        // 破壊は攻撃を受けた側のこの部品が担当する。
        public void Break()
        {
            if (isBroken) return;
            isBroken = true;

            // 攻撃したPlayerへスコアを渡す(攻撃以外で壊れたときは lastAttacker が空なので加算なし)
            if (lastAttacker != null)
            {
                lastAttacker.AddScore(ScoreFor(gameObject.tag));
                lastAttacker.AddFever(FeverFor(gameObject.tag)); // フィーバー中は加算されない
                lastAttacker.NotifyBreak(); // 破壊で加速する(フィーバー中も)
            }

            // 消滅エフェクト
            if (effectPrefab != null && debricPrefab != null)
            {
                EffectManager.PlayEffectWithOwnerSpriteAndDebris(effectPrefab, debricPrefab, transform, 15, positionOffset);
            }

            // 衝撃波エフェクト
            if (shockPrefab != null)
            {
                EffectManager.PlayEffect(shockPrefab, transform, positionOffset);
            }

            AudioManager.PlaySE(breakSe);

            Destroy(gameObject);
        }

        // フィーバー中のPlayerに壊される(接触・終了時の一掃)。耐久回数や突進攻撃の条件は無視して一発で壊れる
        public void FeverBreak(PlayerCombat player) => BreakBy(player);

        // 攻撃以外の方法(踏みつけ・見切りなど)でPlayerに倒されたとき。スコアはそのPlayerへ渡す
        public void BreakBy(PlayerCombat player)
        {
            if (isBroken || !isActiveAndEnabled) return;
            lastAttacker = player;
            Break();
        }

        // このオブジェクトのタグに対応するフィーバー加算量(表にないタグは0)
        float FeverFor(string tag)
        {
            if (scoreByTag == null) return 0f;
            foreach (var rule in scoreByTag)
                if (rule.tagName == tag) return rule.fever;
            return 0f;
        }

        // このオブジェクトのタグに対応する点数(表にないタグは0)
        int ScoreFor(string tag)
        {
            if (scoreByTag == null) return 0;
            foreach (var rule in scoreByTag)
                if (rule.tagName == tag) return rule.points;
            return 0;
        }

        HitAttackBox FindAttackBox()
        {
            // OnHitには攻撃元が含まれないため、実際に重なる有効なHitboxを調べる。
            attackContacts.Clear();
            objectCollider.Overlap(new ContactFilter2D { useTriggers = true }, attackContacts);
            HitAttackBox source = null;
            foreach (var other in attackContacts)
            {
                if (other == null) continue;
                var attackBox = other.GetComponentInParent<HitAttackBox>();
                if (attackBox == null || !attackBox.isActiveAndEnabled
                    || FindTaggedPlayer(attackBox.transform) == null) continue;
                // 複数の攻撃元が重なる場合は、通知元を推測して耐久を減らさない。
                if (source != null && source != attackBox) return null;
                source = attackBox;
            }
            return source;
        }

        Transform FindTaggedPlayer(Transform contact)
        {
            // 子のColliderから接触しても、親をたどってPlayer本体を見つける。
            for (var current = contact; current != null; current = current.parent)
                if (MatchesAnyTag(current.gameObject, playerTags)) return current;
            return null;
        }

        static bool MatchesAnyTag(GameObject target, string[] tags)
        {
            if (target == null || tags == null) return false;
            string targetTag = target.tag;
            foreach (string tag in tags)
                if (targetTag == tag) return true;
            return false;
        }
    }

}