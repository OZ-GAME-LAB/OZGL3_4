using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]

public class DeathZone : MonoBehaviour
{
    [Header("에디터 표시")]
    [SerializeField] private Color _gizmoColor = Color.red;


    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!GimmickUtil.TryGetFromPlayerCollider(collision, out Player player)) return;

        if (player.IsDead) return;

        player.Die();

        Debug.Log($"[DeathZone] (OnTriggerEnter2D) - 낙사: {gameObject.name}");
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        Gizmos.color = _gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
