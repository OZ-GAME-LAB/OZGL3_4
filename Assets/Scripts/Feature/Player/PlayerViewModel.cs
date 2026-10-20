using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerViewModel : ViewModelBase, IContainerPropertyChanged<ShadowType>
{
    public event Action<string, ContainerEventType, ShadowType> ContainerPropertyChanged;

    private ShadowType _currentShadowType;
    public ShadowType CurrentShadowType
    {
        get { return _currentShadowType; }
        set
        {
            if(_currentShadowType != value)
            {
                _currentShadowType = value;
                OnPropertyChanged(nameof(CurrentShadowType));
            }
        }
    }

    private HashSet<ShadowType> _owenrShadowType = new HashSet<ShadowType>();
    public HashSet<ShadowType> OwnerShadowType
    {
        get { return _owenrShadowType; }
        set
        {
            if (_owenrShadowType != value)
            {
                _owenrShadowType = value;
                OnPropertyChanged(nameof(OwnerShadowType));
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