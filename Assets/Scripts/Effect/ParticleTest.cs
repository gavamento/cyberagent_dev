/*
* ファイル名　ParticleTest.cs
* タイトル　　パーティクルのテスト
* 作成者　　　久保木幹太
* 作成日　　　10月21日
* 更新日　　　10月21日
*/

using UnityEngine;
using System.Collections;

public class ParticleTest : MonoBehaviour
{
    [SerializeField] private GameObject effectPrefab;       // パーティクルのプレハブ
    [SerializeField] private GameObject debrisPrefab;       // 破片のプレハブ
    [SerializeField] private GameObject shockPrefab;        // 衝撃波のプレハブ
    [SerializeField] private float timeToDestroy = 3.0f;    // 壊れるまでの時間
    [SerializeField] private float effectLeadTime = 0.0f;   // 壊れる何秒前にパーティクルを出すか

    void Start()
    {
        // 生成されたら自動でタイマーカウントを開始
        StartCoroutine(DestroyTimerRoutine());
    }

    IEnumerator DestroyTimerRoutine()
    {
        // パーティクルを出すタイミングまで待機
        float waitTime = Mathf.Max(0f, timeToDestroy - effectLeadTime);
        yield return new WaitForSeconds(waitTime);

        // 遅延がある場合は少し待つ
        if (effectLeadTime > 0f)
        {
            yield return new WaitForSeconds(effectLeadTime);
        }

        EffectManager.PlayEffectWithOwnerSpriteAndDebris(effectPrefab, debrisPrefab, transform, 10);
        EffectManager.PlayEffect(shockPrefab, transform);

        // 自身を消滅
        Destroy(gameObject);
    }
}