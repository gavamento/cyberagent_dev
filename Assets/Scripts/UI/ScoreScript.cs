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

    //スコアの格納先
    private int score;
    
    //足すスコアの格納先
    private int totalAddScore;

    //足すスコア描画に使用する変数
    private int viewAddScore;

    private void Start()
    {
        addScoreText.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.text = "Score:" + score.ToString();
        addScoreText.text = "+" + viewAddScore.ToString();

        if (totalAddScore > 0)
        {
            totalAddScore = totalAddScore - 1;
            score = score + 1;
        }

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
        totalAddScore += add;
        viewAddScore = add;

        Color color = addScoreText.color;
        color.a = 1.0f;
        addScoreText.color = color;
    }
}
