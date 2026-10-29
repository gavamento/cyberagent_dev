/*
* ファイル名　ParticleEffectCompareTest.cs
* タイトル　　VFX 版と Particle System 版の見比べ用テスト
* 作成者　　　秋田蓮音
* 作成日　　　10月29日
* 更新日　　　10月29日
*/

using System.Collections;
using UnityEngine;

// テストシーン専用。チャージと撃破を VFX 版と Particle System 版で同時に繰り返し再生する
public class ParticleEffectCompareTest : MonoBehaviour
{
    [Header("チャージ")]
    [SerializeField] private ChargeEffect chargeVfx;
    [SerializeField] private ChargeEffectParticle chargeParticle;
    [Tooltip("溜め始めてから完了までの時間")]
    [SerializeField] private float chargeTime = 1.0f;
    [Tooltip("完了してから放つまでの時間")]
    [SerializeField] private float holdTime = 1.5f;
    [Tooltip("放ってから次の溜めまでの時間")]
    [SerializeField] private float chargeInterval = 1.0f;

    [Header("撃破")]
    [SerializeField] private GameObject breakVfxPrefab;
    [SerializeField] private BreakEffectParticle breakParticlePrefab;
    [Tooltip("VFX 版を出す場所。子の SpriteRenderer から色を拾う")]
    [SerializeField] private Transform breakVfxTarget;
    [Tooltip("Particle 版を出す場所。子の SpriteRenderer から色を拾う")]
    [SerializeField] private Transform breakParticleTarget;
    [SerializeField] private float breakInterval = 2.0f;

    private void Start()
    {
        StartCoroutine(ChargeLoop());
        StartCoroutine(BreakLoop());
    }

    private IEnumerator ChargeLoop()
    {
        while (true)
        {
            if (chargeVfx != null) chargeVfx.StartCharge();
            chargeParticle.StartCharge();

            for (float t = 0.0f; t < chargeTime; t += Time.deltaTime)
            {
                if (chargeVfx != null) chargeVfx.SetCharge(t / chargeTime);
                chargeParticle.SetCharge(t / chargeTime);
                yield return null;
            }

            if (chargeVfx != null) chargeVfx.Ready();
            chargeParticle.Ready();
            yield return new WaitForSeconds(holdTime);

            if (chargeVfx != null) chargeVfx.Release();
            chargeParticle.Release();
            yield return new WaitForSeconds(chargeInterval);
        }
    }

    private IEnumerator BreakLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(breakInterval);

            if (breakVfxPrefab != null && breakVfxTarget != null)
            {
                EffectManager.PlayEffectWithOwnerSprite(breakVfxPrefab, breakVfxTarget);
            }

            if (breakParticlePrefab != null && breakParticleTarget != null)
            {
                SpriteRenderer sr = breakParticleTarget.GetComponentInChildren<SpriteRenderer>();
                BreakEffectParticle effect = Instantiate(breakParticlePrefab, breakParticleTarget.position, Quaternion.identity);
                effect.Play(sr != null ? sr.sprite : null);
            }
        }
    }
}
