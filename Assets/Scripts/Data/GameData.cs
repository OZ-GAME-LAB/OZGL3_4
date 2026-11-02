[System.Serializable]
public class GameDataBase
{
    public string Id;
}

[System.Serializable]
public class DialogData : GameDataBase
{
    public string Dialog;
    public string NextId;
}

[System.Serializable]
public class MapData : GameDataBase
{
    public string Name;
}

[System.Serializable]
public class MapRoomData : GameDataBase
{
    public string Name;
    public string MapId;
    public System.Collections.Generic.List<MapSlotData> OccupiedSlots = new System.Collections.Generic.List<MapSlotData>();
    public System.Collections.Generic.List<string> RewardIds = new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<string> HiddenIds = new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<MapRoomExitData> Exits = new System.Collections.Generic.List<MapRoomExitData>();
}

[System.Serializable]
public class MapTeleportData : GameDataBase
{
    public string MapId;
    public string RevealRoomIds;
}

[System.Serializable]
public class MapRoomExitData
{
    public string Id;
    public string TargetRoomId;
}

// 구역 전체의 원점을 기준으로 한 점유 청크 좌표다.
[System.Serializable]
public class MapSlotData
{
    public int X;
    public int Y;
}