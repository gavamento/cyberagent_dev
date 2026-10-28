using UnityEngine;
using UnityEngine.UI;

public class ScoreJudgeScript : MonoBehaviour
{
    [Header("デバックモード")]
    [SerializeField] private bool Active;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("使うテクスチャデータ")]
    [SerializeField] private Sprite perfectTexture;
    [SerializeField] private Sprite greatTexture;
    [SerializeField] private Sprite goodTexture;
    [SerializeField] private Sprite badTexture;

    [Header("子オブジェクト")]
    [SerializeField] private Image judgeObject;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 scale;
    [SerializeField] private float timer;

    [Header("SetScore関数のデフォルト値")]
    [SerializeField] private const float defaultTime = 1;

    void Start()
    {
        judgeObject.gameObject.SetActive(false);
        judgeObject.sprite = perfectTexture;
        judgeObject.preserveAspect = true;
        // scale が 0 のまま(シーンの初期値)なら、画像の元の大きさを使う
        if (scale != Vector3.zero) judgeObject.transform.localScale = scale;
    }

    // Update is called once per frame
    void Update()
    {
        if (judgeObject != null)
        {
            timer -= Time.deltaTime;

            if (timer < 0)
            {
                timer = 0;
                judgeObject.gameObject.SetActive(false);
            }

        }

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
        judgeObject.transform.position = (position + offset);
        timer = time;

        judgeObject.gameObject.SetActive(true);
        judgeObject.sprite = perfectTexture;
    }

    public void SetScoreGreat(Vector3 position = default,float time = defaultTime)
    {
        judgeObject.transform.position = position + offset;
        timer = time;

        judgeObject.gameObject.SetActive(true);
        judgeObject.sprite = greatTexture;
    }

    public void SetScoreGood(Vector3 position = default,float time = defaultTime)
    {
        judgeObject.transform.position = position + offset;
        timer = time;

        judgeObject.gameObject.SetActive(true);
        judgeObject.sprite = goodTexture;
    }

    public void SetScoreBad(Vector3 position = default,float time = defaultTime)
    {
        judgeObject.transform.position = position + offset;
        timer = time;

        judgeObject.gameObject.SetActive(true);
        judgeObject.sprite = badTexture;
    }
}
