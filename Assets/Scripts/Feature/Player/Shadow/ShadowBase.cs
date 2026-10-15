using UnityEngine;

public enum ShadowType
{
    None = 0,
    Default,
    Mouse,
    Rabbit,
}

public abstract class ShadowBase : MonoBehaviour
{
    protected Player _player;

    [Header("Move")]
    protected float _speed = 75f;
    public float Speed => _speed;

    [Header("Jump")]
    protected float _jumpPower = 100f;
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

    public void Enter()
    {
        _player.Animator.SetTrigger("Absorb");
        _player.Animator.runtimeAnimatorController = _player.ShadowAnimatorList[_player.PlayerViewModel.CurrentShadowType];
        _player.Animator.SetTrigger("Appear");
    }

    public void Exit()
    {

    }
}
