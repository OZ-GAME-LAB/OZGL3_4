using Cysharp.Threading.Tasks;
using UnityEngine;

public class MapPlayerTracker : MonoBehaviour
{
    private MapManager Manager_Map;
    [SerializeField] private string _regionId = "Region_01";

    private MapService _mapService;

    private void Start()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[MapPlayerTracker] NetworkManager가 없습니다.");
            return;
        }
        InitAsync().Forget();
    }

    // MapService는 NetworkManager.Start에서 만들어지므로 준비될 때까지 기다린 뒤 연결한다.
    private async UniTaskVoid InitAsync()
    {
        bool isCanceled = await UniTask.WaitUntil(HasMapService, cancellationToken: this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow();
        if (isCanceled)
        {
            return;
        }
        Init(NetworkManager.Instance.MapService);
    }

    private bool HasMapService()
    {
        NetworkManager networkManager = NetworkManager.Instance;
        if (networkManager == null)
        {
            return false;
        }
        return networkManager.MapService != null;
    }

    private void Init(MapService mapService)
    {
        MapManager mapManager = MapManager.Instance;
        if (mapManager == null)
        {
            Debug.LogError("[MapPlayerTracker] MapManager.Instance가 없습니다.");
            return;
        }
        UnBind();
        Manager_Map = mapManager;
        _mapService = mapService;
        if (isActiveAndEnabled)
        {
            Bind();
            UpdatePlayerChunk();
        }
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

    public void SetRegionId(string regionId)
    {
        if (_regionId == regionId)
        {
            return;
        }
        ClearCurrentRoom();
        _regionId = regionId;
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
        if (string.IsNullOrWhiteSpace(_regionId))
        {
            ClearCurrentRoom();
            return;
        }
        _mapService.RequestUpdateChunk(_regionId, chunkIndex);
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
