using UnityEngine;
using UnityEngine.UI;

public class ScoreRankScript : MonoBehaviour
{
    [Header("使うテキスト")]
    [SerializeField] private Text scoreText;

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

    // Update is called once per frame
    void Update()
    {
        int nowScore = ScoreScript.GetScore();

        if (nowScore > limitS)      { scoreText.text = "S"; scoreText.color = colorS; }
        else if (nowScore > limitA) { scoreText.text = "A"; scoreText.color = colorA; } 
        else if (nowScore > limitB) { scoreText.text = "B"; scoreText.color = colorB; }  
        else if (nowScore > limitC) { scoreText.text = "C"; scoreText.color = colorC; }
        else if (nowScore > limitD) { scoreText.text = "D"; scoreText.color = colorD; }
        else { scoreText.text = "E"; scoreText.color = colorE; }
       
    }
}
