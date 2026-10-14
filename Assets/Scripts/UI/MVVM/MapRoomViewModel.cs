using System;
using System.Collections.Generic;

public class MapRoomViewModel : ViewModelBase
{
    private readonly MapRoomModel _roomModel;
    private readonly List<string> _rewardIds;
    private readonly List<string> _hiddenIds;
    private readonly List<MapRoomExitData> _exits;
    private bool _canShowExplorationRate;

    public string RoomId { get; }
    public string RoomName { get; }
    public string MapId { get; }
    public bool HasTeleport { get; }
    public IReadOnlyList<string> TeleportRevealRoomIds { get; }
    public bool IsTerrainRevealed
    {
        get
        {
            return IsRevealed;
        }
    }

    public bool IsRewardInfoRevealed
    {
        get
        {
            return _roomModel.IsRewardInfoRevealed;
        }
    }

    // null은 정보 미공개다. 보상이 없는 상태(false)와 구분한다.
    public bool? HasUncollectedReward
    {
        get
        {
            if (IsRewardInfoRevealed)
            {
                return CollectedRewardCount < TotalRewardCount;
            }

            return null;
        }
    }

    public bool CanShowExplorationRate
    {
        get
        {
            return _canShowExplorationRate;
        }
    }

    public float? DisplayedExplorationRate
    {
        get
        {
            if (CanShowExplorationRate)
            {
                return ExplorationRate;
            }

            return null;
        }
    }

    public IReadOnlyList<MapRoomExitData> Exits { get; }
    public IReadOnlyList<string> CollectedRewardIds { get; }
    public IReadOnlyList<string> DiscoveredHiddenIds { get; }
    public IReadOnlyList<string> UnlockedExitIds { get; }
    public int TotalRewardCount
    {
        get
        {
            return _rewardIds.Count;
        }
    }

    public int TotalHiddenCount
    {
        get
        {
            return _hiddenIds.Count;
        }
    }

    public int CollectedRewardCount
    {
        get
        {
            return GetRecordedCount(_rewardIds, _roomModel.CollectedRewardIds);
        }
    }

    public int DiscoveredHiddenCount
    {
        get
        {
            return GetRecordedCount(_hiddenIds, _roomModel.DiscoveredHiddenIds);
        }
    }

    // 0~100. 수집 대상이 없는 방은 방문 시 100%다.
    public float ExplorationRate
    {
        get
        {
            int totalCount = TotalRewardCount + TotalHiddenCount;
            if (totalCount == 0)
            {
                if (IsVisited)
                {
                    return 100f;
                }

                return 0f;
            }
            return (CollectedRewardCount + DiscoveredHiddenCount) * 100f / totalCount;
        }
    }

    public bool IsVisited
    {
        get
        {
            return _roomModel.IsVisited;
        }
        private set
        {
            if (_roomModel.IsVisited == value)
            {
                return;
            }
            _roomModel.IsVisited = value;
            OnPropertyChanged(nameof(IsVisited));
            OnPropertyChanged(nameof(ExplorationRate));
            OnPropertyChanged(nameof(DisplayedExplorationRate));
        }
    }

    public bool IsRevealed
    {
        get
        {
            return _roomModel.IsRevealed;
        }
        private set
        {
            if (_roomModel.IsRevealed == value)
            {
                return;
            }
            _roomModel.IsRevealed = value;
            OnPropertyChanged(nameof(IsRevealed));
            OnPropertyChanged(nameof(IsTerrainRevealed));
        }
    }

    public bool IsTeleportActivated
    {
        get
        {
            return _roomModel.IsTeleportActivated;
        }
        private set
        {
            if (_roomModel.IsTeleportActivated == value)
            {
                return;
            }
            _roomModel.IsTeleportActivated = value;
            OnPropertyChanged(nameof(IsTeleportActivated));
        }
    }

    public MapRoomViewModel(MapRoomModel roomModel, MapRoomData roomData)
    {
        if (roomModel == null)
        {
            throw new ArgumentNullException(nameof(roomModel));
        }
        if (roomData == null)
        {
            throw new ArgumentNullException(nameof(roomData));
        }
        if (string.IsNullOrWhiteSpace(roomData.Id) || roomModel.RoomId != roomData.Id)
        {
            throw new ArgumentException("방 데이터와 Model의 Id가 일치해야 합니다.");
        }

        _roomModel = roomModel;
        RoomId = roomData.Id;
        RoomName = roomData.Name;
        MapId = roomData.MapId;
        HasTeleport = roomData.HasTeleport;
        TeleportRevealRoomIds = GetUniqueIds(roomData.TeleportRevealRoomIds).AsReadOnly();
        _rewardIds = GetUniqueIds(roomData.RewardIds);
        _hiddenIds = GetUniqueIds(roomData.HiddenIds);
        foreach (string hiddenId in _hiddenIds)
        {
            if (_rewardIds.Contains(hiddenId))
            {
                throw new ArgumentException("보상과 히든에 같은 Id를 중복 등록할 수 없습니다: " + hiddenId);
            }
        }

        _exits = new List<MapRoomExitData>();
        if (roomData.Exits != null)
        {
            foreach (MapRoomExitData exit in roomData.Exits)
            {
                if (exit == null || string.IsNullOrWhiteSpace(exit.Id) || string.IsNullOrWhiteSpace(exit.TargetRoomId))
                {
                    throw new ArgumentException("출입구의 Id와 TargetRoomId가 필요합니다.");
                }
                if (GetExit(exit.Id) != null)
                {
                    throw new ArgumentException("중복된 출입구 Id: " + exit.Id);
                }
                _exits.Add(exit);
            }
        }
        Exits = _exits.AsReadOnly();
        CollectedRewardIds = _roomModel.CollectedRewardIds.AsReadOnly();
        DiscoveredHiddenIds = _roomModel.DiscoveredHiddenIds.AsReadOnly();
        UnlockedExitIds = _roomModel.UnlockedExitIds.AsReadOnly();
    }

    // 방문 시 지도 공개, 반대편 출입구 해금 등의 조건은 Service에서 결정한다.
    public bool TrySetVisited(bool isVisited)
    {
        if (IsVisited == isVisited)
        {
            return false;
        }
        IsVisited = isVisited;
        return true;
    }

    public bool TrySetRevealed(bool isRevealed)
    {
        if (IsRevealed == isRevealed)
        {
            return false;
        }
        IsRevealed = isRevealed;
        return true;
    }

    public bool TrySetTerrainRevealed(bool isRevealed)
    {
        return TrySetRevealed(isRevealed);
    }

    public bool TrySetRewardInfoRevealed(bool isRevealed)
    {
        if (IsRewardInfoRevealed == isRevealed)
        {
            return false;
        }
        _roomModel.IsRewardInfoRevealed = isRevealed;
        OnPropertyChanged(nameof(IsRewardInfoRevealed));
        OnPropertyChanged(nameof(HasUncollectedReward));
        return true;
    }

    // 표시 여부는 저장하지 않고 소속 테마의 텔레포트 진행에서 다시 계산한다.
    internal void SetExplorationRateVisible(bool isVisible)
    {
        if (_canShowExplorationRate == isVisible)
        {
            return;
        }
        _canShowExplorationRate = isVisible;
        OnPropertyChanged(nameof(CanShowExplorationRate));
        OnPropertyChanged(nameof(DisplayedExplorationRate));
    }

    public bool TrySetTeleportActivated(bool isActivated)
    {
        if (!HasTeleport || IsTeleportActivated == isActivated)
        {
            return false;
        }
        IsTeleportActivated = isActivated;
        return true;
    }

    public bool TryCollectReward(string rewardId)
    {
        if (!_rewardIds.Contains(rewardId) || _roomModel.CollectedRewardIds.Contains(rewardId))
        {
            return false;
        }
        _roomModel.CollectedRewardIds.Add(rewardId);
        OnPropertyChanged(nameof(CollectedRewardIds));
        OnPropertyChanged(nameof(CollectedRewardCount));
        OnPropertyChanged(nameof(HasUncollectedReward));
        OnPropertyChanged(nameof(ExplorationRate));
        OnPropertyChanged(nameof(DisplayedExplorationRate));
        return true;
    }

    public bool TryDiscoverHidden(string hiddenId)
    {
        if (!_hiddenIds.Contains(hiddenId) || _roomModel.DiscoveredHiddenIds.Contains(hiddenId))
        {
            return false;
        }
        _roomModel.DiscoveredHiddenIds.Add(hiddenId);
        OnPropertyChanged(nameof(DiscoveredHiddenIds));
        OnPropertyChanged(nameof(DiscoveredHiddenCount));
        OnPropertyChanged(nameof(ExplorationRate));
        OnPropertyChanged(nameof(DisplayedExplorationRate));
        return true;
    }

    public bool GetExitUnlocked(string exitId)
    {
        return GetExit(exitId) != null && _roomModel.UnlockedExitIds.Contains(exitId);
    }

    public bool TrySetExitUnlocked(string exitId, bool isUnlocked)
    {
        if (GetExit(exitId) == null || GetExitUnlocked(exitId) == isUnlocked)
        {
            return false;
        }
        if (isUnlocked)
        {
            _roomModel.UnlockedExitIds.Add(exitId);
        }
        else
        {
            // 로드 데이터에 중복이 있어도 모두 제거한다.
            while (_roomModel.UnlockedExitIds.Remove(exitId)) { }
        }
        OnPropertyChanged(nameof(UnlockedExitIds));
        return true;
    }

    public void InvokeOnceOnInit()
    {
        OnPropertyChanged(nameof(IsVisited));
        OnPropertyChanged(nameof(IsRevealed));
        OnPropertyChanged(nameof(IsTerrainRevealed));
        OnPropertyChanged(nameof(IsRewardInfoRevealed));
        OnPropertyChanged(nameof(HasUncollectedReward));
        OnPropertyChanged(nameof(IsTeleportActivated));
        OnPropertyChanged(nameof(CollectedRewardIds));
        OnPropertyChanged(nameof(DiscoveredHiddenIds));
        OnPropertyChanged(nameof(UnlockedExitIds));
        OnPropertyChanged(nameof(CollectedRewardCount));
        OnPropertyChanged(nameof(DiscoveredHiddenCount));
        OnPropertyChanged(nameof(ExplorationRate));
        OnPropertyChanged(nameof(CanShowExplorationRate));
        OnPropertyChanged(nameof(DisplayedExplorationRate));
    }

    private MapRoomExitData GetExit(string exitId)
    {
        foreach (MapRoomExitData exit in _exits)
        {
            if (exit.Id == exitId)
            {
                return exit;
            }
        }
        return null;
    }

    private static List<string> GetUniqueIds(List<string> ids)
    {
        List<string> result = new List<string>();
        if (ids == null)
        {
            return result;
        }
        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id) && !result.Contains(id))
            {
                result.Add(id);
            }
        }
        return result;
    }

    private static int GetRecordedCount(List<string> validIds, List<string> recordedIds)
    {
        int count = 0;
        foreach (string id in validIds)
        {
            if (recordedIds.Contains(id))
            {
                count = count + 1;
            }
        }
        return count;
    }
}
