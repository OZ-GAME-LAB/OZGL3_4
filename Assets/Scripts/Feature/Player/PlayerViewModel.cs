using System;
using System.Collections.Generic;

public class PlayerViewModel : ViewModelBase, IContainerPropertyChanged<ShadowType>
{
    public event Action<string, ContainerEventType, ShadowType> ContainerPropertyChanged;

    private readonly PlayerModel _playerModel;
    private bool _isChangingShadow;

    public PlayerViewModel() : this(new PlayerModel()) { }

    public PlayerViewModel(PlayerModel playerModel)
    {
        if (playerModel == null)
        {
            throw new ArgumentNullException(nameof(playerModel));
        }
        _playerModel = playerModel;
    }

    public ShadowType CurrentShadowType
    {
        get { return _playerModel.CurrentShadowType; }
        set
        {
            if(_playerModel.CurrentShadowType != value)
            {
                _playerModel.CurrentShadowType = value;
                OnPropertyChanged(nameof(CurrentShadowType));
            }
        }
    }

    public HashSet<ShadowType> OwnerShadowType
    {
        get { return _playerModel.OwnerShadowType; }
        set
        {
            if (_playerModel.OwnerShadowType != value)
            {
                _playerModel.OwnerShadowType = value;
                OnPropertyChanged(nameof(OwnerShadowType));
            }
        }
    }

    public bool IsChangingShadow
    {
        get { return _isChangingShadow; }
        set
        {
            if (_isChangingShadow != value)
            {
                _isChangingShadow = value;
                OnPropertyChanged(nameof(IsChangingShadow));
            }
        }
    }

    public void InvokeContainerPropertyChanged(string containerName, ContainerEventType type, ShadowType shadowType)
    {
        ContainerPropertyChanged?.Invoke(containerName, type, shadowType);
    }
}

public static class PlayerViewModelExtention
{
    public static void ChangeShadow(this PlayerViewModel playerViewModel, ShadowType type)
    {
        if (playerViewModel.OwnerShadowType.Contains(type) == false)
            return;
        if (playerViewModel.CurrentShadowType == type)
            return;

        playerViewModel.CurrentShadowType = type;
    }

    public static void AddShadow(this PlayerViewModel playerViewModel, ShadowType newShadow)
    {
        if (playerViewModel.OwnerShadowType.Contains(newShadow) == true)
            return;

        playerViewModel.OwnerShadowType.Add(newShadow);
        playerViewModel.InvokeContainerPropertyChanged(nameof(playerViewModel.OwnerShadowType), ContainerEventType.Add, newShadow);
    }
}
