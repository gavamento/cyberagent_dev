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

    private void Start()
    {
        addScoreText.gameObject.SetActive(false);
        score = 0;
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.text = "Score:" + Mathf.FloorToInt(viewScore).ToString();
        addScoreText.text = "+" + viewAddScore.ToString();

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

    public static int GetScore()
    {  return score; }
}
