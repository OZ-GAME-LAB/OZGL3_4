using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 청크(Tile x, y)마다 따로 있는 Ground Tilemap의 경계에 생기는 룰 타일 테두리를 없애는 에디터 도구.
/// 메뉴: Tools > Map > Ground Seam Bake Tool
/// - 룰 타일은 같은 Tilemap 안의 이웃만 보기 때문에, 청크 가장자리 칸은 옆이 비었다고 보고 테두리를 그린다.
/// - 이 도구는 지역 안 모든 청크의 Ground를 임시 Tilemap 한 장에 합쳐 룰 타일 모양을 계산한 뒤,
///   그 결과를 각 청크의 Ground에 모양이 고정된 일반 Tile(구운 타일)로 써넣는다.
/// - 지형을 고친 뒤에는 이 도구를 다시 실행한다. (타일이 있는 칸은 모두 지정한 룰 타일로 보고 다시 계산)
/// - 구운 타일은 회전/뒤집기 정보까지 타일 에셋에 담으므로 Refresh Tilemaps를 실행해도 모양이 유지된다.
/// - 비밀방 가림막(이름이 Secret으로 시작하거나 RevealTargetSender/SecretArea가 붙은 Tilemap)도 함께 굽는다. 가림막은 Ground와 합쳐진 모양으로 계산해서
///   주변 지형과 이어져 보이게 하고, 그 아래 Ground는 가림막 없이 계산해서 가림막이 걷히면 방 테두리가 보인다.
/// - 다른 도구(Tile Bake Tool, Refresh Tilemaps)의 코드와 에셋은 건드리지 않는다. 구운 타일도 별도 폴더에 저장한다.
/// </summary>
public class GroundSeamBakeTool : EditorWindow
{
    private const string GroundName = "Ground";
    private const string SecretNamePrefix = "Secret";
    private const string ToolVersion = "v4";   // 창과 로그에 표시해서 최신 코드가 컴파일됐는지 확인하는 용도
    private const int ChunkSize = 32;
    private const string DefaultRuleTileGuid = "8dc28c1f013137d439125cdc4139aaae";

    [SerializeField] private Transform Transform_RegionRoot;
    [SerializeField] private TileBase TileBase_RuleTile;
    [SerializeField] private string _bakedTileFolder = "Assets/Prefabs/Tile/Tiles_SeamBaked";
    [SerializeField] private bool _isOutsideSolid = false;
    [SerializeField] private bool _isBakeSecretCover = true;

    private struct BakedKey
    {
        public Sprite Sprite;
        public Tile.ColliderType ColliderType;
        public Matrix4x4 Matrix;
    }

    private class ChunkGround
    {
        public Tilemap Ground;
        public List<Vector3Int> Cells = new List<Vector3Int>();        // 청크 Ground 안의 칸
        public List<Vector3Int> MergedCells = new List<Vector3Int>();  // 합친 Tilemap에서의 칸
    }

    private Dictionary<BakedKey, Tile> _bakedTiles = new Dictionary<BakedKey, Tile>();

    [MenuItem("Tools/Map/Ground Seam Bake Tool")]
    private static void OpenWindow()
    {
        GroundSeamBakeTool window = GetWindow<GroundSeamBakeTool>("Ground Seam Bake");
        window.minSize = new Vector2(380f, 260f);
    }

    private void OnEnable()
    {
        if (TileBase_RuleTile == null)
        {
            string path = AssetDatabase.GUIDToAssetPath(DefaultRuleTileGuid);
            if (string.IsNullOrEmpty(path) == false)
            {
                TileBase_RuleTile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "청크 경계의 룰 타일 테두리를 없앱니다.\n" +
            "지역 안 모든 청크의 Ground를 한 장으로 합쳐 모양을 계산한 뒤, 각 청크에 구운 타일로 써넣습니다.\n" +
            "지형을 고친 뒤에는 다시 실행하세요. (도구 버전 " + ToolVersion + ")",
            MessageType.Info);

        Transform_RegionRoot = (Transform)EditorGUILayout.ObjectField("대상 (Region 루트)", Transform_RegionRoot, typeof(Transform), true);
        TileBase_RuleTile = (TileBase)EditorGUILayout.ObjectField("계산에 쓸 룰 타일", TileBase_RuleTile, typeof(TileBase), false);
        _bakedTileFolder = EditorGUILayout.TextField("구운 타일 저장 폴더", _bakedTileFolder);
        _isOutsideSolid = EditorGUILayout.Toggle(new GUIContent("바깥을 막힌 것으로 계산", "옆에 청크가 없는 가장자리를 벽이 이어진 것으로 보고 테두리를 그리지 않습니다."), _isOutsideSolid);

        _isBakeSecretCover = EditorGUILayout.Toggle(new GUIContent("비밀방 가림막도 굽기", "비밀방 가림막 Tilemap(Secret_A 등)을 주변 Ground와 이어진 모양으로 굽습니다."), _isBakeSecretCover);

        EditorGUILayout.Space();

        if (GUILayout.Button("선택한 오브젝트를 대상으로"))
        {
            Transform_RegionRoot = Selection.activeTransform;
        }

        string message = Validate();
        if (message != null)
        {
            EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(message != null))
        {
            if (GUILayout.Button("경계 이음 굽기", GUILayout.Height(32f)))
            {
                Bake();
            }
        }
    }

    /// <summary>
    /// 실행 전 입력값 검사. 문제가 없으면 null, 있으면 안내 문구를 반환한다.
    /// </summary>
    private string Validate()
    {
        if (Transform_RegionRoot == null)
            return "대상(Region 루트, Room 또는 Tile)을 지정하세요.";

        if (TileBase_RuleTile == null)
            return "계산에 쓸 룰 타일을 지정하세요.";

        if (TileBase_RuleTile is Tile)
            return "룰 타일이 아닌 일반 Tile이 지정되어 있습니다. Ground에 쓰는 룰 타일을 넣으세요.";

        if (string.IsNullOrEmpty(_bakedTileFolder) || _bakedTileFolder.StartsWith("Assets") == false)
            return "구운 타일 저장 폴더는 Assets로 시작해야 합니다.";

        return null;
    }

    private void Bake()
    {
        List<ChunkGround> chunks = CollectChunks();
        if (chunks.Count == 0)
        {
            EditorUtility.DisplayDialog("Ground Seam Bake", "대상 아래에서 Ground Tilemap이 있는 청크(MapChunk)를 찾지 못했습니다.", "확인");
            return;
        }

        if (EditorUtility.DisplayDialog("Ground Seam Bake", $"청크 {chunks.Count}개의 Ground를 구운 타일로 바꿉니다. 계속할까요?", "굽기", "취소") == false)
            return;

        if (EnsureFolder(_bakedTileFolder) == false)
            return;

        LoadBakedTiles(_bakedTileFolder);

        GameObject mergedRoot = new GameObject("GroundSeamBake_Temp");
        mergedRoot.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            mergedRoot.AddComponent<Grid>();

            GameObject mergedObject = new GameObject("Merged");
            mergedObject.hideFlags = HideFlags.HideAndDontSave;
            mergedObject.transform.SetParent(mergedRoot.transform, false);
            Tilemap merged = mergedObject.AddComponent<Tilemap>();

            // 1단계: Ground만 합쳐서 계산하고 각 청크에 써넣는다
            int cellCount = FillMerged(merged, chunks);
            merged.RefreshAllTiles();

            int bakedCount = WriteBack(merged, chunks);

            // 2단계: 가림막 칸을 더해 다시 계산하고 가림막에만 써넣는다 (Ground와 이어진 모양이 된다)
            int coverCount = 0;
            int coverBakedCount = 0;
            string coverNames = "";
            if (_isBakeSecretCover)
            {
                List<ChunkGround> covers = CollectCovers();
                if (covers.Count > 0)
                {
                    AddCells(merged, covers);
                    merged.RefreshAllTiles();
                    coverBakedCount = WriteBack(merged, covers);
                    coverCount = covers.Count;

                    for (int i = 0; i < covers.Count; i++)
                    {
                        Transform parent = covers[i].Ground.transform.parent;
                        coverNames += $"\n - {(parent != null ? parent.name : "")}/{covers[i].Ground.name} ({covers[i].Cells.Count}칸)";
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GroundSeamBakeTool {ToolVersion}] 청크 {chunks.Count}개 / 칸 {cellCount}개 중 {bakedCount}개, 비밀방 가림막 {coverCount}개 / {coverBakedCount}칸을 구웠습니다. (가림막 굽기 옵션: {(_isBakeSecretCover ? "켜짐" : "꺼짐")}) 프리팹 모드라면 저장(Ctrl+S)해 주세요.{coverNames}");
        }
        finally
        {
            DestroyImmediate(mergedRoot);
        }
    }

    /// <summary>
    /// 대상 아래 모든 청크의 Ground와 타일이 있는 칸을 모은다.
    /// </summary>
    private List<ChunkGround> CollectChunks()
    {
        List<ChunkGround> chunks = new List<ChunkGround>();
        MapChunk[] mapChunks = Transform_RegionRoot.GetComponentsInChildren<MapChunk>(true);

        for (int i = 0; i < mapChunks.Length; i++)
        {
            Transform groundTransform = mapChunks[i].transform.Find(GroundName);
            if (groundTransform == null)
                continue;

            Tilemap ground = groundTransform.GetComponent<Tilemap>();
            if (ground == null)
                continue;

            chunks.Add(CreateEntry(ground));
        }

        return chunks;
    }

    /// <summary>
    /// 대상 아래 모든 비밀방 가림막 Tilemap과 타일이 있는 칸을 모은다.
    /// 가림막 판정: 이름이 Secret으로 시작하거나, RevealTargetSender 또는 SecretArea가 붙어 있는 Tilemap.
    /// </summary>
    private List<ChunkGround> CollectCovers()
    {
        List<ChunkGround> covers = new List<ChunkGround>();
        Tilemap[] tilemaps = Transform_RegionRoot.GetComponentsInChildren<Tilemap>(true);

        for (int i = 0; i < tilemaps.Length; i++)
        {
            if (IsSecretCover(tilemaps[i]) == false)
                continue;

            covers.Add(CreateEntry(tilemaps[i]));
        }

        return covers;
    }

    private bool IsSecretCover(Tilemap tilemap)
    {
        if (tilemap.name.StartsWith(SecretNamePrefix))
            return true;

        if (tilemap.GetComponent<RevealTargetSender>() != null)
            return true;

        return tilemap.GetComponentInParent<SecretArea>(true) != null;
    }

    private ChunkGround CreateEntry(Tilemap tilemap)
    {
        ChunkGround entry = new ChunkGround();
        entry.Ground = tilemap;

        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (tilemap.HasTile(cell) == false)
                continue;

            entry.Cells.Add(cell);
            entry.MergedCells.Add(ToMergedCell(tilemap.GetCellCenterWorld(cell)));
        }

        return entry;
    }

    // 이미 채워진 합친 Tilemap에 칸을 더 채운다
    private void AddCells(Tilemap merged, List<ChunkGround> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            int count = entries[i].MergedCells.Count;
            if (count == 0)
                continue;

            Vector3Int[] positions = entries[i].MergedCells.ToArray();
            TileBase[] tiles = new TileBase[count];
            for (int j = 0; j < count; j++)
            {
                tiles[j] = TileBase_RuleTile;
            }

            merged.SetTiles(positions, tiles);
        }
    }

    // 합친 Tilemap은 원점에 칸 크기 1로 놓이므로, 월드 좌표를 내림하면 칸 좌표가 된다.
    private Vector3Int ToMergedCell(Vector3 world)
    {
        return new Vector3Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y), 0);
    }

    /// <summary>
    /// 모든 청크의 칸을 합친 Tilemap에 룰 타일로 채운다. 반환값은 채운 칸 수.
    /// </summary>
    private int FillMerged(Tilemap merged, List<ChunkGround> chunks)
    {
        HashSet<Vector3Int> solidCells = new HashSet<Vector3Int>();
        HashSet<Vector3Int> chunkAreaCells = new HashSet<Vector3Int>();

        for (int i = 0; i < chunks.Count; i++)
        {
            for (int j = 0; j < chunks[i].MergedCells.Count; j++)
            {
                solidCells.Add(chunks[i].MergedCells[j]);
            }

            if (_isOutsideSolid)
            {
                AddChunkArea(chunks[i].Ground, chunkAreaCells);
            }
        }

        int cellCount = solidCells.Count;

        if (_isOutsideSolid)
        {
            AddOutsideCells(solidCells, chunkAreaCells);
        }

        Vector3Int[] positions = new Vector3Int[solidCells.Count];
        TileBase[] tiles = new TileBase[solidCells.Count];
        int index = 0;
        foreach (Vector3Int cell in solidCells)
        {
            positions[index] = cell;
            tiles[index] = TileBase_RuleTile;
            index++;
        }

        merged.SetTiles(positions, tiles);
        return cellCount;
    }

    // 청크가 차지하는 영역(청크 중심 기준 32 x 32칸)을 기록한다. 바깥 판정에 쓴다.
    private void AddChunkArea(Tilemap ground, HashSet<Vector3Int> chunkAreaCells)
    {
        Vector3 center = ground.transform.parent.position;
        int minX = Mathf.RoundToInt(center.x) - ChunkSize / 2;
        int minY = Mathf.RoundToInt(center.y) - ChunkSize / 2;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                chunkAreaCells.Add(new Vector3Int(minX + x, minY + y, 0));
            }
        }
    }

    // 어떤 청크에도 속하지 않는 바깥 칸 중, 타일이 있는 칸에 맞닿은 칸을 막힌 것으로 채운다.
    private void AddOutsideCells(HashSet<Vector3Int> solidCells, HashSet<Vector3Int> chunkAreaCells)
    {
        List<Vector3Int> outsideCells = new List<Vector3Int>();

        foreach (Vector3Int cell in solidCells)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    Vector3Int neighbor = new Vector3Int(cell.x + dx, cell.y + dy, 0);
                    if (chunkAreaCells.Contains(neighbor) == false)
                    {
                        outsideCells.Add(neighbor);
                    }
                }
            }
        }

        for (int i = 0; i < outsideCells.Count; i++)
        {
            solidCells.Add(outsideCells[i]);
        }
    }

    /// <summary>
    /// 합친 Tilemap에서 계산된 모양을 각 청크 Ground에 구운 타일로 써넣는다. 반환값은 써넣은 칸 수.
    /// </summary>
    private int WriteBack(Tilemap merged, List<ChunkGround> chunks)
    {
        int bakedCount = 0;

        for (int i = 0; i < chunks.Count; i++)
        {
            ChunkGround chunk = chunks[i];
            if (chunk.Cells.Count == 0)
                continue;

            Vector3Int[] positions = new Vector3Int[chunk.Cells.Count];
            TileBase[] tiles = new TileBase[chunk.Cells.Count];
            int count = 0;

            for (int j = 0; j < chunk.Cells.Count; j++)
            {
                Vector3Int mergedCell = chunk.MergedCells[j];
                Sprite sprite = merged.GetSprite(mergedCell);
                if (sprite == null)
                {
                    Debug.LogWarning($"[GroundSeamBakeTool] 스프라이트가 계산되지 않은 칸을 건너뜁니다: {chunk.Ground.name} {chunk.Cells[j]}", chunk.Ground);
                    continue;
                }

                Tile tile = GetOrCreateBakedTile(sprite, merged.GetColliderType(mergedCell), merged.GetTransformMatrix(mergedCell));
                if (tile == null)
                    continue;

                positions[count] = chunk.Cells[j];
                tiles[count] = tile;
                count++;
            }

            if (count == 0)
                continue;

            System.Array.Resize(ref positions, count);
            System.Array.Resize(ref tiles, count);

            Undo.RegisterCompleteObjectUndo(chunk.Ground, "Ground Seam Bake");
            chunk.Ground.SetTiles(positions, tiles);
            EditorUtility.SetDirty(chunk.Ground);

            // 프리팹 인스턴스 안의 Tilemap(가림막 등)은 바뀐 내용을 오버라이드로 기록해야 저장된다
            if (PrefabUtility.IsPartOfPrefabInstance(chunk.Ground))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(chunk.Ground);
            }
            EditorSceneManager.MarkSceneDirty(chunk.Ground.gameObject.scene);

            bakedCount += count;
        }

        return bakedCount;
    }

    private Tile GetOrCreateBakedTile(Sprite sprite, Tile.ColliderType colliderType, Matrix4x4 matrix)
    {
        BakedKey key = new BakedKey();
        key.Sprite = sprite;
        key.ColliderType = colliderType;
        key.Matrix = matrix;

        if (_bakedTiles.TryGetValue(key, out Tile saved) && saved != null)
            return saved;

        Tile tile = CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = colliderType;
        tile.transform = matrix;

        // 회전/뒤집기가 있는 모양은 이름 뒤에 표시를 붙여 구분한다
        string suffix = matrix == Matrix4x4.identity ? "" : $"_m{Mathf.Abs(matrix.GetHashCode()) % 100000}";
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_bakedTileFolder}/{sprite.name}_{colliderType}{suffix}.asset");
        AssetDatabase.CreateAsset(tile, path);

        if (tile == null)
        {
            Debug.LogError($"[GroundSeamBakeTool] 타일 에셋 생성 실패: {path}");
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
        for (int i = 0; i < guids.Length; i++)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (tile == null || tile.sprite == null)
                continue;

            BakedKey key = new BakedKey();
            key.Sprite = tile.sprite;
            key.ColliderType = tile.colliderType;
            key.Matrix = tile.transform;

            if (_bakedTiles.ContainsKey(key) == false)
            {
                _bakedTiles.Add(key, tile);
            }
        }
    }

    private bool EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return true;

        string[] parts = folder.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
        {
            Debug.LogError($"[GroundSeamBakeTool] 폴더 경로는 Assets로 시작해야 합니다: {folder}");
            return false;
        }

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i]))
                continue;

            string next = $"{current}/{parts[i]}";
            if (AssetDatabase.IsValidFolder(next) == false)
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }

        return true;
    }
}
