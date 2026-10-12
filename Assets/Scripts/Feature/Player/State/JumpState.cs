using UnityEngine;

public class JumpState : PlayerStateBase
{
    public JumpState(Player player) : base(player) { }

    public override void Enter()
    {
        // 애니메이션 실행
    }

    public override void FixedUpdate()
    {
        if(_player.IsGrounded == true && _player.Rigid.linearVelocity.y < 0.01f)
        {
            ChangeToLocomotion();
        }

        float moveSpeed = _player.CurrentShadow.Speed;
        _player.SetVelocity(moveSpeed);
    }
}