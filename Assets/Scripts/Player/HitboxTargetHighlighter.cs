//===============================================================================-
//攻撃ヒットボックスの範囲に入った敵を強調表示するスクリプト    秋田蓮音
//===============================================================================-
using System.Collections.Generic;
using UnityEngine;
using CyberAgentDev.ObjectPrototypeLearning;

// Player に付ける。攻撃していない間も、今攻撃したら当たる相手の輪郭を点滅させる
// ためている間(ため攻撃が出せる状態)は、ため攻撃の大きさで調べる
[RequireComponent(typeof(PlayerCombat))]
public class HitboxTargetHighlighter : MonoBehaviour
{
    [Header("対象")]
    [Tooltip("強調するタグ(HitAttackBox の Hit Tags と合わせる)")]
    [SerializeField] private string[] targetTags = { "Enemy", "Iron", "Object" };

    [Header("輪郭")]
    [Tooltip("単色で描くマテリアル (SpriteOutline)")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color outlineColor = new Color(1f, 0.95f, 0.2f, 1f);
    [Tooltip("輪郭の太さ(ユニット)")]
    [SerializeField, Min(0f)] private float outlineWidth = 0.06f;

    [Header("点滅")]
    [Tooltip("1秒あたりの点滅回数")]
    [SerializeField, Min(0f)] private float blinkSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.25f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;

    private PlayerCombat combat;
    private readonly List<Collider2D> hits = new List<Collider2D>();
    private HashSet<TargetOutline> current = new HashSet<TargetOutline>();
    private HashSet<TargetOutline> next = new HashSet<TargetOutline>();
    private ContactFilter2D filter;

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
        filter = new ContactFilter2D().NoFilter();
        filter.useTriggers = true; // 敵は Is Trigger の Collider

        if (outlineMaterial == null)
        {
            Debug.LogWarning("HitboxTargetHighlighter: outlineMaterial が未設定です", this);
            enabled = false;
        }
    }

    private void OnDisable()
    {
        ClearAll();
    }

    // 敵やプレイヤーが動いた後に調べる
    private void LateUpdate()
    {
        next.Clear();

        if (!combat.IsFever // フィーバー中は触れるだけで壊れるので出さない
            && combat.TryGetAttackBox(combat.PreviewScaleMultiplier, out Vector2 center, out Vector2 size))
        {
            Physics2D.OverlapBox(center, size, 0f, filter, hits);
            foreach (var hit in hits)
            {
                var target = hit != null ? hit.GetComponentInParent<DestructibleObject2D>() : null;
                if (target == null || target.IsBroken || !IsTarget(target.gameObject)) continue;
                next.Add(GetOutline(target));
            }
        }

        // 外れたものは消す(壊れて消えたものは null になっている)
        foreach (var outline in current)
            if (outline != null && !next.Contains(outline)) outline.SetHighlighted(false);
        foreach (var outline in next)
            outline.SetHighlighted(true);

        (current, next) = (next, current);
    }

    private TargetOutline GetOutline(DestructibleObject2D target)
    {
        if (!target.TryGetComponent(out TargetOutline outline))
        {
            outline = target.gameObject.AddComponent<TargetOutline>();
            outline.Setup(outlineMaterial, outlineColor, outlineWidth, blinkSpeed, minAlpha, maxAlpha);
        }
        return outline;
    }

    private bool IsTarget(GameObject target)
    {
        foreach (var tag in targetTags)
            if (!string.IsNullOrEmpty(tag) && target.CompareTag(tag)) return true;
        return false;
    }

    private void ClearAll()
    {
        foreach (var outline in current)
            if (outline != null) outline.SetHighlighted(false);
        current.Clear();
    }
}
