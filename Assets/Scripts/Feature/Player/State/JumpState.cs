using UnityEngine;

public class JumpState : PlayerStateBase
{
    private bool _isFalling;

    public JumpState(Player player) : base(player) { }

    public override void Enter()
    {
        // 애니메이션 실행
        _isFalling = _player.Rigid.linearVelocityY < 0;

        if (_isFalling == false)
        {
            _player.Animator.Play("Jump");
        }
    }

    public override void FixedUpdate()
    {
        if(_player.IsGrounded == true && _player.Rigid.linearVelocity.y < 0.01f)
        {
            _player.Animator.Play("Land");
            ChangeToLocomotion();

            return;
        }

        if(_isFalling == false && _player.Rigid.linearVelocity.y < 0)
        {
            _player.Animator.Play("Fall");
        }

        float moveSpeed = _player.CurrentShadow.Speed;
        _player.SetVelocity(moveSpeed);
    }
}