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
    public static bool HasRank { get; private set; } // HUD のランク表示が無いシーンなら false
    public static string LastRank { get; private set; }
    public static Color LastRankColor { get; private set; }

    public static void SetScore(int score, int coins)
    {
        LastScore = score;
        LastCoins = coins;
        HasScore = true;

        // HUD はシーンと一緒に消えるので、文字と色を写しておく
        var rank = ScoreRankScript.GetRank();
        HasRank = rank != null;
        LastRank = HasRank ? rank.text : null;
        LastRankColor = HasRank ? rank.color : Color.white;
    }

    // Domain Reload を切っていても前回のプレイの値を持ち越さない
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()
    {
        LastScore = 0;
        LastCoins = 0;
        HasScore = false;
        LastRank = null;
        LastRankColor = Color.white;
        HasRank = false;
    }
}
