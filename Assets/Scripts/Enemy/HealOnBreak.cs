using UnityEngine;

namespace CyberAgentDev.ObjectPrototypeLearning
{
    // Playerに壊されたら、そのPlayerのHPを回復する。ステージの後片付けで消えたときは回復しない。
    // 壊れる処理は同じGameObjectのDestructibleObject2Dに任せる。
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DestructibleObject2D))]
    public sealed class HealOnBreak : MonoBehaviour
    {
        [Tooltip("壊したときに回復するHP")]
        [Min(1)][SerializeField] int healAmount = 1;

        [Header("効果音")]
        [SerializeField] AudioClip healSe;

        DestructibleObject2D destructible;

        void Awake() => destructible = GetComponent<DestructibleObject2D>();
        void OnEnable() => destructible.OnBroken += HandleBroken;
        void OnDisable() => destructible.OnBroken -= HandleBroken;

        void HandleBroken(PlayerCombat breaker)
        {
            if (breaker == null) return;
            var health = breaker.GetComponent<PlayerHealth>();
            if (health == null || health.IsDead) return;
            health.Heal(healAmount);
            AudioManager.PlaySE(healSe);
        }
    }
}
