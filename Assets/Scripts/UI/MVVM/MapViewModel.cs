using System;
using System.Collections.Generic;
using System.ComponentModel;

public class MapViewModel : ViewModelBase, IDisposable
{
    private readonly List<MapRoomViewModel> _rooms = new List<MapRoomViewModel>();
    private TeleportViewModel _teleportViewModel;
    private string _currentMapId;

    public IReadOnlyList<MapRoomViewModel> Rooms { get; }

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

    public float TeleportActivationRate
    {
        get
        {
            if (_teleportViewModel == null)
            {
                return 0f;
            }

            return _teleportViewModel.GetTeleportActivationRate(CurrentMapId);
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
        if (_teleportViewModel == null)
        {
            return 0;
        }
        if (activatedOnly)
        {
            return _teleportViewModel.GetActivatedTeleportCount(mapId);
        }
        return _teleportViewModel.GetTotalTeleportCount(mapId);
    }

    private bool GetCanShowExplorationRate(string mapId)
    {
        int total = GetTeleportCount(mapId, false);
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

    public void BindTeleportViewModel(TeleportViewModel teleportViewModel)
    {
        if (_teleportViewModel == teleportViewModel)
        {
            return;
        }
        if (_teleportViewModel != null)
        {
            _teleportViewModel.PropertyChanged -= OnChangeTeleportProperty;
        }
        _teleportViewModel = teleportViewModel;
        if (_teleportViewModel != null)
        {
            _teleportViewModel.PropertyChanged += OnChangeTeleportProperty;
        }
        UpdateTeleportProgress();
    }

    private void OnChangeTeleportProperty(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(TeleportViewModel.ActivatedTeleportIds))
        {
            UpdateTeleportProgress();
        }
    }

    private void UpdateTeleportProgress()
    {
        foreach (MapRoomViewModel room in _rooms)
        {
            room.SetExplorationRateVisible(GetCanShowExplorationRate(room.MapId));
        }
        UpdateThemeProgress();
    }

    private void UpdateThemeProgress()
    {
        OnPropertyChanged(nameof(TotalTeleportCount));
        OnPropertyChanged(nameof(ActivatedTeleportCount));
        OnPropertyChanged(nameof(TeleportActivationRate));
        OnPropertyChanged(nameof(CanShowExplorationRate));
        OnPropertyChanged(nameof(DisplayedExplorationRate));
    }

    public void Dispose()
    {
        if (_teleportViewModel != null)
        {
            _teleportViewModel.PropertyChanged -= OnChangeTeleportProperty;
        }
        foreach (MapRoomViewModel room in _rooms)
        {
            room.PropertyChanged -= OnChangeRoomProperty;
        }
    }
}

