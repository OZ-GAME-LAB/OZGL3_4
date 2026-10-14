using System;
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

        // 두 요청을 모두 실행한다. 이미 지형이 공개된 방도 첫 방문은 기록해야 한다.
        bool visitedChanged = roomViewModel.TrySetVisited(true);
        bool terrainChanged = roomViewModel.TrySetTerrainRevealed(true);

        if (visitedChanged || terrainChanged)
        {
            return true;
        }

        return false;
    }
}
