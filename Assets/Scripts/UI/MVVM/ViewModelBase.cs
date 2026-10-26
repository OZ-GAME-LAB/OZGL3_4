using System;
using System.ComponentModel;

public enum ContainerEventType
{
    None,
    Add,    // 요소 추가
    Remove, // 요소 제거
    Update, // 요소 업데이트
}

public interface IContainerPropertyChanged<T>
{
    event Action<string, ContainerEventType, T> ContainerPropertyChanged;
}

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
