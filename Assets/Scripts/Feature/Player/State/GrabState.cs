using UnityEngine;

public class GrabState : PlayerStateBase
{
    private readonly int _isGrabHash = Animator.StringToHash("IsGrab");

    private Grabbable _target;
    private float _attachTimer;

    public GrabState(Player player) : base(player) { }

    public override void Enter()
    {
        _target = _player.GrabTarget;
        if (_target == null)
        {
            ChangeToLocomotion();
            return;
        }

        var targetCollider = _target.GetComponent<Collider2D>();
        float dir = _player.Facing;

        SnapToTarget(targetCollider, dir);

        _target.OnGrabbed();
        _player.Animator.SetBool(_isGrabHash, true);
        _attachTimer = _player.Animator.GetCurrentAnimatorClipInfo(0).Length;
    }

    private void SnapToTarget(Collider2D targetCollider, float dir)
    {
        ColliderDistance2D d = _player.Collider.Distance(targetCollider);
        if (d.isValid && d.distance > 0f)
            _player.Rigid.position += new Vector2(dir * d.distance, 0f);
    }

    public override void FixedUpdate()
    {
        if(_attachTimer > 0)
        {
            _attachTimer -= Time.deltaTime;

            if(_attachTimer <= 0)
            {
                FinishAttach();
            }

            return;
        }

        // 플레이어 이동
        float speed = _player.CurrentShadow.Speed * _target.SpeedMultiplier;
        _player.SetVelocity(speed, true);

        // 물체도 같은 속도로
        float playerVx = _player.Rigid.linearVelocity.x;
        _target.SetVelocity(playerVx);

        // 애니메이션
        float movement = Mathf.Abs(_player.Rigid.linearVelocity.x);
        _player.Animator.SetFloat("Speed", movement);
    }

    private void FinishAttach()
    {
        _target.OnGrabbed();
    }

    public override void Exit()
    {
        _player.Animator.SetBool(_isGrabHash, false);
        _target.OnReleased();
    }
}