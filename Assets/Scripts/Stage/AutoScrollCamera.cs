using UnityEngine;

// 右へ進むカメラ。地面の高さにゆるく追従する
// 同じオブジェクトに ScrollSpeedProgression があれば、その速度で進む
public class AutoScrollCamera : MonoBehaviour
{
    [Tooltip("右へ進む速さ (マス/秒)。ScrollSpeedProgression があるときは初速")]
    [SerializeField] float speed = 8f;
    [Tooltip("高さの追従に使う生成器。未指定なら追従しない")]
    [SerializeField] StageGenerator stage;
    [Tooltip("地面の上面からのカメラの高さ")]
    [SerializeField] float yOffset = 3f;
    [Tooltip("高さの追従の速さ。大きいほど素早く合わせる")]
    [Min(0f)][SerializeField] float followSmooth = 2f;

    ScrollSpeedProgression progression;

    public float BaseSpeed => speed;
    public float CurrentSpeed => progression != null ? progression.CurrentSpeed : speed;

    void Awake()
    {
        progression = GetComponent<ScrollSpeedProgression>();
    }

    void LateUpdate()
    {
        var pos = transform.position;
        pos.x += CurrentSpeed * Time.deltaTime;

        if (stage != null && stage.TryGetSurfaceY(pos.x, out float groundY))
        {
            float t = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
            pos.y = Mathf.Lerp(pos.y, groundY + yOffset, t);
        }
        transform.position = pos;
    }
}
