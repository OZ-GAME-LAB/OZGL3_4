using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Rule Tile의 Tiling Rules를 조건이 많은 순서로 정렬하는 에디터 기능.
/// 사용: Rule Tile 에셋 Inspector 오른쪽 위 ⋮ 메뉴 > 우선순위 자동 정렬
/// - 조건 수 = ✓(This)와 ✕(NotThis)로 지정한 칸 수. 빈칸(상관없음)은 세지 않는다.
/// - 조건 수가 같으면 기존 순서를 유지한다.
/// - 규칙 내용(스프라이트, 조건, 출력)은 바꾸지 않고 순서만 바꾼다.
/// </summary>
public static class RuleTileSortTool
{
    private class RuleEntry
    {
        public RuleTile.TilingRule Rule;
        public int ConditionCount;
        public int OriginalIndex;
    }

    [MenuItem("CONTEXT/RuleTile/우선순위 자동 정렬")]
    private static void SortRules(MenuCommand command)
    {
        RuleTile ruleTile = command.context as RuleTile;
        if (ruleTile == null)
        {
            Debug.LogError("[RuleTileSortTool] (SortRules) Rule Tile을 찾을 수 없습니다.");
            return;
        }

        List<RuleTile.TilingRule> rules = ruleTile.m_TilingRules;
        if (rules == null || rules.Count < 2)
        {
            Debug.Log($"[RuleTileSortTool] {ruleTile.name}: 정렬할 규칙이 2개 미만입니다.");
            return;
        }

        List<RuleEntry> entries = new List<RuleEntry>(rules.Count);
        for (int i = 0; i < rules.Count; i++)
        {
            RuleEntry entry = new RuleEntry();
            entry.Rule = rules[i];
            entry.ConditionCount = CountConditions(rules[i]);
            entry.OriginalIndex = i;
            entries.Add(entry);
        }

        entries.Sort(CompareEntry);

        int movedCount = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].OriginalIndex != i)
            {
                movedCount++;
            }
        }

        if (movedCount == 0)
        {
            Debug.Log($"[RuleTileSortTool] {ruleTile.name}: 이미 정렬되어 있습니다.");
            return;
        }

        Undo.RecordObject(ruleTile, "Rule Tile Sort");

        rules.Clear();
        foreach (RuleEntry entry in entries)
        {
            rules.Add(entry.Rule);
        }

        EditorUtility.SetDirty(ruleTile);
        AssetDatabase.SaveAssetIfDirty(ruleTile);

        LogResult(ruleTile, entries, movedCount);
    }

    /// <summary>
    /// ✓(This), ✕(NotThis) 등 지정된 칸 수. 0(상관없음)은 세지 않는다.
    /// </summary>
    private static int CountConditions(RuleTile.TilingRule rule)
    {
        if (rule.m_Neighbors == null)
        {
            return 0;
        }

        int count = 0;
        foreach (int neighbor in rule.m_Neighbors)
        {
            if (neighbor != 0)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 조건 수 내림차순, 같으면 기존 순서 오름차순 (안정 정렬).
    /// </summary>
    private static int CompareEntry(RuleEntry a, RuleEntry b)
    {
        if (a.ConditionCount != b.ConditionCount)
        {
            return b.ConditionCount.CompareTo(a.ConditionCount);
        }

        return a.OriginalIndex.CompareTo(b.OriginalIndex);
    }

    private static void LogResult(RuleTile ruleTile, List<RuleEntry> entries, int movedCount)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine($"[RuleTileSortTool] {ruleTile.name}: 규칙 {entries.Count}개 중 {movedCount}개 위치 변경");

        for (int i = 0; i < entries.Count; i++)
        {
            RuleEntry entry = entries[i];
            string spriteName = GetSpriteName(entry.Rule);
            builder.AppendLine($"  {i + 1}. 조건 {entry.ConditionCount}개 / {spriteName} (기존 {entry.OriginalIndex + 1}번)");
        }

        Debug.Log(builder.ToString(), ruleTile);
    }

    private static string GetSpriteName(RuleTile.TilingRule rule)
    {
        if (rule.m_Sprites == null || rule.m_Sprites.Length == 0 || rule.m_Sprites[0] == null)
        {
            return "(스프라이트 없음)";
        }

        return rule.m_Sprites[0].name;
    }
}