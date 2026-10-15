using System;
using System.Collections.Generic;

public class TeleportService
{
    private readonly MapService _mapService;
    private readonly TeleportViewModel _teleportViewModel;
    private readonly Dictionary<string, string> _teleportMapIds = new Dictionary<string, string>();
    private readonly Dictionary<string, IReadOnlyList<string>> _teleportRevealRooms = new Dictionary<string, IReadOnlyList<string>>();

    public TeleportService(TeleportModel teleportModel, IReadOnlyList<MapTeleportData> teleportDataList, MapService mapService)
    {
        if (teleportModel == null)
        {
            throw new ArgumentNullException(nameof(teleportModel));
        }
        if (mapService == null)
        {
            throw new ArgumentNullException(nameof(mapService));
        }
        _mapService = mapService;
        InitTeleports(teleportDataList);
        _teleportViewModel = new TeleportViewModel(teleportModel, _teleportMapIds);
        _mapService.GetMapViewModel().BindTeleportViewModel(_teleportViewModel);
    }

    public TeleportViewModel GetTeleportViewModel()
    {
        return _teleportViewModel;
    }

    public IReadOnlyList<string> GetTeleportRevealRoomIds(string teleportId)
    {
        if (string.IsNullOrWhiteSpace(teleportId) || !_teleportRevealRooms.ContainsKey(teleportId))
        {
            return null;
        }
        return _teleportRevealRooms[teleportId];
    }

    // 상호작용 성공 후 활성화를 요청하고, 성공했을 때 지도 공개를 요청한다.
    public bool RequestActivateTeleport(string teleportId)
    {
        IReadOnlyList<string> roomIds = GetTeleportRevealRoomIds(teleportId);
        if (roomIds == null)
        {
            return false;
        }
        TeleportViewModel viewModel = GetTeleportViewModel();
        if (viewModel.TryActivateTeleport(teleportId) == false)
        {
            return false;
        }
        _mapService.RequestRevealRooms(roomIds);
        return true;
    }

    private void InitTeleports(IReadOnlyList<MapTeleportData> teleportDataList)
    {
        if (teleportDataList == null)
        {
            return;
        }
        foreach (MapTeleportData data in teleportDataList)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Id) || string.IsNullOrWhiteSpace(data.MapId))
            {
                throw new ArgumentException("텔레포트 Id와 MapId가 필요합니다.");
            }
            if (_teleportMapIds.ContainsKey(data.Id))
            {
                throw new ArgumentException("중복된 텔레포트 Id: " + data.Id);
            }
            string[] idParts = data.Id.Split('_');
            if (idParts.Length != 3 || idParts[0] != "Teleport")
            {
                throw new ArgumentException("텔레포트 Id 형식은 Teleport_테마번호_순번입니다: " + data.Id);
            }
            int themeNumber;
            int teleportNumber;
            if (!int.TryParse(idParts[1], out themeNumber) || themeNumber < 1
                || !int.TryParse(idParts[2], out teleportNumber) || teleportNumber < 1)
            {
                throw new ArgumentException("텔레포트 테마번호와 순번은 양의 정수여야 합니다: " + data.Id);
            }
            bool hasMap = false;
            foreach (MapRoomViewModel room in _mapService.GetMapViewModel().Rooms)
            {
                if (room.MapId == data.MapId)
                {
                    hasMap = true;
                    break;
                }
            }
            if (!hasMap)
            {
                throw new ArgumentException("존재하지 않는 텔레포트 테마: " + data.MapId);
            }

            List<string> roomIds = new List<string>();
            if (!string.IsNullOrWhiteSpace(data.RevealRoomIds))
            {
                string[] splitIds = data.RevealRoomIds.Split(',');
                foreach (string splitId in splitIds)
                {
                    string roomId = splitId.Trim();
                    if (roomId.Length == 0 || roomIds.Contains(roomId))
                    {
                        continue;
                    }
                    MapRoomViewModel target = _mapService.GetMapViewModel().GetRoomViewModel(roomId);
                    if (target == null || target.MapId != data.MapId)
                    {
                        throw new ArgumentException("텔레포트 공개 대상은 같은 테마의 유효한 방이어야 합니다: " + roomId);
                    }
                    roomIds.Add(roomId);
                }
            }

            _teleportMapIds.Add(data.Id, data.MapId);
            _teleportRevealRooms.Add(data.Id, roomIds.AsReadOnly());
        }
    }

}

