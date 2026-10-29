/*
* ファイル名　BackEffectParticle.cs
* タイトル　　背景の風エフェクト(Particle System 版)
* 作成者　　　秋田蓮音
* 作成日　　　10月29日
* 更新日　　　10月29日
*/

using UnityEngine;

// backEffect.vfx を Particle System で作り直したもの。WebGL でも動く
// 置き方は BackEffect と同じ。X だけプレイヤーに追従し、Y は最初の位置で固定
// プレイヤーの横の速さに合わせて、風の量と流れる速さを変える
public class BackEffectParticle : MonoBehaviour
{
    [SerializeField] private ParticleSystem backEffect;
    [SerializeField] private Rigidbody2D player;

    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    [Header("風の量")]
    [Tooltip("プレイヤーがこの速さのとき、maxRate 個/秒 出す(0 のときは 0 個)")]
    [SerializeField, Min(0.01f)] private float rateReferenceSpeed = 30.0f;
    [SerializeField, Min(0.0f)] private float maxRate = 200.0f;

    [Header("風の流れる速さ")]
    [Tooltip("プレイヤーがこの速さのとき、fastSpeedRange になる")]
    [SerializeField, Min(0.01f)] private float speedReferenceSpeed = 20.0f;
    [Tooltip("プレイヤーが止まっているときの速さの範囲(最小, 最大)")]
    [SerializeField] private Vector2 slowSpeedRange = new Vector2(1.0f, 10.0f);
    [Tooltip("プレイヤーが speedReferenceSpeed のときの速さの範囲(最小, 最大)")]
    [SerializeField] private Vector2 fastSpeedRange = new Vector2(20.0f, 40.0f);

    private float fixedY;

    private void Start()
    {
        // 最初のY座標を保持
        fixedY = backEffect.transform.position.y;
    }

    private void Update()
    {
        if (backEffect == null || player == null) return;

        // X だけプレイヤーに追従し、Y は固定する
        Vector3 position = backEffect.transform.position;
        position.x = player.transform.position.x + positionOffset.x;
        position.y = fixedY + positionOffset.y;
        backEffect.transform.position = position;

        // VFX 版の Remap と同じく、範囲の外でもそのまま伸ばす
        float speed = Mathf.Abs(player.linearVelocity.x);

        var emission = backEffect.emission;
        emission.rateOverTimeMultiplier = speed / rateReferenceSpeed * maxRate;

        float t = speed / speedReferenceSpeed;
        var main = backEffect.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            Mathf.LerpUnclamped(slowSpeedRange.x, fastSpeedRange.x, t),
            Mathf.LerpUnclamped(slowSpeedRange.y, fastSpeedRange.y, t));
    }
}
