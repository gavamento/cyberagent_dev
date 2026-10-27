/*
* ファイル名　BounceDbris.cs
* タイトル　　跳ね返る破片くん
* 作成者　　　久保木幹太
* 作成日　　　10月22日
* 更新日　　　10月27日
*/

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
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

    [Header("影")]
    [SerializeField] private bool useShadow = true;
    // 影をずらす量
    [SerializeField] private Vector2 shadowOffset = new Vector2(0.06f, -0.06f);
    // 影の色
    [SerializeField] private Color shadowColor = new Color(0.0f, 0.0f, 0.0f, 0.45f);
    // 影の大きさ
    [SerializeField] private float shadowScale = 1.0f;

    // フラッシュの後にもどる元の色
    private Color targetColor = Color.white;
    // 自動生成する影
    private SpriteRenderer shadowRenderer;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 影を作る
        if (useShadow) CreateShadow();
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

    private void LateUpdate()
    {
        if (shadowRenderer == null) return;

        // 回転しても影は常に右下に出す
        shadowRenderer.transform.position = transform.position + (Vector3)shadowOffset;

        // あとからスプライトが変わった場合も合わせる
        if (shadowRenderer.sprite != spriteRenderer.sprite)
        {
            shadowRenderer.sprite = spriteRenderer.sprite;
        }
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

            // 影も同じ割合で薄くしていく
            if (shadowRenderer != null)
            {
                Color sc = shadowColor;
                sc.a = Mathf.Lerp (shadowColor.a, 0.0f, t);
                shadowRenderer.color = sc;
            }

            yield return null;
        }

        Destroy(gameObject);
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

    // 本体と同じスプライトで影を作る
    private void CreateShadow()
    {
        // 影を本体の子オブジェクトとして生成
        GameObject shadowObject = new GameObject("Shadow");
        shadowObject.transform.SetParent(transform, false);
        shadowObject.transform.localScale = Vector3.one * shadowScale;

        // 本体の見た目や回転とかをコピーする
        shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = spriteRenderer.sprite;
        shadowRenderer.flipX = spriteRenderer.flipX;
        shadowRenderer.flipY = spriteRenderer.flipY;

        // 本体より奥に描画
        shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        shadowRenderer.color = shadowColor;
    }
}