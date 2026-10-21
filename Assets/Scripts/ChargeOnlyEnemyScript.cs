using UnityEngine;

namespace CyberAgentDev.ObjectPrototypeLearning
{
    // 同じEnemyのDestructibleObject2Dへ、チャージ限定の条件だけを追加する。
    [DisallowMultipleComponent]
    public sealed class ChargeOnlyEnemyScript : MonoBehaviour
    {
        bool missingAttackInfoReported;

        // trueは受理、falseは通常攻撃の拒否、nullは攻撃種別が不明。
        public bool? CanReceiveAttack(HitAttackBox attackBox, bool? isChargeAttack = null)
        {
            if (!isChargeAttack.HasValue && attackBox is IObjectAttackInfo attackInfo)
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
