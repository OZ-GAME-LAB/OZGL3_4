using System;
using System.Collections.Generic;
using UnityEngine;

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

    public bool RequestUpdateChunk(string mapId, Vector2Int chunkIndex)
    {
        MapViewModel viewModel = GetMapViewModel();
        MapRoomViewModel targetRoom = null;
        bool hasMap = false;
        foreach (MapRoomViewModel room in viewModel.Rooms)
        {
            if (room.MapId != mapId)
            {
                continue;
            }
            hasMap = true;
            if (room.ContainsSlot(chunkIndex))
            {
                if (targetRoom != null)
                {
                    Debug.LogError("[MapService] 방 슬롯 중복: " + targetRoom.RoomId + ", " + room.RoomId);
                    return false;
                }
                targetRoom = room;
            }
        }
        if (!hasMap)
        {
            return false;
        }
        if (targetRoom != null)
        {
            return RequestEnterRoom(targetRoom.RoomId);
        }
        bool mapChanged = viewModel.TrySetCurrentMap(mapId);
        bool roomExited = RequestExitRoom(viewModel.CurrentRoomId);
        return mapChanged || roomExited;
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
}

