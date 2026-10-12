using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;

public class Player : MonoBehaviour
{
    [Header("Move")]
    private float _speed = 75f;
    private Vector2 _movement = Vector2.zero;

    [Header("Jump")]
    private float _jumpPower = 100f;
    private int _maxJumpCount = 1;
    private int _jumpCount;
    private bool _isGrounded;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer;
    private Vector2 _groundCheckSize = new Vector2(0.5f, 0.1f);
    private float _groundLockTime = 0.1f;
    private float _groundLockTimer;

    [Header("Dash")]
    private float _dashSpeed = 20f;
    private float _dashDuration = 0.15f;
    private float _dashCooldown = 0.5f;
    private bool _isDashing;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashDirection;
    private float _defaultGravity;
    private float _facing = 1f;
    
    private PlayerInput _playerInput;
    private Rigidbody2D _rigid;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _rigid = GetComponent<Rigidbody2D>();
        _defaultGravity = _rigid.gravityScale;
    }

    private void FixedUpdate()
    {
        if(_dashCooldownTimer > 0f)
        {
            _dashCooldownTimer -= Time.fixedDeltaTime;
        }

        if (_isDashing)
        {
            _rigid.linearVelocity = new Vector2(_dashDirection * _dashSpeed, 0f);
            _dashTimer -= Time.fixedDeltaTime;

            if(_dashTimer <= 0)
            {
                EndDash();
            }

            return;
        }

        // 이동
        Move();

        // 착지 판정 & 점프 횟수 리셋
        if (_groundLockTime > 0f)
        {
            _groundLockTime -= Time.fixedDeltaTime;
            _isGrounded = false;
        }
        else
        {
            _isGrounded = Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0f, _groundLayer);
        }
        
        if(_isGrounded && _rigid.linearVelocity.y <= 0.01f)
        {
            _jumpCount = 0;
        }
    }

    private void OnMove(InputValue value)
    {
        _movement = value.Get<Vector2>();

        if(_movement.x != 0)
        {
            _facing = Mathf.Sign(_movement.x);
        }
    }
    
    private void Move()
    {
        float xOffset = _movement.x * _speed * Time.fixedDeltaTime;
        _rigid.linearVelocity = new Vector2(xOffset, _rigid.linearVelocity.y);
    }


    private void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            Jump();
        }
    }

    private void Jump()
    {
        if (_jumpCount >= _maxJumpCount)
            return;

        _rigid.AddForce(Vector2.up * _jumpPower);
        _jumpCount++;
        _groundLockTimer = _groundLockTime;
    }

    private void OnDash(InputValue value)
    {
        if (value.isPressed)
        {
            Dash();
        }
    }

    private void Dash()
    {
        if (_isDashing || _dashCooldownTimer > 0)
            return;

        _isDashing = true;
        _dashTimer = _dashDuration;
        _dashCooldownTimer = _dashCooldown;
        _dashDirection = _facing;
        _rigid.gravityScale = 0f;
    }

    private void EndDash()
    {
        _isDashing = false;
        _rigid.gravityScale = _defaultGravity;
        _rigid.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmos()
    {
        if (_groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_groundCheck.position, _groundCheckSize);
    }
}
