//===============================================================================-
//プレイヤーの移動系のスクリプト    鍋坂明穂
//===============================================================================-
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Charge")]
    [SerializeField] private float chargeThreshold = 0.26f; // これ以上押したらため攻撃

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

    // ---- スコア ----
    public int Score { get; private set; }
    // 引数: (今回の加算量, 合計スコア)
    public event Action<int, int> OnScoreChanged;

    private Rigidbody2D rb;
    private HitAttackBox hitbox;
    private readonly HashSet<Collider2D> touching = new HashSet<Collider2D>(); // 触れているCollider
    private bool IsGrounded => touching.Count > 0; // 床でも壁でも、触れていればジャンプ可
    private float pressStartTime;
    private bool isPressing;
    private Vector3 hitboxBaseScale;
    private const float DashDuration = 0.1f; // ダッシュの時間
    private bool isAttacking; // 攻撃(ダッシュ)中は待機後退を止める
    private float jumpBufferTimer;
    private float coyoteTimer;
    private Camera cam;
    private ScrollSpeedProgression progression; // カメラの速度管理。なければ加速しない
    private PlayerHealth health;
    private int lastHp;

    // 外部から「今攻撃中か」を読めるようにする(判定が出ている間+ダッシュ中)
    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        hitbox = attackHitbox.GetComponent<HitAttackBox>();
        hitboxBaseScale = attackHitbox.transform.localScale;
        attackHitbox.SetActive(false);
        cam = Camera.main;
        progression = cam.GetComponent<ScrollSpeedProgression>();

        health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            lastHp = health.MaxHp; // PlayerHealth.Awake より先に呼ばれても良いように
            health.OnHpChanged += HandleHpChanged;
        }
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

    // 点数を加算する(倒された敵やオブジェクト側から呼ばれる)
    public void AddScore(int points)
    {
        if (points <= 0) return;

        OnScoreChanged?.Invoke(points, Score);

        if (scoreScript != null) scoreScript.AddScore(points); // UIの表示
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float vy = rb.linearVelocity.y;

        // 上昇中は接地とみなさない(ジャンプ直後に猶予が復活するのを防ぐ)
        if (IsGrounded && vy <= 0.1f) coyoteTimer = coyoteTime;
        else coyoteTimer -= dt;
        jumpBufferTimer -= dt;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            vy = jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // 攻撃中はMovePositionで動かすので、速度での移動は止める
        // スクロールが初速より速くなった分を足して、画面に対する下がり方を保つ
        float gain = progression != null ? progression.SpeedGain : 0f;
        float vx = isAttacking ? 0f : -idleBackSpeed + gain;
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
        if (ctx.started)
        {
            isPressing = true;
            pressStartTime = Time.time;
        }
        else if (ctx.canceled && isPressing)
        {
            isPressing = false;
            float held = Time.time - pressStartTime;

            if (held >= chargeThreshold)
                DoAttack(chargeScaleMultiplier, "ため攻撃", true);
            else
                DoAttack(1f, "通常攻撃", false);
        }
    }

    private void DoAttack(float scaleMultiplier, string label, bool isCharge)
    {
        Debug.Log(label);
        StopAllCoroutines();
        StartCoroutine(AttackRoutine(scaleMultiplier, isCharge));
    }

    private IEnumerator AttackRoutine(float scaleMultiplier, bool isCharge)
    {
        isAttacking = true;

        attackHitbox.transform.localScale = hitboxBaseScale * scaleMultiplier;
        hitbox.SetAttackType(isCharge);
        attackHitbox.SetActive(true);

        // 判定時間が終わるか、敵に当たるまで待つ
        float t = 0f;
        while (t < hitboxActiveTime && !hitbox.HitEnemy)
        {
            t += Time.deltaTime;
            yield return null;
        }

        bool hit = hitbox.HitEnemy;
        attackHitbox.SetActive(false);

        // 当たったら前へ、外れたら後ろへ
        if (hit)
            yield return Dash(forwardDistance, forwardSpeed);
        else
        {
            if (progression != null) progression.Whiff();
            yield return Dash(-backDistance, backSpeed);
        }

        isAttacking = false;
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