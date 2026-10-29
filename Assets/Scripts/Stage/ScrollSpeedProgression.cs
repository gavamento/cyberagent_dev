using UnityEngine;

// スクロール速度を決める。距離で基礎速度が上がり、破壊の勢い(flow)で指数的に加速する
// AutoScrollCamera と同じオブジェクトに付ける。初速は AutoScrollCamera の speed を使う
[RequireComponent(typeof(AutoScrollCamera))]
public class ScrollSpeedProgression : MonoBehaviour
{
    [Header("距離による基礎速度")]
    [Tooltip("この距離(マス)進むごとに基礎速度が上がる")]
    [Min(1f)][SerializeField] float stepDistance = 100f;
    [Tooltip("1段で上がる速さ(マス/秒)")]
    [Min(0f)][SerializeField] float stepAdd = 0.25f;
    [Tooltip("基礎速度の上限(マス/秒)")]
    [SerializeField] float baseMax = 10f;

    [Header("破壊による加速 (flow)")]
    [Tooltip("1回壊すと増える flow")]
    [Min(0f)][SerializeField] float flowPerBreak = 1f;
    [Tooltip("flow の上限")]
    [Min(0f)][SerializeField] float flowMax = 17f;
    [Tooltip("速度 = 基礎速度 × この値の flow 乗")]
    [Min(1f)][SerializeField] float accelRate = 1.057f;
    [Tooltip("最後に壊してからこの秒数たつと flow が減り始める")]
    [Min(0f)][SerializeField] float flowDecayDelay = 1.35f;
    [Tooltip("flow が減る速さ(1秒あたり)")]
    [Min(0f)][SerializeField] float flowDecayRate = 0.85f;
    [Tooltip("最終的な速度の上限(マス/秒)")]
    [SerializeField] float speedCap = 16f;

    [Header("空振り")]
    [Tooltip("空振りで減る flow")]
    [Min(0f)][SerializeField] float whiffFlowLoss = 3.2f;
    [Tooltip("空振りで速度に掛ける倍率")]
    [Range(0f, 1f)][SerializeField] float whiffSpeedRate = 0.67f;
    [Tooltip("空振り後、目標速度を抑える時間(秒)")]
    [Min(0f)][SerializeField] float whiffBrakeTime = 0.48f;

    [Header("被弾")]
    [Tooltip("被弾で減る flow")]
    [Min(0f)][SerializeField] float hurtFlowLoss = 4f;
    [Tooltip("被弾で速度に掛ける倍率")]
    [Range(0f, 1f)][SerializeField] float hurtSpeedRate = 0.76f;

    [Header("なめらかさ")]
    [Tooltip("目標速度へ近づく速さ。大きいほど素早く合わせる")]
    [Min(0f)][SerializeField] float followRate = 5.2f;
    [Tooltip("空振りの抑制中に近づく速さ")]
    [Min(0f)][SerializeField] float brakeFollowRate = 2.3f;
    [Tooltip("空振りの抑制中、目標速度に掛ける倍率")]
    [Range(0f, 1f)][SerializeField] float brakeTargetRate = 0.78f;
    [Tooltip("ペナルティで下がる速度の下限 (初速に対する倍率)")]
    [Range(0f, 1f)][SerializeField] float minSpeedRate = 0.5f;

    [Header("フィーバー明け")]
    [Tooltip("フィーバーが終わってから、初速(一番遅い速度)までゆっくり下がる時間(秒)。0で一瞬で下がる")]
    [Min(0f)][SerializeField] float feverDropTime = 4.0f;
    [Tooltip("初速まで下がった後、フィーバー前の速度に戻るまでの時間(秒)")]
    [Min(0f)][SerializeField] float feverRecoverTime = 4f;

    [Header("確認用 (実行中の値)")]
    [SerializeField] float currentSpeed;
    [SerializeField] float flow;
    [SerializeField] float distance;

    AutoScrollCamera scroll;
    float startX;
    float lastBreakTime = float.NegativeInfinity;
    float brakeTimer;
    float preFeverSpeed;
    float preFeverFlow;
    float recoverTimer;
    float dropTimer;
    float dropFromSpeed;

    public float StartSpeed => scroll.BaseSpeed;
    public float CurrentSpeed => currentSpeed;
    // 初速から増えた分。プレイヤーの前進速度に足す
    public float SpeedGain => currentSpeed - StartSpeed;
    // 初速で 0、上限で 1
    public float Speed01 => Mathf.InverseLerp(StartSpeed, speedCap, currentSpeed);
    public float Flow => flow;
    // flow の割合。0 で無し、1 で上限
    public float Flow01 => flowMax > 0f ? flow / flowMax : 0f;

    void Awake()
    {
        scroll = GetComponent<AutoScrollCamera>();
        startX = transform.position.x;
        currentSpeed = StartSpeed;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        distance = transform.position.x - startX;
        brakeTimer = Mathf.Max(0f, brakeTimer - dt);

        if (Time.time - lastBreakTime > flowDecayDelay)
            flow = Mathf.Max(0f, flow - flowDecayRate * dt);

        float target = Mathf.Min(speedCap, BaseSpeed() * Mathf.Pow(accelRate, flow));
        if (brakeTimer > 0f) target *= brakeTargetRate;

        float rate = brakeTimer > 0f ? brakeFollowRate : followRate;
        currentSpeed += (target - currentSpeed) * Mathf.Min(1f, dt * rate);

        // フィーバー明け: まず初速までゆっくり下げ、そのあとフィーバー前の速度まで、だんだん上限を上げていく
        if (dropTimer > 0f)
        {
            dropTimer = Mathf.Max(0f, dropTimer - dt);
            float p = feverDropTime > 0f ? 1f - dropTimer / feverDropTime : 1f;
            float limit = Mathf.Lerp(dropFromSpeed, StartSpeed, Mathf.SmoothStep(0f, 1f, p));
            currentSpeed = Mathf.Min(currentSpeed, limit);
        }
        else if (recoverTimer > 0f)
        {
            recoverTimer = Mathf.Max(0f, recoverTimer - dt);
            float p = feverRecoverTime > 0f ? 1f - recoverTimer / feverRecoverTime : 1f;
            float limit = Mathf.Lerp(StartSpeed, Mathf.Max(StartSpeed, preFeverSpeed), Mathf.SmoothStep(0f, 1f, p));
            currentSpeed = Mathf.Min(currentSpeed, limit);
        }
    }

    // 距離に応じた基礎速度
    float BaseSpeed()
    {
        int steps = Mathf.FloorToInt(Mathf.Max(0f, distance) / stepDistance);
        return Mathf.Min(baseMax, StartSpeed + steps * stepAdd);
    }

    // 敵やオブジェクトを壊したとき
    public void AddBreak() => AddFlow(flowPerBreak);

    // flow を増やす(コインなど)。減り始めるまでの待ちもやり直す
    public void AddFlow(float amount)
    {
        if (amount <= 0f) return;
        flow = Mathf.Min(flowMax, flow + amount);
        lastBreakTime = Time.time;
    }

    // 攻撃が外れたとき
    public void Whiff()
    {
        flow = Mathf.Max(0f, flow - whiffFlowLoss);
        Slow(whiffSpeedRate);
        brakeTimer = Mathf.Max(brakeTimer, whiffBrakeTime);
    }

    // ダメージを受けたとき
    public void Hurt()
    {
        flow = Mathf.Max(0f, flow - hurtFlowLoss);
        Slow(hurtSpeedRate);
    }

    // フィーバーが始まったとき。そのときの速度と flow を覚えておく
    public void OnFeverStart()
    {
        preFeverSpeed = currentSpeed;
        preFeverFlow = flow;
    }

    // フィーバーが終わったとき。今の速度から feverDropTime 秒かけて初速(一番遅い速度)までゆっくり下げ、
    // そのあと feverRecoverTime 秒かけてフィーバー前の速度まで戻していく
    public void OnFeverEnd()
    {
        flow = preFeverFlow;          // フィーバー中に増えた flow は捨てて、前の勢いに戻す
        lastBreakTime = Time.time;
        brakeTimer = 0f;
        dropFromSpeed = Mathf.Max(StartSpeed, currentSpeed); // ここからゆっくり下げる(一気に落とさない)
        dropTimer = feverDropTime;
        recoverTimer = feverRecoverTime;
    }

    void Slow(float rate)
    {
        currentSpeed = Mathf.Max(StartSpeed * minSpeedRate, currentSpeed * rate);
    }
}