using UnityEngine;
using UnityEngine.UI;

public class ScoreScript : MonoBehaviour
{
    [Header("Debug(Spaceキーで100点追加)")]
    [SerializeField] private bool Active;

    [Header("TotalScoreの表示テキスト")]
    [SerializeField] private Text scoreText;
    [Header("AddScoreの表示テキスト")]
    [SerializeField] private Text addScoreText;
    [Header("AddScoreが消えるまでの速度")]
    [SerializeField] private float fadeTime = 1.5f;
    [Header("加算分を数え終えるまでの時間(秒)。0以下で即反映")]
    [SerializeField] private float countUpTime = 0.5f;

    //足すスコアの格納先
    static private int score;

    //スコア描画に使用する変数(少しずつ数えるので端数を持つ)
    private float viewScore;

    //1秒あたりに数え上げる点数
    private float countSpeed;

    //足すスコア描画に使用する変数
    private int viewAddScore;

    // 今テキストに出している値。-1 は未表示
    private int shownScore = -1;
    private int shownAddScore = -1;

    private void Start()
    {
        addScoreText.gameObject.SetActive(false);
        score = 0;
    }

    // Update is called once per frame
    void Update()
    {
        RefreshTexts();

        // 加算分を countUpTime 秒で数え終える(フレームレートに依存しない)
        if (viewScore < score)
            viewScore = Mathf.MoveTowards(viewScore, score, countSpeed * Time.deltaTime);

        if (addScoreText.color.a > 0)
        {
            Color color = addScoreText.color;
            color.a -= fadeTime * Time.deltaTime;
            addScoreText.color = color;
        }
        else { addScoreText.gameObject.SetActive(false); }

        if (Active)
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {
                AddScore(100);
            }
        }
    }

    public void AddScore(int add)
    {
        addScoreText.gameObject.SetActive(true);
        viewAddScore = add;

        Color color = addScoreText.color;
        color.a = 1.0f;
        addScoreText.color = color;

        score += add;

        // 残りの差を countUpTime 秒で数え終える速さにする(連続で入っても間に合う)
        float remain = score - viewScore;
        countSpeed = countUpTime > 0f ? remain / countUpTime : float.MaxValue;
    }

    // 値が変わったときだけテキストを作り直す(毎フレームの文字列生成を避ける)
    private void RefreshTexts()
    {
        int view = Mathf.FloorToInt(viewScore);
        if (view != shownScore)
        {
            shownScore = view;
            scoreText.text = "Score:" + view;
        }
        if (viewAddScore != shownAddScore)
        {
            shownAddScore = viewAddScore;
            addScoreText.text = "+" + viewAddScore;
        }
    }

    public static int GetScore()
    {  return score; }
}
