using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class BackEffect : MonoBehaviour
{
    [SerializeField] private VisualEffect backEffect;
    [SerializeField] private Rigidbody2D player;
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    [Header("フィーバー")]
    [Tooltip("フィーバー状態を調べる対象 空だったらplayerと同じオブジェクトから探す")]
    [SerializeField] private PlayerCombat playerCombat;
    [Tooltip("フィーバーに入ってから線が最大までの時間")]
    [SerializeField, Min(0.01f)] private float feverBlendInTime = 0.3f;
    [Tooltip("フィーバーが終わってから、通常の本数に戻るまでの時間")]
    [SerializeField, Min(0.01f)] private float feverBlendOutTime = 0.6f;

    private static readonly int PlayerSpeedID = Shader.PropertyToID("PlayerSpeed");
    private static readonly int FeverBlendID = Shader.PropertyToID("FeverBlend");

    private float fixedY;

    // 0=通常 1=フィーバー(なめらかに切り替えるための値)
    private float feverBlend;

    private void Start()
    {
        // 最初のY座標を保持
        fixedY = backEffect.transform.position.y;

        // 指定がなければプレイヤーのオブジェクトから探す
        if (playerCombat == null && player != null)
        {
            playerCombat = player.GetComponent<PlayerCombat>();
        }
    }

    private void Update()
    {
        if (backEffect == null || player == null) return;

        // Xだけプレイヤーに追従してYは固定する
        Vector3 position = backEffect.transform.position;

        position.x = player.transform.position.x + positionOffset.x;
        position.y = fixedY + positionOffset.y;

        backEffect.transform.position = position;

        // 横方向の速度をVFXに送る
        float speed = Mathf.Abs(player.linearVelocity.x);
        backEffect.SetFloat(PlayerSpeedID, speed);

        // ふぇーばー中は1、それ以外は0に向かって動かす
        float target = (playerCombat != null && playerCombat.IsFever) ? 1.0f : 0.0f;

        // 増えるときは速く、戻るときはゆっくりにする
        float time = target > feverBlend ? feverBlendInTime : feverBlendOutTime;
        feverBlend = Mathf.MoveTowards(feverBlend, target, Time.deltaTime / time);
        backEffect.SetFloat(FeverBlendID, feverBlend);
    }
}