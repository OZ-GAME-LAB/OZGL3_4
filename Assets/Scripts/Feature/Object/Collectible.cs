using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class Collectible : MonoBehaviour, IInteractable
{
    [Header("수집 설정")]
    [SerializeField] private string _rewardId;


    public Vector2 Position => transform.position;
    private MapRoomViewModel _roomViewModel;


    private void Start()
    {
        if (string.IsNullOrEmpty(_rewardId))
        {
            Debug.LogError($"[Collectible] (Start) RewardId가 비어 있음: {gameObject.name}");

            return;
        }

        if (!MapUtil.TryGetRoomId(this, nameof(Start), out string roomId)) return;

        if (!MapUtil.TryGetMapService(this, nameof(Start), out MapService mapService)) return;

        _roomViewModel = mapService.GetMapViewModel().GetRoomViewModel(roomId);
        if (_roomViewModel == null)
        {
            Debug.LogError($"[Collectible] (Start) - 방 데이터를 찾을 수 없음: {roomId} / {_rewardId}");
            return;
        }

        _roomViewModel.PropertyChanged += OnRoomPropertyChanged;
        RefreshCollected();
    }

    private void OnDestroy()
    {
        if (_roomViewModel != null)
        {
            _roomViewModel.PropertyChanged -= OnRoomPropertyChanged;
            _roomViewModel = null;
        }
    }


    private void OnRoomPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MapRoomViewModel.CollectedRewardIds))
        {
            return;
        }
        RefreshCollected();
    }


    private void RefreshCollected()
    {
        if (_roomViewModel == null)
        {
            return;
        }
        if (_roomViewModel.CollectedRewardIds.Contains(_rewardId))
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