using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 선택한 오브젝트 하위의 모든 MapChunk 이름을 청크 좌표 기준 "Tile x, y"로 일괄 변경하는 에디터 도구.
/// 메뉴: Tools > Map > Chunk Tool
/// 좌표 계산은 MapManager.GetChunkIndex와 같은 방식(기준점 = 청크 중심)을 사용한다.
/// </summary>
public class ChunkTool : EditorWindow
{
    private const string NamePrefix = "Tile";

    private int _chunkSize = 32;
    private bool _includeInactive = true;

    [MenuItem("Tools/Map/Chunk Tool")]
    private static void OpenWindow()
    {
        ChunkTool window = GetWindow<ChunkTool>("Chunk Tool");
        window.minSize = new Vector2(320f, 220f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("선택한 오브젝트 하위 청크의 이름을 좌표 기준으로 변경합니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        _chunkSize = EditorGUILayout.IntField("청크 크기", _chunkSize);
        _includeInactive = EditorGUILayout.Toggle("비활성 오브젝트 포함", _includeInactive);

        EditorGUILayout.Space();

        List<MapChunk> chunks = GetSelectedChunks();
        int renameCount = CountRenameTargets(chunks);
        EditorGUILayout.LabelField($"선택: {Selection.gameObjects.Length}개 / 청크: {chunks.Count}개 / 이름 변경 대상: {renameCount}개");

        EditorGUI.BeginDisabledGroup(chunks.Count == 0 || _chunkSize <= 0);
        if (GUILayout.Button("이름 변경"))
        {
            ConfirmAndRename(chunks);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "프리팹은 Prefab Mode로 열어 루트를 선택한 뒤 실행하세요.\n" +
            "청크 크기는 MapManager의 _chunkSize와 같아야 합니다.\n" +
            "좌표 중복, 격자 어긋남은 Console에 경고로 표시됩니다.\n" +
            "Ctrl+Z로 되돌릴 수 있습니다.",
            MessageType.Info);
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private List<MapChunk> GetSelectedChunks()
    {
        List<MapChunk> result = new List<MapChunk>();
        HashSet<MapChunk> added = new HashSet<MapChunk>();

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null)
            {
                continue;
            }

            MapChunk[] found = selected.GetComponentsInChildren<MapChunk>(_includeInactive);
            foreach (MapChunk chunk in found)
            {
                if (added.Add(chunk))
                {
                    result.Add(chunk);
                }
            }
        }

        return result;
    }

    private int CountRenameTargets(List<MapChunk> chunks)
    {
        if (_chunkSize <= 0)
        {
            return 0;
        }

        int count = 0;
        foreach (MapChunk chunk in chunks)
        {
            if (chunk.gameObject.name != GetChunkName(GetChunkIndex(chunk.transform.position)))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// MapManager.GetChunkIndex와 같은 계산. 둘 중 하나를 바꾸면 다른 쪽도 같이 바꿔야 한다.
    /// </summary>
    private Vector2Int GetChunkIndex(Vector3 worldPosition)
    {
        float half = _chunkSize * 0.5f;

        int x = Mathf.FloorToInt((worldPosition.x + half) / _chunkSize);
        int y = Mathf.FloorToInt((worldPosition.y + half) / _chunkSize);

        return new Vector2Int(x, y);
    }

    private string GetChunkName(Vector2Int index)
    {
        return $"{NamePrefix} {index.x}, {index.y}";
    }

    private void ConfirmAndRename(List<MapChunk> chunks)
    {
        if (_chunkSize <= 0)
        {
            Debug.LogError($"[ChunkTool] (ConfirmAndRename) 청크 크기가 0 이하. (_chunkSize: {_chunkSize})");
            return;
        }

        string message = $"청크 {chunks.Count}개의 이름을 \"{NamePrefix} x, y\" 형식으로 변경합니다.\n계속할까요?";
        if (EditorUtility.DisplayDialog("Chunk Tool", message, "변경", "취소") == false)
        {
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Chunk Rename");

        Dictionary<Vector2Int, MapChunk> usedIndexes = new Dictionary<Vector2Int, MapChunk>();
        int renamedCount = 0;
        int warningCount = 0;

        foreach (MapChunk chunk in chunks)
        {
            Vector3 position = chunk.transform.position;
            Vector2Int index = GetChunkIndex(position);

            if (IsAlignedToGrid(position, index) == false)
            {
                Debug.LogWarning($"[ChunkTool] (ConfirmAndRename) 격자 어긋남: {chunk.name} 위치 ({position.x}, {position.y}) → 기대 위치 ({index.x * _chunkSize}, {index.y * _chunkSize})", chunk);
                warningCount++;
            }

            if (usedIndexes.TryGetValue(index, out MapChunk duplicated))
            {
                Debug.LogWarning($"[ChunkTool] (ConfirmAndRename) 청크 좌표 중복: {index} / {duplicated.name}, {chunk.name}", chunk);
                warningCount++;
            }
            else
            {
                usedIndexes.Add(index, chunk);
            }

            if (RenameChunk(chunk, GetChunkName(index)))
            {
                renamedCount++;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[ChunkTool] 청크 {chunks.Count}개 중 {renamedCount}개 이름 변경, 경고 {warningCount}개");
    }

    private bool IsAlignedToGrid(Vector3 position, Vector2Int index)
    {
        float expectedX = index.x * _chunkSize;
        float expectedY = index.y * _chunkSize;

        return Mathf.Approximately(position.x, expectedX) && Mathf.Approximately(position.y, expectedY);
    }

    private bool RenameChunk(MapChunk chunk, string newName)
    {
        if (chunk == null)
        {
            Debug.LogWarning("[ChunkTool] (RenameChunk) chunk가 null입니다.");
            return false;
        }

        GameObject target = chunk.gameObject;
        if (target.name == newName)
        {
            return false;
        }

        Undo.RecordObject(target, "Chunk Rename");
        target.name = newName;
        MarkDirty(target);

        return true;
    }

    private void MarkDirty(GameObject target)
    {
        EditorUtility.SetDirty(target);

        if (PrefabUtility.IsPartOfPrefabInstance(target))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        if (target.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(target.scene);
        }
    }
}