using UnityEngine;

// 팝업 밖에서 공용 지도 상태를 소유하고 입력과 플레이어 추적에 연결한다.
public class MapSceneSetup : MonoBehaviour
{
    [SerializeField] private MapUIInput Input_Map;
    [SerializeField] private MapPlayerTracker Tracker_Player;

    private MapModel _mapModel;
    private MapViewModel _mapViewModel;
    private MapService _mapService;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        if (GameDataManager.Instance == null || Input_Map == null)
        {
            Debug.LogError("[MapSceneSetup] GameDataManager와 지도 입력을 연결하세요.");
            return;
        }

        _mapModel = new MapModel();
        _mapViewModel = new MapViewModel(_mapModel, GameDataManager.Instance.GetAllData<MapRoomData>());
        _mapService = new MapService(_mapViewModel);
        Input_Map.Init(_mapViewModel);
        if (Tracker_Player != null)
        {
            Tracker_Player.Init(_mapService);
        }
        else
        {
            Debug.LogWarning("[MapSceneSetup] Tracker가 없어 플레이어 이동은 지도에 반영되지 않습니다.");
        }
    }

    public MapService GetMapService()
    {
        return _mapService;
    }

    private void OnDestroy()
    {
        if (_mapViewModel != null)
        {
            _mapViewModel.Dispose();
        }
    }
}
