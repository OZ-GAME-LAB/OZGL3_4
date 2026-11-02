using System.Threading;
using UnityEngine;

public class ChangeState : PlayerStateBase
{
    private enum Phase
    {
        Absorb,
        Appear,
    }

    private float _timer;
    private Phase _phase;

    public ChangeState(Player player) : base(player) { }

    public override void Enter()
    {
        base.Enter();

        _player.Animator.SetTrigger("Absorb");
        _timer = _player.Animator.GetCurrentAnimatorClipInfo(0).Length;
        _phase = Phase.Absorb;
    }

    public override void Update()
    {
        base.Update();

        _player.SetVelocity(0);

        _timer -= Time.deltaTime;
        if (_timer > 0)
            return;

        if (_phase == Phase.Absorb)
        {
            _player.ApplyPendingShadow();
            _player.Animator.SetTrigger("Appear");
            _phase = Phase.Appear;
            _timer = _player.Animator.GetCurrentAnimatorClipInfo(0).Length;
            return;
        }

        _player.ChangeState(PlayerState.Idle);
    }
}