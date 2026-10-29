/*
* ファイル名　ChargeEffectParticle.cs
* タイトル　　チャージエフェクト(Particle System 版)
* 作成者　　　秋田蓮音
* 作成日　　　10月29日
* 更新日　　　10月29日
*/

using System.Collections;
using UnityEngine;

// ChargeEffectCute.vfx を Particle System で作り直したもの。WebGL でも動く
// 呼び方は ChargeEffect と同じ(StartCharge → SetCharge → Ready → Release / Cancel)
// 星の位置は毎フレームこのスクリプトで決めて、Particle System には描画だけさせる
public class ChargeEffectParticle : MonoBehaviour
{
    [Header("パーティクル")]
    [Tooltip("周りに並ぶ星")]
    [SerializeField] private ParticleSystem stars;
    [Tooltip("溜め攻撃で星が砕けた破片")]
    [SerializeField] private ParticleSystem shards;

    [Header("星の並び")]
    [SerializeField, Min(1)] private int starCount = 8;
    [Tooltip("楕円の半径。x が横、y が縦")]
    [SerializeField] private Vector2 orbitRadius = new Vector2(1.6f, 2.5f);
    [Tooltip("StarScale が 1 のときの星の大きさ")]
    [SerializeField, Min(0.0f)] private float starSize = 0.3f;
    [SerializeField] private Color starColor = new Color(1.0f, 1.0f, 0.49f, 1.0f);

    [Header("チャージ中")]
    [Tooltip("溜め途中の時の星の最大の濃さ")]
    [SerializeField, Range(0.0f, 1.0f)] private float chargingMaxAlpha = 0.6f;

    [Header("チャージ完了")]
    [Tooltip("チャージ後に星が回る速さ(ラジアン/秒)")]
    [SerializeField] private float orbitSpeed = 6.0f;
    [Tooltip("チャージ完了の瞬間に星が大きくなる倍率")]
    [SerializeField] private float popScale = 1.6f;
    [Tooltip("大きくなった星が元の大きさに戻るまでの時間")]
    [SerializeField] private float popDuration = 0.12f;

    [Header("溜め攻撃の時")]
    [Tooltip("回転を止めてから砕けるまでの時間")]
    [SerializeField] private float freezeTime = 0.06f;
    [Tooltip("星 1 個から出る破片の数")]
    [SerializeField, Min(0)] private int shardsPerStar = 5;
    [SerializeField] private float shardLifetime = 0.5f;
    [SerializeField] private float shardSize = 0.12f;
    [Tooltip("破片の初速の範囲(最小)")]
    [SerializeField] private Vector2 shardVelocityMin = new Vector2(-6.0f, 2.0f);
    [Tooltip("破片の初速の範囲(最大)")]
    [SerializeField] private Vector2 shardVelocityMax = new Vector2(6.0f, 9.0f);
    [Tooltip("破片の回転の速さの最大(度/秒)")]
    [SerializeField] private float shardSpin = 720.0f;

    [Header("追従先")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 offset = Vector3.zero;

    // 星が出ている間の基本の倍率と、砕ける直前の倍率
    private const float BaseScale = 1.5f;
    private const float FreezeScale = 2.0f;
    // 星は自分で消すので寿命は十分長くしておく
    private const float StarLifetime = 99.0f;

    // 星が出ているか
    public bool IsCharging { get; private set; } = false;

    // 溜め攻撃ができるかどうか
    public bool IsReady { get; private set; } = false;

    // 溜め攻撃して砕けるまでの待ち時間中か
    private bool isShattering = false;

    // VFX 版のプロパティに当たる値
    private bool starAlive = false;
    private float starAlpha = 0.0f;
    private float starScale = BaseScale;
    private float currentOrbitSpeed = 0.0f;

    private float[] angles;
    private ParticleSystem.Particle[] buffer;

    private void Awake()
    {
        angles = new float[starCount];
        buffer = new ParticleSystem.Particle[starCount];

        var main = stars.main;
        main.maxParticles = starCount;
    }

    // 押した瞬間に呼ぶ
    public void StartCharge()
    {
        // 砕けると同時に再開すると新しい星まで消えちゃうから待つ
        if (IsCharging || isShattering) return;
        IsCharging = true;
        IsReady = false;

        // 溜めはじめは透明、等倍、回転なし
        starAlpha = 0.0f;
        starScale = BaseScale;
        currentOrbitSpeed = 0.0f;

        // 等間隔に並べる
        for (int i = 0; i < starCount; i++)
        {
            angles[i] = Mathf.PI * 2.0f * i / starCount;
        }

        starAlive = true;
        stars.Play();
        UpdateStars();
    }

    // 溜め具合を反映する(0~1で押してる間に毎フレーム呼ぶ)
    public void SetCharge(float rate)
    {
        // チャージが完了したら完了の見た目を優先
        if (!IsCharging || IsReady) return;

        starAlpha = Mathf.Clamp01(rate) * chargingMaxAlpha;
    }

    // 溜め攻撃の準備完了
    public void Ready()
    {
        // 毎フレーム呼ばれても動くのは一回だけ
        if (IsReady) return;
        // StartCharge を通してなくても動くようにする
        if (!IsCharging) StartCharge();
        IsReady = true;

        starAlpha = 1.0f;
        currentOrbitSpeed = orbitSpeed;
        StartCoroutine(Pop());
    }

    // 溜め攻撃を放つ
    public void Release()
    {
        if (!IsReady) return;
        IsReady = false;
        IsCharging = false;

        StopAllCoroutines();
        StartCoroutine(FreezeAndShatter());
    }

    // 溜めを中断(星を消す)
    public void Cancel()
    {
        if (!IsCharging && !isShattering) return;
        IsCharging = false;
        IsReady = false;
        isShattering = false;
        StopAllCoroutines();

        HideStars();
    }

    // チャージ完了した瞬間的に星を大きくして元の大きさに戻す
    private IEnumerator Pop()
    {
        float elapsedTime = 0.0f;

        while (elapsedTime < popDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / popDuration);
            starScale = Mathf.Lerp(popScale, BaseScale, t);
            yield return null;
        }

        starScale = BaseScale;
    }

    // 回転を止めて一瞬膨らませてから砕く
    private IEnumerator FreezeAndShatter()
    {
        isShattering = true;
        currentOrbitSpeed = 0.0f;
        starScale = FreezeScale;
        yield return new WaitForSeconds(freezeTime);

        // 星があった場所から破片を出す
        EmitShards();
        HideStars();
        isShattering = false;
    }

    private void EmitShards()
    {
        if (shards == null || shardsPerStar <= 0) return;

        shards.Play();
        var emitParams = new ParticleSystem.EmitParams
        {
            startLifetime = shardLifetime,
            startSize = shardSize,
            startColor = Color.white,
        };

        for (int i = 0; i < starCount; i++)
        {
            emitParams.position = StarPosition(i);
            for (int j = 0; j < shardsPerStar; j++)
            {
                emitParams.velocity = new Vector3(
                    Random.Range(shardVelocityMin.x, shardVelocityMax.x),
                    Random.Range(shardVelocityMin.y, shardVelocityMax.y),
                    0.0f);
                emitParams.angularVelocity = Random.Range(-shardSpin, shardSpin);
                shards.Emit(emitParams, 1);
            }
        }
    }

    private void HideStars()
    {
        starAlive = false;
        // シーンを閉じる時は子が先に消えていることがある
        if (stars == null) return;
        stars.Clear();
        stars.Stop();
    }

    // Particle System のローカル座標での星の位置
    private Vector3 StarPosition(int index)
    {
        return new Vector3(Mathf.Cos(angles[index]) * orbitRadius.x, Mathf.Sin(angles[index]) * orbitRadius.y, 0.0f);
    }

    // 星の位置・大きさ・濃さを Particle System に書き込む
    private void UpdateStars()
    {
        Color color = starColor;
        color.a *= starAlpha;

        for (int i = 0; i < starCount; i++)
        {
            buffer[i].position = StarPosition(i);
            buffer[i].startSize = starSize * starScale;
            buffer[i].startColor = color;
            buffer[i].startLifetime = StarLifetime;
            buffer[i].remainingLifetime = StarLifetime;
        }

        stars.SetParticles(buffer, starCount);
    }

    // プレイヤーの移動が終わった後に位置を合わせてから星を動かす
    private void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + offset;
        }

        if (!starAlive) return;

        // VFX 版と同じく時計回り
        for (int i = 0; i < starCount; i++)
        {
            angles[i] -= currentOrbitSpeed * Time.deltaTime;
        }

        UpdateStars();
    }

    // 非表示や破棄の時に星が残らないようにする
    private void OnDisable()
    {
        Cancel();
    }
}
