using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// タイトル背景に StreamingAssets の動画を無音ループで流す (WebGL でも動くよう URL 再生)
// 動画がない・読めないときは付けた先の Image をそのまま背景として残す
public class TitleVideoBackground : MonoBehaviour
{
    [Tooltip("StreamingAssets からの相対パス。WebGL でも再生するなら H.264 の .mp4")]
    [SerializeField] private string fileName = "TitleBackground.mp4";
    [Tooltip("動画の上に重ねる黒の濃さ")]
    [SerializeField, Range(0f, 1f)] private float dim = 0f;

    private VideoPlayer player;
    private RawImage videoImage;
    private AspectRatioFitter fitter;
    private Image dimImage;
    private RenderTexture renderTexture;

    void Awake()
    {
        CreateDim();

        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        // URL (WebGL) は事前に存在を確かめられないので、読めなければ errorReceived で諦める
        if (!path.Contains("://") && !File.Exists(path)) return;

        CreateVideoImage();

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.source = VideoSource.Url;
        player.url = path;
        player.isLooping = true;
        player.skipOnDrop = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.prepareCompleted += OnPrepared;
        player.errorReceived += OnError;
        player.Prepare();
    }

    // 動画を画面いっぱいに覆うように出す。準備ができるまでは隠しておく
    void CreateVideoImage()
    {
        var go = new GameObject("Video", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();
        videoImage = go.AddComponent<RawImage>();
        videoImage.raycastTarget = false;
        videoImage.enabled = false;
        fitter = go.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
    }

    void CreateDim()
    {
        var go = new GameObject("Dim", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        dimImage = go.AddComponent<Image>();
        dimImage.raycastTarget = false;
        ApplyDim();
    }

    void OnPrepared(VideoPlayer source)
    {
        renderTexture = new RenderTexture((int)source.width, (int)source.height, 0);
        source.targetTexture = renderTexture;
        videoImage.texture = renderTexture;
        fitter.aspectRatio = (float)source.width / source.height;
        videoImage.enabled = true;
        source.Play();
    }

    void OnError(VideoPlayer source, string message)
    {
        Debug.LogWarning($"タイトル背景の動画を再生できません ({source.url}): {message}");
        if (videoImage != null) videoImage.enabled = false;
    }

    void ApplyDim()
    {
        if (dimImage != null) dimImage.color = new Color(0f, 0f, 0f, dim);
    }

    // Play 中に Inspector で濃さを動かしても反映する
    void OnValidate()
    {
        ApplyDim();
    }

    void OnDestroy()
    {
        if (renderTexture != null) Destroy(renderTexture);
    }
}
