using UnityEngine;

/// <summary>
/// 기믹에서 공통으로 쓰는 기능 모음입니다.
/// </summary>
public static class GimmickUtil
{
    /// <summary>
    /// 콜라이더가 플레이어의 것이면, 플레이어 오브젝트에 붙은 T 컴포넌트를 찾아 돌려줍니다.
    /// 예) TryGetFromPlayerCollider(other, out Player player)
    /// </summary>
    public static bool TryGetFromPlayerCollider<T>(Collider2D other, out T component) where T : Component
    {
        component = null;

        if (other == null)
            return false;

        // 플레이어 콜라이더는 보통 Rigidbody2D와 같은 오브젝트(플레이어 본체)에 있습니다.
        Rigidbody2D attachedRigidbody = other.attachedRigidbody;
        if (attachedRigidbody != null && attachedRigidbody.GetComponent<Player>() != null)
        {
            component = attachedRigidbody.GetComponent<T>();
            return component != null;
        }

        Player player = other.GetComponentInParent<Player>();
        if (player == null)
            return false;

        component = player.GetComponent<T>();
        return component != null;
    }

    /// <summary>
    /// 오브젝트가 있을 때만, 현재 상태와 다를 때만 켜고 끕니다. (말주머니 표시 등)
    /// </summary>
    public static void SetActiveSafe(GameObject target, bool isActive)
    {
        if (target != null && target.activeSelf != isActive)
        {
            target.SetActive(isActive);
        }
    }
}
