using UnityEngine;

public class NetworkManager : SingletonBase<NetworkManager>
{
    public NetworkPlayerService PlayerService;
    public MapService MapService;
    public TeleportService TeleportService;

    private SaveRepository _saveRepository = new SaveRepository();

    private void Start()
    {
        PlayerService = new NetworkPlayerService();
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

        _saveRepository.Save(saveData);
        Debug.Log("[NetworkManager] 데이터 저장 완료");
    }

    public void LoadGame(out string lastSaveTeleportId)
    {
        if (_saveRepository.TryLoad(out SaveData data) == false) 
        {
            Debug.LogWarning("[NetworkManager] 저장된 데이터가 없습니다.");
            lastSaveTeleportId = string.Empty;
            return;
        }

        lastSaveTeleportId = data.LastSaveTeleportId;

        var shadowList = data.UnlockedShadowIds;
        foreach (var shadow in shadowList)
        {
            PlayerService.RequestGetNewShadow(shadow);
        }

        var teleportList = data.ActivatedTeleportIds;
        foreach(var teleportId in teleportList)
        {
            TeleportService.RequestActivateTeleport(teleportId);
        }
    }
}
