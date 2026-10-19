using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapModel
{
    [SerializeField] private List<MapRoomModel> _rooms = new List<MapRoomModel>();

    public List<MapRoomModel> Rooms
    {
        get
        {
            return _rooms;
        }
    }
}

[Serializable]
public class MapRoomModel
{
    [SerializeField] private string _roomId;
    [SerializeField] private bool _isVisited;
    [SerializeField] private bool _isRevealed;
    [SerializeField] private bool _isRewardInfoRevealed;
    [SerializeField] private List<string> _collectedRewardIds = new List<string>();
    [SerializeField] private List<string> _discoveredHiddenIds = new List<string>();
    [SerializeField] private List<string> _unlockedExitIds = new List<string>();

    public string RoomId
    {
        get
        {
            return _roomId;
        }
        set
        {
            _roomId = value;
        }
    }

    public bool IsVisited
    {
        get
        {
            return _isVisited;
        }
        set
        {
            _isVisited = value;
        }
    }

    public bool IsRevealed
    {
        get
        {
            return _isRevealed;
        }
        set
        {
            _isRevealed = value;
        }
    }

    public bool IsRewardInfoRevealed
    {
        get
        {
            return _isRewardInfoRevealed;
        }
        set
        {
            _isRewardInfoRevealed = value;
        }
    }

    public List<string> CollectedRewardIds
    {
        get
        {
            return _collectedRewardIds;
        }
    }

    public List<string> DiscoveredHiddenIds
    {
        get
        {
            return _discoveredHiddenIds;
        }
    }

    public List<string> UnlockedExitIds
    {
        get
        {
            return _unlockedExitIds;
        }
    }
}

