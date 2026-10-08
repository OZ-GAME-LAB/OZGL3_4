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
