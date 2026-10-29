using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct TeleportInfo
{
    public readonly string Id;
    public readonly string MapId;
    public readonly int ThemeNumber;
    public readonly int TeleportNumber;
    public readonly bool IsActivated;

    public TeleportInfo(string id, string mapId, int themeNumber, int teleportNumber, bool isActivated)
    {
        Id = id;
        MapId = mapId;
        ThemeNumber = themeNumber;
        TeleportNumber = teleportNumber;
        IsActivated = isActivated;
    }
}

public class TeleportService
{
    private readonly MapService _mapService;
    private readonly TeleportViewModel _teleportViewModel;
    private readonly Dictionary<string, string> _teleportMapIds = new Dictionary<string, string>();
    private readonly Dictionary<string, IReadOnlyList<string>> _teleportRevealRooms = new Dictionary<string, IReadOnlyList<string>>();
    private readonly Dictionary<string, Vector2> _teleportPositions = new Dictionary<string, Vector2>();

    private readonly List<string> _teleportIds = new List<string>();   // 정렬된 순서 유지
    private readonly Dictionary<string, (int theme, int number)> _teleportNumbers = new Dictionary<string, (int, int)>();

    public event Action<Vector2> OnTeleportRequested;

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
            _teleportIds.Add(data.Id);                                    
            _teleportNumbers.Add(data.Id, (themeNumber, teleportNumber));
        }

        _teleportIds.Sort((a, b) =>
        {
            var x = _teleportNumbers[a];
            var y = _teleportNumbers[b];
            return x.theme != y.theme ? x.theme.CompareTo(y.theme) : x.number.CompareTo(y.number);
        });
    }

    public IReadOnlyList<TeleportInfo> GetTeleportInfos(bool activatedOnly)
    {
        List<TeleportInfo> result = new List<TeleportInfo>(_teleportIds.Count);
        foreach (string teleportId in _teleportIds)
        {
            bool isActivated = _teleportViewModel.GetTeleportActivated(teleportId);   // ← 실제 메서드명으로 교체
            if (activatedOnly && !isActivated)
            {
                continue;
            }
            var numbers = _teleportNumbers[teleportId];
            result.Add(new TeleportInfo(teleportId, _teleportMapIds[teleportId], numbers.theme, numbers.number, isActivated));
        }
        return result;
    }

    public bool RegisterTeleportPosition(string teleportId, Vector2 position)
    {
        if (string.IsNullOrWhiteSpace(teleportId) || !_teleportMapIds.ContainsKey(teleportId))
        {
            return false;
        }
        _teleportPositions[teleportId] = position;
        return true;
    }

    public bool RequestTeleport(string teleportId)
    {
        if (!_teleportViewModel.GetTeleportActivated(teleportId))
        {
            return false;
        }
        if (!_teleportPositions.TryGetValue(teleportId, out Vector2 position))
        {
            //Debug.LogError("[TeleportService] 좌표가 등록되지 않은 텔레포트: " + teleportId);
            return false;
        }
        OnTeleportRequested?.Invoke(position);
        return true;
    }
}

