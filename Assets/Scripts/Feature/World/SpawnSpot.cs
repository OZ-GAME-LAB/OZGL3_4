using UnityEngine;

// 부활 위치 표시. 방 안에서 진행 순서대로 _stage를 1, 2, 3… 으로 적는다.
// 플레이어가 지나가면 그 방의 도달 단계가 갱신되고, 사망 시 도달한 최고 단계에서 부활한다.
[RequireComponent(typeof(BoxCollider2D))]
public class SpawnSpot : MonoBehaviour
{
    [Header("부활 단계")]
    [SerializeField] private int _stage = 1;

    [Header("에디터 표시")]
    [SerializeField] private Color _gizmoColor = Color.green;
    [SerializeField] private float _gizmoRadius = 0.5f;

    public Vector2 Position => transform.position;
    public int Stage => _stage;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!GimmickUtil.TryGetFromPlayerCollider(collision, out Player player))
            return;

        if (!MapUtil.TryGetRoomId(this, nameof(OnTriggerEnter2D), out string roomId))
            return;

        if (MapManager.Instance == null)
        {
            Debug.LogError("[SpawnSpot] (OnTriggerEnter2D) - MapManager 없음");

            return;
        }

        MapManager.Instance.ReachSpawnStage(roomId, _stage);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, _gizmoRadius);
    }
}