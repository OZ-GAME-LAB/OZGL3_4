using System;
using System.Collections.Generic;
using System.ComponentModel;

public class MapViewModel : ViewModelBase, IDisposable
{
    private readonly List<MapRoomViewModel> _rooms = new List<MapRoomViewModel>();
    private string _currentMapId;

    public IReadOnlyList<MapRoomViewModel> Rooms { get; }
    // 화면에서 선택한 테마다. 실제 플레이어의 위치나 저장 복귀 지점이 아니다.
    public string CurrentMapId
    {
        get
        {
            return _currentMapId;
        }
    }

    public int TotalTeleportCount
    {
        get
        {
            return GetTeleportCount(CurrentMapId, false);
        }
    }

    public int ActivatedTeleportCount
    {
        get
        {
            return GetTeleportCount(CurrentMapId, true);
        }
    }

    // 0~1. 실제 보상 강화 수치/지급 방식은 추후 Service가 결정한다.
    public float TeleportActivationRate
    {
        get
        {
            if (TotalTeleportCount == 0)
            {
                return 0f;
            }

            return (float)ActivatedTeleportCount / TotalTeleportCount;
        }
    }

    public bool CanShowExplorationRate
    {
        get
        {
            return GetCanShowExplorationRate(CurrentMapId);
        }
    }

    public float? DisplayedExplorationRate
    {
        get
        {
            if (CanShowExplorationRate)
            {
                return GetExplorationRate(CurrentMapId);
            }

            return null;
        }
    }

    // JSON 조회와 Model 생성/소유는 호출 측에서 담당한다.
    public MapViewModel(MapModel mapModel, IReadOnlyList<MapRoomData> roomDataList)
    {
        if (mapModel == null)
        {
            throw new ArgumentNullException(nameof(mapModel));
        }
        if (roomDataList == null)
        {
            throw new ArgumentNullException(nameof(roomDataList));
        }

        Rooms = _rooms.AsReadOnly();

        foreach (MapRoomData roomData in roomDataList)
        {
            if (roomData == null || string.IsNullOrWhiteSpace(roomData.Id) || string.IsNullOrWhiteSpace(roomData.MapId))
            {
                throw new ArgumentException("방 데이터와 Id, MapId가 필요합니다.");
            }
            if (GetRoomViewModel(roomData.Id) != null)
            {
                throw new ArgumentException("중복된 방 Id: " + roomData.Id);
            }

            MapRoomModel roomModel = null;
            foreach (MapRoomModel savedRoom in mapModel.Rooms)
            {
                if (savedRoom.RoomId == roomData.Id)
                {
                    roomModel = savedRoom;
                    break;
                }
            }
            if (roomModel == null)
            {
                roomModel = new MapRoomModel { RoomId = roomData.Id };
                mapModel.Rooms.Add(roomModel);
            }

            _rooms.Add(new MapRoomViewModel(roomModel, roomData));
        }

        foreach (MapRoomViewModel room in _rooms)
        {
            foreach (string targetId in room.TeleportRevealRoomIds)
            {
                MapRoomViewModel target = GetRoomViewModel(targetId);
                if (!room.HasTeleport || target == null || target.MapId != room.MapId)
                {
                    throw new ArgumentException("텔레포트 공개 대상은 같은 테마의 유효한 방이어야 합니다: " + targetId);
                }
            }
        }
        if (_rooms.Count > 0)
        {
            _currentMapId = _rooms[0].MapId;
        }
        foreach (MapRoomViewModel room in _rooms)
        {
            room.SetExplorationRateVisible(GetCanShowExplorationRate(room.MapId));
            room.PropertyChanged += OnChangeRoomProperty;
        }
    }

    public MapRoomViewModel GetRoomViewModel(string roomId)
    {
        foreach (MapRoomViewModel room in _rooms)
        {
            if (room.RoomId == roomId)
            {
                return room;
            }
        }
        return null;
    }

    public void InvokeOnceOnInit()
    {
        OnPropertyChanged(nameof(Rooms));
        OnPropertyChanged(nameof(CurrentMapId));
        UpdateThemeProgress();
        foreach (MapRoomViewModel room in _rooms)
        {
            room.InvokeOnceOnInit();
        }
    }

    public bool TrySetCurrentMap(string mapId)
    {
        if (CurrentMapId == mapId)
        {
            return false;
        }
        foreach (MapRoomViewModel room in _rooms)
        {
            if (room.MapId != mapId)
            {
                continue;
            }
            _currentMapId = mapId;
            OnPropertyChanged(nameof(CurrentMapId));
            UpdateThemeProgress();
            return true;
        }
        return false;
    }

    private int GetTeleportCount(string mapId, bool activatedOnly)
    {
        int count = 0;
        foreach (MapRoomViewModel room in _rooms)
        {
            if (room.MapId != mapId || !room.HasTeleport)
            {
                continue;
            }
            if (!activatedOnly || room.IsTeleportActivated)
            {
                count = count + 1;
            }
        }
        return count;
    }

    private bool GetCanShowExplorationRate(string mapId)
    {
        int total = GetTeleportCount(mapId, false);
        // 텔레포트 없는 테마를 자동으로 전부 활성화한 것으로 처리하지 않는다.
        return total > 0 && GetTeleportCount(mapId, true) == total;
    }

    private float GetExplorationRate(string mapId)
    {
        int totalCount = 0;
        int completedCount = 0;
        int roomCount = 0;
        int visitedCount = 0;
        foreach (MapRoomViewModel room in _rooms)
        {
            if (room.MapId != mapId)
            {
                continue;
            }
            totalCount = totalCount + room.TotalRewardCount + room.TotalHiddenCount;
            completedCount = completedCount + room.CollectedRewardCount + room.DiscoveredHiddenCount;
            roomCount = roomCount + 1;
            if (room.IsVisited)
            {
                visitedCount = visitedCount + 1;
            }
        }
        if (totalCount > 0)
        {
            return completedCount * 100f / totalCount;
        }
        if (roomCount == 0)
        {
            return 0f;
        }

        return visitedCount * 100f / roomCount;
    }

    private void OnChangeRoomProperty(object sender, PropertyChangedEventArgs args)
    {
        MapRoomViewModel changedRoom = (MapRoomViewModel)sender;
        if (args.PropertyName == nameof(MapRoomViewModel.IsTeleportActivated))
        {
            bool canShow = GetCanShowExplorationRate(changedRoom.MapId);
            foreach (MapRoomViewModel room in _rooms)
            {
                if (room.MapId == changedRoom.MapId)
                {
                    room.SetExplorationRateVisible(canShow);
                }
            }
            if (changedRoom.MapId == CurrentMapId)
            {
                UpdateThemeProgress();
            }
            return;
        }
        if (changedRoom.MapId != CurrentMapId)
        {
            return;
        }
        if (args.PropertyName == nameof(MapRoomViewModel.CollectedRewardCount)
            || args.PropertyName == nameof(MapRoomViewModel.DiscoveredHiddenCount)
            || args.PropertyName == nameof(MapRoomViewModel.IsVisited))
        {
            OnPropertyChanged(nameof(DisplayedExplorationRate));
        }
    }

    private void UpdateThemeProgress()
    {
        OnPropertyChanged(nameof(TotalTeleportCount));
        OnPropertyChanged(nameof(ActivatedTeleportCount));
        OnPropertyChanged(nameof(TeleportActivationRate));
        OnPropertyChanged(nameof(CanShowExplorationRate));
        OnPropertyChanged(nameof(DisplayedExplorationRate));
    }

    // 화면 닫힘이 아닌, 이 ViewModel을 소유한 기능이 종료될 때 호출한다.
    public void Dispose()
    {
        foreach (MapRoomViewModel room in _rooms)
        {
            room.PropertyChanged -= OnChangeRoomProperty;
        }
    }
}
