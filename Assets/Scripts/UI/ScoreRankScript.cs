using UnityEngine;
using UnityEngine.UI;

public class ScoreRankScript : MonoBehaviour
{
    [Header("Rank表示テキスト")]
    [SerializeField] private Text scoreText;
    [SerializeField] static private Text scoreSendText;

    [Header("Eランクに行く値")]
    [SerializeField] private int limitE;
    [SerializeField] private Color colorE;
    [Header("Dランクに行く値")]
    [SerializeField] private int limitD;
    [SerializeField] private Color colorD;
    [Header("Cランクに行く値")]
    [SerializeField] private int limitC;
    [SerializeField] private Color colorC;
    [Header("Bランクに行く値")]
    [SerializeField] private int limitB;
    [SerializeField] private Color colorB;
    [Header("Aランクに行く値")]
    [SerializeField] private int limitA;
    [SerializeField] private Color colorA;
    [Header("Sランクに行く値")]
    [SerializeField] private int limitS;
    [SerializeField] private Color colorS;

    [Header("NextRank表示テキスト")]
    [SerializeField] private Text nextText;
   
    // Update is called once per frame
    void Update()
    {
        int nowScore = ScoreScript.GetScore();

        if (nowScore > limitS)      { scoreText.text = "S"; scoreText.color = colorS; nextText.text = "";}
        else if (nowScore > limitA) { scoreText.text = "A"; scoreText.color = colorA; nextText.text = "RankUp→" + limitS; nextText.color = colorS; } 
        else if (nowScore > limitB) { scoreText.text = "B"; scoreText.color = colorB; nextText.text = "RankUp→" + limitA; nextText.color = colorA; }  
        else if (nowScore > limitC) { scoreText.text = "C"; scoreText.color = colorC; nextText.text = "RankUp→" + limitB; nextText.color = colorB; }
        else if (nowScore > limitD) { scoreText.text = "D"; scoreText.color = colorD; nextText.text = "RankUp→" + limitC; nextText.color = colorC; }
        else { scoreText.text = "E"; scoreText.color = colorE; nextText.text = "RankUp→" + limitD; nextText.color = colorD; }

        scoreSendText = scoreText;
    }

    static Text GetRank() {  return scoreSendText; }
}
