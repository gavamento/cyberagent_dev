namespace CyberAgentDev.ObjectPrototypeLearning
{
    // DestructibleObject2Dと同じGameObjectに付け、攻撃の受け付け条件を差し替える。
    public interface IAttackAcceptRule
    {
        // trueは受理、falseは拒否(弾かれる)、nullは攻撃種別が不明で保留。
        bool? CanReceiveAttack(HitAttackBox attackBox, bool? isChargeAttack);

        // 攻撃中のPlayerに触れたとき、接触ダメージを止めるか。
        bool BlocksContactDamage(PlayerCombat player);
    }
}
