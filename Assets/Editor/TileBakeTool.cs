using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 지역 단위로 그린 원본 Ground Tilemap(Rule Tile 등)을 청크 단위로 나눠,
/// 각 청크(Tile x, y)의 Ground Tilemap에 일반 Tile로 구워 넣는 에디터 도구.
/// 메뉴: Tools > Map > Tile Bake Tool
/// 원본 Ground, RoomArea(선택)는 창에서 직접 지정한다.
/// - RoomArea의 표시 종류마다 Room_번호를 만들어 청크를 나눠 넣는다. 표시가 없는 청크는 Room_null.
/// - 다시 구울 때는 표시 구역의 기존 청크가 주로 속한 Room을 그대로 사용한다.
/// - 구운 결과는 일반 Tile이라 실행 중 Rule Tile 계산이 없다.
/// - 좌표 계산은 MapManager.GetChunkIndex와 같은 방식(기준점 = 청크 중심)을 사용한다.
/// </summary>
public class TileBakeTool : EditorWindow
{
    private const string NamePrefix = "Tile";
    private const string GroundName = "Ground";
    private const string RoomPrefix = "Room_";
    private const string NullRoomName = "Room_null";
    private const string DefaultTemplatePath = "Assets/Prefabs/Develop_DH/Tile_Prefab/DevTile/Tile n, n.prefab";

    [SerializeField] private Transform Transform_RegionRoot;
    [SerializeField] private Tilemap Tilemap_SourceGround;
    [SerializeField] private Tilemap Tilemap_SourceRoomArea;
    [SerializeField] private GameObject GameObject_ChunkTemplate;
    [SerializeField] private string _bakedTileFolder = "Assets/Prefabs/Develop_DH/Tiles_Baked";
    [SerializeField] private int _chunkSize = 32;
    [SerializeField] private bool _isRelocateExisting = false;

    private Dictionary<(Sprite, Tile.ColliderType), Tile> _bakedTiles = new Dictionary<(Sprite, Tile.ColliderType), Tile>();
    private Dictionary<TileBase, int> _markerCounts = new Dictionary<TileBase, int>();
    private int _createdRoomCount = 0;
    private int _nextRoomNumber = 0;

    private class BakePlan
    {
        public List<Vector3Int> Cells = new List<Vector3Int>();
        public Dictionary<Vector2Int, MapChunk> Chunks = new Dictionary<Vector2Int, MapChunk>();
        public HashSet<Vector2Int> UsedIndexes = new HashSet<Vector2Int>();
        public List<Vector2Int> MissingIndexes = new List<Vector2Int>();
    }

    [MenuItem("Tools/Map/Tile Bake Tool")]
    private static void OpenWindow()
    {
        TileBakeTool window = GetWindow<TileBakeTool>("Tile Bake Tool");
        window.minSize = new Vector2(360f, 380f);
    }

    private void OnEnable()
    {
        if (GameObject_ChunkTemplate == null)
        {
            GameObject_ChunkTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTemplatePath);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("원본 Ground를 청크 단위로 나눠 각 Room의 청크 Ground에 일반 타일로 굽습니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        Transform_RegionRoot = (Transform)EditorGUILayout.ObjectField("대상 Region 루트", Transform_RegionRoot, typeof(Transform), true);
        Tilemap_SourceGround = (Tilemap)EditorGUILayout.ObjectField("원본 Ground", Tilemap_SourceGround, typeof(Tilemap), true);
        Tilemap_SourceRoomArea = (Tilemap)EditorGUILayout.ObjectField("원본 RoomArea (선택)", Tilemap_SourceRoomArea, typeof(Tilemap), true);

        EditorGUILayout.Space();

        GameObject_ChunkTemplate = (GameObject)EditorGUILayout.ObjectField("청크 템플릿 프리팹", GameObject_ChunkTemplate, typeof(GameObject), false);
        _bakedTileFolder = EditorGUILayout.TextField("구운 타일 저장 폴더", _bakedTileFolder);
        _chunkSize = EditorGUILayout.IntField("청크 크기", _chunkSize);

        EditorGUI.BeginDisabledGroup(Tilemap_SourceRoomArea == null);
        _isRelocateExisting = EditorGUILayout.Toggle("기존 청크도 재배치", _isRelocateExisting);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        string error = Validate();
        if (error != null)
        {
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        }

        EditorGUI.BeginDisabledGroup(error != null);
        if (GUILayout.Button("굽기"))
        {
            ConfirmAndBake();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "원본 Ground, RoomArea는 지역 밖(EditOnlyObj 붙인 오브젝트)에 두고, 대상 Region 루트와 같은 위치에 두세요.\n" +
            "RoomArea 표시 종류마다 Room_번호를 만들고, 표시가 없는 청크는 Room_null에 넣습니다. RoomArea가 없으면 Room_0.\n" +
            "굽기 시 지역 안 모든 청크의 Ground를 비운 뒤 다시 채웁니다.\n" +
            "씬의 지역 사본에 구웠다면 Overrides > Apply로 프리팹에 반영하세요.\n" +
            "Ctrl+Z로 되돌릴 수 있습니다.",
            MessageType.Info);
    }

    /// <summary>
    /// 굽기 전 입력값 검사. 문제가 없으면 null, 있으면 안내 문구를 반환한다.
    /// </summary>
    private string Validate()
    {
        if (Transform_RegionRoot == null)
        {
            return "대상 Region 루트를 지정하세요.";
        }

        if (Tilemap_SourceGround == null)
        {
            return "원본 Ground Tilemap을 지정하세요.";
        }

        if (Transform_RegionRoot.GetComponent<Region>() == null)
        {
            return "대상 Region 루트에 Region 컴포넌트가 없습니다.";
        }

        if (Tilemap_SourceGround.transform.IsChildOf(Transform_RegionRoot))
        {
            return "원본이 지역 안에 있으면 프리팹에 함께 저장됩니다. 지역 밖으로 옮기세요.";
        }

        Grid regionGrid = Transform_RegionRoot.GetComponent<Grid>();
        Grid sourceGrid = Tilemap_SourceGround.layoutGrid;
        if (regionGrid == null || sourceGrid == null)
        {
            return "원본 또는 대상 Region 루트에 Grid가 없습니다.";
        }

        if (regionGrid.cellSize != sourceGrid.cellSize)
        {
            return $"Grid 셀 크기가 다릅니다. 원본 {sourceGrid.cellSize} / 대상 {regionGrid.cellSize}";
        }

        if (Tilemap_SourceRoomArea != null)
        {
            if (Tilemap_SourceRoomArea.transform.IsChildOf(Transform_RegionRoot))
            {
                return "원본 RoomArea가 지역 안에 있으면 프리팹에 함께 저장됩니다. 지역 밖으로 옮기세요.";
            }

            if (Tilemap_SourceRoomArea.layoutGrid == null || Tilemap_SourceRoomArea.layoutGrid.cellSize != regionGrid.cellSize)
            {
                return "원본 RoomArea의 Grid가 없거나 셀 크기가 대상과 다릅니다.";
            }
        }

        if (GameObject_ChunkTemplate == null)
        {
            return "청크 템플릿 프리팹을 지정하세요.";
        }

        if (GameObject_ChunkTemplate.GetComponent<MapChunk>() == null)
        {
            return "청크 템플릿에 MapChunk 컴포넌트가 없습니다.";
        }

        if (FindChildTilemap(GameObject_ChunkTemplate.transform, GroundName) == null)
        {
            return $"청크 템플릿에 '{GroundName}' Tilemap 자식이 없습니다.";
        }

        if (_chunkSize <= 0)
        {
            return "청크 크기는 1 이상이어야 합니다.";
        }

        if (string.IsNullOrEmpty(_bakedTileFolder) || _bakedTileFolder.StartsWith("Assets") == false)
        {
            return "구운 타일 저장 폴더는 Assets로 시작해야 합니다.";
        }

        return null;
    }

    private void ConfirmAndBake()
    {
        string error = Validate();
        if (error != null)
        {
            Debug.LogError($"[TileBakeTool] (ConfirmAndBake) {error}");
            return;
        }

        Tilemap_SourceGround.RefreshAllTiles();

        BakePlan plan = CreatePlan();
        if (plan.Cells.Count == 0)
        {
            Debug.LogWarning("[TileBakeTool] (ConfirmAndBake) 원본에 그려진 칸이 없습니다.");
            return;
        }

        string areaText = Tilemap_SourceRoomArea != null ? (_isRelocateExisting ? "사용 (기존 청크 재배치 포함)" : "사용 (새 청크만)") : "없음 (Room_0)";

        string message =
            $"원본 {plan.Cells.Count}칸을 청크 {plan.UsedIndexes.Count}개에 굽습니다.\n" +
            $"새로 만들 청크: {plan.MissingIndexes.Count}개\n" +
            $"Room 구역: {areaText}\n\n" +
            $"지역 안 모든 청크의 Ground를 비운 뒤 다시 채웁니다.\n계속할까요?";
        if (EditorUtility.DisplayDialog("Tile Bake Tool", message, "굽기", "취소") == false)
        {
            return;
        }

        string folder = _bakedTileFolder.TrimEnd('/');
        if (EnsureFolder(folder) == false)
        {
            return;
        }

        LoadBakedTiles(folder);

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Tile Bake");

        _createdRoomCount = 0;
        _nextRoomNumber = FindNextRoomNumber();

        Dictionary<Vector2Int, Room> targetRooms = ResolveRooms(plan);

        int relocatedCount = 0;
        if (Tilemap_SourceRoomArea != null && _isRelocateExisting)
        {
            relocatedCount = RelocateExistingChunks(plan, targetRooms);
        }

        int createdCount = CreateMissingChunks(plan, targetRooms);
        Dictionary<Vector2Int, Tilemap> grounds = ClearGrounds(plan);

        int bakedCount = 0;
        int skippedCount = 0;

        foreach (Vector3Int cell in plan.Cells)
        {
            if (BakeCell(cell, grounds, folder))
            {
                bakedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        foreach (Tilemap ground in grounds.Values)
        {
            ground.CompressBounds();
            MarkDirty(ground, ground.gameObject);
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TileBakeTool] 굽기 완료: {bakedCount}칸 / 건너뜀 {skippedCount}칸 / 새 청크 {createdCount}개 / 새 Room {_createdRoomCount}개 / 재배치 {relocatedCount}개 / 구운 타일 종류 {_bakedTiles.Count}개");
    }

    /// <summary>
    /// 지역 안 기존 청크와 원본에 그려진 칸을 모아, 새로 만들 청크 좌표를 계산한다.
    /// </summary>
    private BakePlan CreatePlan()
    {
        BakePlan plan = new BakePlan();

        MapChunk[] found = Transform_RegionRoot.GetComponentsInChildren<MapChunk>(true);
        foreach (MapChunk chunk in found)
        {
            Vector2Int index = GetChunkIndex(chunk.transform.position);

            if (plan.Chunks.TryGetValue(index, out MapChunk duplicated))
            {
                Debug.LogWarning($"[TileBakeTool] (CreatePlan) 청크 좌표 중복: {index} / {duplicated.name}, {chunk.name}. 먼저 찾은 청크에만 굽습니다.", chunk);
                continue;
            }

            plan.Chunks.Add(index, chunk);
        }

        BoundsInt bounds = Tilemap_SourceGround.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (Tilemap_SourceGround.HasTile(cell) == false)
            {
                continue;
            }

            plan.Cells.Add(cell);

            Vector2Int index = GetChunkIndex(Tilemap_SourceGround.GetCellCenterWorld(cell));
            if (plan.UsedIndexes.Add(index) && plan.Chunks.ContainsKey(index) == false)
            {
                plan.MissingIndexes.Add(index);
            }
        }

        plan.MissingIndexes.Sort(CompareIndex);

        return plan;
    }

    /// <summary>
    /// 청크 좌표마다 들어갈 Room을 정한다. 필요한 Room은 이 단계에서 만든다.
    /// - RoomArea 없음: 새 청크는 Room_0
    /// - 표시 있음: 표시 구역의 기존 청크가 주로 속한 Room을 재사용, 없으면 Room_번호 생성
    /// - 표시 없음: 새 청크만 Room_null (기존 청크는 그대로 둔다)
    /// </summary>
    private Dictionary<Vector2Int, Room> ResolveRooms(BakePlan plan)
    {
        Dictionary<Vector2Int, Room> result = new Dictionary<Vector2Int, Room>();

        if (Tilemap_SourceRoomArea == null)
        {
            Room defaultRoom = null;
            foreach (Vector2Int index in plan.MissingIndexes)
            {
                if (defaultRoom == null)
                {
                    defaultRoom = FindOrCreateRoom(RoomPrefix + "0");
                }

                result.Add(index, defaultRoom);
            }

            return result;
        }

        List<Vector2Int> targets = new List<Vector2Int>(plan.UsedIndexes);
        if (_isRelocateExisting)
        {
            foreach (Vector2Int index in plan.Chunks.Keys)
            {
                if (plan.UsedIndexes.Contains(index) == false)
                {
                    targets.Add(index);
                }
            }
        }

        targets.Sort(CompareIndex);

        Dictionary<TileBase, List<Vector2Int>> groups = new Dictionary<TileBase, List<Vector2Int>>();
        List<TileBase> groupOrder = new List<TileBase>();
        Room nullRoom = null;

        foreach (Vector2Int index in targets)
        {
            TileBase marker = FindMainMarker(index);

            if (marker == null)
            {
                if (plan.Chunks.ContainsKey(index))
                {
                    continue;
                }

                if (nullRoom == null)
                {
                    nullRoom = FindOrCreateRoom(NullRoomName);
                }

                result.Add(index, nullRoom);
                continue;
            }

            if (groups.TryGetValue(marker, out List<Vector2Int> group) == false)
            {
                group = new List<Vector2Int>();
                groups.Add(marker, group);
                groupOrder.Add(marker);
            }

            group.Add(index);
        }

        Dictionary<TileBase, Room> groupRooms = new Dictionary<TileBase, Room>();
        HashSet<Room> claimedRooms = new HashSet<Room>();

        foreach (TileBase marker in groupOrder)
        {
            Room existing = FindMajorityRoom(groups[marker], plan);
            if (existing != null && claimedRooms.Add(existing))
            {
                groupRooms.Add(marker, existing);
            }
        }

        foreach (TileBase marker in groupOrder)
        {
            if (groupRooms.TryGetValue(marker, out Room room) == false)
            {
                room = CreateRoom(RoomPrefix + _nextRoomNumber);
                _nextRoomNumber++;
                groupRooms.Add(marker, room);
            }

            foreach (Vector2Int index in groups[marker])
            {
                result.Add(index, room);
            }
        }

        return result;
    }

    /// <summary>
    /// 표시 구역 안 기존 청크들이 가장 많이 속한 Room. Room_null은 제외한다.
    /// </summary>
    private Room FindMajorityRoom(List<Vector2Int> group, BakePlan plan)
    {
        Dictionary<Room, int> counts = new Dictionary<Room, int>();
        Room bestRoom = null;
        int bestCount = 0;

        foreach (Vector2Int index in group)
        {
            if (plan.Chunks.TryGetValue(index, out MapChunk chunk) == false)
            {
                continue;
            }

            Room room = chunk.transform.parent != null ? chunk.transform.parent.GetComponentInParent<Room>(true) : null;
            if (room == null || room.name == NullRoomName)
            {
                continue;
            }

            counts.TryGetValue(room, out int count);
            count++;
            counts[room] = count;

            if (count > bestCount)
            {
                bestRoom = room;
                bestCount = count;
            }
        }

        return bestRoom;
    }

    /// <summary>
    /// 청크 영역의 RoomArea 표시 중 가장 많은 것. 표시가 없으면 null.
    /// </summary>
    private TileBase FindMainMarker(Vector2Int index)
    {
        _markerCounts.Clear();

        float half = _chunkSize * 0.5f;
        Vector3 cellSize = Tilemap_SourceRoomArea.layoutGrid.cellSize;

        Vector3 min = new Vector3(index.x * _chunkSize - half + cellSize.x * 0.5f, index.y * _chunkSize - half + cellSize.y * 0.5f, 0f);
        Vector3 max = new Vector3(index.x * _chunkSize + half - cellSize.x * 0.5f, index.y * _chunkSize + half - cellSize.y * 0.5f, 0f);

        Vector3Int minCell = Tilemap_SourceRoomArea.WorldToCell(min);
        Vector3Int maxCell = Tilemap_SourceRoomArea.WorldToCell(max);

        TileBase bestMarker = null;
        int bestCount = 0;

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                TileBase marker = Tilemap_SourceRoomArea.GetTile(new Vector3Int(x, y, minCell.z));
                if (marker == null)
                {
                    continue;
                }

                _markerCounts.TryGetValue(marker, out int count);
                count++;
                _markerCounts[marker] = count;

                if (count > bestCount)
                {
                    bestMarker = marker;
                    bestCount = count;
                }
            }
        }

        return bestMarker;
    }

    /// <summary>
    /// 지역 안 Room_번호 중 가장 큰 번호 + 1. 없으면 0.
    /// </summary>
    private int FindNextRoomNumber()
    {
        int next = 0;

        Room[] rooms = Transform_RegionRoot.GetComponentsInChildren<Room>(true);
        foreach (Room room in rooms)
        {
            if (room.name.StartsWith(RoomPrefix) == false)
            {
                continue;
            }

            if (int.TryParse(room.name.Substring(RoomPrefix.Length), out int number) && number >= next)
            {
                next = number + 1;
            }
        }

        return next;
    }

    private Room FindOrCreateRoom(string roomName)
    {
        foreach (Transform child in Transform_RegionRoot)
        {
            if (child.name != roomName)
            {
                continue;
            }

            Room room = child.GetComponent<Room>();
            if (room != null)
            {
                return room;
            }
        }

        return CreateRoom(roomName);
    }

    /// <summary>
    /// 대상 Region 루트 아래에 Room을 새로 만든다. RoomId는 비워 두고(ChunkTool로 맞춤), ActivationMode는 기본값(Group).
    /// </summary>
    private Room CreateRoom(string roomName)
    {
        GameObject created = new GameObject(roomName);
        created.transform.SetParent(Transform_RegionRoot, false);

        Room room = created.AddComponent<Room>();

        Undo.RegisterCreatedObjectUndo(created, "Tile Bake");
        MarkDirty(created, created);
        _createdRoomCount++;

        Debug.Log($"[TileBakeTool] (CreateRoom) Room 생성: {roomName}", created);

        return room;
    }

    /// <summary>
    /// 기존 청크를 Room 구역 기준으로 다시 배치한다. 표시가 없는 청크는 그대로 둔다.
    /// </summary>
    private int RelocateExistingChunks(BakePlan plan, Dictionary<Vector2Int, Room> targetRooms)
    {
        int relocatedCount = 0;

        foreach (KeyValuePair<Vector2Int, MapChunk> pair in plan.Chunks)
        {
            if (targetRooms.TryGetValue(pair.Key, out Room room) == false)
            {
                continue;
            }

            Transform chunkTransform = pair.Value.transform;
            if (chunkTransform.parent == room.transform)
            {
                continue;
            }

            if (CanRestructure(pair.Value.gameObject) == false)
            {
                Debug.LogWarning($"[TileBakeTool] (RelocateExistingChunks) 프리팹 인스턴스 하위는 부모 변경 불가. Prefab Mode에서 실행하세요: {pair.Value.name}", pair.Value);
                continue;
            }

            Undo.SetTransformParent(chunkTransform, room.transform, "Tile Bake");

            if (chunkTransform.parent != room.transform)
            {
                Debug.LogWarning($"[TileBakeTool] (RelocateExistingChunks) 부모 변경 실패: {pair.Value.name}", pair.Value);
                continue;
            }

            MarkDirty(pair.Value.gameObject, pair.Value.gameObject);
            relocatedCount++;
        }

        return relocatedCount;
    }

    private int CreateMissingChunks(BakePlan plan, Dictionary<Vector2Int, Room> targetRooms)
    {
        int createdCount = 0;

        foreach (Vector2Int index in plan.MissingIndexes)
        {
            if (targetRooms.TryGetValue(index, out Room room) == false || room == null)
            {
                Debug.LogError($"[TileBakeTool] (CreateMissingChunks) 넣을 Room을 정하지 못함: {index}");
                continue;
            }

            GameObject created = PrefabUtility.InstantiatePrefab(GameObject_ChunkTemplate, room.transform) as GameObject;
            if (created == null)
            {
                Debug.LogError($"[TileBakeTool] (CreateMissingChunks) 청크 생성 실패: {index}");
                continue;
            }

            Undo.RegisterCreatedObjectUndo(created, "Tile Bake");

            created.name = GetChunkName(index);
            created.transform.position = new Vector3(index.x * _chunkSize, index.y * _chunkSize, room.transform.position.z);

            plan.Chunks.Add(index, created.GetComponent<MapChunk>());
            MarkDirty(created, created);
            createdCount++;
        }

        return createdCount;
    }

    /// <summary>
    /// 지역 안 모든 청크의 Ground를 비우고, 청크 좌표 → Ground Tilemap 목록을 만든다.
    /// </summary>
    private Dictionary<Vector2Int, Tilemap> ClearGrounds(BakePlan plan)
    {
        Dictionary<Vector2Int, Tilemap> grounds = new Dictionary<Vector2Int, Tilemap>();

        foreach (KeyValuePair<Vector2Int, MapChunk> pair in plan.Chunks)
        {
            Tilemap ground = FindChildTilemap(pair.Value.transform, GroundName);
            if (ground == null)
            {
                Debug.LogWarning($"[TileBakeTool] (ClearGrounds) '{GroundName}' Tilemap이 없는 청크: {pair.Value.name}", pair.Value);
                continue;
            }

            Undo.RegisterCompleteObjectUndo(ground, "Tile Bake");
            ground.ClearAllTiles();
            grounds.Add(pair.Key, ground);
        }

        return grounds;
    }

    private bool BakeCell(Vector3Int cell, Dictionary<Vector2Int, Tilemap> grounds, string folder)
    {
        Sprite sprite = Tilemap_SourceGround.GetSprite(cell);
        if (sprite == null)
        {
            Debug.LogWarning($"[TileBakeTool] (BakeCell) 스프라이트가 없는 칸: {cell}");
            return false;
        }

        Vector3 world = Tilemap_SourceGround.GetCellCenterWorld(cell);
        Vector2Int index = GetChunkIndex(world);

        if (grounds.TryGetValue(index, out Tilemap ground) == false)
        {
            Debug.LogWarning($"[TileBakeTool] (BakeCell) 넣을 Ground가 없는 칸: {cell} / 청크 {index}");
            return false;
        }

        Tile tile = GetOrCreateBakedTile(sprite, Tilemap_SourceGround.GetColliderType(cell), folder);
        if (tile == null)
        {
            return false;
        }

        Vector3Int targetCell = ground.WorldToCell(world);
        ground.SetTile(targetCell, tile);

        Matrix4x4 matrix = Tilemap_SourceGround.GetTransformMatrix(cell);
        Color color = Tilemap_SourceGround.GetColor(cell);
        if (matrix != Matrix4x4.identity || color != Color.white)
        {
            ground.SetTileFlags(targetCell, TileFlags.None);
            ground.SetTransformMatrix(targetCell, matrix);
            ground.SetColor(targetCell, color);
        }

        return true;
    }

    private Tile GetOrCreateBakedTile(Sprite sprite, Tile.ColliderType colliderType, string folder)
    {
        (Sprite, Tile.ColliderType) key = (sprite, colliderType);

        if (_bakedTiles.TryGetValue(key, out Tile saved) && saved != null)
        {
            return saved;
        }

        Tile tile = CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = colliderType;

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{sprite.name}_{colliderType}.asset");
        AssetDatabase.CreateAsset(tile, path);

        if (tile == null)
        {
            Debug.LogError($"[TileBakeTool] (GetOrCreateBakedTile) 타일 에셋 생성 실패: {path}");
            return null;
        }

        _bakedTiles[key] = tile;

        return tile;
    }

    /// <summary>
    /// 저장 폴더에 이미 있는 구운 타일을 불러와 재사용한다.
    /// </summary>
    private void LoadBakedTiles(string folder)
    {
        _bakedTiles.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Tile", new[] { folder });
        foreach (string guid in guids)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(guid));
            if (tile == null || tile.sprite == null)
            {
                continue;
            }

            (Sprite, Tile.ColliderType) key = (tile.sprite, tile.colliderType);
            if (_bakedTiles.ContainsKey(key) == false)
            {
                _bakedTiles.Add(key, tile);
            }
        }
    }

    private bool EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return true;
        }

        string[] parts = folder.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
        {
            Debug.LogError($"[TileBakeTool] (EnsureFolder) 폴더 경로는 Assets로 시작해야 합니다: {folder}");
            return false;
        }

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i]))
            {
                continue;
            }

            string next = $"{current}/{parts[i]}";
            if (AssetDatabase.IsValidFolder(next) == false)
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }

        return true;
    }

    private Tilemap FindChildTilemap(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<Tilemap>();
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

    private int CompareIndex(Vector2Int a, Vector2Int b)
    {
        if (a.x != b.x)
        {
            return a.x.CompareTo(b.x);
        }

        return a.y.CompareTo(b.y);
    }

    /// <summary>
    /// 바깥 프리팹 인스턴스에 속한 자식은 부모 변경이 막혀 있다. ChunkTool.CanRestructure와 같은 판정.
    /// </summary>
    private bool CanRestructure(GameObject target)
    {
        GameObject outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(target);

        return outermost == null || outermost == target;
    }

    private void MarkDirty(Object target, GameObject owner)
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