/*
* ファイル名　BounceDbris.cs
* タイトル　　跳ね返る破片くん
* 作成者　　　久保木幹太
* 作成日　　　10月22日
* 更新日　　　10月22日
*/

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class BounceDebris : MonoBehaviour
{
    [Header("飛び散る速度")]
    [SerializeField] private float minSpeed = 5.0f;
    [SerializeField] private float maxSpeed = 20.0f;

    [Header("回転速度")]
    [SerializeField] private float minAngularSpeed = -360.0f;
    [SerializeField] private float maxAngularSpeed = 360.0f;

    [Header("生存時間")]
    [SerializeField] private float lifeTime = 2.0f;

    [Header("フェード時間")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("飛び散る方向,(Min, Max)")]
    [SerializeField] private Vector2 DirectionX = new Vector2(0.0f, 5.0f);
    [SerializeField] private Vector2 DirectionY = new Vector2(0.0f, 10.0f);

    [Header("出現したときのフラッシュ")]
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private Color flashColor = Color.white;

    private Color targetColor = Color.white;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // 上方向を中心にランダムな方向に飛ばす
        // 左から右のオートランなので右側に飛ばして疾走感を出す
        Vector2 direction = new Vector2(
            Random.Range(DirectionX.x, DirectionX.y),
            Random.Range(DirectionY.x, DirectionY.y)
            ).normalized;

        // スピード決定
        float speed = Random.Range(minSpeed, maxSpeed);

        // 初速
        rb.linearVelocity = direction * speed;

        // ランダム回転
        rb.angularVelocity = Random.Range(minAngularSpeed, maxAngularSpeed);

        // フラッシュ
        StartCoroutine(Flash());
        // フェードアウト消滅
        StartCoroutine(FadeOut());
    }

    // フェードして消えていく
    private IEnumerator FadeOut()
    {
        // フェード開始まで待つ
        float waitTime = Mathf.Max(0.0f, lifeTime - fadeDuration);
        yield return new WaitForSeconds(waitTime);

        // 現在の色とα値もらう
        Color color = spriteRenderer.color;
        float startAlpha = color.a;

        float elapsedTime = 0.0f;

        // だんだん透明にする
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / fadeDuration);

            color.a = Mathf.Lerp(startAlpha, 0.0f, t);
            spriteRenderer.color = color;

            yield return null;
        }

        Destroy(gameObject, lifeTime);
    }

    // 破片に色を付ける(最初は白)
    public void SetColor(Color color)
    {
        targetColor = color;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = flashColor;
        }
    }

    // 白から元の色に戻す
    private IEnumerator Flash()
    {
        float elapsedTime = 0.0f;
        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / flashDuration);
            spriteRenderer.color = Color.Lerp(flashColor, targetColor, t);
            yield return null;
        }
        spriteRenderer.color = targetColor;
    }
}