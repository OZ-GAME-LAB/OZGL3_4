using UnityEngine;

public class NetworkManager : SingletonBase<NetworkManager>
{
    public NetworkPlayerService PlayerService;

    private void OnEnable()
    {
        PlayerService = new NetworkPlayerService();
    }
}
