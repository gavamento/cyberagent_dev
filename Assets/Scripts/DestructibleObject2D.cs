using System.Collections.Generic;
using UnityEngine;

namespace CyberAgentDev.ObjectPrototypeLearning
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class DestructibleObject2D : MonoBehaviour
    {
        const float MinimumDamageIntervalSeconds = 0.01f;
        public const string DefaultPlayerTag = "Player";
        public const string DefaultObjectTag = "Object";

        [SerializeField] string[] playerTags = { DefaultPlayerTag };
        [SerializeField] string[] objectTags = { DefaultObjectTag };
        [SerializeField] bool canDamagePlayer = true;
        [Min(1)] [SerializeField] int contactDamage = 1;
        [Min(MinimumDamageIntervalSeconds)] [SerializeField] float contactDamageIntervalSeconds = 1f;
        [SerializeField] bool canBreakFromAttack = true;
        [Min(1)] [SerializeField] int hitsToBreak = 1;
        [SerializeField] bool isBroken;
        [Min(0)] [SerializeField] int currentHitsTaken;

        readonly List<Collider2D> contacts = new List<Collider2D>();
        readonly HashSet<Transform> handledPlayers = new HashSet<Transform>();
        readonly HashSet<HitAttackBox> receivedAttackBoxes = new HashSet<HitAttackBox>();
        BoxCollider2D objectCollider;
        float nextDamageTime;

        public bool CanDamagePlayer => canDamagePlayer;
        public int ContactDamage => contactDamage;
        public bool IsBroken => isBroken;
        public int RemainingHits => Mathf.Max(0, hitsToBreak - currentHitsTaken);

        void Awake() => objectCollider = GetComponent<BoxCollider2D>();
        void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
        void OnTriggerEnter2D(Collider2D other) => RegisterContact(other);
        void OnTriggerStay2D(Collider2D other) => RegisterContact(other);
        void OnTriggerExit2D(Collider2D other) => contacts.Remove(other);

        void RegisterContact(Collider2D other)
        {
            if (!isActiveAndEnabled || other == null) return;
            var attackBox = other.GetComponentInParent<HitAttackBox>();
            if (attackBox != null)
            {
                if (attackBox.isActiveAndEnabled && !isBroken && canBreakFromAttack
                    && MatchesAnyTag(gameObject, objectTags) && FindTaggedPlayer(other.transform) != null
                    && receivedAttackBoxes.Add(attackBox)) ReceiveAttack();
                return;
            }
            if (!contacts.Contains(other)) contacts.Add(other);
        }

        void OnDisable()
        {
            contacts.Clear();
            handledPlayers.Clear();
            receivedAttackBoxes.Clear();
            nextDamageTime = 0f;
        }

        void FixedUpdate()
        {
            receivedAttackBoxes.RemoveWhere(attack => attack == null || !attack.isActiveAndEnabled);
            if (isBroken || !MatchesAnyTag(gameObject, objectTags)) return;
            bool canDamageNow = canDamagePlayer && Time.time >= nextDamageTime;
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
                if (!other.Distance(objectCollider).isOverlapped) continue;
                var player = FindTaggedPlayer(other.transform);
                if (player == null || !handledPlayers.Add(player)) continue;
                if (HandleContact(player, canDamageNow)) damageSent = true;
            }
            if (damageSent) nextDamageTime = Time.time + contactDamageIntervalSeconds;
        }

        bool HandleContact(Transform player, bool canDamageNow)
        {
            var attack = player.GetComponent<IObjectAttackSource2D>();
            if (attack != null && attack.IsAttacking)
            {
                if (canBreakFromAttack && attack.TryRegisterHit(gameObject)) ReceiveAttack();
                return false;
            }
            var activeAttackBox = player.GetComponentInChildren<HitAttackBox>();
            if (activeAttackBox != null && activeAttackBox.isActiveAndEnabled) return false;
            if (!canDamageNow) return false;
            if (contactDamage < 1 || contactDamageIntervalSeconds <= 0f
                || float.IsNaN(contactDamageIntervalSeconds) || float.IsInfinity(contactDamageIntervalSeconds))
            {
                Debug.LogError("接触ダメージと通知間隔は正の値にしてください。", this);
                enabled = false;
                return false;
            }
            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                if (!health.isActiveAndEnabled || health.IsDead) return false;
                health.TakeDamage(contactDamage);
                return true;
            }
            var receiver = player.GetComponent<IObjectDamageReceiver2D>();
            if (receiver == null) return false;
            if (receiver is Behaviour behaviour && !behaviour.isActiveAndEnabled) return false;
            receiver.ReceiveDamage(contactDamage);
            return true;
        }

        void ReceiveAttack()
        {
            if (!canBreakFromAttack || isBroken) return;
            if (hitsToBreak < 1)
            {
                Debug.LogError("攻撃回数は1以上にしてください。", this);
                return;
            }
            if (currentHitsTaken < hitsToBreak) currentHitsTaken++;
            if (currentHitsTaken >= hitsToBreak) Break();
        }

        public void Break()
        {
            if (isBroken) return;
            isBroken = true;
            Destroy(gameObject);
        }

        Transform FindTaggedPlayer(Transform contact)
        {
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

    public interface IObjectAttackSource2D
    {
        bool IsAttacking { get; }
        bool TryRegisterHit(GameObject target);
    }

    public interface IObjectDamageReceiver2D
    {
        void ReceiveDamage(int damage);
    }
}