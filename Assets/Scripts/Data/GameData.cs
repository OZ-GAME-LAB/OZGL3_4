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
public class MapRoomData : GameDataBase
{
    public string Name;
    public string MapId;
    public System.Collections.Generic.List<string> RewardIds = new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<string> HiddenIds = new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<MapRoomExitData> Exits = new System.Collections.Generic.List<MapRoomExitData>();
    public bool HasTeleport;
    // 이 방의 텔레포트가 공개할 같은 테마의 방 Id 목록. 적용 시점은 Service가 결정한다.
    public System.Collections.Generic.List<string> TeleportRevealRoomIds = new System.Collections.Generic.List<string>();
}

// 출입구 Id는 방 안에서 고유하다. 방향 대신 목적지 방 Id로 연결한다.
[System.Serializable]
public class MapRoomExitData
{
    public string Id;
    public string TargetRoomId;
}
