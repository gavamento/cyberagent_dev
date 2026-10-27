using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ほぼ一本道のステージをタイルマップに自動生成する。段差は上りのみ、敵もランダム配置する。
// 敵の出方は距離の区間に加えて、経過時間・ラッシュ/休憩・flow でも変えられる。
public class StageGenerator : MonoBehaviour
{
    [Header("タイルマップ")]
    [Tooltip("チャンクの元にするタイルマップ。これをコピーしてチャンクごとに塗る。生成時に中身は全消去されるので専用のものを使う")]
    [SerializeField] Tilemap tilemap;
    [Tooltip("1 チャンクの幅 (マス)。チャンク単位でまとめて生成・削除し、足元の当たり判定を作り直さないようにする")]
    [Min(1)][SerializeField] int chunkWidth = 20;
    [Tooltip("地面の表面に塗るタイル")]
    [SerializeField] TileBase groundTile;
    [Tooltip("表面より下に塗るタイル。未指定なら表面タイルを使う")]
    [SerializeField] TileBase fillTile;
    [Tooltip("一番低い地面からさらに下に塗る深さ (マス)")]
    [Min(1)][SerializeField] int groundDepth = 8;

    [Header("モード")]
    [Tooltip("ON: エンドレス / OFF: ゴールあり")]
    [SerializeField] bool endless = false;
    [Tooltip("乱数シード。0 なら毎回ランダム")]
    [SerializeField] int seed = 0;
    [Tooltip("開始時に自動で生成する")]
    [SerializeField] bool generateOnStart = true;

    [Header("ゴールあり")]
    [Tooltip("ステージの長さ (マス)")]
    [Min(20)][SerializeField] int stageLength = 300;
    [SerializeField] GameObject goalPrefab;
    [Tooltip("ゴール手前の平地の長さ (マス)。ここには段差も敵も置かない")]
    [Min(1)][SerializeField] int goalAreaLength = 12;
    [Tooltip("地面の上面からのゴールの位置ずれ")]
    [SerializeField] Vector2 goalOffset = Vector2.zero;

    [Header("逐次生成")]
    [Tooltip("前方生成と後方削除の基準。未指定ならメインカメラ")]
    [SerializeField] Transform target;
    [Tooltip("基準より何マス先まで生成しておくか")]
    [Min(10)][SerializeField] int generateAhead = 40;
    [Tooltip("基準より何マス後ろまで残すか")]
    [Min(5)][SerializeField] int keepBehind = 20;

    [Header("地形")]
    [Tooltip("スタート地点の地面の高さ (セル座標)")]
    [SerializeField] int baseHeight = 0;
    [Tooltip("スタート直後の平地の長さ (マス)")]
    [Min(1)][SerializeField] int startFlatLength = 15;
    [Tooltip("段差の密度。最短平地を越えた後、1マスごとに段差が出る確率")]
    [Range(0f, 1f)][SerializeField] float stepDensity = 0.08f;
    [Tooltip("段差と段差の間の最短距離 (マス)")]
    [Min(1)][SerializeField] int minFlatLength = 8;
    [Tooltip("段差と段差の間の最長距離 (マス)。これを超えたら必ず段差を入れる。0 なら無制限")]
    [Min(0)][SerializeField] int maxFlatLength = 40;
    [Tooltip("1回の段差の高さ (マス)")]
    [Min(1)][SerializeField] int minStepUp = 1;
    [Min(1)][SerializeField] int maxStepUp = 2;
    [Tooltip("基準の高さから何マスまで上がるか。超える段差の代わりに基準の高さまで落とす")]
    [Min(1)][SerializeField] int maxHeight = 6;

    [Header("敵")]
    [Tooltip("ステージ先頭から順に並べる区間。最後の区間は以降ずっと続く")]
    [SerializeField] EnemySection[] enemySections;
    [Tooltip("最後の区間に入った後、一定距離進むごとに増える密度")]
    [Range(0f, 1f)][SerializeField] float densityIncrease = 0.01f;
    [Tooltip("密度を増やす間隔 (マス)")]
    [Min(1)][SerializeField] int densityIncreaseInterval = 100;
    [Tooltip("増やした密度の上限")]
    [Range(0f, 1f)][SerializeField] float maxEnemyDensity = 0.3f;
    [Tooltip("敵同士の最短間隔 (マス)")]
    [Min(1)][SerializeField] int minEnemyGap = 3;
    [Tooltip("段差の手前で敵を置かない距離 (マス)")]
    [Min(0)][SerializeField] int stepSafeBefore = 4;
    [Tooltip("段差の直後で敵を置かない距離 (マス)")]
    [Min(0)][SerializeField] int stepSafeAfter = 2;

    [Header("時間による変化")]
    [Tooltip("開始からの秒数で敵の出方を変える。開始秒の早い順に並べ、後の設定ほど優先する。敵はカメラの先で決まるので、画面に出るのは数秒遅れる")]
    [SerializeField] TimeModifier[] timeModifiers;
    [Tooltip("一定時間だけ密度を変える。倍率を上げればラッシュ、0 なら休憩")]
    [SerializeField] TimedEvent[] timedEvents;

    [Header("flow 連動")]
    [Tooltip("flow を読むスクロール速度。未指定なら連動しない")]
    [SerializeField] ScrollSpeedProgression speedProgression;
    [Tooltip("flow 最大のとき、密度に (1 + この値) を掛ける")]
    [Min(0f)][SerializeField] float flowDensityBonus = 0.5f;
    [Tooltip("flow 最大のとき、群れの数に足す数")]
    [Min(0)][SerializeField] int flowGroupAdd = 1;

    [Header("確認用 (実行中の値)")]
    [SerializeField] float debugElapsed;
    [Tooltip("時間 × イベント × flow の密度倍率")]
    [SerializeField] float debugDensityRate;
    [SerializeField] string debugActiveEvents;
    [Tooltip("敵ごとの出やすさの倍率と群れの加算")]
    [SerializeField] string debugEnemyRates;

    [System.Serializable]
    class TimeModifier
    {
        [Tooltip("開始からの秒数")]
        [Min(0f)] public float startTime;
        [Tooltip("この秒数かけて前の値から変える。0 なら即切り替え")]
        [Min(0f)] public float rampTime = 3f;
        [Tooltip("全体の密度の倍率")]
        [Min(0f)] public float densityRate = 1f;
        [Tooltip("敵ごとの変化。ここに無い敵は前の値のまま")]
        public EnemyModifier[] enemies;
    }

    [System.Serializable]
    class EnemyModifier
    {
        [Tooltip("対象の敵。どの区間に出るものにも効く")]
        public GameObject prefab;
        [Tooltip("出やすさの倍率。0 なら出なくなる")]
        [Min(0f)] public float weightRate = 1f;
        [Tooltip("群れの数に足す数。負なら減る (最低 1)")]
        public int groupAdd;
    }

    [System.Serializable]
    class TimedEvent
    {
        [Tooltip("確認用の名前")]
        public string label;
        [Tooltip("開始からの秒数")]
        [Min(0f)] public float startTime;
        [Tooltip("続く秒数")]
        [Min(0f)] public float duration = 5f;
        [Tooltip("この間の密度の倍率。0 なら敵を出さない")]
        [Min(0f)] public float densityRate = 2f;
        [Tooltip("繰り返す間隔 (秒)。0 なら 1 回だけ")]
        [Min(0f)] public float repeatInterval;
    }

    [System.Serializable]
    class EnemyEntry
    {
        public GameObject prefab;
        [Tooltip("出やすさ。同じ区間の他の敵との比で抽選する")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("地面の上面からの位置ずれ")]
        public Vector2 offset;

        [Header("群れ")]
        [Tooltip("1回で並べて出す数の範囲。1 なら単体")]
        [Min(1)] public int groupMin = 1;
        [Min(1)] public int groupMax = 1;
        [Tooltip("群れの中の間隔 (マス)")]
        [Min(1)] public int groupSpacing = 2;

        [Header("制限")]
        [Tooltip("この区間で出る最大数。0 なら無制限")]
        [Min(0)] public int maxPerSection = 0;
        [Tooltip("同じ種類どうしの最短間隔 (マス)。0 なら制限なし")]
        [Min(0)] public int minSameGap = 0;
        [Tooltip("直前に出た敵と同じ種類なら出さない")]
        public bool noRepeat;
    }

    [System.Serializable]
    class EnemySection
    {
        [Tooltip("区間の長さ (マス)。最後の区間では無視する")]
        [Min(1)] public int length = 100;
        [Tooltip("1マスごとに敵が出る確率")]
        [Range(0f, 1f)] public float density = 0.05f;
        [Tooltip("この区間に出る敵")]
        public EnemyEntry[] enemies;
    }

    // 段差の位置 (上った先の最初の列)。予告表示などに使う
    public IReadOnlyList<int> StepColumns => stepColumns;
    public bool IsEndless => endless;

    readonly List<int> stepColumns = new List<int>();
    readonly Dictionary<int, int> heights = new Dictionary<int, int>();
    // 出した敵。段差の手前で取り除いたときに出現の記録を戻せるよう、種類と区間も覚えておく
    readonly List<(int x, GameObject obj, EnemyEntry entry, int section)> spawned = new List<(int, GameObject, EnemyEntry, int)>();
    readonly HashSet<EnemyEntry> removedEntries = new HashSet<EnemyEntry>();
    // 群れの残りを出す列
    readonly Queue<int> pendingGroup = new Queue<int>();
    readonly Dictionary<EnemyEntry, int> sectionCounts = new Dictionary<EnemyEntry, int>();
    readonly Dictionary<EnemyEntry, int> lastColumnOf = new Dictionary<EnemyEntry, int>();
    // チャンク番号 → そのチャンクのタイルマップ
    readonly Dictionary<int, Tilemap> chunks = new Dictionary<int, Tilemap>();
    // 時間による変化を毎フレームまとめた値。prefab → (出やすさの倍率, 群れの加算)
    readonly Dictionary<GameObject, (float weight, float group)> enemyRates = new Dictionary<GameObject, (float, float)>();
    readonly System.Text.StringBuilder debugBuilder = new System.Text.StringBuilder();

    System.Random rng;
    float startTime;
    float timeDensityRate = 1f;
    float eventDensityRate = 1f;
    float flowDensityRate = 1f;
    int flowGroup;
    float nextDebugTime;
    bool goalPlaced;
    Transform container;
    Transform chunkContainer;
    int nextColumn;
    int deletedUntil;
    int currentHeight;
    int flatRun;
    int lastStepColumn;
    int lastEnemyColumn;
    int currentSection;
    EnemyEntry pendingEntry;
    EnemyEntry lastEntry;

    void OnValidate()
    {
        if (maxStepUp < minStepUp) maxStepUp = minStepUp;
        if (maxFlatLength > 0 && maxFlatLength < minFlatLength) maxFlatLength = minFlatLength;
        if (enemySections == null) return;
        foreach (var s in enemySections)
        {
            if (s?.enemies == null) continue;
            foreach (var e in s.enemies)
                if (e != null && e.groupMax < e.groupMin) e.groupMax = e.groupMin;
        }
    }

    void Start()
    {
        if (generateOnStart) Generate();
    }

    void Update()
    {
        if (tilemap == null || rng == null) return;
        UpdateRates();
        var t = GetTarget();
        if (t == null) return;

        int cx = tilemap.WorldToCell(t.position).x;
        while (!IsFinished && nextColumn <= cx + generateAhead) GenerateChunk();
        while (deletedUntil < cx - keepBehind) DeleteColumn(deletedUntil++);
    }

    // ゴールありでゴールまで作り終えたか
    bool IsFinished => !endless && nextColumn >= stageLength;
    float Elapsed => Time.time - startTime;

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (tilemap == null || groundTile == null)
        {
            Debug.LogError($"{name}: タイルマップと表面タイルを指定してください", this);
            return;
        }

        Clear();
        rng = new System.Random(seed != 0 ? seed : System.Environment.TickCount);
        container = new GameObject("Spawned").transform;
        container.SetParent(transform, false);
        chunkContainer = new GameObject("StageChunks").transform;
        chunkContainer.SetParent(tilemap.transform.parent, false);

        currentHeight = baseHeight;
        flatRun = 0;
        lastStepColumn = int.MinValue / 2;
        lastEnemyColumn = int.MinValue / 2;
        currentSection = -1;
        pendingEntry = null;
        lastEntry = null;
        pendingGroup.Clear();
        sectionCounts.Clear();
        lastColumnOf.Clear();
        nextColumn = 0;
        deletedUntil = 0;
        goalPlaced = false;
        startTime = Time.time;
        UpdateRates();

        // 先の分だけ作り、残りは Update で進みに合わせて作る。エディタでの確認用にはゴールまで全部作る
        int length = !endless && !Application.isPlaying ? stageLength : startFlatLength + generateAhead;
        while (!IsFinished && nextColumn < length) GenerateChunk();
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        if (tilemap != null) tilemap.ClearAllTiles();
        heights.Clear();
        stepColumns.Clear();
        spawned.Clear();

        if (container == null) container = transform.Find("Spawned");
        if (container != null) DestroyObj(container.gameObject);
        container = null;

        chunks.Clear();
        if (chunkContainer == null && tilemap != null) chunkContainer = tilemap.transform.parent.Find("StageChunks");
        if (chunkContainer != null) DestroyObj(chunkContainer.gameObject);
        chunkContainer = null;
    }

    // 次のチャンクを一度に生成する。チャンクを分けておけば、乗っているチャンクの当たり判定は作り直されない
    void GenerateChunk()
    {
        int end = (ChunkOf(nextColumn) + 1) * chunkWidth;
        if (!endless) end = Mathf.Min(end, stageLength);
        while (nextColumn < end) GenerateColumn(nextColumn);

        if (IsFinished && !goalPlaced)
        {
            goalPlaced = true;
            PlaceGoal();
        }
    }

    int ChunkOf(int x) => Mathf.FloorToInt((float)x / chunkWidth);

    Tilemap GetChunk(int x)
    {
        int index = ChunkOf(x);
        if (chunks.TryGetValue(index, out var map)) return map;
        map = Instantiate(tilemap, chunkContainer);
        map.name = $"Chunk {index}";
        chunks[index] = map;
        return map;
    }

    void GenerateColumn(int x)
    {
        bool inStart = x < startFlatLength;
        bool inGoal = !endless && x >= stageLength - goalAreaLength;

        if (!inStart && !inGoal && ShouldStep())
        {
            int step = rng.Next(minStepUp, maxStepUp + 1);
            // 上がりすぎたら基準の高さまで落として戻す
            currentHeight = currentHeight + step > baseHeight + maxHeight ? baseHeight : currentHeight + step;
            flatRun = 0;
            lastStepColumn = x;
            stepColumns.Add(x);
            RemoveEnemiesBefore(x);
            // 群れは段差をまたがせない
            pendingGroup.Clear();
        }
        else
        {
            flatRun++;
        }

        heights[x] = currentHeight;
        var map = GetChunk(x);
        int bottom = baseHeight - groundDepth;
        for (int y = bottom; y <= currentHeight; y++)
        {
            var tile = y == currentHeight || fillTile == null ? groundTile : fillTile;
            map.SetTile(new Vector3Int(x, y, 0), tile);
        }

        if (inStart || inGoal) pendingGroup.Clear();
        else if (pendingGroup.Count > 0) SpawnPending(x);
        else TrySpawnEnemy(x);
        nextColumn = x + 1;
    }

    bool ShouldStep()
    {
        if (flatRun < minFlatLength) return false;
        if (maxFlatLength > 0 && flatRun >= maxFlatLength) return true;
        return rng.NextDouble() < stepDensity;
    }

    void TrySpawnEnemy(int x)
    {
        int index = SectionIndexAt(x, out int sectionStart);
        if (index < 0) return;
        if (index != currentSection)
        {
            currentSection = index;
            sectionCounts.Clear();
        }
        var section = enemySections[index];
        if (section.enemies == null || section.enemies.Length == 0) return;
        if (x - lastStepColumn <= stepSafeAfter) return;
        if (x - lastEnemyColumn < minEnemyGap) return;

        float density = section.density;
        // 最後の区間だけ、進むほど密度を上げる
        if (index == enemySections.Length - 1)
        {
            int steps = Mathf.Max(0, x - Mathf.Max(sectionStart, startFlatLength)) / densityIncreaseInterval;
            density = Mathf.Max(density, Mathf.Min(maxEnemyDensity, density + densityIncrease * steps));
        }
        density *= timeDensityRate * eventDensityRate * flowDensityRate;
        if (rng.NextDouble() >= Mathf.Min(1f, density)) return;

        var entry = PickEnemy(section.enemies, x);
        if (entry == null) return;

        int count = rng.Next(entry.groupMin, Mathf.Max(entry.groupMin, entry.groupMax) + 1);
        count = Mathf.Max(1, count + GroupAdd(entry));
        if (entry.maxPerSection > 0) count = Mathf.Min(count, entry.maxPerSection - CountInSection(entry));
        pendingEntry = entry;
        for (int i = 1; i < count; i++) pendingGroup.Enqueue(x + i * entry.groupSpacing);
        Spawn(entry, x);
    }

    // 群れの残りを、予定の列に来たら出す
    void SpawnPending(int x)
    {
        if (pendingGroup.Peek() != x) return;
        pendingGroup.Dequeue();
        Spawn(pendingEntry, x);
    }

    void Spawn(EnemyEntry entry, int x)
    {
        var obj = Instantiate(entry.prefab, SurfacePosition(x) + (Vector3)entry.offset, Quaternion.identity, container);
        spawned.Add((x, obj, entry, currentSection));
        lastEnemyColumn = x;
        lastEntry = entry;
        lastColumnOf[entry] = x;
        sectionCounts[entry] = CountInSection(entry) + 1;
    }

    int CountInSection(EnemyEntry entry) => sectionCounts.TryGetValue(entry, out int n) ? n : 0;

    // 列 x にこの敵を出してよいか
    bool CanSpawn(EnemyEntry e, int x)
    {
        if (e == null || e.prefab == null || EffectiveWeight(e) <= 0f) return false;
        if (e.maxPerSection > 0 && CountInSection(e) >= e.maxPerSection) return false;
        if (e.minSameGap > 0 && lastColumnOf.TryGetValue(e, out int last) && x - last < e.minSameGap) return false;
        if (e.noRepeat && e == lastEntry) return false;
        return true;
    }

    // 列 x が入る区間の番号と開始列。区間が無ければ -1
    int SectionIndexAt(int x, out int start)
    {
        start = 0;
        if (enemySections == null || enemySections.Length == 0) return -1;
        for (int i = 0; i < enemySections.Length - 1; i++)
        {
            if (x < start + enemySections[i].length) return i;
            start += enemySections[i].length;
        }
        return enemySections.Length - 1;
    }

    // 出せる敵の中から重みに応じて 1 体選ぶ。候補が無ければ null
    EnemyEntry PickEnemy(EnemyEntry[] enemies, int x)
    {
        float total = 0f;
        EnemyEntry fallback = null;
        foreach (var e in enemies)
        {
            if (!CanSpawn(e, x)) continue;
            total += EffectiveWeight(e);
            fallback = e;
        }
        if (fallback == null) return null;

        double r = rng.NextDouble() * total;
        foreach (var e in enemies)
        {
            if (!CanSpawn(e, x)) continue;
            r -= EffectiveWeight(e);
            if (r < 0) return e;
        }
        return fallback;
    }

    // 時間による倍率を掛けた出やすさ
    float EffectiveWeight(EnemyEntry e) =>
        enemyRates.TryGetValue(e.prefab, out var r) ? e.weight * r.weight : e.weight;

    // 時間と flow で群れに足す数
    int GroupAdd(EnemyEntry e) =>
        (enemyRates.TryGetValue(e.prefab, out var r) ? Mathf.RoundToInt(r.group) : 0) + flowGroup;

    // 時間・イベント・flow による倍率を今の時刻で計算し直す
    void UpdateRates()
    {
        float t = Elapsed;

        // 開始秒を過ぎた設定を順に、直前の値からその設定の値へ近づける
        timeDensityRate = 1f;
        enemyRates.Clear();
        if (timeModifiers != null)
        {
            foreach (var m in timeModifiers)
            {
                if (m == null || t < m.startTime) continue;
                float p = m.rampTime > 0f ? Mathf.Clamp01((t - m.startTime) / m.rampTime) : 1f;
                timeDensityRate = Mathf.Lerp(timeDensityRate, m.densityRate, p);
                if (m.enemies == null) continue;
                foreach (var e in m.enemies)
                {
                    if (e == null || e.prefab == null) continue;
                    var prev = enemyRates.TryGetValue(e.prefab, out var r) ? r : (weight: 1f, group: 0f);
                    enemyRates[e.prefab] = (Mathf.Lerp(prev.weight, e.weightRate, p), Mathf.Lerp(prev.group, e.groupAdd, p));
                }
            }
        }

        eventDensityRate = 1f;
        if (timedEvents != null)
        {
            foreach (var ev in timedEvents)
                if (ev != null && IsActive(ev, t)) eventDensityRate *= ev.densityRate;
        }
        // 休憩中は出しかけの群れも出さない
        if (eventDensityRate <= 0f) pendingGroup.Clear();

        float flow01 = speedProgression != null ? speedProgression.Flow01 : 0f;
        flowDensityRate = 1f + flowDensityBonus * flow01;
        flowGroup = Mathf.RoundToInt(flowGroupAdd * flow01);

        UpdateDebugView(t);
    }

    static bool IsActive(TimedEvent ev, float t)
    {
        if (t < ev.startTime) return false;
        float local = t - ev.startTime;
        if (ev.repeatInterval > 0f) local %= ev.repeatInterval;
        return local < ev.duration;
    }

    // インスペクターの確認用表示。文字列を作るのは間引く
    void UpdateDebugView(float t)
    {
        debugElapsed = t;
        debugDensityRate = timeDensityRate * eventDensityRate * flowDensityRate;
        if (Time.unscaledTime < nextDebugTime) return;
        nextDebugTime = Time.unscaledTime + 0.25f;

        debugBuilder.Clear();
        if (timedEvents != null)
        {
            foreach (var ev in timedEvents)
            {
                if (ev == null || !IsActive(ev, t)) continue;
                if (debugBuilder.Length > 0) debugBuilder.Append(", ");
                debugBuilder.Append(string.IsNullOrEmpty(ev.label) ? "(無名)" : ev.label);
            }
        }
        debugActiveEvents = debugBuilder.ToString();

        debugBuilder.Clear();
        foreach (var pair in enemyRates)
        {
            if (debugBuilder.Length > 0) debugBuilder.Append(", ");
            debugBuilder.Append(pair.Key.name).Append(" x").Append(pair.Value.weight.ToString("0.##"));
            int add = Mathf.RoundToInt(pair.Value.group);
            if (add != 0) debugBuilder.Append(add > 0 ? " +" : " ").Append(add);
        }
        if (flowGroup != 0) debugBuilder.Append(debugBuilder.Length > 0 ? ", " : "").Append("flow 群れ +").Append(flowGroup);
        debugEnemyRates = debugBuilder.ToString();
    }

    // 段差が決まった時点で、その手前に置いた敵を取り除く。出現の記録も取り除いた分だけ戻す
    void RemoveEnemiesBefore(int stepX)
    {
        removedEntries.Clear();
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            var s = spawned[i];
            if (s.x < stepX - stepSafeBefore) break;
            if (s.obj != null) DestroyObj(s.obj);
            spawned.RemoveAt(i);
            // 区間が変わった後なら数は数え直しになっているので触らない
            if (s.section == currentSection && sectionCounts.TryGetValue(s.entry, out int n))
                sectionCounts[s.entry] = Mathf.Max(0, n - 1);
            removedEntries.Add(s.entry);
        }
        if (removedEntries.Count == 0) return;

        // 残っている中で最後の敵を、直前に出た敵とする
        if (spawned.Count > 0)
        {
            var last = spawned[spawned.Count - 1];
            lastEnemyColumn = last.x;
            lastEntry = last.entry;
        }
        else
        {
            lastEnemyColumn = int.MinValue / 2;
            lastEntry = null;
        }
        foreach (var entry in removedEntries)
        {
            int index = spawned.FindLastIndex(s => s.entry == entry);
            if (index >= 0) lastColumnOf[entry] = spawned[index].x;
            else lastColumnOf.Remove(entry);
        }
    }

    void DeleteColumn(int x)
    {
        if (!heights.Remove(x)) return;
        // タイルは列ごとに消さず、チャンクの最後の列を過ぎたらチャンクごと消す
        int index = ChunkOf(x);
        if ((x + 1) % chunkWidth == 0 && chunks.TryGetValue(index, out var map))
        {
            DestroyObj(map.gameObject);
            chunks.Remove(index);
        }

        // 敵は列の順に並んでいるので先頭から見る
        while (spawned.Count > 0 && spawned[0].x <= x)
        {
            if (spawned[0].obj != null) DestroyObj(spawned[0].obj);
            spawned.RemoveAt(0);
        }
        while (stepColumns.Count > 0 && stepColumns[0] <= x) stepColumns.RemoveAt(0);
    }

    void PlaceGoal()
    {
        if (goalPrefab == null)
        {
            Debug.LogWarning($"{name}: ゴールプレハブが未指定です", this);
            return;
        }
        int x = stageLength - goalAreaLength / 2;
        Instantiate(goalPrefab, SurfacePosition(x) + (Vector3)goalOffset, Quaternion.identity, container);
    }

    // 列 x の地面の上面中央のワールド座標
    Vector3 SurfacePosition(int x)
    {
        var cell = new Vector3Int(x, heights[x] + 1, 0);
        return tilemap.CellToWorld(cell) + new Vector3(tilemap.layoutGrid.cellSize.x * 0.5f, 0f, 0f);
    }

    // ワールド座標 X の位置にある地面の上面の Y。未生成の列なら false
    public bool TryGetSurfaceY(float worldX, out float worldY)
    {
        worldY = 0f;
        if (tilemap == null) return false;
        int x = tilemap.WorldToCell(new Vector3(worldX, 0f, 0f)).x;
        if (!heights.ContainsKey(x)) return false;
        worldY = SurfacePosition(x).y;
        return true;
    }

    Transform GetTarget()
    {
        if (target != null) return target;
        return Camera.main != null ? Camera.main.transform : null;
    }

    static void DestroyObj(GameObject obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
