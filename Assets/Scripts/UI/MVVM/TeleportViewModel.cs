using System;
using System.Collections.Generic;

public class TeleportViewModel : ViewModelBase
{
    private readonly TeleportModel _teleportModel;
    private readonly Dictionary<string, string> _teleportRegionIds;

    public IReadOnlyList<string> TeleportIds { get; }
    public IReadOnlyList<string> ActivatedTeleportIds { get; }

    internal TeleportViewModel(TeleportModel teleportModel, Dictionary<string, string> teleportRegionIds)
    {
        if (teleportModel == null)
        {
            throw new ArgumentNullException(nameof(teleportModel));
        }
        if (teleportRegionIds == null)
        {
            throw new ArgumentNullException(nameof(teleportRegionIds));
        }
        _teleportModel = teleportModel;
        _teleportRegionIds = new Dictionary<string, string>(teleportRegionIds);
        TeleportIds = new List<string>(_teleportRegionIds.Keys).AsReadOnly();
        ActivatedTeleportIds = _teleportModel.ActivatedTeleportIds.AsReadOnly();
    }

    public bool GetTeleportActivated(string teleportId)
    {
        if (string.IsNullOrWhiteSpace(teleportId) || !_teleportRegionIds.ContainsKey(teleportId))
        {
            return false;
        }
        return _teleportModel.ActivatedTeleportIds.Contains(teleportId);
    }

    public int GetTotalTeleportCount(string regionId)
    {
        int count = 0;
        foreach (string teleportRegionId in _teleportRegionIds.Values)
        {
            if (teleportRegionId == regionId)
            {
                count = count + 1;
            }
        }
        return count;
    }

    public int GetActivatedTeleportCount(string regionId)
    {
        int count = 0;
        foreach (KeyValuePair<string, string> teleport in _teleportRegionIds)
        {
            if (teleport.Value == regionId && GetTeleportActivated(teleport.Key))
            {
                count = count + 1;
            }
        }
        return count;
    }

    public float GetTeleportActivationRate(string regionId)
    {
        int total = GetTotalTeleportCount(regionId);
        if (total == 0)
        {
            return 0f;
        }
        return (float)GetActivatedTeleportCount(regionId) / total;
    }

    public bool TryActivateTeleport(string teleportId)
    {
        if (string.IsNullOrWhiteSpace(teleportId) || !_teleportRegionIds.ContainsKey(teleportId))
        {
            return false;
        }
        if (GetTeleportActivated(teleportId))
        {
            return false;
        }
        _teleportModel.ActivatedTeleportIds.Add(teleportId);
        OnPropertyChanged(nameof(ActivatedTeleportIds));
        return true;
    }

    public List<string> GetActivedAllTeleport()
    {
        return _teleportModel.ActivatedTeleportIds;
    }

    public void InvokeOnceOnInit()
    {
        OnPropertyChanged(nameof(TeleportIds));
        OnPropertyChanged(nameof(ActivatedTeleportIds));
    }
}
