using UnityEngine;

public class WalkState : PlayerStateBase
{
    public WalkState(Player player) : base(player) { }

    public override void Enter()
    {
        // 애니메이션 실행
        _player.Animator.SetFloat("Speed", 1f);
    }

    public override void FixedUpdate()
    {
        if(_player.HasMoveInput == false)
        {
            _player.ChangeState(PlayerState.Idle);
            return;
        }

        float moveSpeed = _player.CurrentShadow.Speed;
        _player.SetVelocity(moveSpeed);
    }
}