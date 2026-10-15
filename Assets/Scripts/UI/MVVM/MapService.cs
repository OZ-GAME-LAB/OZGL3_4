using System;
using System.Collections.Generic;

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

    // 모든 대상이 유효한지 먼저 확인한 뒤, 방문 기록 없이 지도 정보만 공개한다.
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

    // 실제 방 진입이 확인된 뒤 호출한다. 시작 위치나 이동으로 진입한 경우에도 사용한다.
    // 방문/지형 공개 중 하나라도 새로 변경되면 true, 유효하지 않거나 변경이 없으면 false다.
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
}

