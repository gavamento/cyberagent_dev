/*
* ファイル名　ParticleTest.cs
* タイトル　　パーティクルのテスト
* 作成者　　　久保木幹太
* 作成日　　　10月21日
* 更新日　　　10月21日
*/

using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// VFXのパーティクルがすべて消えたら、自分のGameObjectを削除する
/// </summary>
[RequireComponent(typeof(VisualEffect))]
public class VFXAutoDestroy : MonoBehaviour
{
    [SerializeField] private float minLifeTime = 0.2f; // 生成直後はまだパーティクルが0個なので、この時間はチェックしない
    [SerializeField] private float maxLifeTime = 3.0f; // 何かあってもこの時間が過ぎたら必ず削除する

    private VisualEffect vfx;
    private float timer;

    void Awake()
    {
        vfx = GetComponent<VisualEffect>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 生成直後はパーティクルがまだ出ていないので判定しない
        if (timer < minLifeTime) return;

        // 生きているパーティクルが0になったか、最大時間を過ぎたら削除
        if (vfx.aliveParticleCount == 0 || timer >= maxLifeTime)
        {
            Destroy(gameObject);
        }
    }
}
