//===============================================================================-
//プレイヤーの移動系のスクリプト    鍋坂明穂
//===============================================================================-
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using CyberAgentDev.ObjectPrototypeLearning;

public class PlayerCombat : MonoBehaviour
{
    [Header("Charge")]
    [SerializeField] private float chargeThreshold = 0.26f; // これ以上押したらため攻撃
    [Tooltip("ONなら、ためが溜まってから一定時間たつと、ボタンを離さなくても自動でため攻撃が出る")]
    [SerializeField] private bool autoChargeAttack = true;
    [Tooltip("ためが溜まった(Charge Thresholdを越えた)後、自動でため攻撃が出るまでの時間(秒)")]
    [Min(0.05f)][SerializeField] private float autoChargeDelay = 2f;

    [Header("チャージゲージ(丸)")]
    [Tooltip("丸く減る/増えるゲージのImage(丸いスプライトを設定)。Image Type等はスクリプトが自動で Filled/Radial360 にする。空ならゲージなし")]
    [SerializeField] private Image chargeGauge;
    [Tooltip("ゲージ全体(背景の輪なども含む)の親オブジェクト。空ならゲージのImage自身を出し入れする")]
    [SerializeField] private GameObject chargeGaugeRoot;
    [Tooltip("押し始めてからゲージを出すまでの時間(秒)。通常攻撃のたびにチラつかないようにする")]
    [Min(0f)][SerializeField] private float chargeGaugeShowDelay = 0.08f;
    [Tooltip("プレイヤーの足元(Transformの位置)から見た、ゲージを出す位置。ユニット。頭の上に合わせる")]
    [SerializeField] private Vector2 chargeGaugeOffset = new Vector2(0f, 1.8f);
    [Tooltip("ためている途中の色")]
    [SerializeField] private Color chargingColor = Color.white;
    [Tooltip("ため攻撃が出せるようになった(満タン)ときの色")]
    [SerializeField] private Color chargeReadyColor = new Color(1f, 0.76f, 0.1f);

    [Header("Attack Hitbox")]
    [SerializeField] private GameObject attackHitbox;
    [SerializeField] private float hitboxActiveTime = 0.13f;
    [SerializeField] private float chargeScaleMultiplier = 1.5f;

    [Header("前進(敵に当たったとき)")]
    [Tooltip("前に進む距離")]
    [SerializeField] private float forwardDistance = 1.35f;
    [Tooltip("ぐんっと感の強さ。大きいほど出だしが急加速")]
    [SerializeField, Range(1f, 8f)] private float forwardSpeed = 2f;

    [Header("後退(敵に外れたとき)")]
    [Tooltip("後ろに下がる距離")]
    [SerializeField] private float backDistance = 0.46f;
    [Tooltip("ぐんっと感の強さ。大きいほど出だしが急加速")]
    [SerializeField, Range(1f, 8f)] private float backSpeed = 2f;

    [Header("待機中の後退")]
    [Tooltip("何もしていないときに後ろへ下がる速さ(ユニット/秒)。0で下がらない")]
    [SerializeField] private float idleBackSpeed = 0.76f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 14.6f;
    [Tooltip("Rigidbody2DのGravity Scaleをこの値で上書きする")]
    [SerializeField] private float gravityScale = 4.5f;
    [Tooltip("着地の少し前に押したジャンプを受け付ける時間(秒)")]
    [SerializeField] private float jumpBufferTime = 0.22f;
    [Tooltip("地面を離れた直後でもジャンプできる猶予(秒)")]
    [SerializeField] private float coyoteTime = 0.16f;

    [Header("前進の限界")]
    [Tooltip("画面の左端を0、右端を1としたときの、これ以上進めない位置")]
    [SerializeField, Range(0.3f, 0.9f)] private float forwardLimit = 0.5f;
    [Tooltip("速いほど限界を左へ寄せて、先を見やすくする")]
    [SerializeField] private bool shiftLimitWithSpeed = true;
    [Tooltip("最高速のときに左へ寄せる量(画面幅に対する割合)")]
    [SerializeField, Range(0f, 0.3f)] private float limitShiftAtMax = 0.19f;

    [Header("スコア")]
    [Tooltip("スコア表示のスクリプト(ScoreScript)。UIのオブジェクトをドラッグ")]
    [SerializeField] private ScoreScript scoreScript;

    [Header("フィーバー(突進)")]
    [Tooltip("フィーバーの継続時間(秒)")]
    [SerializeField] private float feverDuration = 5f;
    [Tooltip("フィーバー中に前へ進む速さ(ユニット/秒)。HTMLは140px/s≒2.6")]
    [SerializeField] private float feverForwardSpeed = 2.6f;
    [Tooltip("フィーバー中の速度倍率(世界のスクロール速度などに掛ける用)")]
    [SerializeField] private float feverSpeedMultiplier = 1.3f;

    [Tooltip("フィーバーゲージのスライダー(SliderScript)。UIのオブジェクトをドラッグ")]
    [SerializeField] private SliderScript feverSlider;

    [Header("アニメーション")]
    [Tooltip("プレイヤーのAnimator。空なら自分か子オブジェクトから自動で探す")]
    [SerializeField] private Animator animator;
    [Tooltip("攻撃中にONにするAnimatorのパラメーター名(Bool。Triggerでも動く)")]
    [SerializeField] private string attackParam = "IsAttack";
    [Tooltip("地面に触れているかをAnimatorへ送るBoolパラメーター名。空中のスプライトに切り替える用")]
    [SerializeField] private string groundedParam = "IsGrounded";
    [Tooltip("フィーバー中だけONにするAnimatorのBoolパラメーター名。ループする攻撃アニメ(フィーバー用)へ切り替える。無ければ従来どおり IsAttack をONにし続ける")]
    [SerializeField] private string feverParam = "IsFever";
    [Tooltip("フィーバー中のアニメーション再生速度の倍率(1で通常)。歩きが速く見える")]
    [SerializeField, Min(0.1f)] private float feverAnimSpeed = 1.8f;

    [Header("効果音")]
    [SerializeField] private AudioClip jumpSe;
    [SerializeField] private AudioClip attackSe;
    [SerializeField] private AudioClip chargeAttackSe;
    [SerializeField] private AudioClip feverStartSe;

    [Header("デバッグ")]
    [Tooltip("今フィーバー中か。実行中に自分でON/OFFすると、フィーバーを強制的に開始/終了できる")]
    [SerializeField] private bool debugIsFever;

    // ---- フィーバー ----
    public const float FeverMax = 100f;
    public float Fever { get; private set; }          // 0〜100(UI用)
    public bool IsFever { get; private set; }         // フィーバー中(攻撃扱い+無敵)
    public float SpeedMultiplier => IsFever ? feverSpeedMultiplier : 1f;
    // 引数: (現在のゲージ 0〜100, フィーバー中か)
    public event Action<float, bool> OnFeverChanged;
    private float feverTimer;
    private bool feverForced; // デバッグで強制ONにした間は、時間が経っても終わらない

    // ---- スコア ----
    public int Score { get; private set; }
    // 引数: (今回の加算量, 合計スコア)
    public event Action<int, int> OnScoreChanged;

    // 攻撃が当たって前へダッシュし始めたとき。引数: ため攻撃か(演出用)
    public event Action<bool> OnForwardDash;

    // 攻撃ボタンを離して攻撃が始まったとき。引数: ため攻撃か(剣士の見切り用)
    public event Action<bool> OnAttackStarted;

    private Rigidbody2D rb;
    private HitAttackBox hitbox;
    private readonly HashSet<Collider2D> touching = new HashSet<Collider2D>(); // 触れているCollider
    private bool IsGrounded => touching.Count > 0; // 床でも壁でも、触れていればジャンプ可
    private float pressStartTime;
    private bool isPressing;
    private Vector3 hitboxBaseScale;
    private const float DashDuration = 0.1f; // ダッシュの時間
    private bool isAttacking; // 攻撃(ダッシュ)中は待機後退を止める
    private bool attackLanded;          // 今の攻撃が相手に通ったか
    private bool currentAttackIsCharge; // 今の攻撃がため攻撃か
    private float jumpBufferTimer;
    private float coyoteTimer;
    private Camera cam;
    private ScrollSpeedProgression progression; // カメラの速度管理。なければ加速しない
    private PlayerHealth health;
    private int lastHp;
    private int attackHash;
    private bool hasAttackParam;
    private AnimatorControllerParameterType attackParamType;
    private int groundedHash;
    private bool hasGroundedParam;
    private int feverHash;
    private bool hasFeverParam;
    private Canvas gaugeCanvas; // チャージゲージが入っているCanvas
    private Collider2D hitboxCollider; // 攻撃ヒットボックスの当たり判定

    private ChargeEffect chargeEffect; // チャージエフェクト

    // 外部から「今攻撃中か」を読めるようにする(判定が出ている間+ダッシュ中)
    // フィーバー中は常に「攻撃中」扱い(接触ダメージを受けない)
    public bool IsAttacking => isAttacking || IsFever;

    // 今の攻撃がため攻撃か(チャージ限定のものの接触ダメージ判定用)
    public bool IsChargeAttacking => isAttacking && currentAttackIsCharge;

    // 攻撃が実際に相手へ通った(壊せた/耐久を削れた)ときに、相手側から呼ばれる。
    // 通常攻撃がIronに弾かれたときなどは呼ばれないので、「外れ」扱いになる
    public void NotifyHit()
    {
        attackLanded = true;
    }

    // 通常攻撃のヒットボックスが届く横の範囲(ワールドX)。ヒットボックスが非アクティブでも求められる。
    // 見切りの敵が「ヒットボックスの範囲に入ったか」を調べるために使う。Box/Circle/Capsuleに対応
    public bool TryGetAttackXRange(out float minX, out float maxX)
    {
        minX = maxX = 0f;
        if (!TryGetAttackBox(1f, out Vector2 center, out Vector2 size)) return false;

        minX = center.x - size.x * 0.5f;
        maxX = center.x + size.x * 0.5f;
        return true;
    }

    // 今ボタンを離したときに出る攻撃の大きさの倍率(ため攻撃が出せる状態なら、ため攻撃の大きさ)
    public float PreviewScaleMultiplier =>
        isPressing && Time.unscaledTime - pressStartTime >= chargeThreshold ? chargeScaleMultiplier : 1f;

    // ヒットボックスが届く範囲(ワールドの箱)。ヒットボックスが非アクティブでも求められる。
    // scaleMultiplier はため攻撃などで大きくする倍率。Box/Circle/Capsuleに対応(回転は考えない)
    public bool TryGetAttackBox(float scaleMultiplier, out Vector2 center, out Vector2 size)
    {
        center = size = Vector2.zero;
        if (hitboxCollider == null) return false;

        var t = attackHitbox.transform;
        Vector3 parentScale = t.parent != null ? t.parent.lossyScale : Vector3.one;
        Vector3 scale = Vector3.Scale(parentScale, hitboxBaseScale) * scaleMultiplier; // 攻撃中に大きくなる前の大きさが基準

        Vector2 offset;
        Vector2 local;
        if (hitboxCollider is BoxCollider2D box)
        {
            offset = box.offset;
            local = box.size;
        }
        else if (hitboxCollider is CircleCollider2D circle)
        {
            offset = circle.offset;
            float d = circle.radius * 2f * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            local = new Vector2(d / Mathf.Abs(scale.x), d / Mathf.Abs(scale.y));
        }
        else if (hitboxCollider is CapsuleCollider2D capsule)
        {
            offset = capsule.offset;
            local = capsule.size;
        }
        else return false;

        center = (Vector2)t.position + Vector2.Scale(offset, scale);
        size = new Vector2(local.x * Mathf.Abs(scale.x), local.y * Mathf.Abs(scale.y));
        return true;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        hitbox = attackHitbox.GetComponent<HitAttackBox>();
        hitboxBaseScale = attackHitbox.transform.localScale;
        hitboxCollider = attackHitbox.GetComponent<Collider2D>();
        attackHitbox.SetActive(false);
        cam = Camera.main;
        progression = cam.GetComponent<ScrollSpeedProgression>();
        SetupAnimator();
        SetupChargeGauge();
        // 子オブジェクトに付けたチャージエフェクトを探す
        chargeEffect = GetComponentInChildren<ChargeEffect>();

        health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            lastHp = health.MaxHp; // PlayerHealth.Awake より先に呼ばれても良いように
            health.OnHpChanged += HandleHpChanged;
        }
    }

    // 丸いチャージゲージの初期設定(押していない間は隠しておく)
    private void SetupChargeGauge()
    {
        if (chargeGauge == null) return;
        if (chargeGaugeRoot == null) chargeGaugeRoot = chargeGauge.gameObject;

        chargeGauge.type = Image.Type.Filled;
        chargeGauge.fillMethod = Image.FillMethod.Radial360;
        chargeGauge.fillOrigin = (int)Image.Origin360.Top; // 真上から時計回りに
        chargeGauge.fillClockwise = true;
        chargeGauge.fillAmount = 0f;
        chargeGaugeRoot.SetActive(false);
        gaugeCanvas = chargeGaugeRoot.GetComponentInParent<Canvas>();
        if (gaugeCanvas != null) gaugeCanvas = gaugeCanvas.rootCanvas;
    }

    // ゲージをプレイヤーの頭の上に付ける。カメラやプレイヤーが動いた後に合わせるため LateUpdate
    private void LateUpdate()
    {
        if (chargeGauge == null || !chargeGaugeRoot.activeSelf) return;

        Vector3 world = transform.position + (Vector3)chargeGaugeOffset;
        var t = chargeGaugeRoot.transform;

        if (gaugeCanvas == null || gaugeCanvas.renderMode == RenderMode.WorldSpace)
        {
            t.position = world; // ワールド空間のCanvas(または普通のオブジェクト)
            return;
        }

        // 画面に固定するCanvas(Overlay / Camera)なら、ワールドの位置を画面の位置へ変換する
        Vector2 screen = cam.WorldToScreenPoint(world);
        if (gaugeCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            t.position = screen;
        }
        else
        {
            var canvasRect = (RectTransform)gaugeCanvas.transform;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screen, gaugeCanvas.worldCamera, out Vector3 pos))
                t.position = pos;
        }
    }

    // 押し始めから自動でため攻撃が出るまでの時間(溜まるまで + 溜まってからの待ち)
    private float AutoChargeTime => chargeThreshold + autoChargeDelay;

    // 押している間だけ、ためた割合(0〜1)を丸ゲージで見せる。色が変わるとため攻撃が出せる。満タンで自動発動
    private void Update()
    {
        float held = isPressing ? Time.unscaledTime - pressStartTime : 0f;

        // 最大までためたら、離さなくても自動でため攻撃
        if (autoChargeAttack && isPressing && !IsFever && held >= AutoChargeTime && gameObject.activeInHierarchy)
        {
            isPressing = false;
            DoAttack(chargeScaleMultiplier, "ため攻撃(自動)", true);
            held = 0f;
        }

        // 溜め具合を星に反映する
        if (chargeEffect != null && isPressing && !IsFever)
        {
            if (held >= chargeThreshold)
            {
                chargeEffect.Ready();
            }
            else
            {
                chargeEffect.SetCharge(held / chargeThreshold);
            }
        }

        if (chargeGauge == null) return;

        bool show = isPressing && !IsFever && held >= chargeGaugeShowDelay;
        if (chargeGaugeRoot.activeSelf != show) chargeGaugeRoot.SetActive(show);
        if (!show) return;

        // 溜まるまでは増えていく。溜まったら色が変わり、満タンのまま(自動で攻撃が出るまで)
        if (held < chargeThreshold)
        {
            chargeGauge.fillAmount = Mathf.Clamp01(held / chargeThreshold);
            chargeGauge.color = chargingColor;
        }
        else
        {
            chargeGauge.fillAmount = 1f;
            chargeGauge.color = chargeReadyColor;
        }
    }

    // Animatorと、攻撃のパラメーター(IsAttack)を探しておく
    private void SetupAnimator()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("PlayerCombat: AnimatorまたはAnimator Controllerが見つかりません。攻撃アニメーションは再生されません。", this);
            return;
        }

        attackHash = Animator.StringToHash(attackParam);
        groundedHash = Animator.StringToHash(groundedParam);
        feverHash = Animator.StringToHash(feverParam);
        foreach (var param in animator.parameters)
        {
            if (param.nameHash == attackHash)
            {
                attackParamType = param.type;
                hasAttackParam = true;
            }
            else if (param.nameHash == groundedHash && param.type == AnimatorControllerParameterType.Bool)
            {
                hasGroundedParam = true;
            }
            else if (param.nameHash == feverHash && param.type == AnimatorControllerParameterType.Bool)
            {
                hasFeverParam = true;
            }
        }
        if (!hasAttackParam)
            Debug.LogWarning($"PlayerCombat: Animatorに「{attackParam}」パラメーターがありません。Animator Controllerに追加してください。", this);
    }

    // 攻撃アニメーションのON/OFF。Boolなら攻撃中だけON、Triggerなら開始の瞬間に発火
    private void SetAttackAnim(bool on)
    {
        if (!hasAttackParam) return;

        if (attackParamType == AnimatorControllerParameterType.Bool)
            animator.SetBool(attackHash, on);
        else if (attackParamType == AnimatorControllerParameterType.Trigger && on)
            animator.SetTrigger(attackHash);
    }

    // フィーバー中のアニメーション。IsFever パラメーターがあれば、ループする専用の攻撃アニメへ。
    // なければ従来どおり、通常の攻撃アニメ(IsAttack)をONにし続ける
    private void SetFeverAnim(bool on)
    {
        if (hasFeverParam)
        {
            animator.SetBool(feverHash, on);
            SetAttackAnim(false);
        }
        else
        {
            SetAttackAnim(on);
        }
    }

    // アニメーションの再生速度(フィーバー中だけ速くする)
    private void SetAnimSpeed(float speed)
    {
        if (animator != null) animator.speed = speed;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnHpChanged -= HandleHpChanged;
    }

    // HPが減ったら失速させる
    private void HandleHpChanged(int hp, int max)
    {
        if (hp < lastHp && hp > 0 && progression != null) progression.Hurt();
        lastHp = hp;
    }

    // 敵やオブジェクトを壊したとき(壊された側から呼ばれる)
    public void NotifyBreak()
    {
        if (progression != null) progression.AddBreak();
    }

    // flow を増やして加速させる(コインなどから呼ばれる)
    public void AddFlow(float amount)
    {
        if (progression != null) progression.AddFlow(amount);
    }

    // 点数を加算する(倒された敵やオブジェクト側から呼ばれる)
    public void AddScore(int points)
    {
        if (points <= 0) return;

        Score += points;
        OnScoreChanged?.Invoke(points, Score);

        if (scoreScript != null) scoreScript.AddScore(points); // UIの表示
    }

    // フィーバーゲージを加算する(倒された敵やオブジェクト側から呼ばれる)。フィーバー中は加算しない
    public void AddFever(float amount)
    {
        if (IsFever || amount <= 0f) return;

        Fever = Mathf.Min(FeverMax, Fever + amount);
        NotifyFever(false);

        if (Fever >= FeverMax) StartFever();
    }

    private void StartFever()
    {
        if (progression != null) progression.OnFeverStart(); // 戻り先の速度を覚えさせる
        IsFever = true;
        SetAnimSpeed(feverAnimSpeed);
        debugIsFever = true;
        Fever = FeverMax;
        feverTimer = feverDuration;

        // 攻撃の途中だったら中断する
        StopAllCoroutines();
        attackHitbox.SetActive(false);
        isAttacking = false;
        isPressing = false;
        // 溜め中だったらエフェクトを止める
        if (chargeEffect != null) chargeEffect.Cancel();
        SetFeverAnim(true); // フィーバー中はずっと攻撃アニメーション(ループ)

        Debug.Log("突進!!");
        AudioManager.PlaySE(feverStartSe);
        NotifyFever(true);
    }

    // ゲージの変化をイベントとスライダーUIへ伝える
    private void NotifyFever(bool isFever)
    {
        OnFeverChanged?.Invoke(Fever, isFever);
        if (feverSlider != null) feverSlider.SetSliderValue(Fever);
    }

    private void UpdateFever(float dt)
    {
        if (!IsFever) return;

        if (feverForced) return; // ゲージも100のまま、タイマーも進めない

        feverTimer -= dt;
        Fever = Mathf.Max(0f, feverTimer / feverDuration * FeverMax); // ゲージが減っていく
        NotifyFever(true);

        if (feverTimer <= 0f) EndFever();
    }

    // 終了時に画面内のものを一掃する(この間はまだIsFeverなので、ゲージは増えない)
    private void EndFever()
    {
        Debug.Log("一掃!!");

        float height = cam.orthographicSize * 2f;
        float right = cam.transform.position.x + height * cam.aspect * 0.5f;
        foreach (var obj in FindObjectsByType<DestructibleObject2D>())
        {
            if (obj.transform.position.x < right) obj.FeverBreak(this);
        }

        IsFever = false;
        SetFeverAnim(false); // 攻撃アニメーションを止めて、歩きに戻す
        SetAnimSpeed(1f);
        debugIsFever = false;
        feverForced = false;
        Fever = 0f;
        feverTimer = 0f;
        NotifyFever(false);

        // フィーバーが終わったら、スクロール速度を初速にして、だんだんフィーバー前の速度へ戻す
        if (progression != null) progression.OnFeverEnd();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // デバッグ用チェックボックスが手動で切り替えられたら、フィーバーを強制開始/終了
        if (debugIsFever != IsFever)
        {
            if (debugIsFever) { StartFever(); feverForced = true; } // 時間で終わらない
            else EndFever();
        }
        UpdateFever(dt);
        float vy = rb.linearVelocity.y;

        // 地面に触れているかをAnimatorへ伝える(ジャンプ用スプライトの切り替え)
        if (hasGroundedParam) animator.SetBool(groundedHash, IsGrounded);

        // 上昇中は接地とみなさない(ジャンプ直後に猶予が復活するのを防ぐ)
        if (IsGrounded && vy <= 0.1f) coyoteTimer = coyoteTime;
        else coyoteTimer -= dt;
        jumpBufferTimer -= dt;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            vy = jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            AudioManager.PlaySE(jumpSe);
        }

        // 攻撃中はMovePositionで動かすので、速度での移動は止める
        // スクロールが初速より速くなった分を足して、画面に対する動きを保つ
        // フィーバー中は前へ自動で進む
        float gain = progression != null ? progression.SpeedGain : 0f;
        float vx = IsFever ? feverForwardSpeed + gain
                           : (isAttacking ? 0f : -idleBackSpeed + gain);
        rb.linearVelocity = new Vector2(vx, vy);

        float maxX = MaxForwardX();
        if (rb.position.x > maxX)
            rb.position = new Vector2(maxX, rb.position.y);
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        jumpBufferTimer = jumpBufferTime; // 実際のジャンプはFixedUpdateで判定する
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (IsFever) return; // フィーバー中は攻撃操作なし

        if (ctx.started)
        {
            isPressing = true;
            pressStartTime = Time.unscaledTime; // スローやヒットストップ中も実時間で押した長さを測る
            // 推した瞬間に薄い星を出す
            if (chargeEffect != null) chargeEffect.StartCharge();
        }
        else if (ctx.canceled && isPressing)
        {
            isPressing = false;
            // 死亡などで非アクティブになる途中のキャンセルでは攻撃しない(非アクティブではコルーチンを始められない)
            // PlayerInput.OnDisable から呼ばれた時点では、自分の OnDisable 前なので isActiveAndEnabled はまだ true のまま
            if (!gameObject.activeInHierarchy)
            {
                // 攻撃しないからエフェクトだけ止める
                if (chargeEffect != null) chargeEffect.Cancel();
                return;
            }

            float held = Time.unscaledTime - pressStartTime;

            if (held >= chargeThreshold)
                DoAttack(chargeScaleMultiplier, "ため攻撃", true);
            else
                DoAttack(1f, "通常攻撃", false);
        }
    }

    private void DoAttack(float scaleMultiplier, string label, bool isCharge)
    {
        Debug.Log(label);
        AudioManager.PlaySE(isCharge ? chargeAttackSe : attackSe);

        // チャージエフェクトの後処理
        if (chargeEffect != null)
        {
            // チャージ攻撃なら解放エフェクトを出す
            if (isCharge)
            {
                // ゲージがたまったフレーム丁度で攻撃するとReadyが送られないから予防線
                chargeEffect.Ready();
                // 力を解放
                chargeEffect.Release();
            }
            else
            {
                // 通常攻撃ならチャージエフェクトを出さない(今後エフェクト追加するかも)
                chargeEffect.Cancel();
            }
        }

        StopAllCoroutines();
        StartCoroutine(AttackRoutine(scaleMultiplier, isCharge));
        // 攻撃の初期化の後に通知する(受け取った側が NotifyHit すると前へ踏み込む)
        OnAttackStarted?.Invoke(isCharge);
    }

    private IEnumerator AttackRoutine(float scaleMultiplier, bool isCharge)
    {
        isAttacking = true;
        attackLanded = false;
        currentAttackIsCharge = isCharge;
        SetAttackAnim(true); // 攻撃アニメーションを再生

        // 前の攻撃の判定が残っていたら一度切る。入れ直すと重なっている相手にも新しい攻撃として当たる
        attackHitbox.SetActive(false);
        attackHitbox.transform.localScale = hitboxBaseScale * scaleMultiplier;
        hitbox.SetAttackType(isCharge);
        attackHitbox.SetActive(true);

        // 少なくとも1回は物理の判定を通してから消す。剣士の見切りのように物理の外で「当たった」が決まっても、
        // 重なっている他の相手にも攻撃が届くようにする(ヒットストップ中は物理が止まるので、明けるまで待つ)
        yield return new WaitForFixedUpdate();

        // 判定時間が終わるか、敵に当たるまで待つ
        float t = 0f;
        // (タグが合うだけでは「当たった」にしない。相手が攻撃を受け付けたときだけ NotifyHit が呼ばれる)
        while (t < hitboxActiveTime && !attackLanded)
        {
            t += Time.deltaTime;
            yield return null;
        }

        bool hit = attackLanded;
        attackHitbox.SetActive(false);

        // 当たったら前へ、外れたら後ろへ
        if (hit)
        {
            OnForwardDash?.Invoke(isCharge);
            yield return Dash(forwardDistance, forwardSpeed);
        }
        else
        {
            if (progression != null) progression.Whiff();
            yield return Dash(-backDistance, backSpeed);
        }

        isAttacking = false;
        SetAttackAnim(false);
    }

    // イージングで distance だけ動く(符号が向き)。sharpness が大きいほど「ぐんっ」
    private IEnumerator Dash(float distance, float sharpness)
    {
        float elapsed = 0f;
        float prev = 0f;

        while (elapsed < DashDuration)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            float eased = EaseOutPower(Mathf.Clamp01(elapsed / DashDuration), sharpness);
            float current = eased * distance;

            Vector2 target = rb.position + Vector2.right * (current - prev);
            target.x = Mathf.Min(target.x, MaxForwardX()); // 限界を超えない
            rb.MovePosition(target);
            prev = current;
        }
    }

    // 指数を可変にしたEaseOut。1で等速、大きいほど出だしが急で終わりがゆっくり
    private static float EaseOutPower(float t, float power)
    {
        return 1f - Mathf.Pow(1f - t, power);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        touching.Add(col.collider);
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        touching.Remove(col.collider);
    }

    // 触れていた相手が消えたとき(Destroyなど)にExitが呼ばれない場合に備える
    private void OnDisable()
    {
        touching.Clear();
        isPressing = false; // 押したまま無効になったら、再び有効になったときに攻撃が出ないようにする
        // 溜め中に無効になったらエフェクトも止める
        if (chargeEffect != null) chargeEffect.Cancel();
        // 死亡などで無効になると Update が止まって隠せないので、チャージゲージもここで消す
        if (chargeGauge != null)
        {
            chargeGauge.fillAmount = 0f;
            chargeGaugeRoot.SetActive(false);
        }
    }

    // 画面の左端から forwardLimit の位置(ワールドX)
    private float MaxForwardX()
    {
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        float limit = forwardLimit;
        if (shiftLimitWithSpeed && progression != null)
            limit -= limitShiftAtMax * progression.Speed01;
        return cam.transform.position.x - width * 0.5f + width * limit;
    }
}