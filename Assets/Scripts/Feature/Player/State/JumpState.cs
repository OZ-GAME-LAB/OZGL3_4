using UnityEngine;

public class JumpState : PlayerStateBase
{
    private bool _isFalling;
    private float _rotateSpeed = 12f;

    private float _jumpStartTime;

    public JumpState(Player player) : base(player) { }

    public override void Enter()
    {
        // 애니메이션 실행
        _isFalling = _player.Rigid.linearVelocityY < 0;
        _jumpStartTime = Time.time;

        if (_isFalling == false)
        {
            _player.Animator.Play("Jump");
        }
    }

    public override void Update()
    {
        float targetAngle = 0f;
        Quaternion target = Quaternion.Euler(0f, 0f, targetAngle);
        _player.transform.localRotation = Quaternion.Lerp(_player.transform.localRotation, target, _rotateSpeed * Time.deltaTime);
    }

    public override void FixedUpdate()
    {
        Vector2 velocity = _player.Rigid.linearVelocity;

        bool jumpGraceOver = Time.time - _jumpStartTime > 0.1f;                         // 점프 직후 바로 착지 판정 방지
        bool movingAwayFromGround = Vector2.Dot(velocity, _player.GroundHit.normal) > 0.1f;  // 바닥에서 멀어지는 중인가

        if (_player.IsGrounded && jumpGraceOver && !movingAwayFromGround)
        {
            _player.Animator.Play("Land");
            ChangeToLocomotion();
            return;
        }

        if (!_isFalling && velocity.y < 0f)
        {
            _isFalling = true;              // 한 번만 재생되도록
            _player.Animator.Play("Fall");
        }

        float moveSpeed = _player.CurrentShadow.Speed;
        _player.SetVelocity(moveSpeed);
    }
}