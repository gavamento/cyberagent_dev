using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ほぼ一本道のステージをタイルマップに自動生成する。段差は上りのみ、敵もランダム配置する。
public class StageGenerator : MonoBehaviour
{
    [Header("タイルマップ")]
    [Tooltip("生成先のタイルマップ。生成時に中身は全消去されるので専用のものを使う")]
    [SerializeField] Tilemap tilemap;
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

    [Header("エンドレス")]
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
    [Tooltip("ランダムに選んで配置する敵 (2体以上)")]
    [SerializeField] GameObject[] enemyPrefabs;
    [Tooltip("スタート時の敵の密度。1マスごとに敵が出る確率")]
    [Range(0f, 1f)][SerializeField] float enemyDensity = 0.05f;
    [Tooltip("一定距離進むごとに増える密度")]
    [Range(0f, 1f)][SerializeField] float densityIncrease = 0.01f;
    [Tooltip("密度を増やす間隔 (マス)")]
    [Min(1)][SerializeField] int densityIncreaseInterval = 100;
    [Tooltip("密度の上限")]
    [Range(0f, 1f)][SerializeField] float maxEnemyDensity = 0.3f;
    [Tooltip("敵同士の最短間隔 (マス)")]
    [Min(1)][SerializeField] int minEnemyGap = 3;
    [Tooltip("段差の手前で敵を置かない距離 (マス)")]
    [Min(0)][SerializeField] int stepSafeBefore = 4;
    [Tooltip("段差の直後で敵を置かない距離 (マス)")]
    [Min(0)][SerializeField] int stepSafeAfter = 2;
    [Tooltip("地面の上面からの敵の位置ずれ")]
    [SerializeField] Vector2 enemyOffset = Vector2.zero;

    // 段差の位置 (上った先の最初の列)。予告表示などに使う
    public IReadOnlyList<int> StepColumns => stepColumns;
    public bool IsEndless => endless;

    readonly List<int> stepColumns = new List<int>();
    readonly Dictionary<int, int> heights = new Dictionary<int, int>();
    readonly List<(int x, GameObject obj)> spawned = new List<(int, GameObject)>();

    System.Random rng;
    Transform container;
    int nextColumn;
    int deletedUntil;
    int currentHeight;
    int flatRun;
    int lastStepColumn;
    int lastEnemyColumn;

    void OnValidate()
    {
        if (maxStepUp < minStepUp) maxStepUp = minStepUp;
        if (maxFlatLength > 0 && maxFlatLength < minFlatLength) maxFlatLength = minFlatLength;
        if (maxEnemyDensity < enemyDensity) maxEnemyDensity = enemyDensity;
        if (enemyPrefabs != null && enemyPrefabs.Length > 0 && enemyPrefabs.Length < 2)
            Debug.LogWarning($"{name}: 敵プレハブは2体以上指定してください", this);
    }

    void Start()
    {
        if (generateOnStart) Generate();
    }

    void Update()
    {
        if (!endless || tilemap == null || rng == null) return;
        var t = GetTarget();
        if (t == null) return;

        int cx = tilemap.WorldToCell(t.position).x;
        while (nextColumn <= cx + generateAhead) GenerateColumn(nextColumn);
        while (deletedUntil < cx - keepBehind) DeleteColumn(deletedUntil++);
    }

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

        currentHeight = baseHeight;
        flatRun = 0;
        lastStepColumn = int.MinValue / 2;
        lastEnemyColumn = int.MinValue / 2;
        nextColumn = 0;
        deletedUntil = 0;

        int length = endless ? startFlatLength + generateAhead : stageLength;
        while (nextColumn < length) GenerateColumn(nextColumn);

        if (!endless) PlaceGoal();
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
        }
        else
        {
            flatRun++;
        }

        heights[x] = currentHeight;
        int bottom = baseHeight - groundDepth;
        for (int y = bottom; y <= currentHeight; y++)
        {
            var tile = y == currentHeight || fillTile == null ? groundTile : fillTile;
            tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }

        if (!inStart && !inGoal) TrySpawnEnemy(x);
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
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (x - lastStepColumn <= stepSafeAfter) return;
        if (x - lastEnemyColumn < minEnemyGap) return;
        if (rng.NextDouble() >= DensityAt(x)) return;

        var prefab = enemyPrefabs[rng.Next(enemyPrefabs.Length)];
        if (prefab == null) return;

        var obj = Instantiate(prefab, SurfacePosition(x) + (Vector3)enemyOffset, Quaternion.identity, container);
        spawned.Add((x, obj));
        lastEnemyColumn = x;
    }

    float DensityAt(int x)
    {
        int steps = Mathf.Max(0, x - startFlatLength) / densityIncreaseInterval;
        return Mathf.Min(maxEnemyDensity, enemyDensity + densityIncrease * steps);
    }

    // 段差が決まった時点で、その手前に置いた敵を取り除く
    void RemoveEnemiesBefore(int stepX)
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            var (x, obj) = spawned[i];
            if (x < stepX - stepSafeBefore) break;
            if (obj != null) DestroyObj(obj);
            spawned.RemoveAt(i);
        }
    }

    void DeleteColumn(int x)
    {
        if (!heights.TryGetValue(x, out int h)) return;
        for (int y = baseHeight - groundDepth; y <= h; y++)
            tilemap.SetTile(new Vector3Int(x, y, 0), null);
        heights.Remove(x);

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
