using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Charge")]
    [SerializeField] private float chargeThreshold = 0.5f;

    [Header("Attack Hitbox")]
    [SerializeField] private GameObject attackHitbox;
    [SerializeField] private float hitboxActiveTime = 0.15f;
    [SerializeField] private float chargeScaleMultiplier = 2f;

    [Header("Dash (イージング移動)")]
    [Header("前進(敵に当たったとき)")]
    [Tooltip("前に進む距離")]
    [SerializeField] private float forwardDistance = 1.5f;
    [Tooltip("ぐんっと感の強さ。大きいほど出だしが急加速")]
    [SerializeField, Range(1f, 8f)] private float forwardSpeed = 4f;

    [Header("後退(敵に外れたとき)")]
    [Tooltip("後ろに下がる距離")]
    [SerializeField] private float backDistance = 1f;
    [Tooltip("ぐんっと感の強さ。大きいほど出だしが急加速")]
    [SerializeField, Range(1f, 8f)] private float backSpeed = 4f;


    [Header("Jump")]
    [SerializeField] private float jumpForce = 3f;

    [Header("待機中の後退")]
    [Tooltip("何もしていないときに後ろへ下がる速さ(ユニット/秒)。0で下がらない")]
    [SerializeField] private float idleBackSpeed = 0.5f;

    private Rigidbody2D rb;
    private HitAttackBox hitbox;
    private int groundContacts;
    private bool IsGrounded => groundContacts > 0;

    private float pressStartTime;
    private bool isPressing;
    private Vector3 hitboxBaseScale;
    private const float DashDuration = 0.2f;// ダッシュの時間
    private bool isAttacking; // 攻撃(ダッシュ)中は待機後退を止める

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitbox = attackHitbox.GetComponent<HitAttackBox>();
        hitboxBaseScale = attackHitbox.transform.localScale;
        attackHitbox.SetActive(false);
    }
    private void FixedUpdate()
    {
        // 攻撃中はMovePositionで動かすので、速度での移動は止める
        float vx = isAttacking ? 0f : -idleBackSpeed;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !IsGrounded) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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
            yield return Dash(-backDistance, backSpeed);

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

            rb.MovePosition(rb.position + Vector2.right * (current - prev));
            prev = current;
        }
    }

    // 指数を可変にしたEaseOut。1で等速、大きいほど出だしが急で終わりがゆっくり
    private static float EaseOutPower(float t, float power)
    {
        return 1f - Mathf.Pow(1f - t, power);
    }
    // 最初に急加速して、だんだん減速する「ぐんっ」とした動き

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (IsFloor(col)) groundContacts++;
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (groundContacts > 0) groundContacts--;
    }

    private bool IsFloor(Collision2D col)
    {
        foreach (var contact in col.contacts)
            if (contact.normal.y > 0.5f) return true;
        return false;
    }
}