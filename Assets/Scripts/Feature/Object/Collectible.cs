using UnityEngine;

public class Collectible : MonoBehaviour, IInteractable
{
    [Header("수집 설정")]
    [SerializeField] private string _rewardId;

    private Room _room;

    public Vector2 Position => transform.position;


    private void Start()
    {
        if (string.IsNullOrEmpty(_rewardId))
        {
            Debug.LogError($"[Collectible] (Start) RewardId가 비어 있음: {gameObject.name}");

            return;
        }

        if (!TryGetRoomId(nameof(Start), out string roomId)) return;

        if (!TryGetMapService(nameof(Start), out MapService mapService)) return;

        if (mapService.IsRewardCollected(roomId, _rewardId))
        {
            gameObject.SetActive(false);
        }
    }


    public void Interact(Player player)
    {
        if (string.IsNullOrEmpty(_rewardId))
        {
            Debug.LogError($"[Collectible] (Interact) RewardId가 비어 있음: {gameObject.name}");

            return;
        }

        if (!TryGetRoomId(nameof(Interact), out string roomId)) return;

        if (!TryGetMapService(nameof(Interact), out MapService mapService)) return;

        if (mapService.RequestCollectReward(roomId, _rewardId))
        {
            Debug.Log($"[Collectible] (Interact) 획득: {_rewardId}");

            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[Collectible] (Interact) 획득 실패 (RewardIds 미등록 또는 이미 획득): {_rewardId}");
        }
    }

    private bool TryGetMapService(string methodName, out MapService mapService)
    {
        mapService = null;

        if (NetworkManager.Instance == null)
        {
            Debug.LogError($"[Collectible] ({methodName}) : NetworkManager 없음");

            return false;
        }

        mapService = NetworkManager.Instance.MapService;

        if (mapService == null)
        {
            Debug.LogError($"[Collectible] ({methodName}) : MapService 없음");
            
            return false;
        }

        return true;
    }

    private bool TryGetRoomId(string methodName, out string roomId)
    {
        roomId = null;

        if (_room == null)
        {
            _room = GetComponentInParent<Room>();

            if (_room == null)
            {
                Debug.LogError($"[Collectible] ({methodName}) 부모에 Room 없음: {gameObject.name}");

                return false;
            }
        }

        roomId = _room.RoomId;

        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogError($"[Collectible] ({methodName}) RoomId가 비어 있음: {_room.name}");

            return false;
        }

        return true;
    }
}
