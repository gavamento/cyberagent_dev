//===============================================================================-
//ゲームの結果をリザルトシーンへ渡す    秋田蓮音
//===============================================================================-

// シーンをまたいで値を残すため static にする
public static class GameResult
{
    private static int lastScore;

    public static bool HasScore { get; private set; }

    public static int LastScore
    {
        get => lastScore;
        set
        {
            lastScore = value;
            HasScore = true;
        }
    }
}
