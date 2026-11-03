using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 선택한 오브젝트 하위의 모든 Tilemap 타일을 지정한 칸 수만큼 이동하는 에디터 도구.
/// 메뉴: Tools > Map > Tilemap Shift
/// 청크 중심과 타일이 칠해진 범위가 어긋났을 때 한 번에 맞추기 위한 용도.
/// </summary>
public class TilemapShiftTool : EditorWindow
{
    private struct TileRecord
    {
        public Vector3Int Position;
        public TileBase Tile;
        public Matrix4x4 TransformMatrix;
        public Color Color;
        public TileFlags Flags;
    }

    private Vector2Int _offset = new Vector2Int(-4, 3);
    private bool _includeInactive = true;

    [MenuItem("Tools/Map/Tilemap Shift")]
    private static void OpenWindow()
    {
        TilemapShiftTool window = GetWindow<TilemapShiftTool>("Tilemap Shift");
        window.minSize = new Vector2(320f, 200f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("선택한 오브젝트 하위 Tilemap의 타일을 이동합니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        _offset = EditorGUILayout.Vector2IntField("이동할 칸 수 (x, y)", _offset);
        _includeInactive = EditorGUILayout.Toggle("비활성 오브젝트 포함", _includeInactive);

        EditorGUILayout.Space();

        List<Tilemap> tilemaps = GetSelectedTilemaps();
        EditorGUILayout.LabelField($"선택: {Selection.gameObjects.Length}개 / 대상 Tilemap: {tilemaps.Count}개");

        EditorGUI.BeginDisabledGroup(tilemaps.Count == 0 || _offset == Vector2Int.zero);
        if (GUILayout.Button("타일 이동"))
        {
            ConfirmAndShift(tilemaps);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "프리팹은 Prefab Mode로 열어 루트를 선택한 뒤 실행하세요.\n" +
            "씬 인스턴스에서 실행했다면 Overrides > Apply로 프리팹에 반영해야 합니다.\n" +
            "Ctrl+Z로 되돌릴 수 있습니다.",
            MessageType.Info);
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private List<Tilemap> GetSelectedTilemaps()
    {
        List<Tilemap> result = new List<Tilemap>();
        HashSet<Tilemap> added = new HashSet<Tilemap>();

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null)
            {
                continue;
            }

            Tilemap[] found = selected.GetComponentsInChildren<Tilemap>(_includeInactive);
            foreach (Tilemap tilemap in found)
            {
                if (added.Add(tilemap))
                {
                    result.Add(tilemap);
                }
            }
        }

        return result;
    }

    private void ConfirmAndShift(List<Tilemap> tilemaps)
    {
        string message = $"Tilemap {tilemaps.Count}개의 타일을 ({_offset.x}, {_offset.y})칸 이동합니다.\n계속할까요?";
        if (EditorUtility.DisplayDialog("Tilemap Shift", message, "이동", "취소") == false)
        {
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Tilemap Shift");

        int movedTileCount = 0;
        foreach (Tilemap tilemap in tilemaps)
        {
            movedTileCount += ShiftTilemap(tilemap, new Vector3Int(_offset.x, _offset.y, 0));
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[TilemapShiftTool] Tilemap {tilemaps.Count}개, 타일 {movedTileCount}개를 ({_offset.x}, {_offset.y})칸 이동했습니다.");
    }

    private int ShiftTilemap(Tilemap tilemap, Vector3Int offset)
    {
        if (tilemap == null)
        {
            Debug.LogWarning("[TilemapShiftTool] (ShiftTilemap) tilemap이 null입니다.");
            return 0;
        }

        Undo.RegisterCompleteObjectUndo(tilemap, "Tilemap Shift");

        List<TileRecord> records = CollectTiles(tilemap);
        if (records.Count == 0)
        {
            return 0;
        }

        tilemap.ClearAllTiles();

        foreach (TileRecord record in records)
        {
            Vector3Int newPosition = record.Position + offset;

            tilemap.SetTile(newPosition, record.Tile);

            // 잠금 플래그가 있으면 회전/색상이 적용되지 않으므로 잠시 풀었다가 원래대로 되돌린다.
            tilemap.SetTileFlags(newPosition, TileFlags.None);
            tilemap.SetTransformMatrix(newPosition, record.TransformMatrix);
            tilemap.SetColor(newPosition, record.Color);
            tilemap.SetTileFlags(newPosition, record.Flags);
        }

        tilemap.CompressBounds();
        MarkDirty(tilemap);

        return records.Count;
    }

    private List<TileRecord> CollectTiles(Tilemap tilemap)
    {
        List<TileRecord> records = new List<TileRecord>();

        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;

        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(position);
            if (tile == null)
            {
                continue;
            }

            TileRecord record = new TileRecord();
            record.Position = position;
            record.Tile = tile;
            record.TransformMatrix = tilemap.GetTransformMatrix(position);
            record.Color = tilemap.GetColor(position);
            record.Flags = tilemap.GetTileFlags(position);
            records.Add(record);
        }

        return records;
    }

    private void MarkDirty(Tilemap tilemap)
    {
        EditorUtility.SetDirty(tilemap);

        if (PrefabUtility.IsPartOfPrefabInstance(tilemap))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(tilemap);
        }

        if (tilemap.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(tilemap.gameObject.scene);
        }
    }
}