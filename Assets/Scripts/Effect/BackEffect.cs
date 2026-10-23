using UnityEngine;
using UnityEngine.VFX;

public class BackEffect : MonoBehaviour
{
    [SerializeField] private VisualEffect backEffect;
    [SerializeField] private Rigidbody2D player;

    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    private static readonly int PlayerSpeedID = Shader.PropertyToID("PlayerSpeed");

    private float fixedY;

    private void Start()
    {
        // 最初のY座標を保持
        fixedY = backEffect.transform.position.y;
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
    }
}