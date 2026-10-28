//===============================================================================-
//ゲームの結果をリザルトシーンへ渡す    秋田蓮音
//===============================================================================-
using UnityEngine;

// シーンをまたいで値を残すため static にする
public static class GameResult
{
    public static bool HasScore { get; private set; }
    public static int LastScore { get; private set; }
    public static int LastCoins { get; private set; }

    public static void SetScore(int score, int coins)
    {
        LastScore = score;
        LastCoins = coins;
        HasScore = true;
    }

    // Domain Reload を切っていても前回のプレイの値を持ち越さない
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()
    {
        LastScore = 0;
        LastCoins = 0;
        HasScore = false;
    }
}
