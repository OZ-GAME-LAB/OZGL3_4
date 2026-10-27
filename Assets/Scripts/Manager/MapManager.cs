using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class MapManager : SingletonBase<MapManager>
{
    [Header("청크 설정")]
    [SerializeField] private int _chunkSize = 32;
    [SerializeField] private int _activeRadius = 1;

    [Header("추적 대상")]
    [SerializeField] private Transform Transform_Target;

    [Header("지역 설정")]
    [SerializeField] private Transform Transform_RegionRoot;
    [SerializeField] private string _startRegionId;
    [SerializeField] private int _keepRegionDistance = 2;


    private Dictionary<Vector2Int, MapChunk> _chunks = new Dictionary<Vector2Int, MapChunk>();
    private Vector2Int _currentChunkIndex;
    private bool _hasCurrentChunk = false;

    private HashSet<Room> _activeRooms = new HashSet<Room>();
    private bool _isChunkDirty = false;

    private Region _currentRegion;
    public event Action<string> OnChangeRegion;
    private RegionLoader _regionLoader;

    private string _targetRegionId;
    private List<string> _activeRegionIdBuffer = new List<string>();
    private Dictionary<string, int> _regionDistances = new Dictionary<string, int>();
    private Queue<string> _regionQueue = new Queue<string>();


    public string CurrentRegionId => _currentRegion != null ? _currentRegion.RegionId : null;


    // 현재 청크는 HasCurrentChunk가 true일 때만 유효하다.
    public Vector2Int CurrentChunkIndex
    {
        get { return _currentChunkIndex; }
    }

    public bool HasCurrentChunk
    {
        get { return isActiveAndEnabled && _hasCurrentChunk && Transform_Target != null; }
    }


    // 수정됨: 알림을 위한 이벤트
    public event Action<Vector2Int> OnChangePlayerChunk;
    public event Action OnClearPlayerChunk;


    private void Start()
    {
        if (_keepRegionDistance < 1)
        {
            Debug.LogWarning($"[MapManager] (EnsureRegionLoader) _keepRegionDistance가 1 미만({_keepRegionDistance}). 1로 보정");

            _keepRegionDistance = 1;
        }

        EnsureRegionLoader();

        if (!string.IsNullOrEmpty(_startRegionId))
        {
            EnterRegionAsync(_startRegionId).Forget();
        }
    }

    private void Update()
    {
        UpdatePlayerChunk(); // 수정된 부분 - 재욱님 추가

        if (_isChunkDirty && _hasCurrentChunk)
        {
            CheckRegionChange(_currentChunkIndex);
            RefreshActiveChunks(_currentChunkIndex);
        }
    }


    [ContextMenu("테스트: Region_Test01 불러오기")]
    private void TestLoadRegionTest01()
    {
        if (_regionLoader == null)
        {
            Debug.LogWarning("[MapManager] (TestLoadRegion_Test01) 플레이 모드에서만 사용 가능");

            return;
        }

        _regionLoader.LoadRegionAsync("Region_Test01").Forget();
    }

    [ContextMenu("테스트: Region_Test01 내리기")]
    private void TestUnloadRegionTest01()
    {
        if (_regionLoader == null)
        {
            Debug.LogWarning("[MapManager] (TestUnloadRegionTest01) 플레이 모드에서만 사용 가능");
            return;
        }

        _regionLoader.UnloadRegion("Region_Test01");
    }

    [ContextMenu("테스트: 불러오고 바로 내리기")]
    private void TestLoadAndUnloadRegionTest01()
    {
        if (_regionLoader == null)
        {
            Debug.LogWarning("[MapManager] (TestLoadAndUnloadRegionTest01) 플레이 모드에서만 사용 가능");
            return;
        }

        _regionLoader.LoadRegionAsync("Region_Test01").Forget();
        _regionLoader.UnloadRegion("Region_Test01");
    }

    [ContextMenu("테스트: Region_Test02 숨기기")]
    private void TestHideRegionTest02()
    {
        if (_regionLoader == null)
        {
            Debug.LogWarning("[MapManager] (TestHideRegionTest02) 플레이 모드에서만 사용 가능");
            return;
        }

        _regionLoader.SetRegionVisible("Region_Test02", false);
    }

    [ContextMenu("테스트: Region_Test02 보이기")]
    private void TestVisibleRegionTest02()
    {
        if (_regionLoader == null)
        {
            Debug.LogWarning("[MapManager] (TestVisibleRegionTest02) 플레이 모드에서만 사용 가능");
            return;
        }

        _regionLoader.SetRegionVisible("Region_Test02", true);
    }


    public void UpdatePlayerChunk() // 수정된 부분 - 재욱님 추가 (청크가 업데이트 될 때마다 이벤트를 통해 알리기 위함)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }
        if (Transform_Target == null)
        {
            ClearPlayerChunk();
            return;
        }

        Vector2Int index = GetChunkIndex(Transform_Target.position);

        if (_hasCurrentChunk && index == _currentChunkIndex) return;

        _currentChunkIndex = index;
        _hasCurrentChunk = true;

        Debug.Log($"[MapManager] (Update) 청크 이동: {index}"); //청크 이동시 알림 테스트 코드

        CheckRegionChange(index); // 수정된 부분 - 김동혁 (지역 변경 메서드 추가)

        RefreshActiveChunks(index);
        OnChangePlayerChunk?.Invoke(index);
    }

    private void OnDisable()
    {
        ClearPlayerChunk();
    }


    private void ClearPlayerChunk() // 수정된 부분 - 재욱님 추가 (현재 청크를 비우기 위한 코드)
    {
        if (!_hasCurrentChunk)
        {
            return;
        }
        _hasCurrentChunk = false;
        OnClearPlayerChunk?.Invoke();
    }


    //[ContextMenu("청크 개수 출력")]
    //private void LogChunkCount()
    //{
    //    Debug.Log($"[MapManager] 등록된 청크: {_chunks.Count}개");
    //    Debug.Log($"[MapManager] 등록된 청크: {_chunks.Count}개");
    //}


    public Vector2Int GetChunkIndex(Vector3 worldPosition) //청크의 좌표를 Vector2Int로 변환하는 메서드
    {
        if (_chunkSize <= 0)
        {
            Debug.LogError($"[MapManager] (GetChunkIndex) _chunkSize가 0 이하. (_chunkSize: {_chunkSize}) 올바른 값 설정필요.");

            return Vector2Int.zero;
        }

        float half = _chunkSize * 0.5f;

        int x = Mathf.FloorToInt((worldPosition.x + half) / _chunkSize);
        int y = Mathf.FloorToInt((worldPosition.y + half) / _chunkSize);

        return new Vector2Int(x, y);
    }

    public void RegisterChunk(MapChunk chunk)
    {
        if (chunk == null)
        {
            Debug.LogWarning($"[MapManager] (RegisterChunk) chunk가 null. chunk 재확인 필요.");

            return;
        }

        Vector2Int key = GetChunkIndex(chunk.transform.position);

        if (_chunks.ContainsKey(key))
        {
            Debug.LogWarning($"[MapManager] (RegisterChunk) 청크 좌표 중복: {key} / {chunk.name}");

            return;
        }

        _chunks.Add(key, chunk);

        _isChunkDirty = true;

        Debug.Log($"[MapManager] (RegisterChunk) 등록: {key} / {chunk.name} / 총 {_chunks.Count}개"); //등록된 청크들 체크하는 테스트 코드
    }

    public void UnregisterChunk(MapChunk chunk)
    {
        if (chunk == null)
        {
            Debug.LogWarning($"[MapManager] (UnregisterChunk) chunk가 null. chunk 재확인 필요.");

            return;
        }

        Vector2Int key = GetChunkIndex(chunk.transform.position);

        if (_chunks.TryGetValue(key, out MapChunk saved) && saved == chunk)
        {
            _chunks.Remove(key);

            _isChunkDirty = true;

            Debug.Log($"[MapManager] (UnregisterChunk) 해제: {key} / {chunk.name} / 총 {_chunks.Count}개");
        }
    }

    public void SetTarget(Transform target) //플레이어의 위치를 보고 다른 청크로 넘어간 순간을 감지
    {
        Transform_Target = target;

        if (target == null)
        {
            ClearPlayerChunk();
            Debug.LogWarning($"[MapManager] (SetTarget) target == null. 확인 필요.");

            return;
        }
        _hasCurrentChunk = false;
    }

    private bool IsInActiveRange(Vector2Int chunkIndex, Vector2Int center) //사거리 내의 청크들의 On/Off를 지시
    {
        int distanceX = Mathf.Abs(chunkIndex.x - center.x);
        int distanceY = Mathf.Abs(chunkIndex.y - center.y);

        return distanceX <= _activeRadius && distanceY <= _activeRadius;
    }

    private void RefreshActiveChunks(Vector2Int center)
    {
        CollectActiveRooms(center);

        foreach (KeyValuePair<Vector2Int, MapChunk> pair in _chunks)
        {
            pair.Value.SetObjectActive(ShouldBeActive(pair.Key, pair.Value, center));
        }

        _isChunkDirty = false;
    }


    private bool IsGroupChunk(MapChunk chunk)
    {
        return chunk.Room != null && chunk.Room.ActivationMode == RoomActivationMode.Group;
    }

    private void CollectActiveRooms(Vector2Int center)
    {
        _activeRooms.Clear();
        foreach (KeyValuePair<Vector2Int, MapChunk> pair in _chunks)
        {
            if (IsGroupChunk(pair.Value) && IsInActiveRange(pair.Key, center))
            {
                _activeRooms.Add(pair.Value.Room);
            }
        }
    }

    private bool ShouldBeActive(Vector2Int key, MapChunk chunk, Vector2Int center)
    {
        if (IsGroupChunk(chunk))
        {
            return _activeRooms.Contains(chunk.Room);
        }

        return IsInActiveRange(key, center);
    }


    private void CheckRegionChange(Vector2Int index)
    {
        if (!_chunks.TryGetValue(index, out MapChunk chunk)) return;
        if (chunk.Region == null) return;
        if (chunk.Region == _currentRegion) return;

        SetCurrentRegion(chunk.Region);
    }

    private void SetCurrentRegion(Region region)
    {
        string previousId = CurrentRegionId;

        _currentRegion = region;

        Debug.Log($"[MapManager] (SetCurrentRegion) 지역 변경: {previousId} → {region.RegionId}");

        OnChangeRegion?.Invoke(region.RegionId);

        EnterRegionAsync(region.RegionId).Forget();
    }

    private void EnsureRegionLoader()
    {
        if (_regionLoader != null) return;

        Transform root = Transform_RegionRoot != null ? Transform_RegionRoot : transform;
        _regionLoader = new RegionLoader(root);
    }

    public async UniTask EnterRegionAsync(string regionId)
    {
        // 1. 중복 검사
        if (string.IsNullOrEmpty(regionId))
        {
            Debug.LogWarning("[MapManager] (EnterRegionAsync): regionId가 비어 있음");

            return;
        }

        EnsureRegionLoader();

        if (regionId == _targetRegionId && _regionLoader.IsRegionLoaded(regionId)) return;

        _targetRegionId = regionId;


        // 2. 현재 지역 로드
        await _regionLoader.LoadRegionAsync(regionId);

        while (_regionLoader.IsRegionLoading(regionId))
        {
            await UniTask.Yield();
        }


        // 3. 목표가 변경되었는지 확인
        if (_targetRegionId != regionId) return;


        // 4. Region 컴포넌트 가져오기
        Region region = _regionLoader.GetRegion(regionId);

        if (region == null)
        {
            Debug.LogError($"[MapManager] (EnterRegionAsync) 지역 불러오기 실패 또는 Region 컴포넌트 없음: {regionId}");

            return;
        }


        // 5. 이웃 지역 로드
        foreach (string neighborId in region.NeighborRegionIds)
        {
            _regionLoader.LoadRegionAsync(neighborId).Forget();
        }


        // 6. 모든 로드 이전까지 대기
        while (_regionLoader.HasLoadingRegion())
        {
            await UniTask.Yield();
        }


        // 7. 기다리는 동안 목표 변경되었는지 재확인
        if (_targetRegionId != regionId) return;


        //8. 거리 계산
        CalculateRegionDistances(regionId);
        ApplyRegionStates();

        Debug.Log($"[MapManager] (EnterRegionAsync) 지역 진입 완료: {regionId}");
    }

    private void CalculateRegionDistances(string startRegionId)
    {
        _regionDistances.Clear();
        _regionQueue.Clear();

        _regionDistances.Add(startRegionId, 0);

        _regionQueue.Enqueue(startRegionId);

        while (_regionQueue.Count > 0)
        {
            string id = _regionQueue.Dequeue();

            int distance = _regionDistances[id];

            if (distance >= _keepRegionDistance) continue;

            Region region = _regionLoader.GetRegion(id);

            if (region == null) continue;

            foreach (string neighborId in region.NeighborRegionIds)
            {
                if (string.IsNullOrEmpty(neighborId)) continue;
                if (_regionDistances.ContainsKey(neighborId)) continue;

                _regionDistances.Add(neighborId, distance + 1);
                _regionQueue.Enqueue(neighborId);
            }
        }
    }

    private void ApplyRegionStates()
    {
        _regionLoader.CollectActiveRegionIds(_activeRegionIdBuffer);

        foreach (string id in _activeRegionIdBuffer)
        {
            if (!_regionDistances.TryGetValue(id, out int distance) || distance > _keepRegionDistance)
            {
                _regionLoader.UnloadRegion(id);

                continue;
            }

            if (distance > 1)
            {
                _regionLoader.SetRegionVisible(id, false);
            }
            else
            {
                _regionLoader.SetRegionVisible(id, true);
            }
        }

        Debug.Log($"[MapManager] (ApplyRegionStates) 기준: {_targetRegionId} / 대상 {_activeRegionIdBuffer.Count}개");
    }
}
