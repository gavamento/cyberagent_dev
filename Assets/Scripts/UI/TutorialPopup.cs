//===============================================================================-
//敵や段差が初めて画面に出たときに文字と動画のチュートリアルを出し、閉じたら少しスローで再開する    秋田蓮音
//===============================================================================-
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CyberAgentDev.ObjectPrototypeLearning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

// チュートリアルを出したいゲームシーンに置く (今は Game_Easy だけ)
// 項目ごとに、対象が画面の右から入ってきたら出す。見た項目は PlayerPrefs に残し、二度と出さない
// (exe はレジストリ、WebGL はブラウザの IndexedDB に保存される)
// 表示中は時間を止め、閉じたら実時間で slowDuration 秒スローにしてから通常へ戻す
// 表示用の Canvas は自分で作る。時間は timeScale の影響を受けないものを使う
public class TutorialPopup : MonoBehaviour
{
    public enum Trigger
    {
        Enemy,       // 普通に殴って倒せる敵
        Step,        // 段差
        Hole,        // 穴
        ChargeEnemy, // ため攻撃でしか倒せない敵
        StompEnemy,  // 踏まないと倒せない敵
        Coin,
    }

    [System.Serializable]
    private class Entry
    {
        public Trigger trigger;
        public string title;
        [TextArea(2, 6)] public string message;
        [Tooltip("StreamingAssets からの相対パス。WebGL でも再生するなら H.264 の .mp4。空なら動画なし")]
        public string videoFileName;
        [Tooltip("動画のこの秒から流す")]
        [Min(0f)] public float videoStart;
        [Tooltip("この秒まで来たら videoStart へ戻る。0 なら最後まで流す")]
        [Min(0f)] public float videoEnd;

        public Entry(Trigger trigger, string title, string message, string videoFileName)
        {
            this.trigger = trigger;
            this.title = title;
            this.message = message;
            this.videoFileName = videoFileName;
        }
    }

    // 表示中は true。この間はプレイヤーの操作を受け付けない
    public static bool IsOpen { get; private set; }

    [Tooltip("見たことを PlayerPrefs に残すキーの頭。後ろに項目名が付く (例: Tutorial_Easy_Enemy)")]
    [SerializeField] private string prefsKeyPrefix = "Tutorial_Easy_";

    [Tooltip("同じフレームに複数見つかったら上にあるものから出す")]
    [SerializeField] private Entry[] entries =
    {
        new Entry(Trigger.Enemy, "てきを 攻撃",
            "クリック / Enter / X ボタンで 攻撃\nてきを 叩いて 壊そう！", "tutorial_enemy.mp4"),
        new Entry(Trigger.Step, "だんさを ジャンプ",
            "スペース / A ボタンで ジャンプ\nだんさを のりこえよう", "tutorial_step.mp4"),
        new Entry(Trigger.Hole, "あなを ジャンプ",
            "あなに おちないように\nジャンプで とびこえよう", "tutorial_hole.mp4"),
        new Entry(Trigger.ChargeEnemy, "溜め攻撃",
            "この てきは 溜め攻撃で しか 壊せない\n長押しして はなすと 溜め攻撃", "tutorial_charge.mp4"),
        new Entry(Trigger.StompEnemy, "ふんで たおす",
            "この てきは 攻撃が きかない\nジャンプして うえから ふもう", "tutorial_stomp.mp4"),
        new Entry(Trigger.Coin, "コイン",
            "コインを とると スコアが ふえて\nスピードも アップ！", "tutorial_coin.mp4"),
    };

    [Header("出すタイミング")]
    [Tooltip("対象が画面の左端を 0、右端を 1 としたこの位置より左に来たら出す")]
    [SerializeField, Range(0.3f, 1f)] private float appearViewportX = 0.85f;
    [Tooltip("画面に出たかを調べる間隔(実時間の秒)")]
    [SerializeField, Min(0f)] private float checkInterval = 0.1f;
    [Tooltip("段差や穴の位置を知るため。未指定ならシーンから探す")]
    [SerializeField] private StageGenerator stageGenerator;

    [Header("見た目")]
    [Tooltip("未指定なら組み込みフォントを使う (WebGL では日本語が出ないので必ず指定する)")]
    [SerializeField] private Font font;

    [Header("閉じたあとのスロー")]
    [SerializeField, Range(0.05f, 1f)] private float slowScale = 0.3f;
    [Tooltip("スローのままでいる実時間(秒)")]
    [SerializeField, Min(0f)] private float slowDuration = 1f;
    [Tooltip("スローから通常の速さへ戻すのにかける実時間(秒)")]
    [SerializeField, Min(0f)] private float recoverDuration = 0.25f;

    // まだ見ていない項目
    private readonly List<Entry> pending = new List<Entry>();
    private Camera cam;
    private float nextCheckTime;

    private GameObject root;
    private VideoPlayer player;
    private GameObject videoFrame;
    private RawImage videoImage;
    private AspectRatioFitter fitter;
    private RenderTexture renderTexture;
    private Entry current;
    private bool seeking;

    private float defaultFixedDeltaTime;
    private Coroutine slowRoutine;
    private bool timeChanged;

    private void Awake()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Start()
    {
        foreach (var entry in entries)
            if (PlayerPrefs.GetInt(KeyOf(entry), 0) == 0) pending.Add(entry);
        if (pending.Count == 0)
        {
            enabled = false;
            return;
        }
        if (stageGenerator == null) stageGenerator = FindAnyObjectByType<StageGenerator>();
    }

    private string KeyOf(Entry entry) => prefsKeyPrefix + entry.trigger;

    private void Update()
    {
        if (IsOpen)
        {
            LoopVideoSection();
            return;
        }
        if (pending.Count == 0 || Time.unscaledTime < nextCheckTime) return;
        nextCheckTime = Time.unscaledTime + checkInterval;

        var entry = FindAppeared();
        if (entry != null) Open(entry);
    }

    // 表示中は他の時間操作 (剣士のスローなど) に上書きされないよう、毎フレーム止め直す
    private void LateUpdate()
    {
        if (IsOpen && !ScreenShatterTransition.IsRunning) HitStopRunner.SetBaseTimeScale(0f);
    }

    // ---- 出現の判定 ----

    // 画面に入ってきた、まだ見ていない項目。なければ null
    private Entry FindAppeared()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return null;

        // 敵はどれも DestructibleObject2D を持つので、付いている部品で種類を見分ける
        bool enemy = false, charge = false, stomp = false;
        foreach (var obj in FindObjectsByType<DestructibleObject2D>())
        {
            if (!IsOnScreen(obj.transform.position)) continue;
            if (obj.GetComponent<ChargeOnlyEnemyScript>() != null) charge = true;
            else if (obj.GetComponent<StompOnlyEnemy>() != null) stomp = true;
            else if (obj.GetComponent<BladeEnemy>() == null) enemy = true;
        }

        foreach (var entry in pending)
        {
            bool found = entry.trigger switch
            {
                Trigger.Enemy => enemy,
                Trigger.ChargeEnemy => charge,
                Trigger.StompEnemy => stomp,
                Trigger.Step => stageGenerator != null && AnyColumnOnScreen(stageGenerator.StepColumns),
                Trigger.Hole => stageGenerator != null && AnyColumnOnScreen(stageGenerator.HoleColumns),
                Trigger.Coin => AnyCoinOnScreen(),
                _ => false,
            };
            if (found) return entry;
        }
        return null;
    }

    private bool IsOnScreen(Vector3 position)
    {
        Vector3 v = cam.WorldToViewportPoint(position);
        return v.x >= 0f && v.x <= appearViewportX && v.y >= 0f && v.y <= 1f;
    }

    // 段差や穴は横の位置だけで見る (高さは画面内に収まっている前提)
    private bool AnyColumnOnScreen(IReadOnlyList<int> columns)
    {
        foreach (int column in columns)
        {
            float vx = cam.WorldToViewportPoint(new Vector3(stageGenerator.ColumnToWorldX(column), 0f, 0f)).x;
            if (vx >= 0f && vx <= appearViewportX) return true;
        }
        return false;
    }

    private bool AnyCoinOnScreen()
    {
        foreach (var coin in FindObjectsByType<Coin>())
            if (IsOnScreen(coin.transform.position)) return true;
        return false;
    }

    // ---- 開く・閉じる ----

    private void Open(Entry entry)
    {
        current = entry;
        pending.Remove(entry);

        // 閉じた直後のスロー中に次が出たら、スローをやめて止める
        if (slowRoutine != null) StopCoroutine(slowRoutine);
        slowRoutine = null;
        Time.fixedDeltaTime = defaultFixedDeltaTime;

        // シーン切り替えの演出中は演出が時間を止めているので、終わったときに戻す値を 0 に差し替える
        if (!ScreenShatterTransition.SetResumeTimeScale(0f))
            HitStopRunner.SetBaseTimeScale(0f);
        timeChanged = true;
        IsOpen = true;

        var closeButton = BuildUI(entry);
        PrepareVideo(entry);

        if (EventSystem.current == null)
        {
            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            esGo.transform.SetParent(transform, false);
        }
        // パッドの決定でもすぐ閉じられるよう、閉じるボタンを選んでおく
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
    }

    private void Close()
    {
        if (!IsOpen) return;

        PlayerPrefs.SetInt(KeyOf(current), 1);
        PlayerPrefs.Save(); // WebGL ではすぐ書き込まないとタブを閉じたときに消える

        DestroyUI();
        IsOpen = false;
        current = null;

        // 同時に画面に入っていた項目があれば、スローを挟まずに続けて出す
        var next = FindAppeared();
        if (next != null)
        {
            Open(next);
            return;
        }
        slowRoutine = StartCoroutine(SlowStartRoutine());
    }

    private IEnumerator SlowStartRoutine()
    {
        ApplyTimeScale(slowScale);

        float time = 0f;
        while (time < slowDuration)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        time = 0f;
        while (time < recoverDuration)
        {
            time += Time.unscaledDeltaTime;
            ApplyTimeScale(Mathf.Lerp(slowScale, 1f, Mathf.SmoothStep(0f, 1f, time / recoverDuration)));
            yield return null;
        }

        ApplyTimeScale(1f);
        timeChanged = false;
        slowRoutine = null;
    }

    private void ApplyTimeScale(float scale)
    {
        HitStopRunner.SetBaseTimeScale(scale);
        // スロー中も物理の更新回数を保ち、動きがカクつかないようにする (SlowMotion と同じ)
        Time.fixedDeltaTime = defaultFixedDeltaTime * scale;
    }

    // 途中でシーンが変わっても、止めたままや遅いままにしない
    private void OnDestroy()
    {
        IsOpen = false;
        if (timeChanged)
        {
            HitStopRunner.SetBaseTimeScale(1f);
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }
        if (renderTexture != null) Destroy(renderTexture);
    }

    // ---- 動画 ----

    private void PrepareVideo(Entry entry)
    {
        if (string.IsNullOrEmpty(entry.videoFileName))
        {
            videoFrame.SetActive(false);
            return;
        }

        string path = Path.Combine(Application.streamingAssetsPath, entry.videoFileName);
        // URL (WebGL) は事前に存在を確かめられないので、読めなければ errorReceived で諦める
        if (!path.Contains("://") && !File.Exists(path))
        {
            Debug.LogWarning($"チュートリアルの動画が見つかりません ({path})");
            videoFrame.SetActive(false);
            return;
        }

        player = root.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.source = VideoSource.Url;
        player.url = path;
        player.isLooping = true;
        player.skipOnDrop = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.RenderTexture;
        // 時間を止めている間に見せるので、timeScale に関係なく進める
        player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        player.prepareCompleted += OnVideoPrepared;
        player.errorReceived += OnVideoError;
        player.seekCompleted += _ => seeking = false;
        seeking = false;
        player.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        if (videoImage == null) return; // 準備中に閉じられた
        renderTexture = new RenderTexture((int)source.width, (int)source.height, 0);
        source.targetTexture = renderTexture;
        videoImage.texture = renderTexture;
        fitter.aspectRatio = (float)source.width / source.height;
        videoImage.enabled = true;
        if (current != null && current.videoStart > 0f) SeekTo(current.videoStart);
        source.Play();
    }

    // 区間の指定があれば、その中だけを繰り返す
    private void LoopVideoSection()
    {
        if (player == null || current == null || seeking || !player.isPlaying) return;
        float start = current.videoStart;
        float end = current.videoEnd;
        if (player.time < start - 0.05f || (end > start && player.time >= end)) SeekTo(start);
    }

    private void SeekTo(float time)
    {
        seeking = true;
        player.time = time;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning($"チュートリアルの動画を再生できません ({source.url}): {message}");
        if (videoFrame != null) videoFrame.SetActive(false);
    }

    // ---- 表示 ----

    // 1920x1080 を基準にした配置
    private Button BuildUI(Entry entry)
    {
        root = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // ゲームの UI より手前、シーン切り替えの破片 (1000) より奥

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 画面全体を暗くし、後ろのクリックも止める
        var dim = CreateImage("Dim", root.transform, new Color(0f, 0f, 0f, 0.7f));
        Stretch(dim.rectTransform);

        var panel = CreateImage("Panel", root.transform, new Color(0.08f, 0.08f, 0.12f, 0.95f));
        Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 1000f));

        var titleText = CreateText("Title", panel.transform, entry.title, 64);
        Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1400f, 90f));

        // 動画は黒い枠の中に縦横比を保って収める。準備ができるまでは隠しておく
        var frame = CreateImage("VideoFrame", panel.transform, Color.black);
        Place(frame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1088f, 612f));
        frame.raycastTarget = false;
        videoFrame = frame.gameObject;

        var videoGo = new GameObject("Video", typeof(RectTransform));
        videoGo.transform.SetParent(frame.transform, false);
        Stretch((RectTransform)videoGo.transform);
        videoImage = videoGo.AddComponent<RawImage>();
        videoImage.raycastTarget = false;
        videoImage.enabled = false;
        fitter = videoGo.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        var body = CreateText("Message", panel.transform, entry.message, 36);
        Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -755f), new Vector2(1400f, 115f));
        body.lineSpacing = 1.1f;

        // 閉じるボタン
        var buttonImage = CreateImage("CloseButton", panel.transform, Color.white);
        Place(buttonImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(320f, 90f));
        var button = buttonImage.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.65f, 0.15f);
        colors.highlightedColor = new Color(1f, 0.8f, 0.25f);
        colors.selectedColor = new Color(1f, 0.8f, 0.25f);
        colors.pressedColor = new Color(0.7f, 0.5f, 0.1f);
        button.colors = colors;
        button.onClick.AddListener(Close);

        var label = CreateText("Label", buttonImage.transform, "閉じる", 44);
        label.color = new Color(0.1f, 0.08f, 0.05f);
        Stretch(label.rectTransform);

        return button;
    }

    private void DestroyUI()
    {
        if (root != null) Destroy(root);
        root = null;
        player = null;
        videoFrame = null;
        videoImage = null;
        if (renderTexture != null) Destroy(renderTexture);
        renderTexture = null;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(string name, Transform parent, string value, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.text = value;
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    // 親の anchor の位置を基準に、決まった大きさで置く
    private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ---- 確認用 ----

    [ContextMenu("見たフラグを全部消す")]
    private void ClearSeenFlags()
    {
        foreach (var entry in entries) PlayerPrefs.DeleteKey(KeyOf(entry));
        PlayerPrefs.Save();
        Debug.Log($"チュートリアルの見たフラグを消しました ({prefsKeyPrefix}*)");
    }

    // 日本語フォントは使う文字だけのサブセットなので、入っていない文字を書いたら知らせる
    private void OnValidate()
    {
        if (font == null || entries == null) return;
        var missing = new StringBuilder();
        foreach (var entry in entries)
        {
            foreach (char c in entry.title + entry.message)
            {
                if (char.IsWhiteSpace(c) || font.HasCharacter(c)) continue;
                if (missing.ToString().IndexOf(c) < 0) missing.Append(c);
            }
        }
        if (missing.Length > 0)
            Debug.LogWarning($"TutorialPopup: フォント {font.name} に入っていない文字があります: {missing}", this);
    }
}
