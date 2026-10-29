using UnityEngine;

public class JumpState : PlayerStateBase
{
    private bool _isFalling;
    private float _rotateSpeed = 12f;

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

    public override void Update()
    {
        float targetAngle = 0f;
        Quaternion target = Quaternion.Euler(0f, 0f, targetAngle);
        _player.transform.localRotation = Quaternion.Lerp(_player.transform.localRotation, target, _rotateSpeed * Time.deltaTime);
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