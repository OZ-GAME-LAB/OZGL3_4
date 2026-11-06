using UnityEngine;

public class NetworkManager : SingletonBase<NetworkManager>
{
    public NetworkPlayerService PlayerService;
    public MapService MapService;
    public TeleportService TeleportService;

    private SaveRepository saveRepository = new SaveRepository();

    private void OnEnable()
    {
        PlayerService = new NetworkPlayerService();

        
    }

    private void Start()
    {
        InitMapService();
        InitTeleportService();
    }

    private void InitMapService()
    {
        var mapDatas = GameDataManager.Instance.GetAllData<MapRoomData>();
        MapModel mapModel = new MapModel();
        MapViewModel mapViewMode = new MapViewModel(mapModel, mapDatas);

        MapService = new MapService(mapViewMode);
    }

    private void InitTeleportService()
    {
        TeleportModel teleportModel = new TeleportModel();
        var teleportData = GameDataManager.Instance.GetAllData<MapTeleportData>();
        TeleportService = new TeleportService(teleportModel, teleportData, MapService);
    }

    public void SaveGame(string teleportId)
    {
        SaveData saveData = new SaveData();

        // 활성화한 텔레포트 ID들 저장
        var activedTeleportList = TeleportService.GetActivedAllTeleport();
        saveData.ActivatedTeleportIds = activedTeleportList;

        // 해금한 플레이어 그림자 데이터들 저장
        var unlockedShadowList = PlayerService.GetUnlockedShadows();
        saveData.UnlockedShadowIds = unlockedShadowList;

        // 저장한 텔레포트 위치
        saveData.LastSaveTeleportId = teleportId;

        saveRepository.Save(saveData);
        Debug.Log("[NetworkManager] 데이터 저장 완료");
    }
}
