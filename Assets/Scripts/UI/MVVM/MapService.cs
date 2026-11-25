using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapService
{
    private readonly MapViewModel _mapViewModel;

    public MapService(MapViewModel mapViewModel)
    {
        if (mapViewModel == null)
        {
            throw new ArgumentNullException(nameof(mapViewModel));
        }

        _mapViewModel = mapViewModel;
    }

    public bool RequestRevealRooms(IReadOnlyList<string> roomIds)
    {
        if (roomIds == null)
        {
            return false;
        }
        MapViewModel viewModel = GetMapViewModel();
        foreach (string roomId in roomIds)
        {
            if (viewModel.GetRoomViewModel(roomId) == null)
            {
                return false;
            }
        }

        bool changed = false;
        foreach (string roomId in roomIds)
        {
            MapRoomViewModel roomViewModel = viewModel.GetRoomViewModel(roomId);
            bool terrainChanged = roomViewModel.TrySetTerrainRevealed(true);
            bool rewardInfoChanged = roomViewModel.TrySetRewardInfoRevealed(true);
            if (terrainChanged || rewardInfoChanged)
            {
                changed = true;
            }
        }
        return changed;
    }

    public MapViewModel GetMapViewModel()
    {
        return _mapViewModel;
    }

    public bool RequestEnterRoom(string roomId)
    {
        MapViewModel viewModel = GetMapViewModel();
        if (string.IsNullOrWhiteSpace(roomId) || viewModel.GetRoomViewModel(roomId) == null)
        {
            return false;
        }
        bool roomChanged = viewModel.TrySetCurrentRoom(roomId);
        bool progressChanged = RequestUnlockRoomByExploration(roomId);
        return roomChanged || progressChanged;
    }

    // 이전 방에서 늦게 도착한 이탈 요청이 새 방 상태를 지우지 않도록 ID를 확인한다.
    public bool RequestExitRoom(string roomId)
    {
        MapViewModel viewModel = GetMapViewModel();
        if (string.IsNullOrWhiteSpace(roomId) || viewModel.CurrentRoomId != roomId)
        {
            return false;
        }
        return viewModel.TryClearCurrentRoom();
    }

    public bool RequestUpdateChunk(string regionId, Vector2Int chunkIndex)
    {
        MapViewModel viewModel = GetMapViewModel();
        MapRoomViewModel targetRoom = null;
        bool hasRegion = false;
        foreach (MapRoomViewModel room in viewModel.Rooms)
        {
            if (room.RegionId != regionId)
            {
                continue;
            }
            hasRegion = true;
            if (room.ContainsChunk(chunkIndex))
            {
                if (targetRoom != null)
                {
                    Debug.LogError("[MapService] 방 청크 중복: " + targetRoom.RoomId + ", " + room.RoomId);
                    return false;
                }
                targetRoom = room;
            }
        }
        if (!hasRegion)
        {
            return false;
        }
        if (targetRoom != null)
        {
            return RequestEnterRoom(targetRoom.RoomId);
        }
        bool regionChanged = viewModel.TrySetCurrentRegion(regionId);
        bool roomExited = RequestExitRoom(viewModel.CurrentRoomId);
        return regionChanged || roomExited;
    }

    public bool RequestUnlockRoomByExploration(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return false;
        }

        MapViewModel viewModel = GetMapViewModel();

        MapRoomViewModel roomViewModel = viewModel.GetRoomViewModel(roomId);
        if (roomViewModel == null)
        {
            return false;
        }

        bool visitedChanged = roomViewModel.TrySetVisited(true);
        bool terrainChanged = roomViewModel.TrySetTerrainRevealed(true);

        if (visitedChanged || terrainChanged)
        {
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    // 테스트 치트: 화면에만 방을 표시하고 저장 진행도는 바꾸지 않는다.
    public bool RequestUnlockAllRoomsForTesting()
    {
        bool changed = false;
        foreach (MapRoomViewModel room in _mapViewModel.Rooms)
        {
            if (room.TryShowTerrainForTesting())
            {
                changed = true;
            }
        }
        return changed;
    }
#endif

    // 추가 - 김동혁
    public bool RequestCollectReward(string roomId, string rewardId)
    {
        MapRoomViewModel roomViewModel = GetMapViewModel().GetRoomViewModel(roomId);

        if (roomViewModel == null)
        {
            Debug.LogWarning($"[MapService] (RequestCollectReward) : {roomId}를 찾을 수 없습니다.");

            return false;
        }

        return roomViewModel.TryCollectReward(rewardId);
    }

    public bool IsRewardCollected(string roomId, string rewardId)
    {
        MapRoomViewModel roomViewModel = GetMapViewModel().GetRoomViewModel(roomId);

        if (roomViewModel == null)
        {
            Debug.LogWarning($"[MapService] (IsRewardCollected) : {roomId}를 찾을 수 없습니다.");

            return false;
        }

        return roomViewModel.CollectedRewardIds.Contains(rewardId);
    }

    // 엔딩 갈래 판정용. 전체 맵에 등록된 수집품(RewardIds)을 모두 모았으면 true.
    // 등록된 수집품이 하나도 없으면 데이터 오류로 보고 false를 반환한다.
    public bool IsAllRewardsCollected()
    {
        MapViewModel viewModel = GetMapViewModel();
        int totalCount = viewModel.TotalRewardCount;
        if (totalCount <= 0)
        {
            Debug.LogWarning("[MapService] (IsAllRewardsCollected) - 등록된 수집품이 없습니다. MapRoomData의 RewardIds를 확인하세요.");

            return false;
        }

        return viewModel.CollectedRewardCount >= totalCount;
    }

    public bool RequestDiscoverHidden(string roomId, string hiddenId)
    {
        MapRoomViewModel roomViewModel = GetMapViewModel().GetRoomViewModel(roomId);

        if (roomViewModel == null)
        {
            Debug.LogWarning($"[MapService] (RequestDiscoverHidden) - 방 없음: {roomId}");

            return false;
        }

        return roomViewModel.TryDiscoverHidden(hiddenId);
    }

    public bool IsHiddenDiscovered(string roomId, string hiddenId)
    {
        MapRoomViewModel roomViewModel = GetMapViewModel().GetRoomViewModel(roomId);

        if (roomViewModel == null)
        {
            Debug.LogWarning($"[MapService] (IsHiddenDiscovered) - 방 없음: {roomId}");

            return false;
        }

        return roomViewModel.DiscoveredHiddenIds.Contains(hiddenId);
    }

    public List<MapRoomProgressData> CollectRoomProgress()
    {
        List<MapRoomProgressData> progress = new List<MapRoomProgressData>();
        foreach (MapRoomViewModel room in _mapViewModel.Rooms)
        {
            if (room.IsVisited || room.IsTerrainRevealed || room.IsRewardInfoRevealed)
            {
                progress.Add(new MapRoomProgressData
                {
                    RoomId = room.RoomId,
                    IsVisited = room.IsVisited,
                    IsTerrainRevealed = room.IsTerrainRevealed,
                    IsRewardInfoRevealed = room.IsRewardInfoRevealed,
                });
            }
        }
        return progress;
    }

    public void RestoreRoomProgress(IReadOnlyList<MapRoomProgressData> progress)
    {
        if (progress == null)
        {
            return;
        }
        foreach (MapRoomProgressData savedRoom in progress)
        {
            if (savedRoom == null || string.IsNullOrWhiteSpace(savedRoom.RoomId))
            {
                continue;
            }
            MapRoomViewModel room = _mapViewModel.GetRoomViewModel(savedRoom.RoomId);
            if (room == null)
            {
                continue;
            }

            room.TrySetVisited(room.IsVisited || savedRoom.IsVisited);
            room.TrySetTerrainRevealed(room.IsTerrainRevealed || savedRoom.IsTerrainRevealed);
            room.TrySetRewardInfoRevealed(room.IsRewardInfoRevealed || savedRoom.IsRewardInfoRevealed);
        }
    }

    public List<string> GetAllCollectedRewardIds()
    {
        List<string> result = new List<string>();

        foreach (MapRoomViewModel room in GetMapViewModel().Rooms)
        {
            result.AddRange(room.CollectedRewardIds);
        }

        return result;
    }

    public List<string> GetAllDiscoveredHiddenIds()
    {
        List<string> result = new List<string>();

        foreach (MapRoomViewModel room in GetMapViewModel().Rooms)
        {
            result.AddRange(room.DiscoveredHiddenIds);
        }

        return result;
    }

    public void RestoreCollectedRewards(IReadOnlyList<string> rewardIds)
    {
        if (rewardIds == null)
        {
            return;
        }

        IReadOnlyList<MapRoomViewModel> rooms = GetMapViewModel().Rooms;

        foreach (string rewardId in rewardIds)
        {
            bool isRestored = false;

            foreach (MapRoomViewModel room in rooms)
            {
                if (room.TryCollectReward(rewardId))
                {
                    isRestored = true;
                    break;
                }
            }

            if (!isRestored)
            {
                Debug.LogWarning($"[MapService] (RestoreCollectedRewards) - 등록된 방 없음(데이터에서 삭제됐거나 ID 변경): {rewardId}");
            }
        }
    }

    public void RestoreDiscoveredHiddens(IReadOnlyList<string> hiddenIds)
    {
        if (hiddenIds == null)
        {
            return;
        }

        IReadOnlyList<MapRoomViewModel> rooms = GetMapViewModel().Rooms;

        foreach (string hiddenId in hiddenIds)
        {
            bool isRestored = false;

            foreach (MapRoomViewModel room in rooms)
            {
                if (room.TryDiscoverHidden(hiddenId))
                {
                    isRestored = true;
                    break;
                }
            }

            if (!isRestored)
            {
                Debug.LogWarning($"[MapService] (RestoreDiscoveredHiddens) - 등록된 방 없음(데이터에서 삭제됐거나 ID 변경): {hiddenId}");
            }
        }
    }
    // -----
}

