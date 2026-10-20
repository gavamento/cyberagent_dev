using UnityEngine;

public enum BreakType
{
    Once,       // どの攻撃でも一発で壊れる
    ChargeOnly  // ため攻撃でしか壊れない
}

[System.Serializable]
public struct HitTagRule
{
    [Tooltip("Unityのタグ名(Tag Managerに登録したものと完全一致)")]
    public string tagName;

    [Tooltip("攻撃が当たったとみなす(前に出る)")]
    public bool attackHits;

    [Tooltip("オンなら壊せる。オフなら当たるだけで壊れない")]
    public bool canBreak;

    [Tooltip("壊せる場合の条件(canBreakがオンのときだけ使う)")]
    public BreakType breakType;
}

// 仮置き:タグごとの判定をここで管理している。
// 将来は各オブジェクト/敵側に責任を移し、このクラスは「当たった相手に通知する」だけにする想定。
public class HitAttackBox : MonoBehaviour
{
    [Header("タグごとの攻撃判定")]
    [SerializeField]
    private HitTagRule[] hitRules =
    {
        new HitTagRule { tagName = "Enemy",  attackHits = true, canBreak = true,  breakType = BreakType.Once },
        new HitTagRule { tagName = "Object", attackHits = true, canBreak = false, breakType = BreakType.Once },
        new HitTagRule { tagName = "Iron",   attackHits = true, canBreak = true,  breakType = BreakType.ChargeOnly },
    };

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

    //例：↓ 将来、攻撃が当たったときの処理をそれぞれのオブジェクトに書くこともできる　
    //private void OnHit(bool isCharge)
    //{
    //    if (chargeOnly && !isCharge) return; // 壊れない
    //    Destroy(gameObject);                 // 演出や自分のHP処理もここに書ける
    //}

    private void OnTriggerEnter2D(Collider2D other)
    {
        //// 相手に「攻撃を受けた」と通知する。受け取るメソッドがなければ何も起きない
        //other.SendMessage("OnHit", isChargeAttack, SendMessageOptions.DontRequireReceiver);

        foreach (var rule in hitRules)
        {
            if (!other.CompareTag(rule.tagName)) continue;

            if (rule.attackHits)
            {
                HitEnemy = true;
                Debug.Log($"{rule.tagName}を検出: {other.name}");
            }

            if (rule.canBreak)
            {
                if (CanBreak(rule.breakType))
                {
                    Debug.Log($"{other.name}を破壊");
                    Destroy(other.gameObject); // 仮:後でオブジェクト側の処理に置き換える
                }
                else
                {
                    Debug.Log($"{other.name}は壊せない(ため攻撃が必要)");
                }
            }
            return;
        }
    }

    private bool CanBreak(BreakType type)
    {
        return type == BreakType.Once
            || (type == BreakType.ChargeOnly && isChargeAttack);
    }
}