using System;
using System.Collections.Generic;
using UnityEngine;

public class RegionBgmController : MonoBehaviour
{
    [Serializable]
    private class RegionBgm
    {
        public string RegionId;
        public SoundId Bgm;
    }

    [SerializeField] private List<RegionBgm> _regionBgms = new List<RegionBgm>();
    [SerializeField] private SoundId _defaultBgm = SoundId.BGM_Cave;

    private MapManager _mapManager;
    private Dictionary<string, SoundId> _lookup;

    public void Init()
    {
        _lookup = new Dictionary<string, SoundId>();
        foreach (RegionBgm entry in _regionBgms)
        {
            if (!string.IsNullOrWhiteSpace(entry.RegionId))
            {
                _lookup[entry.RegionId] = entry.Bgm;   // 인덱서 대입이라 중복이어도 예외 없음
            }
        }

        _mapManager = MapManager.Instance;
        _mapManager.OnChangeRegion += HandleRegionChanged;

        // 구독하기 전에 이미 지역이 정해져 있었다면 바로 적용
        if (!string.IsNullOrEmpty(_mapManager.CurrentRegionId))
        {
            HandleRegionChanged(_mapManager.CurrentRegionId);
        }
    }

    private void OnDestroy()
    {
        if (_mapManager != null)
        {
            _mapManager.OnChangeRegion -= HandleRegionChanged;
        }
    }

    private void HandleRegionChanged(string regionId)
    {
        SoundId bgm = _lookup.TryGetValue(regionId, out SoundId found) ? found : _defaultBgm;
        SoundManager.Instance.PlayBGM(bgm);
    }
}