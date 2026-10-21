//===============================================================================-
//プレイヤーの体力の管理を行うスクリプト    鍋坂明穂
//===============================================================================
using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int maxHp = 3; // ハートの数

    [Header("画面外")]
    [SerializeField] private float offscreenMargin = 0.5f; // 画面の外側にどれだけ出たら死ぬか(ユニット)


    public int CurrentHp { get; private set; }
    public int MaxHp => maxHp;
    public bool IsDead { get; private set; }

    // 後でUIがこれを購読する(引数: 現在HP, 最大HP)
    public event Action<int, int> OnHpChanged;
    public event Action OnDied;

    private Camera cam;
    private void Awake()
    {
        cam = Camera.main;
        CurrentHp = maxHp;
    }

    private void Start()
    {
        LogHearts();
    }

    private void Update()
    {
        if (IsDead) return;
        if (IsOffscreen()) Die();
    }

    public void TakeDamage(int amount = 1)
    {
        if (IsDead) return;

        CurrentHp = Mathf.Max(CurrentHp - amount, 0);
        LogHearts();
        OnHpChanged?.Invoke(CurrentHp, maxHp);

        if (CurrentHp <= 0) Die();
    }

    public void Heal(int amount = 1)
    {
        if (IsDead) return;

        CurrentHp = Mathf.Min(CurrentHp + amount, maxHp);
        LogHearts();
        OnHpChanged?.Invoke(CurrentHp, maxHp);
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        CurrentHp = 0;
        LogHearts();
        OnHpChanged?.Invoke(CurrentHp, maxHp);
        OnDied?.Invoke();

        Debug.Log("プレイヤーが死亡");
        gameObject.SetActive(false); // 消える
    }

    // 画面(カメラの映る範囲)の外に出たか
    private bool IsOffscreen()
    {
        if (cam == null) return false;

        Vector3 vp = cam.WorldToViewportPoint(transform.position);

        // ビューポート座標は 0〜1 が画面内。マージンをユニット換算で足す
        float marginX = offscreenMargin / (cam.orthographicSize * 2f * cam.aspect);
        float marginY = offscreenMargin / (cam.orthographicSize * 2f);

        return vp.x < -marginX || vp.x > 1f + marginX
            || vp.y < -marginY || vp.y > 1f + marginY;
    }

    // ♥♥♡ のようにログへ出す
    private void LogHearts()
    {
        string hearts = "";
        for (int i = 0; i < maxHp; i++)
            hearts += i < CurrentHp ? "♥" : "♡";

        Debug.Log($"HP {CurrentHp}/{maxHp}  {hearts}");
    }
}
