using UnityEngine;

// シーン開始時に BGM を流す。各シーンに 1 つ置く
public class SceneBgm : MonoBehaviour
{
    [SerializeField] private AudioClip bgm;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("ON なら bgm が空のときに BGM を止める")]
    [SerializeField] private bool stopIfEmpty = false;

    void Start()
    {
        if (bgm != null) AudioManager.PlayBGM(bgm, volume);
        else if (stopIfEmpty) AudioManager.StopBGM();
    }
}
