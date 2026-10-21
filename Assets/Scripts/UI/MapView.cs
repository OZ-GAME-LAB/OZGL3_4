using System;
using System.Collections.Generic;
using UnityEngine;

public class MapView : UIBase
{
    [SerializeField] private RectTransform Transform_MapContent;
    [SerializeField] private Vector2 _slotSize = new Vector2(64f, 64f);
    [SerializeField] private List<RoomShapeBinding> _roomShapes = new List<RoomShapeBinding>();

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

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        if (Transform_MapContent == null || _slotSize.x <= 0f || _slotSize.y <= 0f)
        {
            Debug.LogError("[MapView] MapContent와 양수 슬롯 크기를 설정하세요.");
            return;
        }
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("[MapView] GameDataManager가 없습니다.");
            return;
        }

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

            MapRoomData roomData = GameDataManager.Instance.GetData<MapRoomData>(roomShape.RoomId);
            if (roomData == null || roomData.OccupiedSlots == null || roomData.OccupiedSlots.Count == 0)
            {
                Debug.LogError("[MapView] 방 데이터 또는 점유 슬롯이 없습니다: " + roomShape.RoomId);
                continue;
            }

            bool hasNullSlot = false;
            foreach (MapSlotData slot in roomData.OccupiedSlots)
            {
                if (slot == null)
                {
                    hasNullSlot = true;
                    break;
                }
            }
            if (hasNullSlot)
            {
                Debug.LogError("[MapView] 점유 슬롯에 null이 있습니다: " + roomShape.RoomId);
                continue;
            }

            CreateRoomView(roomData, roomShape.RoomShapePrefab);
            createdRoomIds.Add(roomShape.RoomId);
        }
    }

    private void CreateRoomView(MapRoomData roomData, RectTransform roomShapePrefab)
    {
        int minX = roomData.OccupiedSlots[0].X;
        int minY = roomData.OccupiedSlots[0].Y;
        foreach (MapSlotData slot in roomData.OccupiedSlots)
        {
            if (slot.X < minX)
            {
                minX = slot.X;
            }
            if (slot.Y < minY)
            {
                minY = slot.Y;
            }
        }

        RectTransform roomView = Instantiate(roomShapePrefab, Transform_MapContent, false);
        roomView.name = roomData.Id;
        roomView.anchoredPosition = new Vector2(minX * _slotSize.x, minY * _slotSize.y);
    }
}
