using UnityEngine;

public class NetworkManager : SingletonBase<NetworkManager>
{
    public NetworkPlayerService PlayerService;
    public MapService MapService;
    public TeleportService TeleportService;

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
}
