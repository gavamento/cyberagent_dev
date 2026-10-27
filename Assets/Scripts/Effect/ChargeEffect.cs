/*
* ファイル名　ChargeEffect.cs
* タイトル　　チャージエフェクト
* 作成者　　　久保木幹太
* 作成日　　　10月27日
* 更新日　　　10月27日
*/

using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class ChargeEffect : MonoBehaviour
{
    [Header("チャージエフェクト")]
    [SerializeField] private VisualEffect chargeEffect;

    [Header("チャージ中")]
    [Tooltip("ため途中の時の星の最大の濃さ")]
    [SerializeField, Range(0.0f, 1.0f)] private float chargingMaxAlpha = 0.6f;

    [Header("チャージ完了")]
    [Tooltip("チャージ後に星が回る速さ")]
    [SerializeField] private float orbitSpeed = 6.0f;
    [Tooltip("チャージ完了の瞬間に星が大きくなる倍率")]
    [SerializeField] private float popScale = 1.6f;
    [Tooltip("大きくなった星が元の大きさに戻るまでの時間")]
    [SerializeField] private float popDuration = 0.12f;

    [Header("溜め攻撃の時")]
    [Tooltip("回転を止めてから砕けるまでの時間")]
    [SerializeField] private float freezeTime = 0.06f;

    [Header("追従先")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 offset = Vector3.zero;

    // VFXGraphのプロパティのIDを格納
    private static readonly int StarAlphaID = Shader.PropertyToID("StarAlpha");
    private static readonly int StarScaleID = Shader.PropertyToID("StarScale");
    private static readonly int OrbitSpeedID = Shader.PropertyToID("OrbitSpeed");
    private static readonly int StarAliveID = Shader.PropertyToID("StarAlive");
    private static readonly int ShatterID = Shader.PropertyToID("Shatter");

    // VFXGraphのイベントのIDを格納
    private static readonly int OnChargeStartID = Shader.PropertyToID("OnChargeStart");
    private static readonly int OnChargeReadyID = Shader.PropertyToID("OnChargeReady");
    private static readonly int OnReleaseID = Shader.PropertyToID("OnRelease");
    private static readonly int OnChargeCancelID = Shader.PropertyToID("OnChargeCancel");

    // 星が出ているか
    public bool IsCharging { get; private set; } = false;

    // 溜め攻撃ができるかどうか
    public bool IsReady { get; private set; } = false;

    // 溜め攻撃して砕けるまでの待ち時間中か
    private bool isShattering = false;

    private void Awake()
    {
        // 付け忘れたらオブジェクトから探す
        if (chargeEffect == null)
        {
            chargeEffect = GetComponent<VisualEffect>();
        }
    }

    // 押した瞬間に呼ぶ
    public void StartCharge()
    {
        // 砕けると同時に再開すると新しい星まで消えちゃうから待つ
        if (IsCharging || isShattering) return;
        IsCharging = true;
        IsReady = false;

        // 溜めはじめは透明、等倍、回転なし(各情報をVFXに送る)
        chargeEffect.SetFloat(StarAlphaID, 0.0f);
        chargeEffect.SetFloat(StarScaleID, 1.5f);
        chargeEffect.SetFloat(OrbitSpeedID, 0.0f);
        chargeEffect.SetBool(ShatterID, false);

        // 先に生存フラグを立ててから出す(逆だと一瞬で消えるから注意)
        chargeEffect.SetBool(StarAliveID, true);
        chargeEffect.SendEvent(OnChargeStartID);
    }

    // 溜め具合を反映する(0~1で押してる間に毎フレーム呼ぶ)
    public void SetCharge(float rate)
    {
        // チャージ完了したら官僚の見た目を優先
        if (!IsCharging || IsReady) return;

        // 情報をVFXに送る
        chargeEffect.SetFloat(StarAlphaID, Mathf.Clamp01(rate) * chargingMaxAlpha);
    }

    // 溜め攻撃の準備完了
    public void Ready()
    {
        // 毎フレーム呼ばれても送るのは一回だけ
        if (IsReady) return;
        // StartChargeを通ってなくても動くようにする
        if (!IsCharging) StartCharge();
        IsReady = true;

        // 情報をVFXに送る
        chargeEffect.SetFloat(StarAlphaID, 1.0f);
        chargeEffect.SetFloat(OrbitSpeedID, orbitSpeed);
        chargeEffect.SendEvent(OnChargeReadyID);
        StartCoroutine(Pop());
    }

    // 溜め攻撃を解放
    public void Release()
    {
        if (!IsReady) return;
        IsReady = false;
        IsCharging = false;

        StopAllCoroutines();
        StartCoroutine(FreezeAndShatter());
    }

    // 溜めを中断(消すだけ)
    public void Cancel()
    {
        if (!IsCharging && !isShattering) return;
        IsCharging = false;
        IsReady = false;
        isShattering = false;
        StopAllCoroutines();

        // 情報をVFXに送る
        chargeEffect.SetBool(ShatterID, false);
        chargeEffect.SetBool(StarAliveID, false);
        chargeEffect.SendEvent(OnChargeCancelID);
    }

    // チャージ完了したら瞬間的に星を大きくして元の大きさに戻す
    private IEnumerator Pop()
    {
        float elapsedTime = 0.0f;

        while (elapsedTime < popDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / popDuration);
            chargeEffect.SetFloat(StarScaleID, Mathf.Lerp(popScale, 1.5f, t));
            yield return null;
        }

        chargeEffect.SetFloat(StarScaleID, 1.5f);
    }

    // 回転を止めて一瞬見せてから砕く
    private IEnumerator FreezeAndShatter()
    {
        isShattering = true;
        chargeEffect.SetFloat(OrbitSpeedID, 0.0f);
        chargeEffect.SetFloat(StarScaleID, 2.0f);
        yield return new WaitForSeconds(freezeTime);

        // 砕く設定にしてかrあ星を消す(消えた位置から破片が出る)
        chargeEffect.SetBool(ShatterID, true);
        chargeEffect.SetBool(StarAliveID, false);
        chargeEffect.SendEvent(OnReleaseID);
        isShattering = false;
    }


    // プレイヤーの移動が終わった後に位置を合わせる
    private void LateUpdate()
    {
        if (followTarget == null) return;

        transform.position = followTarget.position + offset;
    }

    // 非表示や破棄の時にループが残らないようにする
    private void OnDisable()
    {
        Cancel();
    }
}