using UnityEngine;

public class Collectible : MonoBehaviour, IInteractable
{
    [Header("수집 설정")]
    [SerializeField] private string _rewardId;


    public Vector2 Position => transform.position;


    private void Start()
    {
        if (string.IsNullOrEmpty(_rewardId))
        {
            Debug.LogError($"[Collectible] (Start) RewardId가 비어 있음: {gameObject.name}");

            return;
        }

        if (!MapUtil.TryGetRoomId(this, nameof(Start), out string roomId)) return;

        if (!MapUtil.TryGetMapService(this, nameof(Start), out MapService mapService)) return;

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

        if (!MapUtil.TryGetRoomId(this, nameof(Interact), out string roomId)) return;

        if (!MapUtil.TryGetMapService(this, nameof(Interact), out MapService mapService)) return;

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
}
