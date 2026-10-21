namespace CyberAgentDev.ObjectPrototypeLearning
{
    // Playerは攻撃種別だけを公開し、破壊条件はObject側が判断する。
    public interface IObjectAttackInfo
    {
        bool IsChargeAttack { get; }
    }
}
