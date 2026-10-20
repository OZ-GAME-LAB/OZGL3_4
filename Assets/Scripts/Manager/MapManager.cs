using System;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : SingletonBase<MapManager>
{
    [Header("청크 설정")]
    [SerializeField] private int _chunkSize = 32;
    [SerializeField] private int _activeRadius = 1;

    [Header("추적 대상")]
    [SerializeField] private Transform Transform_Target;


    private Dictionary<Vector2Int, MapChunk> _chunks = new Dictionary<Vector2Int, MapChunk>();
    private Vector2Int _currentChunkIndex;
    private bool _hasCurrentChunk = false;
    private HashSet<Room> _activeRooms = new HashSet<Room>();
    private bool _isChunkDirty = false;


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


    //private void Start()
    //{

    //}

    private void Update()
    {
        //수정됨
        UpdatePlayerChunk();
    }

    //수정됨: 청크가 업데이트 될 때마다 이벤트를 통해 알리기 위함
    public void UpdatePlayerChunk()
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

        RefreshActiveChunks(index);
        OnChangePlayerChunk?.Invoke(index);
    }

    private void OnDisable()
    {
        ClearPlayerChunk();
    }

    //수정됨: 현재 청크를 비우기 위한
    private void ClearPlayerChunk()
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

        //Debug.Log($"[MapManager] (RegisterChunk) 등록: {key} / {chunk.name} / 총 {_chunks.Count}개"); //등록된 청크들 체크하는 테스트 코드
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

}
