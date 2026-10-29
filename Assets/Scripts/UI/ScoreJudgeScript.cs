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

    [Header("効果音(判定が出たときに鳴らす。空なら鳴らさない)")]
    [SerializeField] private AudioClip perfectSe;
    [Range(0f, 1f)][SerializeField] private float perfectVolume = 1f;
    [SerializeField] private AudioClip greatSe;
    [Range(0f, 1f)][SerializeField] private float greatVolume = 1f;
    [SerializeField] private AudioClip goodSe;
    [Range(0f, 1f)][SerializeField] private float goodVolume = 1f;
    [SerializeField] private AudioClip badSe;
    [Range(0f, 1f)][SerializeField] private float badVolume = 1f;
    [Tooltip("効果音を鳴らすAudioSource。空なら自分に自動で付ける。ミキサーの出力先を決めたいときは、ここに指定する")]
    [SerializeField] private AudioSource seSource;

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
            Vector3 d = new Vector3(940.0f, 540.0f, 0.0f);

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



    // 音量(0〜1)を掛けて、効果音を鳴らす
    private void PlaySe(AudioClip clip, float volume)
    {
        if (clip == null) return;

        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
            seSource.playOnAwake = false;
            seSource.spatialBlend = 0f; // 2Dの音
        }
        seSource.PlayOneShot(clip, volume);
    }

    public void SetScorePerfect(Vector3 position = default, float time = defaultTime)
    {
        UIImageScript UI = Instantiate(Text, transform);
        UI.SetTime(position + offset, time);
        UI.SetText("Perfect!!!");
        UI.SetColor(Perfect);
        PlaySe(perfectSe, perfectVolume);
    }

    public void SetScoreGreat(Vector3 position = default, float time = defaultTime)
    {
        UIImageScript UI = Instantiate(Text, transform);
        UI.SetTime(position + offset, time);
        UI.SetText("Great!!");
        UI.SetColor(Great);
        PlaySe(greatSe, greatVolume);
    }

    public void SetScoreGood(Vector3 position = default, float time = defaultTime)
    {
        UIImageScript UI = Instantiate(Text, transform);
        UI.SetTime(position + offset, time);
        UI.SetText("Good!");
        UI.SetColor(Good);
        PlaySe(goodSe, goodVolume);
    }

    public void SetScoreBad(Vector3 position = default, float time = defaultTime)
    {
        UIImageScript UI = Instantiate(Text, transform);
        UI.SetTime(position + offset, time);
        UI.SetText("Bad...");
        UI.SetColor(Bad);
        PlaySe(badSe, badVolume);
    }
}