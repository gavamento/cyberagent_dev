using UnityEngine;

namespace CyberAgentDev.ObjectPrototypeLearning
{
    // 同じEnemyのDestructibleObject2Dへ、チャージ限定の条件だけを追加する。
    [DisallowMultipleComponent]
    public sealed class ChargeOnlyEnemyScript : MonoBehaviour, IAttackAcceptRule
    {
        bool missingAttackInfoReported;

        // 通常攻撃中でもダメージを受け、ため攻撃の最中だけ守られる。
        public bool BlocksContactDamage(PlayerCombat player) => player.IsChargeAttacking;

        // trueは受理、falseは通常攻撃の拒否、nullは攻撃種別が不明。
        public bool? CanReceiveAttack(HitAttackBox attackBox, bool? isChargeAttack)
        {
            // Playerの確定値を優先し、旧通知の引数で攻撃種別を上書きしない。
            if (attackBox is IObjectAttackInfo attackInfo)
                isChargeAttack = attackInfo.IsChargeAttack;
            if (isChargeAttack.HasValue) return isChargeAttack.Value;
            if (!missingAttackInfoReported)
            {
                Debug.LogWarning("チャージ限定の判定にはHitAttackBoxでIObjectAttackInfo.IsChargeAttackの公開が必要です。攻撃種別が不明なため耐久は減らしません。", this);
                missingAttackInfoReported = true;
            }
            return null;
        }

        void OnDisable() => missingAttackInfoReported = false;
    }
}
