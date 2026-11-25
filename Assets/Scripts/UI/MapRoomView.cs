using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MapRoomView : MonoBehaviour
{
    private MapViewModel _mapViewModel;
    private MapRoomViewModel _roomViewModel;
    private Image _terrainImage;
    private IReadOnlyDictionary<string, Image> _hiddenImages;

    public bool HasTerrainImage => _terrainImage != null;

    public void Init(MapViewModel mapViewModel, MapRoomViewModel roomViewModel, Image terrainImage,
        IReadOnlyDictionary<string, Image> hiddenImages)
    {
        UnBind();
        _mapViewModel = mapViewModel;
        _roomViewModel = roomViewModel;
        _terrainImage = terrainImage;
        _hiddenImages = hiddenImages;
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
        if (args.PropertyName == nameof(MapRoomViewModel.IsTerrainVisible)
            || args.PropertyName == nameof(MapRoomViewModel.DiscoveredHiddenIds))
        {
            UpdateView();
        }
    }

    private void UpdateView()
    {
        if (_mapViewModel == null || _roomViewModel == null)
        {
            return;
        }

        if (_terrainImage != null)
        {
            _terrainImage.enabled = _roomViewModel.IsTerrainVisible;
            _terrainImage.color = _mapViewModel.CurrentRoomId == _roomViewModel.RoomId
                ? new Color(0.8f, 1f, 0.85f)
                : Color.white;
        }

        foreach (KeyValuePair<string, Image> hidden in _hiddenImages)
        {
            hidden.Value.enabled = _roomViewModel.DiscoveredHiddenIds.Contains(hidden.Key);
        }
    }
}
