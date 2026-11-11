using System.Collections.Generic;
using System.Linq;
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
        // 변신 연출 중에는 변경하지 않는다.
        if (viewModel.IsChangingShadow)
            return false;

        ShadowType previousType = viewModel.CurrentShadowType;
        viewModel.ChangeShadow(type);

        return viewModel.CurrentShadowType != previousType;
    }

    public void RequestGetNewShadow(ShadowType type)
    {
        if (type == ShadowType.None)
            return;

        PlayerViewModel viewModel = GetPlayerViewModel();
        viewModel.AddShadow(type);
    }

    public List<ShadowType> GetUnlockedShadows()
    {
        if (_playerViewModel == null)
            return null;

        return _playerViewModel.OwnerShadowType.ToList();
    }
}
