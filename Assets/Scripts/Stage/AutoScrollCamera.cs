using UnityEngine;

// 一定速度で右へ進むカメラ。地面の高さにゆるく追従する。動作確認用
public class AutoScrollCamera : MonoBehaviour
{
    [Tooltip("右へ進む速さ (マス/秒)")]
    [SerializeField] float speed = 8f;
    [Tooltip("高さの追従に使う生成器。未指定なら追従しない")]
    [SerializeField] StageGenerator stage;
    [Tooltip("地面の上面からのカメラの高さ")]
    [SerializeField] float yOffset = 3f;
    [Tooltip("高さの追従の速さ。大きいほど素早く合わせる")]
    [Min(0f)][SerializeField] float followSmooth = 2f;

    void LateUpdate()
    {
        var pos = transform.position;
        pos.x += speed * Time.deltaTime;

        if (stage != null && stage.TryGetSurfaceY(pos.x, out float groundY))
        {
            float t = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
            pos.y = Mathf.Lerp(pos.y, groundY + yOffset, t);
        }
        transform.position = pos;
    }
}
