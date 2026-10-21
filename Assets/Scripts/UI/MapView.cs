using System;
using System.Collections.Generic;
using UnityEngine;

public class MapView : UIBase
{
    [SerializeField] private RectTransform Transform_MapContent;
    [SerializeField] private Vector2 _slotSize = new Vector2(64f, 64f);
    [SerializeField] private List<RoomShapeBinding> _roomShapes = new List<RoomShapeBinding>();

    private MapViewModel _mapViewModel;
    private readonly List<MapRoomView> _roomViews = new List<MapRoomView>();

    [Serializable]
    private class RoomShapeBinding
    {
        [SerializeField] private string _roomId;
        [SerializeField] private RectTransform Prefab_RoomShape;

        public string RoomId
        {
            get { return _roomId; }
        }

        public RectTransform RoomShapePrefab
        {
            get { return Prefab_RoomShape; }
        }
    }

    public void Init(MapViewModel mapViewModel)
    {
        if (mapViewModel == null || Transform_MapContent == null || _slotSize.x <= 0f || _slotSize.y <= 0f)
        {
            Debug.LogError("[MapView] ViewModel, MapContent와 양수 슬롯 크기를 설정하세요.");
            return;
        }
        if (_mapViewModel == mapViewModel)
        {
            return;
        }

        ClearRoomViews();
        _mapViewModel = mapViewModel;
        HashSet<string> createdRoomIds = new HashSet<string>();
        foreach (RoomShapeBinding roomShape in _roomShapes)
        {
            if (roomShape == null || string.IsNullOrWhiteSpace(roomShape.RoomId) || roomShape.RoomShapePrefab == null)
            {
                Debug.LogError("[MapView] 방 Id와 모양 프리팹을 연결하세요.");
                continue;
            }
            if (createdRoomIds.Contains(roomShape.RoomId))
            {
                Debug.LogError("[MapView] 중복된 방 연결입니다: " + roomShape.RoomId);
                continue;
            }
            MapRoomViewModel roomViewModel = _mapViewModel.GetRoomViewModel(roomShape.RoomId);
            if (roomViewModel == null)
            {
                Debug.LogError("[MapView] 방 ViewModel이 없습니다: " + roomShape.RoomId);
                continue;
            }
            CreateRoomView(roomViewModel, roomShape.RoomShapePrefab);
            createdRoomIds.Add(roomShape.RoomId);
        }
    }

    private void CreateRoomView(MapRoomViewModel roomViewModel, RectTransform roomShapePrefab)
    {
        int minX = roomViewModel.OccupiedSlots[0].x;
        int minY = roomViewModel.OccupiedSlots[0].y;
        foreach (Vector2Int slot in roomViewModel.OccupiedSlots)
        {
            if (slot.x < minX)
            {
                minX = slot.x;
            }
            if (slot.y < minY)
            {
                minY = slot.y;
            }
        }

        RectTransform roomTransform = Instantiate(roomShapePrefab, Transform_MapContent, false);
        roomTransform.name = roomViewModel.RoomId;
        roomTransform.anchoredPosition = new Vector2(minX * _slotSize.x, minY * _slotSize.y);
        MapRoomView roomView = roomTransform.GetComponent<MapRoomView>();
        if (roomView == null)
        {
            roomView = roomTransform.gameObject.AddComponent<MapRoomView>();
        }
        roomView.Init(_mapViewModel, roomViewModel);
        _roomViews.Add(roomView);
    }

    private void ClearRoomViews()
    {
        foreach (MapRoomView roomView in _roomViews)
        {
            if (roomView != null)
            {
                roomView.gameObject.SetActive(false);
                Destroy(roomView.gameObject);
            }
        }
        _roomViews.Clear();
    }
}
