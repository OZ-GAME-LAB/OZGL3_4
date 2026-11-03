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

    private Room _targetRoom;


    [MenuItem("Tools/Map/Chunk Tool")]
    private static void OpenWindow()
    {
        ChunkTool window = GetWindow<ChunkTool>("Chunk Tool");
        window.minSize = new Vector2(320f, 400f);
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


        EditorGUI.BeginDisabledGroup(chunks.Count == 0 || _chunkSize <= 0);
        if (GUILayout.Button("Hierarchy 정렬 (X → Y 오름차순)"))
        {
            ConfirmAndSort(chunks);
        }
        EditorGUI.EndDisabledGroup();


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Room 이동", EditorStyles.boldLabel);

        _targetRoom = (Room)EditorGUILayout.ObjectField("이동할 Room", _targetRoom, typeof(Room), true);

        int moveCount = CountMoveTargets(chunks);
        string roomName = _targetRoom != null ? _targetRoom.name : "-";
        EditorGUILayout.LabelField($"이동 대상: {moveCount}개 → {roomName}");

        EditorGUI.BeginDisabledGroup(moveCount == 0 || _targetRoom == null || _chunkSize <= 0);
        if (GUILayout.Button("선택한 청크를 Room으로 이동"))
        {
            ConfirmAndMoveToRoom(chunks);
        }
        EditorGUI.EndDisabledGroup();


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Room ID", EditorStyles.boldLabel);

        List<Room> rooms = GetSelectedRooms();
        int syncCount = CountRoomIdTargets(rooms);
        EditorGUILayout.LabelField($"선택 Room: {rooms.Count}개 / ID 변경 대상: {syncCount}개");

        EditorGUI.BeginDisabledGroup(syncCount == 0);
        if (GUILayout.Button("Room 이름으로 RoomId 맞추기"))
        {
            ConfirmAndSyncRoomIds(rooms);
        }
        EditorGUI.EndDisabledGroup();


        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "프리팹은 Prefab Mode로 열어 루트를 선택한 뒤 실행하세요.\n" +
            "청크 크기는 MapManager의 _chunkSize와 같아야 합니다.\n" +
            "좌표 중복, 격자 어긋남은 Console에 경고로 표시됩니다.\n" +
            "정렬은 부모(Room)별로 X → Y 오름차순입니다.\n" +
            "Room 이동은 같은 Region 안에서만 가능하며, 이동 후 Room을 자동 정렬합니다.\n" +
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

    private void ConfirmAndSort(List<MapChunk> chunks)
    {
        if (_chunkSize <= 0)
        {
            Debug.LogError($"[ChunkTool] (ConfirmAndSort) 청크 크기가 0 이하. (_chunkSize: {_chunkSize})");
            return;
        }

        Dictionary<Transform, List<MapChunk>> groups = GroupByParent(chunks);

        string message = $"부모 {groups.Count}개 하위의 청크 {chunks.Count}개를 X → Y 오름차순으로 정렬합니다.\n계속할까요?";
        if (EditorUtility.DisplayDialog("Chunk Tool", message, "정렬", "취소") == false)
        {
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Chunk Sort");

        int sortedParentCount = 0;
        int movedCount = 0;

        foreach (KeyValuePair<Transform, List<MapChunk>> pair in groups)
        {
            int moved = SortChildren(pair.Key, pair.Value);
            if (moved > 0)
            {
                sortedParentCount++;
                movedCount += moved;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[ChunkTool] 부모 {groups.Count}개 중 {sortedParentCount}개 정렬, 이동한 청크 {movedCount}개");
    }

    private Dictionary<Transform, List<MapChunk>> GroupByParent(List<MapChunk> chunks)
    {
        Dictionary<Transform, List<MapChunk>> result = new Dictionary<Transform, List<MapChunk>>();

        foreach (MapChunk chunk in chunks)
        {
            Transform parent = chunk.transform.parent;
            if (parent == null)
            {
                Debug.LogWarning($"[ChunkTool] (GroupByParent) 부모가 없는 청크는 정렬하지 않음: {chunk.name}", chunk);
                continue;
            }

            if (result.TryGetValue(parent, out List<MapChunk> list) == false)
            {
                list = new List<MapChunk>();
                result.Add(parent, list);
            }

            list.Add(chunk);
        }

        return result;
    }

    /// <summary>
    /// 부모 하위의 청크들을 X → Y 오름차순으로 정렬한다. 가장 앞에 있던 청크 자리부터 연속으로 배치한다.
    /// 반환값: 이동한 청크 수 (건너뛰면 0)
    /// </summary>
    private int SortChildren(Transform parent, List<MapChunk> chunks)
    {
        int startIndex = int.MaxValue;

        foreach (MapChunk chunk in chunks)
        {
            if (CanRestructure(chunk.gameObject) == false)
            {
                Debug.LogWarning($"[ChunkTool] (SortChildren) 프리팹 인스턴스 하위는 순서 변경 불가. Prefab Mode에서 실행하세요: {parent.name}", parent);
                return 0;
            }

            startIndex = Mathf.Min(startIndex, chunk.transform.GetSiblingIndex());
        }

        chunks.Sort(CompareChunkIndex);

        int movedCount = 0;

        for (int i = 0; i < chunks.Count; i++)
        {
            Transform target = chunks[i].transform;
            int targetIndex = startIndex + i;

            if (target.GetSiblingIndex() == targetIndex)
            {
                continue;
            }

            Undo.SetSiblingIndex(target, targetIndex, "Chunk Sort");
            MarkDirty(target.gameObject);
            movedCount++;
        }

        return movedCount;
    }

    private int CompareChunkIndex(MapChunk a, MapChunk b)
    {
        Vector2Int indexA = GetChunkIndex(a.transform.position);
        Vector2Int indexB = GetChunkIndex(b.transform.position);

        if (indexA.x != indexB.x)
        {
            return indexA.x.CompareTo(indexB.x);
        }

        return indexA.y.CompareTo(indexB.y);
    }

    private int CountMoveTargets(List<MapChunk> chunks)
    {
        if (_targetRoom == null)
        {
            return 0;
        }

        int count = 0;
        foreach (MapChunk chunk in chunks)
        {
            if (chunk.transform.parent != _targetRoom.transform)
            {
                count++;
            }
        }

        return count;
    }

    private void ConfirmAndMoveToRoom(List<MapChunk> chunks)
    {
        if (_targetRoom == null)
        {
            Debug.LogError("[ChunkTool] (ConfirmAndMoveToRoom) 이동할 Room이 지정되지 않았습니다.");
            return;
        }

        Region targetRegion = _targetRoom.GetComponentInParent<Region>(true);
        if (targetRegion == null)
        {
            Debug.LogError($"[ChunkTool] (ConfirmAndMoveToRoom) Room이 Region 하위에 있지 않습니다: {_targetRoom.name}", _targetRoom);
            return;
        }

        int moveCount = CountMoveTargets(chunks);
        string message = $"청크 {moveCount}개를 '{_targetRoom.name}'(으)로 옮깁니다.\n계속할까요?";
        if (EditorUtility.DisplayDialog("Chunk Tool", message, "이동", "취소") == false)
        {
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Chunk Move");

        int movedCount = 0;
        int skippedCount = 0;

        foreach (MapChunk chunk in chunks)
        {
            if (chunk.transform.parent == _targetRoom.transform)
            {
                continue;
            }

            if (MoveChunk(chunk, targetRegion))
            {
                movedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        if (movedCount > 0)
        {
            SortChildren(_targetRoom.transform, GetDirectChildChunks(_targetRoom.transform));
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[ChunkTool] '{_targetRoom.name}'(으)로 청크 {movedCount}개 이동, 건너뜀 {skippedCount}개");
    }

    private bool MoveChunk(MapChunk chunk, Region targetRegion)
    {
        if (chunk.GetComponentInParent<Region>(true) != targetRegion)
        {
            Debug.LogWarning($"[ChunkTool] (MoveChunk) 다른 Region의 청크는 옮길 수 없음: {chunk.name}", chunk);
            return false;
        }

        if (CanRestructure(chunk.gameObject) == false)
        {
            Debug.LogWarning($"[ChunkTool] (MoveChunk) 프리팹 인스턴스 하위는 부모 변경 불가. Prefab Mode에서 실행하세요: {chunk.name}", chunk);
            return false;
        }

        Undo.SetTransformParent(chunk.transform, _targetRoom.transform, "Chunk Move");

        if (chunk.transform.parent != _targetRoom.transform)
        {
            Debug.LogWarning($"[ChunkTool] (MoveChunk) 부모 변경 실패: {chunk.name}", chunk);
            return false;
        }

        MarkDirty(chunk.gameObject);

        return true;
    }

    private List<MapChunk> GetDirectChildChunks(Transform parent)
    {
        List<MapChunk> result = new List<MapChunk>();

        foreach (Transform child in parent)
        {
            MapChunk chunk = child.GetComponent<MapChunk>();
            if (chunk != null)
            {
                result.Add(chunk);
            }
        }

        return result;
    }

    /// <summary>
    /// 바깥 프리팹 인스턴스에 속한 자식은 부모 변경과 순서 변경이 막혀 있다.
    /// 프리팹과 무관하거나, 자기 자신이 가장 바깥 인스턴스 루트(중첩 프리팹 청크 등)면 변경 가능.
    /// </summary>
    private bool CanRestructure(GameObject target)
    {
        GameObject outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(target);

        return outermost == null || outermost == target;
    }

    private List<Room> GetSelectedRooms()
    {
        List<Room> result = new List<Room>();
        HashSet<Room> added = new HashSet<Room>();

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null)
            {
                continue;
            }

            Room[] found = selected.GetComponentsInChildren<Room>(_includeInactive);
            foreach (Room room in found)
            {
                if (added.Add(room))
                {
                    result.Add(room);
                }
            }
        }

        return result;
    }

    private int CountRoomIdTargets(List<Room> rooms)
    {
        int count = 0;
        foreach (Room room in rooms)
        {
            if (room.RoomId != room.gameObject.name)
            {
                count++;
            }
        }

        return count;
    }

    private void ConfirmAndSyncRoomIds(List<Room> rooms)
    {
        string message = $"Room {CountRoomIdTargets(rooms)}개의 RoomId를 오브젝트 이름으로 바꿉니다.\n계속할까요?";
        if (EditorUtility.DisplayDialog("Chunk Tool", message, "변경", "취소") == false)
        {
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Room Id Sync");

        HashSet<string> usedIds = new HashSet<string>();
        int changedCount = 0;

        foreach (Room room in rooms)
        {
            string newId = room.gameObject.name;

            if (usedIds.Add(newId) == false)
            {
                Debug.LogWarning($"[ChunkTool] (ConfirmAndSyncRoomIds) 선택한 Room 중 이름 중복: {newId}. RoomId가 겹칩니다.", room);
            }

            if (room.RoomId == newId)
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(room);
            SerializedProperty idProperty = serialized.FindProperty("_roomId");
            if (idProperty == null)
            {
                Debug.LogError("[ChunkTool] (ConfirmAndSyncRoomIds) Room의 _roomId 필드를 찾을 수 없음. Room.cs 필드 이름 확인 필요");
                break;
            }

            idProperty.stringValue = newId;
            serialized.ApplyModifiedProperties();
            changedCount++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[ChunkTool] RoomId {changedCount}개 변경");
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