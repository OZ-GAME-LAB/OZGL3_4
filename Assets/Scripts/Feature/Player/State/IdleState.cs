using UnityEngine;

public class IdleState : PlayerStateBase
{
    public IdleState(Player player) : base(player) { }

    public override void Enter()
    {
        // 애니메이션 실행
    }

    public override void FixedUpdate()
    {
        if (_player.HasMoveInput)
        {
            _player.ChangeState(PlayerState.Walk);
        }

        _player.SetVelocity(0);
    }
}