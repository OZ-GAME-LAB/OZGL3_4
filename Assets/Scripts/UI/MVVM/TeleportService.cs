using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks; // 수정 - 김동혁

public readonly struct TeleportInfo
{
    public readonly string Id;
    public readonly string RegionId;
    public readonly int RegionNumber;
    public readonly int TeleportNumber;
    public readonly bool IsActivated;

    public TeleportInfo(string id, string regionId, int regionNumber, int teleportNumber, bool isActivated)
    {
        Id = id;
        RegionId = regionId;
        RegionNumber = regionNumber;
        TeleportNumber = teleportNumber;
        IsActivated = isActivated;
    }
}

public class TeleportService
{
    private readonly MapService _mapService;
    private readonly TeleportViewModel _teleportViewModel;
    private readonly Dictionary<string, string> _teleportRegionIds = new Dictionary<string, string>();
    private readonly Dictionary<string, IReadOnlyList<string>> _teleportRevealRooms = new Dictionary<string, IReadOnlyList<string>>();
    private readonly Dictionary<string, Vector2> _teleportPositions = new Dictionary<string, Vector2>();

    private readonly List<string> _teleportIds = new List<string>();   // 정렬된 순서 유지
    private readonly Dictionary<string, (int region, int number)> _teleportNumbers = new Dictionary<string, (int, int)>();

    public event Action<Vector2> OnTeleportRequested;

    private const int PositionWaitFrames = 10; // 수정 - 김동혁
    private bool _isPrepared; // 수정 - 김동혁

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
        _teleportViewModel = new TeleportViewModel(teleportModel, _teleportRegionIds);
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
            if (data == null || string.IsNullOrWhiteSpace(data.Id) || string.IsNullOrWhiteSpace(data.RegionId))
            {
                throw new ArgumentException("텔레포트 Id와 RegionId가 필요합니다.");
            }
            if (_teleportRegionIds.ContainsKey(data.Id))
            {
                throw new ArgumentException("중복된 텔레포트 Id: " + data.Id);
            }
            string[] idParts = data.Id.Split('_');
            if (idParts.Length != 3 || idParts[0] != "Teleport")
            {
                throw new ArgumentException("텔레포트 Id 형식은 Teleport_리전번호_순번입니다: " + data.Id);
            }
            int regionNumber;
            int teleportNumber;
            if (!int.TryParse(idParts[1], out regionNumber) || regionNumber < 1
                || !int.TryParse(idParts[2], out teleportNumber) || teleportNumber < 1)
            {
                throw new ArgumentException("텔레포트 리전번호와 순번은 양의 정수여야 합니다: " + data.Id);
            }
            bool hasRegion = false;
            foreach (MapRoomViewModel room in _mapService.GetMapViewModel().Rooms)
            {
                if (room.RegionId == data.RegionId)
                {
                    hasRegion = true;
                    break;
                }
            }
            if (!hasRegion)
            {
                throw new ArgumentException("존재하지 않는 텔레포트 리전: " + data.RegionId);
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
                    if (target == null || target.RegionId != data.RegionId)
                    {
                        throw new ArgumentException("텔레포트 공개 대상은 같은 리전의 유효한 방이어야 합니다: " + roomId);
                    }
                    roomIds.Add(roomId);
                }
            }

            _teleportRegionIds.Add(data.Id, data.RegionId);
            _teleportRevealRooms.Add(data.Id, roomIds.AsReadOnly());
            _teleportIds.Add(data.Id);                                    
            _teleportNumbers.Add(data.Id, (regionNumber, teleportNumber));
        }

        _teleportIds.Sort((a, b) =>
        {
            var x = _teleportNumbers[a];
            var y = _teleportNumbers[b];
            return x.region != y.region ? x.region.CompareTo(y.region) : x.number.CompareTo(y.number);
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
            result.Add(new TeleportInfo(teleportId, _teleportRegionIds[teleportId], numbers.region, numbers.number, isActivated));
        }
        return result;
    }

    public bool RegisterTeleportPosition(string teleportId, Vector2 position)
    {
        if (string.IsNullOrWhiteSpace(teleportId) || !_teleportRegionIds.ContainsKey(teleportId))
        {
            return false;
        }
        _teleportPositions[teleportId] = position;
        return true;
    }

    public List<string> GetActivedAllTeleport()
    {
        if (_teleportViewModel == null)
            return null;

        return _teleportViewModel.GetActivedAllTeleport();
    }

    public bool TryGetTeleportPosition(string teleportId, out Vector2 savedPosition)
    {
        if(_teleportPositions.TryGetValue(teleportId, out savedPosition) == false)
        {
            return false;
        }
        return true;
    }

    public bool RequestTeleport(string teleportId)
    {
        if (!_teleportViewModel.GetTeleportActivated(teleportId))
        {
            return false;
        }
        if (!_teleportRegionIds.TryGetValue(teleportId, out string regionId)) // 수정 - 김동혁
        {
            Debug.LogError("[TeleportService] (RequestTeleport) RegionId가 없는 텔레포트: " + teleportId);
            return false;
        }
        if (MapManager.Instance == null)
        {
            Debug.LogError("[TeleportService] (RequestTeleport) MapManager가 없습니다."); // 수정 - 김동혁
            return false;
        }

        var uiBase = UIManager.Instance.OpenUI(UIRootType.VeryFrontUI, UIType.FadeUI);
        if (uiBase is FadeUI fadeUI)
        {
            UIManager.Instance.CloseUI(UIRootType.PopupUI, UIType.PointUI);
            fadeUI.StartCoroutine(TeleportRoutine(fadeUI, teleportId, regionId)); // 수정 - 김동혁
        }

        return true;
    }

    private IEnumerator TeleportRoutine(FadeUI fadeUI, string teleportId, string regionId)
    {
        yield return fadeUI.FadeIn();

        // 수정 - 김동혁) 도착 지역 준비 ---
        _isPrepared = false;
        yield return MapManager.Instance.PrepareRegionAsync(regionId).ToCoroutine(OnPrepareCompleted);

        if (!_isPrepared)
        {
            Debug.LogError("[TeleportService] (TeleportRoutine) 도착 지역 준비 실패: " + regionId);
            yield return CloseFade(fadeUI);
            yield break;
        }

        int waitFrames = 0;
        while (!_teleportPositions.ContainsKey(teleportId) && waitFrames < PositionWaitFrames)
        {
            waitFrames++;
            yield return null;
        }

        if (!_teleportPositions.TryGetValue(teleportId, out Vector2 position))
        {
            Debug.LogError("[TeleportService] (TeleportRoutine) 좌표가 등록되지 않은 텔레포트: " + teleportId);
            MapManager.Instance.ReleasePreparedRegion(regionId);
            yield return CloseFade(fadeUI);
            yield break;
        }
        // -----
        OnTeleportRequested?.Invoke(position);
        yield return new WaitForFixedUpdate();

        yield return CloseFade(fadeUI); // 수정 - 김동혁
    }

    private void OnPrepareCompleted(bool isReady) // 추가 - 김동혁
    {
        _isPrepared = isReady;
    }

    private IEnumerator CloseFade(FadeUI fadeUI) // 추가 - 김동혁
    {
        yield return fadeUI.FadeOut();

        UIManager.Instance.CloseUI(UIRootType.VeryFrontUI, UIType.FadeUI);
    }
}

