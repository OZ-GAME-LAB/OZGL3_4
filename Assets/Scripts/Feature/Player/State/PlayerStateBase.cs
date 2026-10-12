using UnityEngine;

public abstract class PlayerStateBase : IState
{
    protected readonly Player _player;
    protected PlayerStateBase(Player player)
    {
        this._player = player;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void Exit() { }

    public void TryJump()
    {
        var shadow = _player.CurrentShadow;

        if (_player.JumpCount >= shadow.MaxJumpCount)
            return;

        _player.SetJump();
        _player.ChangeState(PlayerState.Jump);
    }

    public void TryDash()
    {
        if (_player.CanDash)
        {
            _player.ChangeState(PlayerState.Dash);
        }
    }

    public void ChangeToLocomotion()
    {
        if (_player.HasMoveInput)
        {
            _player.ChangeState(PlayerState.Walk);
        }
        else
        {
            _player.ChangeState(PlayerState.Idle);
        }
    }
}