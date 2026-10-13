using UnityEngine;

public class DefaultShadow : ShadowBase
{
    public DefaultShadow(Player player) : base(player) { }

    public override void UseAbility()
    {
        base.UseAbility();

        Debug.Log("기본 그림자는 능력이 없습니다!");
    }
}
