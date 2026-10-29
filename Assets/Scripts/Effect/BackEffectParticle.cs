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
// プレイヤーの横の速さに合わせて、風の量と流れる速さを変える。フィーバー中は風の量を増やす
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

    [Header("フィーバー")]
    [Tooltip("フィーバー状態を調べる対象 空だったらplayerと同じオブジェクトから探す")]
    [SerializeField] private PlayerCombat playerCombat;
    [Tooltip("フィーバー中に風の量を何倍にするか")]
    [SerializeField, Min(1.0f)] private float feverRateScale = 10.0f;
    [Tooltip("フィーバーに入ってから線が最大までの時間")]
    [SerializeField, Min(0.01f)] private float feverBlendInTime = 0.3f;
    [Tooltip("フィーバーが終わってから、通常の本数に戻るまでの時間")]
    [SerializeField, Min(0.01f)] private float feverBlendOutTime = 0.6f;

    private float fixedY;

    // 0=通常 1=フィーバー(なめらかに切り替えるための値)
    private float feverBlend;

    private void Start()
    {
        // 最初のY座標を保持
        fixedY = backEffect.transform.position.y;

        // 指定がなければプレイヤーのオブジェクトから探す
        if (playerCombat == null && player != null)
        {
            playerCombat = player.GetComponent<PlayerCombat>();
        }
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

        // フィーバー中は1、それ以外は0に向かって動かす。増えるときは速く、戻るときはゆっくり
        float target = (playerCombat != null && playerCombat.IsFever) ? 1.0f : 0.0f;
        float time = target > feverBlend ? feverBlendInTime : feverBlendOutTime;
        feverBlend = Mathf.MoveTowards(feverBlend, target, Time.deltaTime / time);

        var emission = backEffect.emission;
        emission.rateOverTimeMultiplier = speed / rateReferenceSpeed * maxRate * Mathf.Lerp(1.0f, feverRateScale, feverBlend);

        float t = speed / speedReferenceSpeed;
        var main = backEffect.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            Mathf.LerpUnclamped(slowSpeedRange.x, fastSpeedRange.x, t),
            Mathf.LerpUnclamped(slowSpeedRange.y, fastSpeedRange.y, t));
    }
}
