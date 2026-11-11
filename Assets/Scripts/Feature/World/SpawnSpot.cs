using UnityEngine;

// 부활 위치 표시. 룸마다 Tile n, n 오브젝트들중 하나의 하위에 SpawnSpot오브젝트 하나 만들어서 넣으면 됨
public class SpawnSpot : MonoBehaviour
{
    [Header("에디터 표시")]
    [SerializeField] private Color _gizmoColor = Color.green;
    [SerializeField] private float _gizmoRadius = 0.5f;

    public Vector2 Position => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, _gizmoRadius);
    }
}