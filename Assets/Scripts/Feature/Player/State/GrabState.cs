using UnityEngine;

public class GrabState : PlayerStateBase
{
    private readonly int GrabHash = Animator.StringToHash("IsGrab");

    private Grabbable _target;

    public GrabState(Player player) : base(player) { }

    public override void Enter()
    {
        _target = _player.GrabTarget;
        if(_target == null)
        {
            ChangeToLocomotion();
            return;
        }

        _player.Animator.SetBool(GrabHash, true);
    }

    public override void FixedUpdate()
    {
        // 1. 플레이어 이동 (물체 무게만큼 느리게)
        float speed = _player.CurrentShadow.Speed * _target.SpeedMultiplier;
        _player.SetVelocity(speed);

        // 2. 물체도 같은 속도로 + 간격이 벌어진 만큼 보정
        _target.SetVelocity(speed);

        // 3. 애니메이션
        float movement = Mathf.Abs(_player.Rigid.linearVelocity.x);
        _player.Animator.SetFloat("Speed", movement);
    }

    public override void Exit()
    {
        _player.Animator.SetBool(GrabHash, false);
    }
}