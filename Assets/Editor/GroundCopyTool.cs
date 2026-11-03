using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 원본 청크의 Ground 타일 내용을 대상 청크의 Ground로 일괄 복사하는 에디터 도구.
/// 메뉴: Tools > Map > Ground Copy Tool
/// - 대상/원본 청크 짝을 목록으로 지정하고 한 번에 교체한다.
/// - 자동 짝짓기: 대상 부모와 원본 부모 하위에서 이름이 같은 청크끼리 목록을 채운다.
/// - 청크 안쪽 칸 좌표 그대로 복사하므로 두 청크의 월드 위치가 달라도 같은 모양으로 들어간다.
/// - 대상 Ground의 Collider, Renderer 설정과 다른 자식은 그대로 둔다.
/// </summary>
public class GroundCopyTool : EditorWindow
{
    private const string GroundName = "Ground";

    [Serializable]
    private class GroundCopyPair
    {
        public MapChunk Target;
        public MapChunk Source;
    }

    [SerializeField] private Transform Transform_TargetParent;
    [SerializeField] private Transform Transform_SourceParent;
    [SerializeField] private bool _includeInactive = true;
    [SerializeField] private List<GroundCopyPair> _pairs = new List<GroundCopyPair>();

    private SerializedObject _serializedObject;
    private ReorderableList _pairList;
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Map/Ground Copy Tool")]
    private static void OpenWindow()
    {
        GroundCopyTool window = GetWindow<GroundCopyTool>("Ground Copy Tool");
        window.minSize = new Vector2(380f, 420f);
    }

    private void OnEnable()
    {
        _serializedObject = new SerializedObject(this);

        SerializedProperty pairsProperty = _serializedObject.FindProperty("_pairs");
        _pairList = new ReorderableList(_serializedObject, pairsProperty, true, true, true, true);
        _pairList.drawHeaderCallback = DrawPairHeader;
        _pairList.drawElementCallback = DrawPairElement;
        _pairList.elementHeight = EditorGUIUtility.singleLineHeight * 2f + 8f;
    }

    private void OnGUI()
    {
        _serializedObject.Update();

        bool isAutoPairRequested = false;
        bool isCopyRequested = false;

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.LabelField("원본 청크의 Ground 타일을 대상 청크의 Ground로 일괄 복사합니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("자동 짝짓기", EditorStyles.boldLabel);
        Transform_TargetParent = (Transform)EditorGUILayout.ObjectField("대상 부모 (기존)", Transform_TargetParent, typeof(Transform), true);
        Transform_SourceParent = (Transform)EditorGUILayout.ObjectField("원본 부모 (구운 결과)", Transform_SourceParent, typeof(Transform), true);
        _includeInactive = EditorGUILayout.Toggle("비활성 오브젝트 포함", _includeInactive);

        EditorGUI.BeginDisabledGroup(Transform_TargetParent == null || Transform_SourceParent == null);
        if (GUILayout.Button("이름으로 자동 짝짓기"))
        {
            isAutoPairRequested = true;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        _pairList.DoLayoutList();

        int validCount = CountValidPairs();
        EditorGUILayout.LabelField($"유효한 짝: {validCount}개 / 전체 {_pairs.Count}개");

        EditorGUI.BeginDisabledGroup(validCount == 0);
        if (GUILayout.Button("일괄 교체"))
        {
            isCopyRequested = true;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "대상 청크의 Ground를 비운 뒤 원본 청크의 Ground 타일로 채웁니다.\n" +
            "청크 안쪽 칸 좌표 그대로 복사하므로 월드 위치가 달라도 같은 모양으로 들어갑니다.\n" +
            "자동 짝짓기는 이름이 같은 청크(Tile x, y)끼리 목록을 새로 채웁니다.\n" +
            "같은 대상이 여러 번 있으면 첫 항목만 처리합니다.\n" +
            "Ctrl+Z로 되돌릴 수 있습니다.",
            MessageType.Info);

        EditorGUILayout.EndScrollView();

        _serializedObject.ApplyModifiedProperties();

        if (isAutoPairRequested)
        {
            AutoPair();
        }

        if (isCopyRequested)
        {
            ConfirmAndCopy();
        }
    }

    private void DrawPairHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Ground 교체 목록 (대상 ← 원본)");
    }

    private void DrawPairElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = _pairList.serializedProperty.GetArrayElementAtIndex(index);
        SerializedProperty targetProperty = element.FindPropertyRelative("Target");
        SerializedProperty sourceProperty = element.FindPropertyRelative("Source");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        Rect targetRect = new Rect(rect.x, rect.y + 2f, rect.width, lineHeight);
        Rect sourceRect = new Rect(rect.x, rect.y + 4f + lineHeight, rect.width, lineHeight);

        EditorGUI.PropertyField(targetRect, targetProperty, new GUIContent($"{index}. 대상 청크"));
        EditorGUI.PropertyField(sourceRect, sourceProperty, new GUIContent("    원본 청크"));
    }

    /// <summary>
    /// 대상 부모와 원본 부모 하위에서 이름이 같은 청크끼리 목록을 새로 채운다.
    /// </summary>
    private void AutoPair()
    {
        if (Transform_TargetParent == null || Transform_SourceParent == null)
        {
            Debug.LogWarning("[GroundCopyTool] (AutoPair) 대상 부모와 원본 부모를 모두 지정하세요.");
            return;
        }

        if (Transform_TargetParent == Transform_SourceParent
            || Transform_TargetParent.IsChildOf(Transform_SourceParent)
            || Transform_SourceParent.IsChildOf(Transform_TargetParent))
        {
            Debug.LogWarning("[GroundCopyTool] (AutoPair) 대상 부모와 원본 부모는 서로 겹치지 않는 별개의 오브젝트여야 합니다.");
            return;
        }

        if (_pairs.Count > 0)
        {
            string message = $"기존 목록 {_pairs.Count}개를 지우고 새로 채웁니다.\n계속할까요?";
            if (EditorUtility.DisplayDialog("Ground Copy Tool", message, "채우기", "취소") == false)
            {
                return;
            }
        }

        Dictionary<string, MapChunk> sources = new Dictionary<string, MapChunk>();
        MapChunk[] sourceChunks = Transform_SourceParent.GetComponentsInChildren<MapChunk>(_includeInactive);
        foreach (MapChunk chunk in sourceChunks)
        {
            if (sources.ContainsKey(chunk.name))
            {
                Debug.LogWarning($"[GroundCopyTool] (AutoPair) 원본 쪽 이름 중복: {chunk.name}. 먼저 찾은 청크를 사용합니다.", chunk);
                continue;
            }

            sources.Add(chunk.name, chunk);
        }

        _pairs.Clear();

        int unmatchedCount = 0;
        MapChunk[] targetChunks = Transform_TargetParent.GetComponentsInChildren<MapChunk>(_includeInactive);
        foreach (MapChunk target in targetChunks)
        {
            if (sources.TryGetValue(target.name, out MapChunk source) == false)
            {
                unmatchedCount++;
                continue;
            }

            GroundCopyPair pair = new GroundCopyPair();
            pair.Target = target;
            pair.Source = source;
            _pairs.Add(pair);
        }

        _serializedObject.Update();
        Repaint();

        Debug.Log($"[GroundCopyTool] 자동 짝짓기: {_pairs.Count}쌍 / 짝 없는 대상 {unmatchedCount}개");
    }

    private int CountValidPairs()
    {
        int count = 0;
        foreach (GroundCopyPair pair in _pairs)
        {
            if (IsValidPair(pair))
            {
                count++;
            }
        }

        return count;
    }

    private bool IsValidPair(GroundCopyPair pair)
    {
        if (pair == null || pair.Target == null || pair.Source == null)
        {
            return false;
        }

        if (pair.Target == pair.Source)
        {
            return false;
        }

        return FindGround(pair.Target.transform) != null && FindGround(pair.Source.transform) != null;
    }

    private void ConfirmAndCopy()
    {
        int validCount = CountValidPairs();

        string message = $"청크 {validCount}개의 Ground를 원본 내용으로 교체합니다.\n대상 Ground의 기존 타일은 지워집니다.\n계속할까요?";
        if (EditorUtility.DisplayDialog("Ground Copy Tool", message, "교체", "취소") == false)
        {
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Ground Copy");

        HashSet<MapChunk> usedTargets = new HashSet<MapChunk>();
        int copiedCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < _pairs.Count; i++)
        {
            if (CopyGround(_pairs[i], i, usedTargets))
            {
                copiedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[GroundCopyTool] Ground 교체 완료: {copiedCount}개 / 건너뜀 {skippedCount}개");
    }

    private bool CopyGround(GroundCopyPair pair, int index, HashSet<MapChunk> usedTargets)
    {
        if (pair == null || pair.Target == null || pair.Source == null)
        {
            Debug.LogWarning($"[GroundCopyTool] (CopyGround) {index}번: 대상 또는 원본이 비어 있음");
            return false;
        }

        if (pair.Target == pair.Source)
        {
            Debug.LogWarning($"[GroundCopyTool] (CopyGround) {index}번: 대상과 원본이 같은 청크: {pair.Target.name}", pair.Target);
            return false;
        }

        if (usedTargets.Add(pair.Target) == false)
        {
            Debug.LogWarning($"[GroundCopyTool] (CopyGround) {index}번: 이미 처리한 대상이라 건너뜀: {pair.Target.name}", pair.Target);
            return false;
        }

        Tilemap targetGround = FindGround(pair.Target.transform);
        Tilemap sourceGround = FindGround(pair.Source.transform);

        if (targetGround == null || sourceGround == null)
        {
            Debug.LogWarning($"[GroundCopyTool] (CopyGround) {index}번: '{GroundName}' Tilemap이 없음. 대상 {pair.Target.name} / 원본 {pair.Source.name}", pair.Target);
            return false;
        }

        Undo.RegisterCompleteObjectUndo(targetGround, "Ground Copy");
        targetGround.ClearAllTiles();

        BoundsInt bounds = sourceGround.cellBounds;
        TileBase[] tiles = sourceGround.GetTilesBlock(bounds);
        targetGround.SetTilesBlock(bounds, tiles);

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (sourceGround.HasTile(cell) == false)
            {
                continue;
            }

            Matrix4x4 matrix = sourceGround.GetTransformMatrix(cell);
            Color color = sourceGround.GetColor(cell);
            if (matrix != Matrix4x4.identity || color != Color.white)
            {
                targetGround.SetTileFlags(cell, TileFlags.None);
                targetGround.SetTransformMatrix(cell, matrix);
                targetGround.SetColor(cell, color);
            }
        }

        targetGround.CompressBounds();
        MarkDirty(targetGround, targetGround.gameObject);

        return true;
    }

    private Tilemap FindGround(Transform chunk)
    {
        Transform child = chunk.Find(GroundName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<Tilemap>();
    }

    private void MarkDirty(UnityEngine.Object target, GameObject owner)
    {
        EditorUtility.SetDirty(target);

        if (PrefabUtility.IsPartOfPrefabInstance(target))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        if (owner.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(owner.scene);
        }
    }
}