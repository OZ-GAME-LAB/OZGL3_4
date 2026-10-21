using UnityEngine;

public class RabbitShadow : ShadowBase
{
    public RabbitShadow(Player player) : base(player) { }

    public override void UseAbility()
    {
        base.UseAbility();

        Debug.Log("토끼 그림자 능력!");
    }
}
