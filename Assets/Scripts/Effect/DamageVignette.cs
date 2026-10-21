//===============================================================================-
//画面のビネット(周辺の暗さ)を管理するスクリプト    秋田蓮音
//===============================================================================-
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// カメラに付ける。通常時は薄く暗くし、被弾時は赤く強める。HP が減るほど常に少し濃くなる
// Volume とポストエフェクトの有効化は実行時に自分で行う
[RequireComponent(typeof(Camera))]
public class DamageVignette : MonoBehaviour
{
    [Tooltip("未指定ならシーンから探す")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("通常時")]
    [SerializeField] private Color baseColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float baseIntensity = 0.25f;
    [Tooltip("ふちのぼかし具合")]
    [SerializeField, Range(0.01f, 1f)] private float smoothness = 0.4f;

    [Header("HP が減ったとき")]
    [Tooltip("HP 0 のときに足される強さ。減った割合に応じて足す")]
    [SerializeField, Range(0f, 1f)] private float lowHpExtraIntensity = 0.15f;
    [Tooltip("HP 0 のときの赤みの割合。減った割合に応じて混ぜる")]
    [SerializeField, Range(0f, 1f)] private float lowHpTint = 0.4f;

    [Header("被弾時")]
    [SerializeField] private Color damageColor = new Color(0.8f, 0f, 0f, 1f);
    [Tooltip("被弾した瞬間に足される強さ")]
    [SerializeField, Range(0f, 1f)] private float damageIntensity = 0.3f;
    [Tooltip("被弾の赤が消えるまでの時間(秒)")]
    [SerializeField, Min(0.01f)] private float flashDuration = 0.4f;

    [Header("Volume")]
    [Tooltip("他の Volume より優先させるための値")]
    [SerializeField] private float volumePriority = 10f;

    private VolumeProfile profile;
    private Vignette vignette;
    private float flash;        // 1=被弾直後、0=通常
    private float missingRatio; // 0=HP満タン、1=HP0
    private int lastHp = -1;

    private void Awake()
    {
        GetComponent<Camera>().GetUniversalAdditionalCameraData().renderPostProcessing = true;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vignette = profile.Add<Vignette>(true);
        vignette.rounded.value = false;

        var go = new GameObject("DamageVignetteVolume");
        go.transform.SetParent(transform, false);
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = volumePriority;
        volume.sharedProfile = profile;

        Apply();
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("DamageVignette: PlayerHealth が見つかりません。通常時の表示だけ行います", this);
            return;
        }

        lastHp = playerHealth.CurrentHp;
        UpdateMissingRatio(playerHealth.CurrentHp, playerHealth.MaxHp);
        playerHealth.OnHpChanged += HandleHpChanged;
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHpChanged -= HandleHpChanged;
        if (profile != null) Destroy(profile);
    }

    private void Update()
    {
        if (flash > 0f)
            flash = Mathf.Max(flash - Time.deltaTime / flashDuration, 0f);

        Apply();
    }

    private void HandleHpChanged(int hp, int maxHp)
    {
        // 減ったときだけ赤く光らせる(回復では光らせない)
        if (lastHp >= 0 && hp < lastHp) flash = 1f;
        lastHp = hp;

        UpdateMissingRatio(hp, maxHp);
    }

    private void UpdateMissingRatio(int hp, int maxHp)
    {
        missingRatio = maxHp > 0 ? 1f - (float)hp / maxHp : 0f;
    }

    private void Apply()
    {
        // 立ち上がりは一瞬、消えるときはゆっくりに見えるよう二乗で減らす
        float f = flash * flash;

        float intensity = baseIntensity + lowHpExtraIntensity * missingRatio + damageIntensity * f;
        float tint = Mathf.Max(lowHpTint * missingRatio, f);

        vignette.intensity.value = Mathf.Clamp01(intensity);
        vignette.smoothness.value = smoothness;
        vignette.color.value = Color.Lerp(baseColor, damageColor, tint);
    }
}
