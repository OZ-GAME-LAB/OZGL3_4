using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class MapRoomView : MonoBehaviour
{
    private MapViewModel _mapViewModel;
    private MapRoomViewModel _roomViewModel;
    private Image[] _images;

    public void Init(MapViewModel mapViewModel, MapRoomViewModel roomViewModel)
    {
        UnBind();
        _mapViewModel = mapViewModel;
        _roomViewModel = roomViewModel;
        _images = GetComponentsInChildren<Image>(true);
        if (isActiveAndEnabled)
        {
            Bind();
        }
        UpdateView();
    }

    private void OnEnable()
    {
        Bind();
        UpdateView();
    }

    private void OnDisable()
    {
        UnBind();
    }

    private void Bind()
    {
        if (_mapViewModel == null || _roomViewModel == null)
        {
            return;
        }
        _mapViewModel.PropertyChanged -= OnChangeMapProperty;
        _mapViewModel.PropertyChanged += OnChangeMapProperty;
        _roomViewModel.PropertyChanged -= OnChangeRoomProperty;
        _roomViewModel.PropertyChanged += OnChangeRoomProperty;
    }

    private void UnBind()
    {
        if (_mapViewModel != null)
        {
            _mapViewModel.PropertyChanged -= OnChangeMapProperty;
        }
        if (_roomViewModel != null)
        {
            _roomViewModel.PropertyChanged -= OnChangeRoomProperty;
        }
    }

    private void OnChangeMapProperty(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MapViewModel.CurrentRoomId))
        {
            UpdateView();
        }
    }

    private void OnChangeRoomProperty(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MapRoomViewModel.IsVisited))
        {
            UpdateView();
        }
    }

    private void UpdateView()
    {
        if (_mapViewModel == null || _roomViewModel == null || _images == null)
        {
            return;
        }
        Color color = Color.red;
        if (_mapViewModel.CurrentRoomId == _roomViewModel.RoomId)
        {
            color = Color.green;
        }
        else if (_roomViewModel.IsVisited)
        {
            color = Color.blue;
        }
        foreach (Image image in _images)
        {
            if (image != null)
            {
                image.color = color;
            }
        }
    }
}
