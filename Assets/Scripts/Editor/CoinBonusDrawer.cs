//===============================================================================-
//CoinBonus の Inspector に枚数→ボーナスのグラフと早見表を描く    秋田蓮音
//===============================================================================-
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CoinBonus))]
public class CoinBonusDrawer : PropertyDrawer
{
    private const float GraphHeight = 160f;
    private const float AxisLabelHeight = 16f;
    private const int GraphSegments = 120; // 折れ線の頂点数の上限

    private static readonly Color GraphBack = new Color(0.15f, 0.15f, 0.15f);
    private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color LineColor = new Color(1f, 0.85f, 0.2f);
    private static readonly Color PointColor = new Color(1f, 0.55f, 0.2f);

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        if (!property.isExpanded) return line;

        int rows = Milestones(Read(property).graphMaxCoins).Count + 1; // 見出しの行を足す
        return line * 4 + GraphHeight + AxisLabelHeight + line * rows + 8f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float line = lineHeight + EditorGUIUtility.standardVerticalSpacing;
        var row = new Rect(position.x, position.y, position.width, lineHeight);

        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        foreach (var name in new[] { "basePoints", "exponent", "graphMaxCoins" })
        {
            row.y += line;
            EditorGUI.PropertyField(row, property.FindPropertyRelative(name));
        }

        // 入力中の値で描き直す
        CoinBonus bonus = Read(property);
        Rect area = EditorGUI.IndentedRect(new Rect(position.x, row.y + line + 4f, position.width, GraphHeight));
        DrawGraph(area, bonus);

        var tableRow = new Rect(area.x, area.yMax + AxisLabelHeight + 4f, area.width, lineHeight);
        DrawTable(tableRow, line, bonus);
        EditorGUI.indentLevel--;

        EditorGUI.EndProperty();
    }

    private static CoinBonus Read(SerializedProperty property)
    {
        return new CoinBonus
        {
            basePoints = property.FindPropertyRelative("basePoints").intValue,
            exponent = property.FindPropertyRelative("exponent").floatValue,
            graphMaxCoins = Mathf.Max(1, property.FindPropertyRelative("graphMaxCoins").intValue),
        };
    }

    // 横軸 0〜graphMaxCoins 枚、縦軸 累計ボーナスの折れ線
    private static void DrawGraph(Rect area, CoinBonus bonus)
    {
        int maxCoins = bonus.graphMaxCoins;
        float maxValue = Mathf.Max(1, bonus.Calculate(maxCoins));
        var plot = new Rect(area.x + 4f, area.y + 18f, area.width - 8f, area.height - 22f);

        EditorGUI.DrawRect(area, GraphBack);
        for (int i = 1; i <= 3; i++)
            EditorGUI.DrawRect(new Rect(plot.x, plot.y + plot.height * i / 4f, plot.width, 1f), GridColor);

        Vector2 ToPoint(float coins, float value) =>
            new Vector2(plot.x + plot.width * coins / maxCoins, plot.yMax - plot.height * value / maxValue);

        // 枚数が多いときは間引いて頂点数を抑える
        int step = Mathf.Max(1, Mathf.CeilToInt(maxCoins / (float)GraphSegments));
        var points = new List<Vector3> { ToPoint(0f, 0f) };
        for (int n = step; n < maxCoins; n += step) points.Add(ToPoint(n, bonus.Calculate(n)));
        points.Add(ToPoint(maxCoins, maxValue));

        Handles.BeginGUI();
        Handles.color = LineColor;
        Handles.DrawAAPolyLine(3f, points.ToArray());
        Handles.EndGUI();

        // 早見表の枚数に点を打つ
        foreach (int n in Milestones(maxCoins))
        {
            Vector2 p = ToPoint(n, bonus.Calculate(n));
            EditorGUI.DrawRect(new Rect(p.x - 2.5f, p.y - 2.5f, 5f, 5f), PointColor);
        }

        var small = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } };
        GUI.Label(new Rect(area.x + 4f, area.y + 1f, area.width - 8f, 16f), $"累計ボーナス (最大 {maxValue:#,0})", small);

        // 横軸の目盛りはグラフの外なので Inspector の文字色のまま
        var axis = new Rect(area.x, area.yMax, area.width, AxisLabelHeight);
        GUI.Label(axis, "0 枚", EditorStyles.miniLabel);
        GUI.Label(axis, $"{maxCoins / 2f:0.#} 枚", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter });
        GUI.Label(axis, $"{maxCoins} 枚", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperRight });
    }

    // 枚数 / その1枚の価値 / 累計 の表
    private static void DrawTable(Rect row, float line, CoinBonus bonus)
    {
        float w = row.width / 3f;
        var head = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
        var cell = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };

        GUI.Label(new Rect(row.x, row.y, w, row.height), "枚数", head);
        GUI.Label(new Rect(row.x + w, row.y, w, row.height), "その1枚の価値", head);
        GUI.Label(new Rect(row.x + w * 2f, row.y, w, row.height), "累計ボーナス", head);

        foreach (int n in Milestones(bonus.graphMaxCoins))
        {
            row.y += line;
            GUI.Label(new Rect(row.x, row.y, w, row.height), $"{n} 枚", cell);
            GUI.Label(new Rect(row.x + w, row.y, w, row.height), bonus.ValueOf(n).ToString("#,0"), cell);
            GUI.Label(new Rect(row.x + w * 2f, row.y, w, row.height), bonus.Calculate(n).ToString("#,0"), cell);
        }
    }

    // 1 枚目と、きりのいい間隔で 6 行前後、最後に graphMaxCoins
    private static List<int> Milestones(int maxCoins)
    {
        int step = 1;
        foreach (int s in new[] { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000 })
        {
            step = s;
            if (maxCoins / s <= 6) break;
        }

        var list = new List<int> { 1 };
        for (int n = step; n <= maxCoins; n += step)
            if (n > 1) list.Add(n);
        if (list[list.Count - 1] != maxCoins) list.Add(maxCoins);
        return list;
    }
}
