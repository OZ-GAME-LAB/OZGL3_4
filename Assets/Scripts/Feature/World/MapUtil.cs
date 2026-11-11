using UnityEngine;

// 맵 오브젝트들이 공통으로 쓰는 기능들을 모아둠
public static class MapUtil
{
    public static bool TryGetRoomId(Component owner, string methodName, out string roomId) // 부모 Room의 RoomId를 찾음, 실패시 에러 출력
    {
        roomId = null;
        string className = owner.GetType().Name;

        Room room = owner.GetComponentInParent<Room>();

        if (room == null)
        {
            Debug.LogError($"[{className}] ({methodName}) - 부모에 Room 없음: {owner.gameObject.name}");

            return false;
        }

        roomId = room.RoomId;

        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogError($"[{className}] ({methodName}) - RoomId가 비어 있음: {room.name}");

            return false;
        }

        return true;
    }

    public static bool TryGetMapService(Component owner, string methodName, out MapService mapService) // 네트워크 매니저의 맵 서비스를 찾음, 실패시 에러 출력
    {
        mapService = null;
        string className = owner.GetType().Name;

        if (NetworkManager.Instance == null)
        {
            Debug.LogError($"[{className}] ({methodName}) - NetworkManager 없음");

            return false;
        }

        mapService = NetworkManager.Instance.MapService;

        if (mapService == null)
        {
            Debug.LogError($"[{className}] ({methodName}) - MapService 없음");

            return false;
        }

        return true;
    }
}
