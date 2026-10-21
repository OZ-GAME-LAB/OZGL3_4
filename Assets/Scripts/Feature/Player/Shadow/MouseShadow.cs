using UnityEngine;

public class MouseShadow : ShadowBase
{
    public MouseShadow(Player player) : base(player) { }

    public override void UseAbility()
    {
        base.UseAbility();

        Debug.Log("쥐 그림자 능력!");
    }
}
