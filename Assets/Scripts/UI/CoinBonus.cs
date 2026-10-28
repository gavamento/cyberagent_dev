//===============================================================================-
//コインの枚数からリザルトのボーナスを出す(後の1枚ほど高くなる累積)    秋田蓮音
//===============================================================================-
using UnityEngine;

// k 枚目の価値 = basePoints × k^exponent を 1 枚目から n 枚目まで足す
// Inspector では CoinBonusDrawer がグラフと早見表を描く
[System.Serializable]
public class CoinBonus
{
    [Tooltip("1枚目の価値")]
    [Min(0)] public int basePoints = 100;
    [Tooltip("1より大きいほど後の1枚の価値が高くなる。1で等差、0で全部同じ")]
    [Min(0f)] public float exponent = 1.5f;
    [Tooltip("Inspector のグラフの横軸の長さ(計算には使わない)")]
    [Min(1)] public int graphMaxCoins = 30;

    // k 枚目の1枚の価値
    public int ValueOf(int k)
    {
        if (k <= 0) return 0;
        double v = basePoints * System.Math.Pow(k, exponent);
        return v >= int.MaxValue ? int.MaxValue : (int)System.Math.Round(v, System.MidpointRounding.AwayFromZero);
    }

    // coins 枚取ったときのボーナスの合計
    public int Calculate(int coins)
    {
        long total = 0;
        for (int k = 1; k <= coins; k++)
        {
            total += ValueOf(k);
            if (total >= int.MaxValue) return int.MaxValue;
        }
        return (int)total;
    }
}
