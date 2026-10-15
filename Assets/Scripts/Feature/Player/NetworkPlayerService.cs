using UnityEngine;

public class NetworkPlayerService
{
    private PlayerViewModel _playerViewModel;

    public PlayerViewModel GetPlayerViewModel()
    {
        if(_playerViewModel == null)
        {
            _playerViewModel = new PlayerViewModel();
        }
        return _playerViewModel;
    }
}