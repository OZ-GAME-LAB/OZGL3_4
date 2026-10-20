using UnityEngine;

public class NetworkManager : SingletonBase<NetworkManager>
{
    public NetworkPlayerService PlayerService;

    private void Awake()
    {
        PlayerService = new NetworkPlayerService();
    }
}
