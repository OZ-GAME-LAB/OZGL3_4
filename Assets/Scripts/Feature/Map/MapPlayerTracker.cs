using UnityEngine;

public class MapPlayerTracker : MonoBehaviour
{
    private MapManager Manager_Map;
    [SerializeField] private string _mapId = "Level_1";

    private MapService _mapService;

    public bool Init(MapService mapService)
    {
        if (mapService == null)
        {
            Debug.LogError("[MapPlayerTracker] MapService가 필요합니다.");
            return false;
        }
        MapManager mapManager = MapManager.Instance;
        if (mapManager == null)
        {
            Debug.LogError("[MapPlayerTracker] MapManager.Instance가 없습니다. 매니저 초기화 후 Init을 호출하세요.");
            return false;
        }
        UnBind();
        if (_mapService != null && _mapService != mapService)
        {
            ClearCurrentRoom();
        }
        Manager_Map = mapManager;
        _mapService = mapService;
        if (isActiveAndEnabled)
        {
            Bind();
            UpdatePlayerChunk();
        }
        return true;
    }

    private void OnEnable()
    {
        if (_mapService == null)
        {
            return;
        }
        Bind();
        UpdatePlayerChunk();
    }

    private void OnDisable()
    {
        UnBind();
        ClearCurrentRoom();
    }

    private void Bind()
    {
        if (Manager_Map == null)
        {
            return;
        }
        UnBind();
        Manager_Map.OnChangePlayerChunk += OnChangePlayerChunk;
        Manager_Map.OnClearPlayerChunk += ClearCurrentRoom;
    }

    private void UnBind()
    {
        if (Manager_Map == null)
        {
            return;
        }
        Manager_Map.OnChangePlayerChunk -= OnChangePlayerChunk;
        Manager_Map.OnClearPlayerChunk -= ClearCurrentRoom;
    }

    public void SetMapId(string mapId)
    {
        if (_mapId == mapId)
        {
            return;
        }
        ClearCurrentRoom();
        _mapId = mapId;
        if (isActiveAndEnabled)
        {
            UpdatePlayerChunk();
        }
    }

    // 구독 전 알림을 놓쳤어도 매니저가 계산해 둔 현재 청크를 전달한다.
    public void UpdatePlayerChunk()
    {
        if (_mapService == null || !isActiveAndEnabled)
        {
            return;
        }
        if (Manager_Map == null || !Manager_Map.HasCurrentChunk)
        {
            ClearCurrentRoom();
            return;
        }
        OnChangePlayerChunk(Manager_Map.CurrentChunkIndex);
    }

    private void OnChangePlayerChunk(Vector2Int chunkIndex)
    {
        if (_mapService == null)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(_mapId))
        {
            ClearCurrentRoom();
            return;
        }
        _mapService.RequestUpdateChunk(_mapId, chunkIndex);
    }

    private void ClearCurrentRoom()
    {
        if (_mapService == null)
        {
            return;
        }
        MapViewModel viewModel = _mapService.GetMapViewModel();
        _mapService.RequestExitRoom(viewModel.CurrentRoomId);
    }
}
