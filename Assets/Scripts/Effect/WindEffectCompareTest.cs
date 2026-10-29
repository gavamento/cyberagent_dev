/*
* ファイル名　WindEffectCompareTest.cs
* タイトル　　風エフェクトの VFX 版と Particle System 版の見比べ用テスト
* 作成者　　　秋田蓮音
* 作成日　　　10月29日
* 更新日　　　10月29日
*/

using UnityEngine;

// テストシーン専用。2 人のランナーを同じ速さで右へ走らせ、それぞれのカメラで追う
// 速さは 0 から maxSpeed まで行ったり来たりする。一定間隔でランナーの前に衝撃波も出す
public class WindEffectCompareTest : MonoBehaviour
{
    [SerializeField] private Rigidbody2D[] runners;
    [SerializeField] private Camera[] cameras;
    [Tooltip("速さの表示先")]
    [SerializeField] private TextMesh speedLabel;

    [SerializeField, Min(0.0f)] private float maxSpeed = 25.0f;
    [Tooltip("0 から maxSpeed まで上がるのにかかる時間(秒)")]
    [SerializeField, Min(0.1f)] private float rampTime = 5.0f;

    [Header("衝撃波")]
    [Tooltip("runners と同じ順で、それぞれの前に出すプレハブ")]
    [SerializeField] private GameObject[] shockPrefabs;
    [Tooltip("ランナーからどれだけ前に出すか")]
    [SerializeField] private Vector3 shockOffset = new Vector3(4.0f, 0.0f, 0.0f);
    [SerializeField, Min(0.1f)] private float shockInterval = 1.5f;

    private float speed;
    private float shockTimer;

    private void FixedUpdate()
    {
        speed = Mathf.PingPong(Time.time / rampTime, 1.0f) * maxSpeed;
        foreach (Rigidbody2D runner in runners)
        {
            runner.linearVelocity = new Vector2(speed, 0.0f);
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < cameras.Length && i < runners.Length; i++)
        {
            Vector3 p = cameras[i].transform.position;
            p.x = runners[i].transform.position.x;
            cameras[i].transform.position = p;
        }

        if (speedLabel != null) speedLabel.text = $"speed {speed:F1}";

        shockTimer += Time.deltaTime;
        if (shockTimer < shockInterval) return;
        shockTimer = 0.0f;

        // ゲームと同じく、その場に置き去りで出す
        for (int i = 0; i < shockPrefabs.Length && i < runners.Length; i++)
        {
            if (shockPrefabs[i] == null) continue;
            Instantiate(shockPrefabs[i], runners[i].transform.position + shockOffset, Quaternion.identity);
        }
    }
}
