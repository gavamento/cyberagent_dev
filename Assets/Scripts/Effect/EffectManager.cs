using UnityEngine;
using UnityEngine.VFX;

public class EffectManager : MonoBehaviour
{
    // 他のスクリプトからアクセスできる
    public static EffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// エフェクトを発生させる(スプライト指定可能)
    /// </summary>
    /// <param name="effectPrefab">生成するVFXのプレハブ</param>
    /// <param name="baseTransform">基準の座標</param>
    /// <param name="positionOffset">オフセット</param>
    /// <param name="targetSprite">使用するスプライト</param>
    public void PlayEffect(GameObject effectPrefab, Transform baseTransform, Vector3 positionOffset = default, Sprite targetSprite = null)
    {
        if (effectPrefab == null || baseTransform == null) return;

        // 生成座標の計算
        Vector3 spawnPosition = baseTransform.position + positionOffset;

        // エフェクトのインスタンス化
        GameObject vfxObj = Instantiate(effectPrefab, spawnPosition, Quaternion.identity);
        VisualEffect vfx = vfxObj.GetComponent<VisualEffect>();

        // スプライトが指定されてたら、テクスチャをVFXのプロパティに渡す
        // テクスチャのランダムな位置から色を読み取って、パーティクルにそれっぽい色を付けるため(VFXの方で処理してる)
        if (vfx != null && targetSprite != null)
        {
            Texture2D texture = targetSprite.texture;
            vfx.SetTexture("SpriteTexture", texture);
        }
    }
}