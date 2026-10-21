//===============================================================================-
//プレイヤーの攻撃判定(ヒットボックス)のスクリプト    鍋坂明穂
//===============================================================================-
using UnityEngine;

// 破壊やダメージの処理は、攻撃を受けた側(オブジェクト/敵)のスクリプトが担当する。
// ここでは「触れた相手に通知する」ことと、「何かに当たったか」を記録するだけ。
public class HitAttackBox : MonoBehaviour
{
    public bool HitEnemy { get; private set; }

    private bool isChargeAttack;

    // SetActive(true) の前に呼ぶ
    public void SetAttackType(bool charge)
    {
        isChargeAttack = charge;
    }

    private void OnEnable()
    {
        HitEnemy = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 相手に「攻撃を受けた」と通知する(受け取るメソッドがなければ何も起きない)
        other.SendMessage("OnHit", isChargeAttack, SendMessageOptions.DontRequireReceiver);

        HitEnemy = true;
        Debug.Log($"検出: {other.name}");
    }
}