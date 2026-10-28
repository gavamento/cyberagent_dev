using UnityEngine;
using UnityEngine.UI;

public class ScoreJudgeScript : MonoBehaviour
{
    [Header("デバックモード")]
    [SerializeField] private bool Active;

    [Header("子オブジェクト")]
    [SerializeField] private UIImageScript Text;

    [Header("子オブジェクトの色")]
    [SerializeField] private Color Perfect;
    [SerializeField] private Color Great;
    [SerializeField] private Color Good;
    [SerializeField] private Color Bad;

    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 scale;
    [SerializeField] private float timer;

    [Header("SetScore関数のデフォルト値")]
    [SerializeField] private const float defaultTime = 1;


    // Update is called once per frame
    void Update()
    {
        if (Active)
        {
            Vector3 d = new Vector3(940.0f,540.0f,0.0f);

            if (Input.GetKeyDown(KeyCode.Q))
            {
                SetScorePerfect(d, 3.0f);
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                SetScoreGreat(d, 3.0f);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                SetScoreGood(d, 3.0f);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                SetScoreBad(d, 3.0f);
            }
        }
    }



    public void SetScorePerfect(Vector3 position = default,float time = defaultTime)
    {
        UIImageScript UI = Instantiate(Text, transform);
        UI.SetTime(position + offset, time);
        UI.SetText("Perfect!!!");
        UI.SetColor(Perfect);
    }

    public void SetScoreGreat(Vector3 position = default,float time = defaultTime)
    {
        UIImageScript UI = Instantiate(Text, transform);
        UI.SetTime(position + offset, time);
        UI.SetText("Great!!");
        UI.SetColor(Great);
    }

    public void SetScoreGood(Vector3 position = default,float time = defaultTime)
    {
        UIImageScript UI = Instantiate(Text, transform);
        UI.SetTime(position + offset, time);
        UI.SetText("Good!");
        UI.SetColor(Good);
    }

    public void SetScoreBad(Vector3 position = default,float time = defaultTime)
    {
        UIImageScript UI = Instantiate(Text, transform);
        UI.SetTime(position + offset, time);
        UI.SetText("Bad...");
        UI.SetColor(Bad);
    }
}
