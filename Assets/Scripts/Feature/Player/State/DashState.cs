using UnityEngine;

public class DashState : PlayerStateBase
{
    private float _dashTimer;
    private float _direction;

    public DashState(Player player) : base(player) { }

    public override void Enter()
    {
        _dashTimer = _player.CurrentShadow.DashDuration;
        _direction = _player.Facing;
        _player.Rigid.gravityScale = 0;
        // 애니메이션 실행
    }

    public override void FixedUpdate()
    {
        _player.Rigid.linearVelocity = new Vector2(_direction * _player.CurrentShadow.DashSpeed, 0f);

        _dashTimer -= Time.fixedDeltaTime;
        if(_dashTimer <= 0)
        {
            ChangeToLocomotion();
        }
    }

    public override void Exit()
    {
        _player.Rigid.gravityScale = _player.DefaultGravity;
        _player.Rigid.linearVelocity = Vector2.zero;
    }
}