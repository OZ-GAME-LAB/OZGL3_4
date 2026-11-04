using System;
using System.Collections.Generic;
using System.ComponentModel;

public class MapViewModel : ViewModelBase, IDisposable
{
    private readonly List<MapRoomViewModel> _rooms = new List<MapRoomViewModel>();
    private TeleportViewModel _teleportViewModel;
    private string _currentRegionId;
    private string _currentRoomId = string.Empty;

    public string CurrentRoomId
    {
        get { return _currentRoomId; }
    }

    public IReadOnlyList<MapRoomViewModel> Rooms { get; }

    public string CurrentRegionId
    {
        get
        {
            return _currentRegionId;
        }
    }

    public int TotalTeleportCount
    {
        get
        {
            return GetTeleportCount(CurrentRegionId, false);
        }
    }

    public int ActivatedTeleportCount
    {
        get
        {
            return GetTeleportCount(CurrentRegionId, true);
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

            return _teleportViewModel.GetTeleportActivationRate(CurrentRegionId);
        }
    }

    public bool CanShowExplorationRate
    {
        get
        {
            return GetCanShowExplorationRate(CurrentRegionId);
        }
    }

    public float? DisplayedExplorationRate
    {
        get
        {
            if (CanShowExplorationRate)
            {
                return GetExplorationRate(CurrentRegionId);
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
            if (roomData == null || string.IsNullOrWhiteSpace(roomData.Id) || string.IsNullOrWhiteSpace(roomData.RegionId))
            {
                throw new ArgumentException("방 데이터와 Id, RegionId가 필요합니다.");
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
            _currentRegionId = _rooms[0].RegionId;
        }
        foreach (MapRoomViewModel room in _rooms)
        {
            room.SetExplorationRateVisible(GetCanShowExplorationRate(room.RegionId));
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
        OnPropertyChanged(nameof(CurrentRegionId));
        OnPropertyChanged(nameof(CurrentRoomId));
        UpdateRegionProgress();
        foreach (MapRoomViewModel room in _rooms)
        {
            room.InvokeOnceOnInit();
        }
    }

    public bool TrySetCurrentRegion(string regionId)
    {
        if (CurrentRegionId == regionId)
        {
            return false;
        }
        foreach (MapRoomViewModel room in _rooms)
        {
            if (room.RegionId != regionId)
            {
                continue;
            }
            _currentRegionId = regionId;
            bool hadRoom = !string.IsNullOrEmpty(CurrentRoomId);
            _currentRoomId = string.Empty;
            OnPropertyChanged(nameof(CurrentRegionId));
            if (hadRoom)
            {
                OnPropertyChanged(nameof(CurrentRoomId));
            }
            UpdateRegionProgress();
            return true;
        }
        return false;
    }

    public bool TrySetCurrentRoom(string roomId)
    {
        if (CurrentRoomId == roomId)
        {
            return false;
        }
        MapRoomViewModel room = GetRoomViewModel(roomId);
        if (room == null)
        {
            return false;
        }
        bool regionChanged = CurrentRegionId != room.RegionId;
        _currentRegionId = room.RegionId;
        _currentRoomId = roomId;
        if (regionChanged)
        {
            OnPropertyChanged(nameof(CurrentRegionId));
        }
        OnPropertyChanged(nameof(CurrentRoomId));
        if (regionChanged)
        {
            UpdateRegionProgress();
        }
        return true;
    }

    public bool TryClearCurrentRoom()
    {
        if (string.IsNullOrEmpty(CurrentRoomId))
        {
            return false;
        }
        _currentRoomId = string.Empty;
        OnPropertyChanged(nameof(CurrentRoomId));
        return true;
    }

    private int GetTeleportCount(string regionId, bool activatedOnly)
    {
        if (_teleportViewModel == null)
        {
            return 0;
        }
        if (activatedOnly)
        {
            return _teleportViewModel.GetActivatedTeleportCount(regionId);
        }
        return _teleportViewModel.GetTotalTeleportCount(regionId);
    }

    private bool GetCanShowExplorationRate(string regionId)
    {
        int total = GetTeleportCount(regionId, false);
        return total > 0 && GetTeleportCount(regionId, true) == total;
    }

    private float GetExplorationRate(string regionId)
    {
        int totalCount = 0;
        int completedCount = 0;
        int roomCount = 0;
        int visitedCount = 0;
        foreach (MapRoomViewModel room in _rooms)
        {
            if (room.RegionId != regionId)
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
        if (changedRoom.RegionId != CurrentRegionId)
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
            room.SetExplorationRateVisible(GetCanShowExplorationRate(room.RegionId));
        }
        UpdateRegionProgress();
    }

    private void UpdateRegionProgress()
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

