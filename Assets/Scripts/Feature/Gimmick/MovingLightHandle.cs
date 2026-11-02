using UnityEngine;

/// <summary>
/// 이동 광원의 손잡이입니다. 잡는 범위(GrabArea) 오브젝트에 트리거 콜라이더와 함께 붙입니다.
/// 플레이어의 PlayerInteractor가 트리거 콜라이더와 같은 오브젝트에서 IInteractable을 찾기 때문에,
/// 상호작용을 받아 부모의 MovingLight에 전달합니다.
/// </summary>
public class MovingLightHandle : MonoBehaviour, IInteractable
{
    [SerializeField] private MovingLight MovingLight_Owner;

    public Vector2 Position
    {
        get { return transform.position; }
    }

    private void Reset()
    {
        MovingLight_Owner = GetComponentInParent<MovingLight>();
    }

    /// <summary>
    /// 플레이어가 상호작용 키를 눌렀을 때 PlayerInteractor가 호출합니다.
    /// </summary>
    public void Interact(Player player)
    {
        if (MovingLight_Owner == null)
        {
            MovingLight_Owner = GetComponentInParent<MovingLight>();
        }

        if (MovingLight_Owner != null)
        {
            MovingLight_Owner.ToggleGrab(player);
        }
    }
}
