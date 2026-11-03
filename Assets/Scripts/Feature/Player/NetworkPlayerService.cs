using UnityEngine;

public class NetworkPlayerService
{
    private PlayerViewModel _playerViewModel;

    public PlayerViewModel GetPlayerViewModel()
    {
        if(_playerViewModel == null)
        {
            _playerViewModel = new PlayerViewModel(new PlayerModel());
        }
        return _playerViewModel;
    }

    public bool RequestChangeShadow(ShadowType type)
    {
        if (type == ShadowType.None)
            return false;

        PlayerViewModel viewModel = GetPlayerViewModel();
        ShadowType previousType = viewModel.CurrentShadowType;
        viewModel.ChangeShadow(type);

        return viewModel.CurrentShadowType != previousType;
    }
}
