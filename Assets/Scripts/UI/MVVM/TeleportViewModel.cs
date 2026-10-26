using System;
using System.Collections.Generic;

public class TeleportViewModel : ViewModelBase
{
    private readonly TeleportModel _teleportModel;
    private readonly Dictionary<string, string> _teleportMapIds;

    public IReadOnlyList<string> TeleportIds { get; }
    public IReadOnlyList<string> ActivatedTeleportIds { get; }

    internal TeleportViewModel(TeleportModel teleportModel, Dictionary<string, string> teleportMapIds)
    {
        if (teleportModel == null)
        {
            throw new ArgumentNullException(nameof(teleportModel));
        }
        if (teleportMapIds == null)
        {
            throw new ArgumentNullException(nameof(teleportMapIds));
        }
        _teleportModel = teleportModel;
        _teleportMapIds = new Dictionary<string, string>(teleportMapIds);
        TeleportIds = new List<string>(_teleportMapIds.Keys).AsReadOnly();
        ActivatedTeleportIds = _teleportModel.ActivatedTeleportIds.AsReadOnly();
    }

    public bool GetTeleportActivated(string teleportId)
    {
        if (string.IsNullOrWhiteSpace(teleportId) || !_teleportMapIds.ContainsKey(teleportId))
        {
            return false;
        }
        return _teleportModel.ActivatedTeleportIds.Contains(teleportId);
    }

    public int GetTotalTeleportCount(string mapId)
    {
        int count = 0;
        foreach (string teleportMapId in _teleportMapIds.Values)
        {
            if (teleportMapId == mapId)
            {
                count = count + 1;
            }
        }
        return count;
    }

    public int GetActivatedTeleportCount(string mapId)
    {
        int count = 0;
        foreach (KeyValuePair<string, string> teleport in _teleportMapIds)
        {
            if (teleport.Value == mapId && GetTeleportActivated(teleport.Key))
            {
                count = count + 1;
            }
        }
        return count;
    }

    public float GetTeleportActivationRate(string mapId)
    {
        int total = GetTotalTeleportCount(mapId);
        if (total == 0)
        {
            return 0f;
        }
        return (float)GetActivatedTeleportCount(mapId) / total;
    }

    public bool TryActivateTeleport(string teleportId)
    {
        if (string.IsNullOrWhiteSpace(teleportId) || !_teleportMapIds.ContainsKey(teleportId))
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

    public void InvokeOnceOnInit()
    {
        OnPropertyChanged(nameof(TeleportIds));
        OnPropertyChanged(nameof(ActivatedTeleportIds));
    }
}
