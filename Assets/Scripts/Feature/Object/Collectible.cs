using UnityEngine;

public class Collectible : MonoBehaviour, IInteractable
{
    [Header("수집 설정")]
    [SerializeField] private string _rewardId;

    public Vector2 Position => transform.position;

    public void Interact(Player player)
    {
        if (string.IsNullOrEmpty(_rewardId))
        {
            Debug.LogError($"[Collectible] (Interact) RewardId가 비어 있음: {gameObject.name}");

            return;
        }

        Debug.Log($"[Collectible] (Interact) 획득: {_rewardId}");

        gameObject.SetActive(false);
    }
}
