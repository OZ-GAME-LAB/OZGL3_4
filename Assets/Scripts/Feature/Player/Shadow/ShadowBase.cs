using UnityEngine;

public enum ShadowType
{
    None = 0,
    Default,
    Mouse,
    Rabbit,
}

// 메모 - 김동혁, 속도 5f 점프파워 3f던걸 각각 6f, 4f로 상향했습니다.
public abstract class ShadowBase
{
    protected Player _player;

    [Header("Move")]
    protected float _speed = 6f;
    public float Speed => _speed;

    [Header("Jump")]
    protected float _jumpPower = 4f;
    protected int _maxJumpCount = 2;
    public float JumpPower => _jumpPower;
    public int MaxJumpCount => _maxJumpCount;

    [Header("Dash")]
    protected float _dashSpeed = 20f;
    protected float _dashDuration = 0.15f;
    protected float _dashCooldown = 0.5f;
    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;
    public float DashCooldown => _dashCooldown;

    public ShadowBase(Player player)
    {
        _player = player;
    }

    public virtual void UseAbility() { }

    public virtual void ReleaseAbility() { }

    public virtual void Enter()
    {
        _player.Animator.runtimeAnimatorController = _player.ShadowAnimatorList[_player.PlayerViewModel.CurrentShadowType];
        //_player.Animator.SetTrigger("Appear");

        //_player.ChangeState(PlayerState.Change);
    }

    public virtual void Exit()
    {
        //_player.Animator.SetTrigger("Absorb");
        //_player.Animator.
    }
}
