using System;
using System.Collections.Generic;

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
public class MapRegionData : GameDataBase
{
    // 표시용 설명. 로직에서는 Id만 사용한다.
    public string Description;
}

[System.Serializable]
public class MapRoomData : GameDataBase
{
    // 표시용 설명. 로직에서는 Id만 사용한다.
    public string Description;
    public string RegionId;
    public System.Collections.Generic.List<ChunkIndexData> OccupiedChunks = new System.Collections.Generic.List<ChunkIndexData>();
    public System.Collections.Generic.List<string> RewardIds = new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<string> HiddenIds = new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<MapRoomExitData> Exits = new System.Collections.Generic.List<MapRoomExitData>();
}

[System.Serializable]
public class MapTeleportData : GameDataBase
{
    public string RegionId;
    public string RevealRoomIds;
}

[System.Serializable]
public class MapRoomExitData
{
    public string Id;
    public string TargetRoomId;
}

// 청크 좌표다. 하이어라키의 Tile x, y 이름과 같은 값이다.
[System.Serializable]
public class ChunkIndexData
{
    public int X;
    public int Y;
}

// 게임 저장용 데이터
[Serializable]
public class SaveData
{
    public List<ShadowType> UnlockedShadowIds = new List<ShadowType>();
    public List<string> ActivatedTeleportIds = new List<string>();
    public string LastSaveTeleportId;
}