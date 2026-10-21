using UnityEngine;

public class DefaultShadow : ShadowBase
{
    public DefaultShadow(Player player) : base(player) { }

    public override void UseAbility()
    {
        base.UseAbility();

        Debug.Log("잡기");
        _player.TryStartGrab(); 
    }

    public override void ReleaseAbility()
    {
        base.ReleaseAbility();

        _player.EndGrab();
    }

    public override void Exit()
    {
        base.Exit();

        //_player.
    }
}
