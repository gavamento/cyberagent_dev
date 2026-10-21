//===============================================================================-
//プレイヤーの攻撃判定(ヒットボックス)のスクリプト    鍋坂明穂
//===============================================================================-
using UnityEngine;

// 破壊やダメージの処理は、攻撃を受けた側(オブジェクト/敵)のスクリプトが担当する。
// ここでは「タグが一致した相手に通知する」ことと、「当たったか」を記録するだけ。
public class HitAttackBox : MonoBehaviour
{
    [Header("攻撃が当たるタグ")]
    [Tooltip("ここに書いたタグに触れたときだけ「当たった」扱いにする(Tag Managerに登録したものと完全一致)")]
    [SerializeField] private string[] hitTags = { "Enemy", "Object", "Iron" };

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
        if (!IsHitTarget(other)) return; // 床や背景など、対象外のタグは無視

        HitEnemy = true;
        Debug.Log($"{other.tag}を検出: {other.name}");
    }

    private bool IsHitTarget(Collider2D other)
    {
        foreach (var tag in hitTags)
        {
            if (other.CompareTag(tag)) return true;
        }
        return false;
    }
}