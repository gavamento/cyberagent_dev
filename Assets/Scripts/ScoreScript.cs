using UnityEngine;
using UnityEngine.UI;

public class ScoreScript : MonoBehaviour
{
    [SerializeField] private int m_score;

    [SerializeField] private Text scoreText;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        m_score += 1;
        scoreText.text = "Score:" + m_score.ToString();
    }

    public void SetScore(int score)
    {
       m_score = score;
    }
}
