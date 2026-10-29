//===============================================================================-
//プレイヤーが触れると拾えるコイン(スコアと加速)    秋田蓮音
//===============================================================================-
using UnityEngine;

// コインのプレハブに付ける。同じ GameObject の Is Trigger の Collider2D にプレイヤーの体が触れたら拾う
// StageGenerator がランダムに置く。取ると点数が入り、敵を壊したときと同じ flow が増えて加速する
// 取った枚数は PlayerCombat.Coins に数え、リザルトで CoinBonus のボーナスになる
public class Coin : MonoBehaviour
{
    [Tooltip("取ったときに入る点数")]
    [SerializeField, Min(0)] private int points = 300;
    [Tooltip("取ったときに増える flow (敵を1回壊すと ScrollSpeedProgression の flowPerBreak だけ増える)")]
    [SerializeField, Min(0f)] private float flowAmount = 1f;
    [SerializeField] private AudioClip pickupSe;

    [Header("ふわふわ動くアニメーション")]
    [Tooltip("動かす対象 空ならオブジェクト自身 このスプライトを指定すると見た目だけ動く")]
    [SerializeField] private Transform visual;
    [Tooltip("上下の揺れ幅")]
    [SerializeField] private float bobHeight = 0.15f;
    [Tooltip("一秒あたりの上下の往復回数")]
    [SerializeField, Min(0.0f)] private float bobFrequency = 0.8f;
    [Tooltip("横の揺れ幅(0なら上下だけ")]
    [SerializeField, Min(0.0f)] private float swayWidth = 0.04f;

    [SerializeField] private GameObject effectPrefab;       // パーティクルのプレハブ
    [SerializeField] private GameObject debrisPrefab;       // 破片のプレハブ
    [SerializeField] private GameObject shockPrefab;        // 衝撃波のプレハブ

    private bool collected;

    // 今足している揺れの量
    private Vector3 appliedOffset;
    // 揺れのタイミングのずれ
    private float phase;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        // 指定がなければ自分自身を動かす
        if (visual == null) visual = transform;
    }

    private void Start()
    {
        // 置かれた位置からのずれをきめる
        Vector3 p = transform.position;
        phase = p.x * 1.7f + p.y * 0.9f;
    }

    private void Update()
    {
        float t = Time.time * bobFrequency * 2.0f * Mathf.PI + phase;

        // 上下の速さはそのままで横は半分の速さにして緩い8の字を描く
        Vector3 offset = new Vector3(Mathf.Sin(t * 0.5f) * swayWidth, Mathf.Sin(t) * bobHeight, 0.0f);

        // 前回との差だけ動かす
        visual.localPosition += offset - appliedOffset;
        appliedOffset = offset;
    }

    private void OnDisable()
    {
        // 無効になった時は揺れを戻してずれたまま残らないようにする
        if (visual != null) visual.localPosition -= appliedOffset;
        appliedOffset = Vector3.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponent<HitAttackBox>() != null) return; // ハンマーでは拾わず、体で触れたときだけ

        var player = other.GetComponentInParent<PlayerCombat>();
        if (player == null) return;

        var health = player.GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;

        collected = true; // 体の Collider が複数あっても1回だけ
        player.AddScore(points);
        player.AddCoin();
        player.AddFlow(flowAmount);
        AudioManager.PlaySE(pickupSe);

        // パーティクル
        EffectManager.PlayEffectWithOwnerSpriteAndDebris(effectPrefab, debrisPrefab, transform, 5);
        EffectManager.PlayEffect(shockPrefab, transform);

        Destroy(gameObject);
    }
}