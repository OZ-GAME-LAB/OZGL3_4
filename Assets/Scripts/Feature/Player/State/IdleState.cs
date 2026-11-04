using UnityEngine;

public class IdleState : PlayerStateBase
{
    public IdleState(Player player) : base(player) { }

    private float _rotateSpeed = 12f;
    private float _maxSlopeAngle = 50f;

    public override void Enter()
    {
        // 애니메이션 실행
        _player.Animator.SetFloat("Speed", 0f);
    }

    public override void Update()
    {
        float targetAngle = 0f;

        if (_player.GroundHit)
        {
            float slopeAngle = Vector2.SignedAngle(Vector2.up, _player.GroundHit.normal);
            if (Mathf.Abs(slopeAngle) <= _maxSlopeAngle)
            {
                targetAngle = slopeAngle;
            }
        }

        Quaternion target = Quaternion.Euler(0f, 0f, targetAngle);
        _player.transform.localRotation = Quaternion.Lerp(_player.transform.localRotation, target, _rotateSpeed * Time.deltaTime);
    }

    public override void FixedUpdate()
    {
        if (_player.HasMoveInput)
        {
            _player.ChangeState(PlayerState.Walk);
        }

        _player.SetVelocity(0);
    }
}